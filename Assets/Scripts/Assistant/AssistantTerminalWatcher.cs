using UnityEngine;

// Pins the assistant terminal on top in Play mode and in builds: a terminal started there once its window opens, and
// any open terminal again whenever it lost that (AssistantTerminal.KeepOnTop). A hidden object of its own, so it keeps
// running when the menu that started the terminal closes; it starts with the scene while the assistant is installed.
public class AssistantTerminalWatcher : MonoBehaviour
{
    static AssistantTerminalWatcher instance;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void StartWithScene()
    {
        if (AssistantTerminal.Installed)
            Watch();
    }

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
        AssistantTerminal.KeepOnTop();
    }
}
