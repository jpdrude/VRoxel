using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;

// Localhost bridge for the VRoxel MCP server (Tools/mcp/vroxel_mcp.py).
// Protocol: one JSON object per line. Request {"id", "command", "args"}, response {"id", "ok", "result" | "error"}.
// Requests arrive on background threads and run on the main thread in Update.
public class McpBridge : MonoBehaviour
{
    [SerializeField]
    int port = 8765;

    [Space]
    [Header("Avatars")]
    [SerializeField]
    GameObject mixamoAvatar;
    public GameObject MixamoAvatar { get { return mixamoAvatar; } }
    [SerializeField]
    AnimationLibrary animationLibrary;
    public AnimationLibrary AnimationLibrary { get { return animationLibrary; } }
    [Tooltip("Layers avatars stand on. Blocks have no colliders and are always walked through")]
    [SerializeField]
    LayerMask groundLayers = (1 << 0) | (1 << 3) | (1 << 6) | (1 << 11);

    public static McpBridge Bridge { get; private set; }

    class PendingRequest
    {
        public JObject request;
        public string response;
        public readonly ManualResetEventSlim done = new ManualResetEventSlim(false);
    }

    readonly Dictionary<string, Func<JObject, object>> commands = new Dictionary<string, Func<JObject, object>>();
    readonly ConcurrentQueue<PendingRequest> pending = new ConcurrentQueue<PendingRequest>();

    TcpListener listener;
    Thread listenerThread;
    volatile bool running;

    // Main-thread requests must not wait forever if the app stops updating
    const int RequestTimeoutMs = 30000;

    void Awake()
    {
        Bridge = this;

        // Requests come from a terminal, so the app must keep updating without focus
        Application.runInBackground = true;
        GroundProbe.Layers = groundLayers;
        MixamoAnimationPlayer.RegisterClipFixes(animationLibrary);

        Register("ping", args => new { scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name, avatars = AvatarRegistry.Count });
        AvatarCommands.Register(this);
        SceneCommands.Register(this);
        AnimationFileCommands.Register(this);
    }

    void OnEnable()
    {
        running = true;
        listener = new TcpListener(IPAddress.Loopback, port);
        listener.Start();

        listenerThread = new Thread(ListenLoop) { IsBackground = true, Name = "McpBridge" };
        listenerThread.Start();

        Debug.Log("McpBridge: listening on 127.0.0.1:" + port);
    }

    void OnDisable()
    {
        running = false;
        listener?.Stop();
    }

    public void Register(string command, Func<JObject, object> handler)
    {
        commands[command] = handler;
    }

    void Update()
    {
        PendingRequest item;
        while (pending.TryDequeue(out item))
        {
            item.response = Execute(item.request);
            item.done.Set();
        }
    }

    string Execute(JObject request)
    {
        JToken id = request["id"];
        string command = (string)request["command"];
        JObject args = request["args"] as JObject ?? new JObject();

        JObject response = new JObject { ["id"] = id };

        Func<JObject, object> handler;
        if (command == null || !commands.TryGetValue(command, out handler))
        {
            response["ok"] = false;
            response["error"] = "Unknown command: " + command;
            return response.ToString(Formatting.None);
        }

        try
        {
            object result = handler(args);
            response["ok"] = true;
            response["result"] = result != null ? JToken.FromObject(result) : JValue.CreateNull();
        }
        catch (McpException e)
        {
            // Expected errors (bad arguments, unknown names) go back to the LLM as a message
            response["ok"] = false;
            response["error"] = e.Message;
        }
        catch (Exception e)
        {
            Debug.LogException(e);
            response["ok"] = false;
            response["error"] = e.GetType().Name + ": " + e.Message;
        }

        return response.ToString(Formatting.None);
    }

    void ListenLoop()
    {
        while (running)
        {
            TcpClient client;
            try
            {
                client = listener.AcceptTcpClient();
            }
            catch (SocketException)
            {
                // Listener stopped
                return;
            }
            catch (ObjectDisposedException)
            {
                return;
            }

            new Thread(() => HandleClient(client)) { IsBackground = true, Name = "McpBridgeClient" }.Start();
        }
    }

    void HandleClient(TcpClient client)
    {
        using (client)
        using (NetworkStream stream = client.GetStream())
        using (StreamReader reader = new StreamReader(stream, new UTF8Encoding(false)))
        using (StreamWriter writer = new StreamWriter(stream, new UTF8Encoding(false)) { AutoFlush = true, NewLine = "\n" })
        {
            try
            {
                string line;
                while (running && (line = reader.ReadLine()) != null)
                {
                    if (string.IsNullOrWhiteSpace(line)) continue;

                    writer.WriteLine(Dispatch(line));
                }
            }
            catch (IOException)
            {
                // Client disconnected
            }
        }
    }

    string Dispatch(string line)
    {
        JObject request;
        try
        {
            request = JObject.Parse(line);
        }
        catch (JsonException e)
        {
            return new JObject { ["id"] = null, ["ok"] = false, ["error"] = "Invalid JSON: " + e.Message }.ToString(Formatting.None);
        }

        PendingRequest item = new PendingRequest { request = request };
        pending.Enqueue(item);

        if (!item.done.Wait(RequestTimeoutMs))
            return new JObject { ["id"] = request["id"], ["ok"] = false, ["error"] = "Timed out waiting for the Unity main thread" }.ToString(Formatting.None);

        return item.response;
    }
}

// Thrown by command handlers for errors the caller should see as a plain message
public class McpException : Exception
{
    public McpException(string message) : base(message) { }
}
