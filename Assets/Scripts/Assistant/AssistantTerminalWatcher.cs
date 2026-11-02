using UnityEngine;

// Waits for a terminal AssistantTerminal started in Play mode to open its window, then pins it on top. A hidden object
// of its own, so it keeps running when the menu that started the terminal closes; it removes itself when done.
public class AssistantTerminalWatcher : MonoBehaviour
{
    static AssistantTerminalWatcher instance;

    public static void Watch()
    {
        if (instance != null)
            return;

        GameObject go = new GameObject("AssistantTerminalWatcher") { hideFlags = HideFlags.HideInHierarchy };
        DontDestroyOnLoad(go);
        instance = go.AddComponent<AssistantTerminalWatcher>();
    }

    void Update()
    {
        AssistantTerminal.CheckPendingLaunch();
        if (!AssistantTerminal.LaunchPending)
            Destroy(gameObject);
    }
}
