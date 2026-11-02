using System.Linq;
using Newtonsoft.Json.Linq;
using UnityEngine;

// MCP commands for speech input (SpeechToText)
public static class SpeechCommands
{
    public static void Register(McpBridge bridge)
    {
        bridge.Register("set_microphone", SetMicrophone);
    }

    // Without a name: the microphones and the one in use. With one: switches to the microphone whose name it is or
    // uniquely contains ("default" for the system's default microphone)
    static object SetMicrophone(JObject args)
    {
        if (!OptionalFeatures.IsInstalled(OptionalFeatures.Feature.SpeechToText))
            throw new McpException("Speech to text is not installed (optionalFeatures.json)");

        string[] devices = Microphone.devices;
        string name = (string)args["microphone"];

        if (!string.IsNullOrEmpty(name))
        {
            string chosen;
            if (name.Trim().ToLowerInvariant() == "default")
                chosen = "";
            else
            {
                chosen = devices.FirstOrDefault(d => d == name);
                if (chosen == null)
                {
                    string[] matches = devices.Where(d => d.ToLowerInvariant().Contains(name.Trim().ToLowerInvariant())).ToArray();
                    if (matches.Length != 1)
                        throw new McpException((matches.Length == 0 ? "No microphone matches '" : "Several microphones match '") + name +
                            "': " + string.Join(", ", devices.Select(d => "'" + d + "'")));
                    chosen = matches[0];
                }
            }
            SpeechToText.SetMicrophone(chosen);
        }

        string current = SpeechToText.CurrentMicrophone;
        return new
        {
            microphone = string.IsNullOrEmpty(current) ? "default" : current,
            available = devices,
            missing = !string.IsNullOrEmpty(current) && !devices.Contains(current)
        };
    }
}
