using UnityEditor;

// VRoxel > Assistant: the same terminals as AssistantMenu, also outside Play mode; greyed out while the assistant is
// not installed (OptionalFeatures)
public static class AssistantTerminalMenu
{
    const string Menu = "VRoxel/Assistant/";

    [MenuItem(Menu + "Claude Code")] static void ClaudeCode() { Open(AssistantTerminal.Tool.ClaudeCode); }
    [MenuItem(Menu + "Codex")] static void Codex() { Open(AssistantTerminal.Tool.Codex); }
    [MenuItem(Menu + "Gemini CLI")] static void GeminiCli() { Open(AssistantTerminal.Tool.GeminiCli); }
    [MenuItem(Menu + "Copilot CLI")] static void CopilotCli() { Open(AssistantTerminal.Tool.CopilotCli); }
    [MenuItem(Menu + "OpenCode")] static void OpenCode() { Open(AssistantTerminal.Tool.OpenCode); }
    [MenuItem(Menu + "Last Custom Command")] static void Custom() { Open(AssistantTerminal.Tool.Custom); }

    [MenuItem(Menu + "Claude Code", true)]
    [MenuItem(Menu + "Codex", true)]
    [MenuItem(Menu + "Gemini CLI", true)]
    [MenuItem(Menu + "Copilot CLI", true)]
    [MenuItem(Menu + "OpenCode", true)]
    [MenuItem(Menu + "Last Custom Command", true)]
    static bool CanOpen()
    {
        OptionalFeatures.Reload();
        return AssistantTerminal.Installed;
    }

    [MenuItem(Menu + "Show Settings File", false, 100)]
    static void ShowSettings()
    {
        // Reading the settings writes the file if it is missing
        OptionalFeatures.Reload();
        if (OptionalFeatures.Current != null)
            EditorUtility.RevealInFinder(OptionalFeatures.FilePath);
    }

    static void Open(AssistantTerminal.Tool tool)
    {
        if (!AssistantTerminal.Open(tool))
            return;

        // Pin the window once it appears, as AssistantTerminalWatcher does in Play mode
        EditorApplication.update -= CheckPendingLaunch;
        EditorApplication.update += CheckPendingLaunch;
    }

    static void CheckPendingLaunch()
    {
        AssistantTerminal.CheckPendingLaunch();
        if (!AssistantTerminal.LaunchPending)
            EditorApplication.update -= CheckPendingLaunch;
    }
}
