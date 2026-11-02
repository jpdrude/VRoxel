using System;
using System.IO;
using UnityEngine;

// The optional features and their settings, each building on the one before: the MCP server, the assistant terminal,
// push to talk (speech to text). The avatars and their animations are part of every installation.
// Whether a feature is installed comes from InstalledFeatures.json next to VRoxel.exe, which the installer writes for
// all users of the machine. Without it (in the editor, or a build run from its folder) the flags in the user's settings
// decide, and in the editor the MCP server counts as installed.
// The settings are in optionalFeatures.json in VRoxel's data folder (Application.persistentDataPath), each user's own,
// written with the defaults if missing.
[Serializable]
public class OptionalFeatures
{
    // Numbered, as buttons store them in the scene (2 was the Mixamo animation library, now always installed)
    public enum Feature { AssistantTerminal = 0, SpeechToText = 1, McpServer = 3 }

    [Serializable]
    public class Switch
    {
        public bool installed = false;
    }

    public Switch mcpServer = new Switch();
    public AssistantTerminal.Settings assistantTerminal = new AssistantTerminal.Settings();
    public SpeechToText.Settings speechToText = new SpeechToText.Settings();

    // What the installer wrote
    [Serializable]
    class Installed
    {
        public bool mcpServer;
        public bool assistantTerminal;
        public bool speechToText;
    }

    public const string FileName = "optionalFeatures.json";
    public const string InstalledFileName = "InstalledFeatures.json";

    public static string FilePath { get { return Path.Combine(Application.persistentDataPath, FileName); } }

    // Next to VRoxel.exe in a build, the project folder in the editor
    public static string InstalledFilePath { get { return Path.GetFullPath(Path.Combine(Application.dataPath, "..", InstalledFileName)); } }

    static OptionalFeatures current;
    static Installed installed;
    static bool installedRead;

    public static OptionalFeatures Current
    {
        get
        {
            if (current == null)
                current = Load();
            return current;
        }
    }

    // A feature counts only if the ones it builds on are installed too
    public static bool IsInstalled(Feature feature)
    {
        switch (feature)
        {
            case Feature.McpServer:
                return Flag(feature);
            case Feature.AssistantTerminal:
                return Flag(feature) && IsInstalled(Feature.McpServer);
            case Feature.SpeechToText:
                return Flag(feature) && IsInstalled(Feature.AssistantTerminal);
            default:
                return false;
        }
    }

    static bool Flag(Feature feature)
    {
        Installed setup = InstalledBySetup();
        if (setup != null)
        {
            switch (feature)
            {
                case Feature.McpServer: return setup.mcpServer;
                case Feature.AssistantTerminal: return setup.assistantTerminal;
                default: return setup.speechToText;
            }
        }

        switch (feature)
        {
            // Part of the project: in the editor it is always there
            case Feature.McpServer: return Application.isEditor || Current.mcpServer.installed;
            case Feature.AssistantTerminal: return Current.assistantTerminal.installed;
            default: return Current.speechToText.installed;
        }
    }

    static Installed InstalledBySetup()
    {
        if (installedRead)
            return installed;

        installedRead = true;
        try
        {
            if (File.Exists(InstalledFilePath))
                installed = JsonUtility.FromJson<Installed>(File.ReadAllText(InstalledFilePath));
        }
        catch (Exception e)
        {
            Debug.LogWarning("Installed features could not be read from " + InstalledFilePath + ": " + e.Message);
        }
        return installed;
    }

    static OptionalFeatures Load()
    {
        try
        {
            if (File.Exists(FilePath))
                return JsonUtility.FromJson<OptionalFeatures>(File.ReadAllText(FilePath)) ?? new OptionalFeatures();

            OptionalFeatures defaults = new OptionalFeatures();
            defaults.Save();
            return defaults;
        }
        catch (Exception e)
        {
            Debug.LogWarning("Optional features could not be read from " + FilePath + ": " + e.Message);
            return new OptionalFeatures();
        }
    }

    // Reads the files again, e.g. after editing them while VRoxel runs
    public static void Reload()
    {
        current = null;
        installedRead = false;
        installed = null;
    }

    // Writes what VRoxel remembers between sessions, such as the assistant's last custom command
    public void Save()
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(FilePath));
            File.WriteAllText(FilePath, JsonUtility.ToJson(this, true));
        }
        catch (Exception e)
        {
            Debug.LogWarning("Optional features could not be written to " + FilePath + ": " + e.Message);
        }
    }
}
