using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// Avatars spawned through the MCP bridge, addressed by short ids ("avatar_1") in tool calls.
// Removed avatars stay registered but inactive, so undo can bring them back.
public static class AvatarRegistry
{
    static readonly Dictionary<string, GameObject> avatars = new Dictionary<string, GameObject>();
    static int nextId = 1;

    public static int Count { get { return All().Count(); } }

    public static string Register(GameObject avatar)
    {
        string id = "avatar_" + nextId++;
        avatars[id] = avatar;
        return id;
    }

    public static GameObject Get(string id)
    {
        GameObject avatar;
        if (id == null || !avatars.TryGetValue(id, out avatar) || avatar == null || !avatar.activeSelf)
            return null;

        return avatar;
    }

    public static IEnumerable<KeyValuePair<string, GameObject>> All()
    {
        Prune();
        List<KeyValuePair<string, GameObject>> active = new List<KeyValuePair<string, GameObject>>();
        foreach (KeyValuePair<string, GameObject> entry in avatars)
            if (entry.Value.activeSelf)
                active.Add(entry);

        return active;
    }

    // Avatars deleted elsewhere (menus, scene reload) leave destroyed entries behind
    static void Prune()
    {
        List<string> destroyed = new List<string>();
        foreach (KeyValuePair<string, GameObject> entry in avatars)
            if (entry.Value == null)
                destroyed.Add(entry.Key);

        foreach (string id in destroyed)
            avatars.Remove(id);
    }
}
