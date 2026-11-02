using TMPro;
using UnityEngine;

// The assistant's panel in the Esc menu (add it to EscapeMenu's panels, so Resume closes it), opened from a button with
// OptionalFeatureButton (AssistantTerminal) so it only opens when the assistant is installed. One button per LLM tool
// calls its Open method; the commands that start the tools are in OptionalFeatures (assistantTerminal). The one custom
// button (OpenCustomFromField) starts whatever command is in the field, which holds the last one, so relaunching it is
// one click. In VR the terminal shows in AssistantTerminalMirror.
public class AssistantMenu : MonoBehaviour
{
    [Tooltip("The field for OpenCustomFromField, filled with the last custom command")]
    [SerializeField]
    TMP_InputField customCommandField;

    void OnEnable()
    {
        if (customCommandField != null && string.IsNullOrEmpty(customCommandField.text))
            customCommandField.text = AssistantTerminal.Current.customCommand;
    }

    public void OpenClaudeCode()
    {
        AssistantTerminal.Open(AssistantTerminal.Tool.ClaudeCode);
    }

    public void OpenCodex()
    {
        AssistantTerminal.Open(AssistantTerminal.Tool.Codex);
    }

    public void OpenGeminiCli()
    {
        AssistantTerminal.Open(AssistantTerminal.Tool.GeminiCli);
    }

    public void OpenCopilotCli()
    {
        AssistantTerminal.Open(AssistantTerminal.Tool.CopilotCli);
    }

    public void OpenOpenCode()
    {
        AssistantTerminal.Open(AssistantTerminal.Tool.OpenCode);
    }

    // Any other tool, by the command that starts it in a terminal (e.g. "aider" or "ollama run llama3")
    public void OpenCustom(string command)
    {
        AssistantTerminal.OpenCustom(command);
    }

    public void OpenCustomFromField()
    {
        if (customCommandField != null)
            OpenCustom(customCommandField.text);
    }
}
