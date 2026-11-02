using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.Networking;
using Valve.VR;

// Push to talk into the assistant terminal, transcribed on this machine by Whisper (whisper.cpp's server, WhisperServer).
// One button, on the keyboard (pushToTalkKey, also while the terminal has focus) or a VR controller (vrAction):
// held, it records, and on release the text is typed into the terminal without sending it; a click sends what is
// typed (Enter), a double click clears the input line (clearKeys). The text goes in as one paste, the clipboard gets
// its old text back afterwards. After typing into the terminal the focus goes back to VRoxel (the editor or the
// game), so work goes on there. Nothing of it waits on VRoxel's main thread: the microphone is opened once, when an
// assistant terminal opens, and records into a loop a press only marks a position in; the key is timed on a thread
// of its own, and TerminalKeyboard types and switches the focus on another.
// An optional feature (OptionalFeatures, speechToText): when installed, it runs on its own hidden object from the
// first scene on.
public class SpeechToText : MonoBehaviour
{
    [Serializable]
    public class Settings
    {
        public bool installed = false;

        // whisper.cpp's server and a ggml model; relative paths start at the project folder in the editor and the
        // installation folder in a build
        public string serverPath = "Whisper/whisper-server.exe";
        public string modelPath = "Whisper/ggml-small.bin";
        public int port = 8178;
        // "auto" or a language code, e.g. "de" or "en"
        public string language = "auto";
        public int threads = 4;
        public string serverArguments = "";

        // Empty: the default microphone
        public string microphone = "";
        // A key name (F1 to F24, RightControl, RightAlt, ScrollLock, Pause, Insert, ...) or a Windows virtual key code
        public string pushToTalkKey = "F8";
        // A boolean SteamVR action; empty for none
        public string vrAction = "/actions/VRoxel/in/PushToTalk";
        // What a double click presses to clear the terminal's input line (Claude Code: Ctrl+U, with the cursor at the end)
        public string clearKeys = "ctrl+u";
        // Held at least this long it records; shorter it is a click
        public float holdSeconds = 0.3f;
        public float doubleClickSeconds = 0.4f;
        public int maxRecordSeconds = 60;
    }

    const int SampleRate = 16000;

    static SpeechToText instance;

    Settings settings;
    WhisperServer server;
    int virtualKey;
    uint processId;
    SteamVR_Action_Boolean action;
    bool actionLookedUp;

    // Press and release times of the key, from the polling thread; times in seconds of the clock below
    static readonly System.Diagnostics.Stopwatch clock = System.Diagnostics.Stopwatch.StartNew();
    readonly object keyLock = new object();
    readonly Queue<KeyValuePair<bool, double>> keyEvents = new Queue<KeyValuePair<bool, double>>();
    Thread keyThread;
    volatile bool polling;
    // The terminal's window, which the key also works in, for the polling thread
    long terminalWindow;
    float nextWindowLookup;
    // VRoxel's window last in front, which gets the focus back after typing into the terminal
    IntPtr ownWindow;

    bool keyDown;
    bool actionDown;
    double downAt;
    bool listening;
    // The microphone in the settings, and the one recording into the loop while a terminal is open (opening it takes
    // a moment, too long for a press); a press marks where its recording starts in the loop
    string device;
    AudioClip loop;
    string loopDevice;
    bool recording;
    int recordStart;
    float nextMicrophoneAttempt;
    const float PreRollSeconds = 0.15f;

    bool clickPending;
    double clickAt;
    // Something was dictated since the last send or clear: the next dictation follows after a space
    bool dictated;
    Coroutine restoreClipboard;

    static double Now { get { return clock.Elapsed.TotalSeconds; } }
    bool Down { get { return keyDown || actionDown; } }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Create()
    {
        if (!OptionalFeatures.IsInstalled(OptionalFeatures.Feature.SpeechToText) || FindObjectOfType<SpeechToText>() != null)
            return;

        // Only hidden: an object kept from saving would outlive Play mode in the editor, with its server and thread
        GameObject go = new GameObject("SpeechToText") { hideFlags = HideFlags.HideInHierarchy };
        DontDestroyOnLoad(go);
        go.AddComponent<SpeechToText>();
    }

    public static string ResolvePath(string path)
    {
        string root = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
        return Path.GetFullPath(Path.Combine(root, Environment.ExpandEnvironmentVariables(path ?? "")));
    }

    // The microphone recordings use: a name from Microphone.devices, or empty for the default one
    public static string CurrentMicrophone { get { return OptionalFeatures.Current.speechToText.microphone; } }

    // Changes the microphone from the next recording on and remembers it in OptionalFeatures; empty for the default
    public static void SetMicrophone(string name)
    {
        OptionalFeatures.Current.speechToText.microphone = name ?? "";
        OptionalFeatures.Current.Save();
        // Update opens it in place of the current one once nothing is being recorded
        if (instance != null)
            instance.device = string.IsNullOrEmpty(name) ? null : name;
    }

    // Also after scripts are reloaded in Play mode, which disables and enables again but doesn't start
    void OnEnable()
    {
        instance = this;
        settings = OptionalFeatures.Current.speechToText;
        server = new WhisperServer(settings);
        virtualKey = ParseKey(settings.pushToTalkKey);
        processId = (uint)System.Diagnostics.Process.GetCurrentProcess().Id;
        device = string.IsNullOrEmpty(settings.microphone) ? null : settings.microphone;

#if UNITY_EDITOR
        UnityEditor.AssemblyReloadEvents.beforeAssemblyReload += Shutdown;
#endif
        Application.quitting += Shutdown;

        if (virtualKey != 0)
        {
            polling = true;
            keyThread = new Thread(PollKey) { IsBackground = true, Name = "Push to talk key" };
            keyThread.Start();
        }
    }

    void OnDisable()
    {
#if UNITY_EDITOR
        UnityEditor.AssemblyReloadEvents.beforeAssemblyReload -= Shutdown;
#endif
        Application.quitting -= Shutdown;
        if (instance == this)
            instance = null;
        Shutdown();
    }

    // Also right before scripts are reloaded and when quitting, in case disabling comes too late: the key thread and
    // the server must not outlive the scripts
    void Shutdown()
    {
        polling = false;
        if (keyThread != null)
            keyThread.Join(100);
        keyThread = null;
        CloseMicrophone();
        if (server != null)
            server.Stop();
    }

    void Update()
    {
        if (Time.unscaledTime >= nextWindowLookup)
        {
            nextWindowLookup = Time.unscaledTime + 0.25f;
            Interlocked.Exchange(ref terminalWindow, (long)AssistantTerminal.Window);
        }

        IntPtr foreground = GetForegroundWindow();
        uint foregroundProcess;
        GetWindowThreadProcessId(foreground, out foregroundProcess);
        if (foregroundProcess == processId)
            ownWindow = foreground;

        // The microphone and the server get ready when a terminal opens and stop when the last one closes
        bool terminalOpen = Interlocked.Read(ref terminalWindow) != 0;
        if (!recording && loop != null && (!terminalOpen || loopDevice != device))
            CloseMicrophone();
        if (terminalOpen && loop == null && Time.unscaledTime >= nextMicrophoneAttempt)
            OpenMicrophone();

        string failure;
        while (TerminalKeyboard.TakeFailure(out failure))
            MessageSystem.DisplayWarning(failure);

        // The key's presses and releases at the times they happened, then the controller's as of this frame
        while (true)
        {
            KeyValuePair<bool, double> key;
            lock (keyLock)
            {
                if (keyEvents.Count == 0)
                    break;
                key = keyEvents.Dequeue();
            }
            Set(ref keyDown, key.Key, key.Value);
        }
        Set(ref actionDown, ActionDown(), Now);

        double now = Now;
        if (Down && recording && !listening && now - downAt >= settings.holdSeconds)
        {
            listening = true;
            MessageSystem.DisplayMessage("Listening ...");
        }

        // A single click once no second one followed
        if (clickPending && !Down && now - clickAt > settings.doubleClickSeconds)
            SendPendingClick();
    }

    // One of the buttons changed: the push to talk button is down while either is
    void Set(ref bool source, bool value, double time)
    {
        bool wasDown = Down;
        source = value;
        if (!wasDown && Down)
            Press(time);
        else if (wasDown && !Down)
            Release(time);
    }

    void Press(double time)
    {
        if (clickPending && time - clickAt > settings.doubleClickSeconds)
            SendPendingClick();

        downAt = time;
        if (loop != null)
        {
            // From a moment before the press, as the press is noticed a few milliseconds late
            int lead = Mathf.RoundToInt((float)(Now - time + PreRollSeconds) * loop.frequency);
            recordStart = ((Microphone.GetPosition(loopDevice) - lead) % loop.samples + loop.samples) % loop.samples;
            recording = true;
        }
        listening = false;
    }

    void Release(double time)
    {
        bool held = time - downAt >= settings.holdSeconds;
        if (recording)
        {
            recording = false;
            if (held)
                Transcribe(time);
        }
        else if (held)
            MessageSystem.DisplayWarning("Open an assistant terminal first");
        listening = false;
        if (held)
            return;

        if (clickPending && time - clickAt <= settings.doubleClickSeconds)
        {
            clickPending = false;
            if (PressInTerminal(settings.clearKeys))
                dictated = false;
            return;
        }

        if (clickPending)
            SendPendingClick();
        clickPending = true;
        clickAt = time;
    }

    void SendPendingClick()
    {
        clickPending = false;
        if (PressInTerminal("enter"))
            dictated = false;
    }

    // Keys in the terminal, typed on TerminalKeyboard's thread, which then gives the focus back to VRoxel
    bool PressInTerminal(string keys)
    {
        IntPtr terminal = AssistantTerminal.Window;
        if (terminal == IntPtr.Zero)
        {
            MessageSystem.DisplayWarning("Open an assistant terminal first");
            return false;
        }

        IntPtr returnTo = ownWindow != IntPtr.Zero ? ownWindow : System.Diagnostics.Process.GetCurrentProcess().MainWindowHandle;
        TerminalKeyboard.Press(terminal, keys, returnTo);
        return true;
    }

    // On the polling thread: the key counts while VRoxel or the terminal is in front, not in other programs
    void PollKey()
    {
        bool was = false;
        while (polling)
        {
            bool down = (GetAsyncKeyState(virtualKey) & 0x8000) != 0 && InFront();
            if (down != was)
            {
                was = down;
                lock (keyLock)
                    keyEvents.Enqueue(new KeyValuePair<bool, double>(down, Now));
            }
            Thread.Sleep(5);
        }
    }

    bool InFront()
    {
        IntPtr foreground = GetForegroundWindow();
        uint process;
        GetWindowThreadProcessId(foreground, out process);
        return process == processId || foreground == (IntPtr)Interlocked.Read(ref terminalWindow);
    }

    bool ActionDown()
    {
        if (!SteamVR.active || string.IsNullOrEmpty(settings.vrAction))
            return false;

        if (!actionLookedUp)
        {
            actionLookedUp = true;
            action = SteamVR_Input.GetActionFromPath<SteamVR_Action_Boolean>(settings.vrAction, false, true);
            if (action == null)
                Debug.LogWarning("Speech to text: no SteamVR action " + settings.vrAction + ", create and bind it in Window > SteamVR Input");
        }
        return action != null && action.GetState(SteamVR_Input_Sources.Any);
    }

    void OpenMicrophone()
    {
        // Not again every frame if there is no microphone or the server can't start
        nextMicrophoneAttempt = Time.unscaledTime + 2;

        // The model loads while the terminal opens, ready for the first sentence
        string error;
        if (!server.Ensure(out error))
            MessageSystem.DisplayWarning(error);

        if (Microphone.devices.Length == 0)
            return;

        int min, max;
        Microphone.GetDeviceCaps(device, out min, out max);
        int frequency = min == 0 && max == 0 ? SampleRate : Mathf.Clamp(SampleRate, min, max);
        loop = Microphone.Start(device, true, Mathf.Max(1, settings.maxRecordSeconds) + 1, frequency);
        loopDevice = device;
    }

    void CloseMicrophone()
    {
        if (loop == null)
            return;

        Microphone.End(loopDevice);
        Destroy(loop);
        loop = null;
        recording = false;
    }

    // The loop from the press to now; held longer than the loop is long, its last round
    void Transcribe(double releasedAt)
    {
        int total = loop.samples;
        int end = Microphone.GetPosition(loopDevice);
        int length = ((end - recordStart) % total + total) % total;
        if ((releasedAt - downAt) * loop.frequency >= total - loop.frequency / 2)
        {
            length = total - 1;
            recordStart = (end + 1) % total;
        }

        // Shorter than a quarter second there is nothing to transcribe
        if (length < loop.frequency / 4)
            return;

        int channels = loop.channels;
        float[] data = new float[length * channels];
        int first = Mathf.Min(length, total - recordStart);
        float[] part = new float[first * channels];
        loop.GetData(part, recordStart);
        Array.Copy(part, data, part.Length);
        if (first < length)
        {
            part = new float[(length - first) * channels];
            loop.GetData(part, 0);
            Array.Copy(part, 0, data, first * channels, part.Length);
        }

        string error;
        if (!server.Ensure(out error))
        {
            MessageSystem.DisplayWarning(error);
            return;
        }
        StartCoroutine(Transcribe(Wav(Resample(Mono(data, channels), loop.frequency, SampleRate), SampleRate)));
    }

    IEnumerator Transcribe(byte[] wav)
    {
        MessageSystem.DisplayMessage("Transcribing ...");

        while (true)
        {
            List<IMultipartFormSection> form = new List<IMultipartFormSection>
            {
                new MultipartFormFileSection("file", wav, "speech.wav", "audio/wav"),
                new MultipartFormDataSection("temperature", "0.0"),
                new MultipartFormDataSection("response_format", "json")
            };

            using (UnityWebRequest request = UnityWebRequest.Post(server.InferenceUrl, form))
            {
                yield return request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.ConnectionError && server.Starting)
                {
                    // Still loading the model
                    yield return new WaitForSecondsRealtime(0.5f);
                    continue;
                }

                if (request.result != UnityWebRequest.Result.Success)
                {
                    MessageSystem.DisplayWarning("Speech to text failed: " + request.error);
                    yield break;
                }

                string text = Clean((string)JObject.Parse(request.downloadHandler.text)["text"]);
                if (text.Length == 0)
                {
                    MessageSystem.DisplayMessage("Nothing recognised");
                    yield break;
                }

                // Pasted in one go; the clipboard is the only way to hand the terminal the whole text at once
                string previous = GUIUtility.systemCopyBuffer;
                string pasted = (dictated ? " " : "") + text;
                GUIUtility.systemCopyBuffer = pasted;
                if (PressInTerminal("ctrl+v"))
                    dictated = true;
                RestoreClipboard(previous, pasted);
                yield break;
            }
        }
    }

    // The clipboard's text from before the paste, once the terminal has read it, unless something else was copied since
    void RestoreClipboard(string previous, string pasted)
    {
        if (restoreClipboard != null)
            StopCoroutine(restoreClipboard);
        restoreClipboard = StartCoroutine(RestoreClipboardSoon(previous, pasted));
    }

    IEnumerator RestoreClipboardSoon(string previous, string pasted)
    {
        yield return new WaitForSecondsRealtime(1);
        if (GUIUtility.systemCopyBuffer == pasted)
            GUIUtility.systemCopyBuffer = previous ?? "";
        restoreClipboard = null;
    }

    // One line (Enter would send it), without Whisper's markers such as [BLANK_AUDIO]
    static string Clean(string text)
    {
        if (string.IsNullOrEmpty(text))
            return "";

        text = System.Text.RegularExpressions.Regex.Replace(text, @"\[[^\]]*\]", " ");
        text = System.Text.RegularExpressions.Regex.Replace(text, @"\s+", " ");
        return text.Trim();
    }

    static float[] Mono(float[] data, int channels)
    {
        if (channels <= 1)
            return data;

        float[] mono = new float[data.Length / channels];
        for (int i = 0; i < mono.Length; ++i)
        {
            float sum = 0;
            for (int c = 0; c < channels; ++c)
                sum += data[i * channels + c];
            mono[i] = sum / channels;
        }
        return mono;
    }

    // Whisper takes 16 kHz; linear interpolation is enough for speech
    static float[] Resample(float[] data, int from, int to)
    {
        if (from == to || data.Length == 0)
            return data;

        float[] result = new float[(int)((long)data.Length * to / from)];
        for (int i = 0; i < result.Length; ++i)
        {
            float source = (float)i * from / to;
            int index = Mathf.Min((int)source, data.Length - 1);
            int next = Mathf.Min(index + 1, data.Length - 1);
            result[i] = Mathf.Lerp(data[index], data[next], source - index);
        }
        return result;
    }

    // 16 bit mono PCM
    static byte[] Wav(float[] samples, int sampleRate)
    {
        using (MemoryStream stream = new MemoryStream(44 + samples.Length * 2))
        using (BinaryWriter writer = new BinaryWriter(stream))
        {
            writer.Write(new[] { 'R', 'I', 'F', 'F' });
            writer.Write(36 + samples.Length * 2);
            writer.Write(new[] { 'W', 'A', 'V', 'E', 'f', 'm', 't', ' ' });
            writer.Write(16);
            writer.Write((short)1);
            writer.Write((short)1);
            writer.Write(sampleRate);
            writer.Write(sampleRate * 2);
            writer.Write((short)2);
            writer.Write((short)16);
            writer.Write(new[] { 'd', 'a', 't', 'a' });
            writer.Write(samples.Length * 2);
            foreach (float sample in samples)
                writer.Write((short)(Mathf.Clamp(sample, -1, 1) * short.MaxValue));
            writer.Flush();
            return stream.ToArray();
        }
    }

    static int ParseKey(string key)
    {
        if (string.IsNullOrEmpty(key))
            return 0;

        int f;
        if (key.Length > 1 && (key[0] == 'F' || key[0] == 'f') && int.TryParse(key.Substring(1), out f) && f >= 1 && f <= 24)
            return 0x70 + f - 1;

        switch (key.ToLowerInvariant())
        {
            case "rightcontrol": return 0xA3;
            case "leftcontrol": return 0xA2;
            case "rightalt": return 0xA5;
            case "rightshift": return 0xA1;
            case "scrolllock": return 0x91;
            case "pause": return 0x13;
            case "insert": return 0x2D;
            case "home": return 0x24;
            case "end": return 0x23;
            case "pageup": return 0x21;
            case "pagedown": return 0x22;
        }

        int code;
        if (key.StartsWith("0x", StringComparison.OrdinalIgnoreCase) && int.TryParse(key.Substring(2), System.Globalization.NumberStyles.HexNumber, null, out code))
            return code;
        if (int.TryParse(key, out code))
            return code;

        Debug.LogWarning("Speech to text: unknown push to talk key " + key);
        return 0;
    }

    [DllImport("user32.dll")] static extern short GetAsyncKeyState(int virtualKey);
    [DllImport("user32.dll")] static extern IntPtr GetForegroundWindow();
    [DllImport("user32.dll")] static extern uint GetWindowThreadProcessId(IntPtr window, out uint processId);
}
