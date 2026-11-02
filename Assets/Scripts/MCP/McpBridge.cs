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
// It opens its port only when the MCP server is installed (OptionalFeatures); without, it stays in the scene for the
// avatars and animation library it holds, which the character menu's animation import uses too.
// The port is closed whenever the bridge stops, also before scripts are reloaded in Play mode: a listener left open
// would keep the port, accept connections and never answer them. If the port is taken anyway, the bridge says so and
// keeps trying.
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
    readonly List<TcpClient> clients = new List<TcpClient>();
    float nextBindAttempt;
    bool bindWarned;
    bool bindWarningShown;
    bool serving;

    // The listener of this script domain, closed before a new one opens
    static TcpListener openListener;

    // Main-thread requests must not wait forever if the app stops updating
    const int RequestTimeoutMs = 30000;

    void Awake()
    {
        Bridge = this;

        // Requests come from a terminal, so the app must keep updating without focus
        Application.runInBackground = true;
        GroundProbe.Layers = groundLayers;
        MixamoAnimationPlayer.RegisterClipFixes(animationLibrary);

        Register("ping", args => new
        {
            scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name,
            avatars = AvatarRegistry.Count,
            speech_to_text = OptionalFeatures.IsInstalled(OptionalFeatures.Feature.SpeechToText)
        });
        AvatarCommands.Register(this);
        SceneCommands.Register(this);
        BlockCommands.Register(this);
        AnimationFileCommands.Register(this);
        SpeechCommands.Register(this);
        PlayerCommands.Register(this);
    }

    void OnEnable()
    {
#if UNITY_EDITOR
        UnityEditor.AssemblyReloadEvents.beforeAssemblyReload += StopListening;
#endif
        Application.quitting += StopListening;
        bindWarned = bindWarningShown = false;
        serving = OptionalFeatures.IsInstalled(OptionalFeatures.Feature.McpServer);
        if (serving)
            TryListen();
    }

    void OnDisable()
    {
#if UNITY_EDITOR
        UnityEditor.AssemblyReloadEvents.beforeAssemblyReload -= StopListening;
#endif
        Application.quitting -= StopListening;
        StopListening();
    }

    // False while the port is taken; Update tries again
    bool TryListen()
    {
        if (openListener != null)
        {
            openListener.Stop();
            openListener = null;
        }

        TcpListener candidate = new TcpListener(IPAddress.Loopback, port);
        try
        {
            candidate.Start();
        }
        catch (SocketException e)
        {
            nextBindAttempt = Time.realtimeSinceStartup + 2;
            if (!bindWarned)
            {
                bindWarned = true;
                Debug.LogWarning("McpBridge: port " + port + " is taken (" + e.Message + "), retrying. If it stays taken, an earlier session left it open: restart Unity.");
            }
            return false;
        }

        listener = openListener = candidate;
        running = true;
        listenerThread = new Thread(ListenLoop) { IsBackground = true, Name = "McpBridge" };
        listenerThread.Start();

        Debug.Log("McpBridge: listening on 127.0.0.1:" + port);
        return true;
    }

    // Closes the port and every connection, so nothing is left accepting requests no one answers
    void StopListening()
    {
        running = false;
        if (listener != null)
        {
            listener.Stop();
            if (openListener == listener)
                openListener = null;
            listener = null;
        }

        lock (clients)
        {
            foreach (TcpClient client in clients)
                client.Close();
            clients.Clear();
        }
    }

    public void Register(string command, Func<JObject, object> handler)
    {
        commands[command] = handler;
    }

    void Update()
    {
        if (serving && listener == null && Time.realtimeSinceStartup >= nextBindAttempt && !TryListen() && !bindWarningShown)
        {
            // Not from OnEnable: the message system is ready only once the scene has started
            bindWarningShown = true;
            MessageSystem.DisplayWarning("MCP bridge: port " + port + " is taken, restart Unity if this persists");
        }

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

            lock (clients)
                clients.Add(client);
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
            catch (ObjectDisposedException)
            {
                // Closed by StopListening
            }
        }

        lock (clients)
            clients.Remove(client);
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
