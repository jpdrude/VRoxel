using System;
using System.IO;
using UnityEngine;

// Which optional features are installed and how they are set up, in one file the installer writes:
// optionalFeatures.json in VRoxel's data folder (Application.persistentDataPath, the same for the editor and builds).
// A missing file or feature means not installed; the file is then written with every feature off, as a template.
[Serializable]
public class OptionalFeatures
{
    public enum Feature { AssistantTerminal }

    public AssistantTerminal.Settings assistantTerminal = new AssistantTerminal.Settings();

    public const string FileName = "optionalFeatures.json";

    public static string FilePath { get { return Path.Combine(Application.persistentDataPath, FileName); } }

    static OptionalFeatures current;

    public static OptionalFeatures Current
    {
        get
        {
            if (current == null)
                current = Load();
            return current;
        }
    }

    public static bool IsInstalled(Feature feature)
    {
        switch (feature)
        {
            case Feature.AssistantTerminal:
                return Current.assistantTerminal.installed;
            default:
                return false;
        }
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

    // Reads the file again, e.g. after editing it while VRoxel runs
    public static void Reload()
    {
        current = null;
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
