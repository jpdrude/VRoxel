using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using UnityEngine;

// Opens a terminal running a command line LLM tool (Claude Code, Codex, ...) and keeps it above all other windows, in
// the editor and in builds. An optional feature: its settings are in OptionalFeatures (assistantTerminal), and
// without "installed": true there it doesn't open. Each tool gets a window of its own, found by its title and then
// remembered while it exists, as the terminal may change or decorate the title for a moment.
public static class AssistantTerminal
{
    [Serializable]
    public class Settings
    {
        public bool installed = false;

        // The terminal and its arguments; {title}, {dir} and {command} are filled in. The terminal has to keep the
        // window title (Windows Terminal: --suppressApplicationTitle)
        public string terminal = "wt.exe";
        public string terminalArguments = "-w new --title \"{title}\" --suppressApplicationTitle -d \"{dir}\" {command}";
        // Empty: the project folder in the editor, Assistant in VRoxel's data folder in a build (where the tools may
        // write and their MCP setup goes); relative paths start at the project or installation folder
        public string workingDirectory = "";
        public string windowTitle = "VRoxel Assistant";
        public bool alwaysOnTop = true;
        // The priority of the processes a launch starts (the tool and its console), so VRoxel gets the CPU first when
        // both want it: Normal, BelowNormal or Idle. Windows Terminal itself, shared by all terminal windows, keeps its own
        public string toolPriority = "BelowNormal";

        // The command starting each tool
        public string claudeCode = "claude";
        public string codex = "codex";
        public string geminiCli = "gemini";
        public string copilotCli = "copilot";
        public string openCode = "opencode";
        // The last command opened with OpenCustom
        public string customCommand = "";

        // The terminal shown in VR (AssistantTerminalMirror, sized by its frame)
        public bool mirrorInVR = true;
        public float mirrorFramesPerSecond = 10;
        // Captured at most this wide; more doesn't show on a mirror a few dozen centimeters wide
        public int mirrorMaxWidth = 1280;
        // A capture is uploaded once at least this many tiles of 16 x 16 pixels (about a character each) changed, so a
        // spinner or the cursor alone doesn't cost an upload; at the latest after mirrorRefreshSeconds anyway
        public int mirrorMinChangedTiles = 4;
        public float mirrorRefreshSeconds = 3;
    }

    public enum Tool { ClaudeCode, Codex, GeminiCli, CopilotCli, OpenCode, Custom }

    public static Settings Current { get { return OptionalFeatures.Current.assistantTerminal; } }

    public static bool Installed { get { return Current.installed; } }

    public static string ToolName(Tool tool)
    {
        switch (tool)
        {
            case Tool.ClaudeCode: return "Claude Code";
            case Tool.Codex: return "Codex";
            case Tool.GeminiCli: return "Gemini CLI";
            case Tool.CopilotCli: return "Copilot CLI";
            case Tool.OpenCode: return "OpenCode";
            default: return "Custom";
        }
    }

    public static string Command(Tool tool)
    {
        Settings s = Current;
        switch (tool)
        {
            case Tool.ClaudeCode: return s.claudeCode;
            case Tool.Codex: return s.codex;
            case Tool.GeminiCli: return s.geminiCli;
            case Tool.CopilotCli: return s.copilotCli;
            case Tool.OpenCode: return s.openCode;
            default: return s.customCommand;
        }
    }

    static string Title(Tool tool)
    {
        return Current.windowTitle + " - " + ToolName(tool);
    }

    public static string WorkingDirectory
    {
        get
        {
            string root = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            string dir = Current.workingDirectory;
            if (!string.IsNullOrEmpty(dir))
                return Path.GetFullPath(Path.Combine(root, Environment.ExpandEnvironmentVariables(dir)));

            return Application.isEditor ? root : Path.Combine(Application.persistentDataPath, "Assistant");
        }
    }

    // Opens the tool's terminal, or brings it to the front if it is open already. False if it could not be started.
    public static bool Open(Tool tool)
    {
        return tool == Tool.Custom ? OpenCustom(Current.customCommand) : Launch(Title(tool), Command(tool));
    }

    // Opens a terminal running any command, e.g. another LLM tool; the command is remembered for the next session
    public static bool OpenCustom(string command)
    {
        if (!Installed)
        {
            Warn("The assistant terminal is not installed");
            return false;
        }

        command = command != null ? command.Trim() : "";
        if (command.Length == 0)
        {
            Warn("Enter the command that starts the assistant");
            return false;
        }

        if (command != Current.customCommand)
        {
            // One custom terminal at a time; closing it here could end a running session
            IntPtr open = FindTitled(Title(Tool.Custom));
            if (open != IntPtr.Zero)
            {
                BringToFront(open);
                Warn("Close the custom assistant (" + Current.customCommand + ") before starting another one");
                return false;
            }

            Current.customCommand = command;
            OptionalFeatures.Current.Save();
        }

        return Launch(Title(Tool.Custom), command);
    }

    // The window of the tool opened last that is still open, or IntPtr.Zero while none is
    public static IntPtr Window
    {
        get
        {
            IntPtr window = lastTitle != null ? FindTitled(lastTitle) : IntPtr.Zero;
            if (window != IntPtr.Zero)
                return window;

            foreach (Tool tool in (Tool[])Enum.GetValues(typeof(Tool)))
            {
                window = FindTitled(Title(tool));
                if (window != IntPtr.Zero)
                {
                    lastTitle = Title(tool);
                    return window;
                }
            }
            return IntPtr.Zero;
        }
    }

    public static bool IsOpen { get { return Window != IntPtr.Zero; } }

    static string lastTitle;
    // The windows found by title and their processes, kept while they exist (a window handle can be reused by another
    // window, the process tells them apart)
    static readonly Dictionary<string, KeyValuePair<IntPtr, uint>> windows = new Dictionary<string, KeyValuePair<IntPtr, uint>>();
    static string pendingTitle;
    static float launchedAt;
    static DateTime launchedAtClock;
    const float LaunchTimeout = 15;

    static bool Launch(string title, string command)
    {
        if (!Installed)
        {
            Warn("The assistant terminal is not installed");
            return false;
        }

        if (string.IsNullOrEmpty(command))
        {
            Warn("No command is set for " + title);
            return false;
        }

        IntPtr window = FindTitled(title);
        if (window != IntPtr.Zero)
        {
            lastTitle = title;
            BringToFront(window);
            return true;
        }

        // Started a moment ago and its window is not there yet
        if (pendingTitle == title && Time.realtimeSinceStartup - launchedAt < LaunchTimeout)
            return true;

        Settings s = Current;
        string dir = WorkingDirectory;
        try
        {
            Directory.CreateDirectory(dir);
            Process.Start(new ProcessStartInfo(Environment.ExpandEnvironmentVariables(s.terminal),
                s.terminalArguments.Replace("{title}", title).Replace("{dir}", dir).Replace("{command}", command))
            {
                WorkingDirectory = dir,
                UseShellExecute = true
            });
        }
        catch (Exception e)
        {
            Warn("The assistant terminal could not be started: " + e.Message);
            return false;
        }

        lastTitle = title;
        pendingTitle = title;
        launchedAt = Time.realtimeSinceStartup;
        launchedAtClock = DateTime.Now;
        if (Application.isPlaying)
            AssistantTerminalWatcher.Watch();
        return true;
    }

    public static bool LaunchPending { get { return pendingTitle != null; } }

    // Called every frame while a launch is pending (AssistantTerminalWatcher, the editor menu): pins the new window
    // once it appears
    public static void CheckPendingLaunch()
    {
        if (pendingTitle == null)
            return;

        IntPtr window = FindTitled(pendingTitle);
        if (window != IntPtr.Zero)
        {
            pendingTitle = null;
            BringToFront(window);
            LowerToolPriority(window);
        }
        else if (Time.realtimeSinceStartup - launchedAt > LaunchTimeout)
        {
            Warn("The assistant terminal did not open a window titled \"" + pendingTitle + "\"");
            pendingTitle = null;
        }
    }

    // The processes the launch started under the terminal's process (its console host and the tool) get toolPriority;
    // what the tool starts later inherits it. Looked for twice on a pool thread, as the tool may start after the window
    static void LowerToolPriority(IntPtr window)
    {
        ProcessPriorityClass priority;
        if (!Enum.TryParse(Current.toolPriority, true, out priority) || priority == ProcessPriorityClass.Normal)
            return;

        uint terminal = ProcessOf(window);
        DateTime since = launchedAtClock.AddSeconds(-2);
        System.Threading.ThreadPool.QueueUserWorkItem(unused =>
        {
            SetDescendantPriority(terminal, since, priority);
            System.Threading.Thread.Sleep(3000);
            SetDescendantPriority(terminal, since, priority);
        });
    }

    static void SetDescendantPriority(uint root, DateTime since, ProcessPriorityClass priority)
    {
        Dictionary<uint, List<uint>> children = new Dictionary<uint, List<uint>>();
        IntPtr snapshot = CreateToolhelp32Snapshot(TH32CS_SNAPPROCESS, 0);
        if (snapshot == new IntPtr(-1))
            return;
        try
        {
            PROCESSENTRY32W entry = new PROCESSENTRY32W { dwSize = (uint)Marshal.SizeOf(typeof(PROCESSENTRY32W)) };
            for (bool more = Process32FirstW(snapshot, ref entry); more; more = Process32NextW(snapshot, ref entry))
            {
                List<uint> list;
                if (!children.TryGetValue(entry.th32ParentProcessID, out list))
                    children[entry.th32ParentProcessID] = list = new List<uint>();
                list.Add(entry.th32ProcessID);
            }
        }
        finally
        {
            CloseHandle(snapshot);
        }

        Queue<uint> open = new Queue<uint>();
        open.Enqueue(root);
        HashSet<uint> seen = new HashSet<uint> { root };
        while (open.Count > 0)
        {
            List<uint> list;
            if (!children.TryGetValue(open.Dequeue(), out list))
                continue;
            foreach (uint id in list)
            {
                if (!seen.Add(id))
                    continue;
                open.Enqueue(id);
                try
                {
                    // Only what this launch started, not the terminal's other tabs
                    using (Process process = Process.GetProcessById((int)id))
                        if (process.StartTime >= since && process.PriorityClass != priority)
                            process.PriorityClass = priority;
                }
                catch (Exception)
                {
                    // Exited meanwhile, or not ours to change
                }
            }
        }
    }

    // The window found for this title before if it still exists, else the window with this title, or one whose title
    // starts with it
    static IntPtr FindTitled(string title)
    {
        KeyValuePair<IntPtr, uint> known;
        if (windows.TryGetValue(title, out known) && IsWindow(known.Key) && ProcessOf(known.Key) == known.Value)
            return known.Key;

        IntPtr window = FindWindow(null, title);
        if (window == IntPtr.Zero)
        {
            EnumWindows((candidate, unused) =>
            {
                StringBuilder text = new StringBuilder(256);
                GetWindowText(candidate, text, text.Capacity);
                if (!IsWindowVisible(candidate) || !text.ToString().StartsWith(title, StringComparison.Ordinal))
                    return true;
                window = candidate;
                return false;
            }, IntPtr.Zero);
        }

        if (window != IntPtr.Zero)
            windows[title] = new KeyValuePair<IntPtr, uint>(window, ProcessOf(window));
        else
            windows.Remove(title);
        return window;
    }

    static uint ProcessOf(IntPtr window)
    {
        uint process;
        GetWindowThreadProcessId(window, out process);
        return process;
    }

    static void BringToFront(IntPtr window)
    {
        if (IsIconic(window))
            ShowWindow(window, SW_RESTORE);

        if (Current.alwaysOnTop)
            SetWindowPos(window, HWND_TOPMOST, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE | SWP_SHOWWINDOW);

        SetForegroundWindow(window);
    }

    // MessageSystem needs the scene's UI; outside Play mode (the editor menu) warnings go to the console
    static void Warn(string message)
    {
        if (Application.isPlaying)
            MessageSystem.DisplayWarning(message);
        else
            UnityEngine.Debug.LogWarning(message);
    }

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    static extern IntPtr FindWindow(string className, string windowName);
    [DllImport("user32.dll")]
    static extern bool SetWindowPos(IntPtr window, IntPtr insertAfter, int x, int y, int width, int height, uint flags);
    [DllImport("user32.dll")]
    static extern bool SetForegroundWindow(IntPtr window);
    [DllImport("user32.dll")]
    static extern bool ShowWindow(IntPtr window, int command);
    [DllImport("user32.dll")]
    internal static extern bool IsIconic(IntPtr window);
    [DllImport("user32.dll")]
    static extern bool IsWindow(IntPtr window);
    [DllImport("user32.dll")]
    static extern uint GetWindowThreadProcessId(IntPtr window, out uint processId);

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    struct PROCESSENTRY32W
    {
        public uint dwSize;
        public uint cntUsage;
        public uint th32ProcessID;
        public IntPtr th32DefaultHeapID;
        public uint th32ModuleID;
        public uint cntThreads;
        public uint th32ParentProcessID;
        public int pcPriClassBase;
        public uint dwFlags;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
        public string szExeFile;
    }

    const uint TH32CS_SNAPPROCESS = 0x2;
    [DllImport("kernel32.dll", SetLastError = true)]
    static extern IntPtr CreateToolhelp32Snapshot(uint flags, uint processId);
    [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
    static extern bool Process32FirstW(IntPtr snapshot, ref PROCESSENTRY32W entry);
    [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
    static extern bool Process32NextW(IntPtr snapshot, ref PROCESSENTRY32W entry);
    [DllImport("kernel32.dll")]
    static extern bool CloseHandle(IntPtr handle);
    [DllImport("user32.dll")]
    static extern bool IsWindowVisible(IntPtr window);
    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    static extern int GetWindowText(IntPtr window, StringBuilder text, int maxCount);
    delegate bool EnumWindowsProc(IntPtr window, IntPtr parameter);
    [DllImport("user32.dll")]
    static extern bool EnumWindows(EnumWindowsProc callback, IntPtr parameter);

    static readonly IntPtr HWND_TOPMOST = new IntPtr(-1);
    const uint SWP_NOSIZE = 0x0001;
    const uint SWP_NOMOVE = 0x0002;
    const uint SWP_SHOWWINDOW = 0x0040;
    const int SW_RESTORE = 9;
}
