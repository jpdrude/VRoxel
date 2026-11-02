using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;

// Types into a terminal window as if on the keyboard, for SpeechToText: brings the window to the foreground, sends the
// keys with SendInput and gives the focus back to VRoxel. Waiting for Windows to switch the focus takes up to a tenth
// of a second each way, so it all happens on a thread of its own, one request after another; failures are reported
// back through TakeFailure. Text goes in as one paste (Ctrl+V, the caller puts it on the clipboard), as terminal tools
// handle typed text a key at a time and would still be busy with it when the focus moves on.
public static class TerminalKeyboard
{
    struct Request
    {
        public IntPtr window;
        public string keys;
        public IntPtr returnTo;
    }

    static readonly BlockingCollection<Request> requests = new BlockingCollection<Request>();
    static readonly ConcurrentQueue<string> failures = new ConcurrentQueue<string>();
    static Thread worker;

    // How long the terminal keeps the focus after the keys, so they reach it before VRoxel is in front again
    const int DeliverMs = 200;

    // Keys pressed in the window one after another, separated by spaces, each with optional modifiers: e.g. "ctrl+u"
    // or "esc esc". Letters, digits, enter, esc, backspace, tab, delete, home and end, with ctrl, alt and shift.
    // Afterwards returnTo gets the focus back, unless more requests are waiting.
    public static void Press(IntPtr window, string keys, IntPtr returnTo)
    {
        if (worker == null || !worker.IsAlive)
        {
            worker = new Thread(Work) { IsBackground = true, Name = "Terminal keyboard" };
            worker.Start();
        }
        requests.Add(new Request { window = window, keys = keys, returnTo = returnTo });
    }

    // A failed request's message, for the main thread to show
    public static bool TakeFailure(out string message)
    {
        return failures.TryDequeue(out message);
    }

    static void Work()
    {
        foreach (Request request in requests.GetConsumingEnumerable())
        {
            if (!Activate(request.window))
            {
                failures.Enqueue("The assistant terminal could not be brought to the front");
                continue;
            }
            if (!PressKeys(request.keys))
            {
                failures.Enqueue("Could not press '" + request.keys + "' in the assistant terminal");
                continue;
            }

            Thread.Sleep(DeliverMs);
            if (requests.Count == 0)
                Activate(request.returnTo);
        }
    }

    static bool PressKeys(string keys)
    {
        List<INPUT> inputs = new List<INPUT>();
        foreach (string combination in keys.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries))
        {
            List<ushort> modifiers = new List<ushort>();
            ushort key = 0;
            foreach (string part in combination.ToLowerInvariant().Split('+'))
            {
                switch (part)
                {
                    case "ctrl": modifiers.Add(VK_CONTROL); break;
                    case "alt": modifiers.Add(VK_MENU); break;
                    case "shift": modifiers.Add(VK_SHIFT); break;
                    default: key = KeyCode(part); break;
                }
            }
            if (key == 0)
                return false;

            foreach (ushort modifier in modifiers)
                inputs.Add(Key(modifier, false));
            inputs.Add(Key(key, false));
            inputs.Add(Key(key, true));
            for (int i = modifiers.Count - 1; i >= 0; --i)
                inputs.Add(Key(modifiers[i], true));
        }
        return inputs.Count > 0 && Send(inputs.ToArray());
    }

    static ushort KeyCode(string name)
    {
        if (name.Length == 1 && char.IsLetterOrDigit(name[0]))
            return char.ToUpperInvariant(name[0]);

        switch (name)
        {
            case "enter": return VK_RETURN;
            case "esc": case "escape": return 0x1B;
            case "backspace": return 0x08;
            case "tab": return 0x09;
            case "delete": return 0x2E;
            case "home": return 0x24;
            case "end": return 0x23;
            default: return 0;
        }
    }

    // A window to the foreground, the terminal or VRoxel's own afterwards. Windows only lets the foreground window's
    // process do that, so this thread joins the foreground window's input queue for the moment.
    static bool Activate(IntPtr window)
    {
        if (window == IntPtr.Zero)
            return false;

        IntPtr foreground = GetForegroundWindow();
        if (foreground == window)
            return true;

        uint ownThread = GetCurrentThreadId();
        uint foregroundThread = GetWindowThreadProcessId(foreground, IntPtr.Zero);
        bool attached = foregroundThread != ownThread && AttachThreadInput(ownThread, foregroundThread, true);
        try
        {
            if (IsIconic(window))
                ShowWindow(window, SW_RESTORE);
            SetForegroundWindow(window);
        }
        finally
        {
            if (attached)
                AttachThreadInput(ownThread, foregroundThread, false);
        }

        // The switch takes a moment
        for (int i = 0; i < 20 && GetForegroundWindow() != window; ++i)
            Thread.Sleep(5);
        return GetForegroundWindow() == window;
    }

    static bool Send(params INPUT[] inputs)
    {
        return SendInput((uint)inputs.Length, inputs, Marshal.SizeOf(typeof(INPUT))) == inputs.Length;
    }

    static INPUT Key(ushort virtualKey, bool up)
    {
        return new INPUT { type = INPUT_KEYBOARD, u = new InputUnion { ki = new KEYBDINPUT { wVk = virtualKey, dwFlags = up ? KEYEVENTF_KEYUP : 0 } } };
    }

    [StructLayout(LayoutKind.Sequential)]
    struct INPUT
    {
        public uint type;
        public InputUnion u;
    }

    // Sized by its largest member, as Windows expects
    [StructLayout(LayoutKind.Explicit)]
    struct InputUnion
    {
        [FieldOffset(0)] public MOUSEINPUT mi;
        [FieldOffset(0)] public KEYBDINPUT ki;
    }

    [StructLayout(LayoutKind.Sequential)]
    struct MOUSEINPUT
    {
        public int dx;
        public int dy;
        public uint mouseData;
        public uint dwFlags;
        public uint time;
        public IntPtr dwExtraInfo;
    }

    [StructLayout(LayoutKind.Sequential)]
    struct KEYBDINPUT
    {
        public ushort wVk;
        public ushort wScan;
        public uint dwFlags;
        public uint time;
        public IntPtr dwExtraInfo;
    }

    const uint INPUT_KEYBOARD = 1;
    const uint KEYEVENTF_KEYUP = 0x0002;
    const ushort VK_RETURN = 0x0D;
    const ushort VK_SHIFT = 0x10;
    const ushort VK_CONTROL = 0x11;
    const ushort VK_MENU = 0x12;
    const int SW_RESTORE = 9;

    [DllImport("user32.dll", SetLastError = true)] static extern uint SendInput(uint count, INPUT[] inputs, int size);
    [DllImport("user32.dll")] static extern IntPtr GetForegroundWindow();
    [DllImport("user32.dll")] static extern bool SetForegroundWindow(IntPtr window);
    [DllImport("user32.dll")] static extern bool ShowWindow(IntPtr window, int command);
    [DllImport("user32.dll")] static extern bool IsIconic(IntPtr window);
    [DllImport("user32.dll")] static extern uint GetWindowThreadProcessId(IntPtr window, IntPtr processId);
    [DllImport("user32.dll")] static extern bool AttachThreadInput(uint attach, uint attachTo, bool doAttach);
    [DllImport("kernel32.dll")] static extern uint GetCurrentThreadId();
}
