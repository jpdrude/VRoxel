using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// Avatars spawned through the MCP bridge, addressed by short ids ("avatar_1") or by their colour ("teal") in tool calls.
// Every active avatar wears a different colour from AvatarColors, so a colour names exactly one avatar.
// Removed avatars stay registered but inactive, so undo can bring them back.
public static class AvatarRegistry
{
    static readonly Dictionary<string, GameObject> avatars = new Dictionary<string, GameObject>();
    static readonly Dictionary<GameObject, string> colors = new Dictionary<GameObject, string>();
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

    // The id of the active avatar with this id or colour name, or null
    public static string Resolve(string reference)
    {
        if (string.IsNullOrWhiteSpace(reference))
            return null;

        if (Get(reference.Trim()) != null)
            return reference.Trim();

        return All().Where(entry => string.Equals(GetColor(entry.Value), reference.Trim(), System.StringComparison.OrdinalIgnoreCase))
            .Select(entry => entry.Key).FirstOrDefault();
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

    public static string GetColor(GameObject avatar)
    {
        string color;
        return avatar != null && colors.TryGetValue(avatar, out color) ? color : null;
    }

    // Colours worn by active avatars, optionally ignoring one of them
    public static HashSet<string> UsedColors(GameObject except = null)
    {
        return new HashSet<string>(All().Where(entry => entry.Value != except).Select(entry => GetColor(entry.Value)).Where(color => color != null));
    }

    // Recolours the avatar from its current materials, so the colour sets' shaders and fade / stroke setup are kept
    public static void ApplyColor(GameObject avatar, string colorName)
    {
        AvatarColors.Entry color = AvatarColors.Find(colorName);
        CharacterModel model = avatar.GetComponent<CharacterModel>();
        if (color == null || model == null || model.CurrentColorSet == null) return;

        model.SetColor(AvatarColors.Build(model.CurrentColorSet, color));
        colors[avatar] = color.name;
    }

    // An avatar brought back by undo may share its colour with one spawned since; give it a free one
    public static void EnsureUniqueColor(GameObject avatar)
    {
        string color = GetColor(avatar);
        if (color != null && UsedColors(avatar).Contains(color))
            ApplyColor(avatar, AvatarColors.PickUnused(UsedColors(avatar)).name);
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

        foreach (GameObject avatar in colors.Keys.Where(avatar => avatar == null).ToList())
            colors.Remove(avatar);
    }
}
