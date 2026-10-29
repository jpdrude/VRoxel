using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;

// MCP commands that save the avatars and their sequences as a named animation, and load them back.
// Files are JSON in %APPDATA%\VRoxel\Animations (next to the autosaves) and store clips by name,
// so they stay valid when the library is rebuilt or the clips are downloaded again.
public static class AnimationFileCommands
{
    public const string Extension = ".vrxlanim";
    const int FileVersion = 1;
    static readonly Regex validName = new Regex(@"^[\w\- ]{1,64}$");

    public static string Folder
    {
        get { return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "VRoxel", "Animations"); }
    }

    class AnimationFile
    {
        public int version = FileVersion;
        public string name;
        public string description;
        public string saved;
        public List<AvatarRecord> avatars = new List<AvatarRecord>();
    }

    class AvatarRecord
    {
        public string name;
        public string color;
        public AvatarDimensions dimensions;
        // How the body was set (weight, sex, ...), so later changes start from it; dimensions alone rebuild it
        public AvatarBodies.Parameters body;
        public float[] anchor;
        public float anchorYaw;
        public string end;
        public List<StepRecord> steps = new List<StepRecord>();
    }

    class StepRecord
    {
        public string animation;
        public float duration;
        public int loops = 1;
        public float fade = -1;
        public float speed = 1;
        public float[] walkTo;
        public float? face;
    }

    public static void Register(McpBridge bridge)
    {
        bridge.Register("save_animation", SaveAnimation);
        bridge.Register("list_saved_animations", ListSavedAnimations);
        bridge.Register("load_animation", LoadAnimation);
    }

    static object SaveAnimation(JObject args)
    {
        string name = ReadName(args["name"]);
        string path = PathFor(name);
        if (File.Exists(path) && !((bool?)args["overwrite"] ?? false))
            throw new McpException("An animation named " + name + " exists. Pass overwrite=true to replace it, or choose another name");

        // Default: every avatar in the scene
        JArray selection = args["avatars"] as JArray;
        List<KeyValuePair<string, GameObject>> avatars = selection == null || selection.Count == 0
            ? AvatarRegistry.All().ToList()
            : selection.Select(reference => AvatarCommands.ResolveId(reference))
                .Distinct()
                .Select(id => new KeyValuePair<string, GameObject>(id, AvatarCommands.GetAvatar(id))).ToList();
        if (avatars.Count == 0)
            throw new McpException("No avatars to save. Spawn some first");

        AnimationFile file = new AnimationFile
        {
            name = name,
            description = (string)args["description"],
            saved = DateTime.Now.ToString("yyyy-MM-dd HH:mm"),
            avatars = avatars.Select(entry => Record(entry.Value)).ToList()
        };

        Directory.CreateDirectory(Folder);
        File.WriteAllText(path, JsonConvert.SerializeObject(file, Formatting.Indented, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore }));

        return new { saved = name, path = path, avatars = file.avatars.Select(avatar => avatar.color).ToList() };
    }

    static object ListSavedAnimations(JObject args)
    {
        if (!Directory.Exists(Folder))
            return new List<object>();

        List<object> animations = new List<object>();
        foreach (string path in Directory.GetFiles(Folder, "*" + Extension).OrderBy(path => path))
        {
            AnimationFile file = TryRead(path);
            if (file == null)
            {
                animations.Add(new { name = Path.GetFileNameWithoutExtension(path), error = "unreadable file" });
                continue;
            }

            animations.Add(new
            {
                name = Path.GetFileNameWithoutExtension(path),
                description = file.description,
                saved = file.saved,
                avatars = file.avatars.Select(avatar => new { name = avatar.name, color = avatar.color, steps = avatar.steps.Count }).ToList()
            });
        }

        return animations;
    }

    static object LoadAnimation(JObject args)
    {
        string name = ReadName(args["name"]);
        string path = PathFor(name);
        if (!File.Exists(path))
            throw new McpException("No saved animation " + name + ". Use list_saved_animations");

        LoadResult result = LoadFile(path, (bool?)args["replace"] ?? true);
        return new { loaded = name, description = result.description, avatars = result.avatars, warnings = result.warnings };
    }

    public class LoadResult
    {
        public string description;
        public List<object> avatars = new List<object>();
        public List<string> warnings = new List<string>();
    }

    // Spawns the saved avatars and replays their sequences from the start, in sync; one MCP undo step reverts the load.
    // Also called by the character menu's animation import for .vrxlanim files. Throws McpException on failure.
    public static LoadResult LoadFile(string path, bool replace)
    {
        if (McpBridge.Bridge == null)
            throw new McpException("Loading saved animations needs an McpBridge in the scene");

        AnimationFile file = TryRead(path);
        if (file == null)
            throw new McpException(Path.GetFileName(path) + " could not be read");

        List<DoAction> actions = new List<DoAction>();
        List<string> warnings = new List<string>();

        if (replace)
            foreach (KeyValuePair<string, GameObject> entry in AvatarRegistry.All().ToList())
            {
                entry.Value.SetActive(false);
                actions.Add(new DoSpawnAvatar(DoAction.DoType.RemoveAvatar, entry.Value));
            }

        List<object> loaded = new List<object>();
        foreach (AvatarRecord record in file.avatars)
        {
            // Colours stay unique: a colour already worn in the scene is swapped for a free one
            HashSet<string> used = AvatarRegistry.UsedColors();
            AvatarColors.Entry color = AvatarColors.Find(record.color);
            if (color == null || used.Contains(color.name))
            {
                AvatarColors.Entry replacement = AvatarColors.PickUnused(used);
                warnings.Add((record.name ?? "avatar") + " was " + (record.color ?? "uncoloured") + ", now " + replacement.name);
                color = replacement;
            }

            Vector3 anchor = ToVector3(record.anchor);
            string id;
            GameObject avatar = AvatarCommands.Spawn(anchor, record.anchorYaw, record.dimensions, color, record.name, out id);
            AvatarBodies.Remember(avatar, record.body);
            actions.Add(new DoSpawnAvatar(DoAction.DoType.SpawnAvatar, avatar));

            SequenceState state = new SequenceState
            {
                anchorPosition = anchor,
                anchorYaw = record.anchorYaw,
                end = ParseEnd(record.end)
            };
            foreach (StepRecord step in record.steps)
            {
                SequenceStep sequenceStep = ToStep(step);
                if (sequenceStep != null)
                    state.steps.Add(sequenceStep);
                else
                    warnings.Add((record.name ?? id) + ": clip " + step.animation + " is not in the library, step skipped");
            }

            AvatarCommands.GetSequencer(avatar).Play(state);
            loaded.Add(AvatarCommands.DescribeAvatar(id, avatar));
        }

        McpHistory.Push(new DoMcpBatch(DoAction.DoType.LoadAnimation, actions));

        return new LoadResult { description = file.description, avatars = loaded, warnings = warnings };
    }

    static AvatarRecord Record(GameObject avatar)
    {
        SequenceState state = AvatarCommands.GetSequencer(avatar).GetState();
        CharacterModel model = avatar.GetComponent<CharacterModel>();

        return new AvatarRecord
        {
            name = avatar.name,
            color = AvatarRegistry.GetColor(avatar),
            dimensions = model != null ? model.Dimensions : null,
            body = AvatarBodies.Get(avatar),
            anchor = AvatarCommands.ToArray(state.anchorPosition),
            anchorYaw = AvatarCommands.Round(state.anchorYaw),
            end = state.end.ToString().ToLowerInvariant(),
            steps = state.steps.Select(step => new StepRecord
            {
                animation = step.clip.name,
                duration = step.duration,
                loops = step.loops,
                fade = step.fade,
                speed = step.speed,
                walkTo = step.hasTarget ? AvatarCommands.ToArray(step.target) : null,
                face = step.hasFacing ? step.facing : (float?)null
            }).ToList()
        };
    }

    static SequenceStep ToStep(StepRecord record)
    {
        AnimationLibraryEntry entry = AvatarCommands.Library().Get(record.animation ?? "");
        if (entry == null || entry.clip == null)
            return null;

        SequenceStep step = AvatarCommands.NewStep(entry);
        step.duration = record.duration;
        step.loops = Mathf.Max(1, record.loops);
        step.fade = record.fade;
        step.speed = record.speed;

        if (record.walkTo != null)
        {
            step.hasTarget = true;
            step.target = ToVector3(record.walkTo);
        }

        if (record.face.HasValue)
        {
            step.hasFacing = true;
            step.facing = record.face.Value;
        }

        return step;
    }

    // Names become file names, so only letters, digits, spaces, '-' and '_'
    static string ReadName(JToken token)
    {
        string name = ((string)token ?? "").Trim();
        if (!validName.IsMatch(name))
            throw new McpException("name must be 1-64 letters, digits, spaces, '-' or '_'");

        return name;
    }

    static string PathFor(string name)
    {
        return Path.Combine(Folder, name + Extension);
    }

    static AnimationFile TryRead(string path)
    {
        try
        {
            return JsonConvert.DeserializeObject<AnimationFile>(File.ReadAllText(path));
        }
        catch (Exception e)
        {
            Debug.LogWarning("AnimationFileCommands: can't read " + path + ": " + e.Message);
            return null;
        }
    }

    static SequenceEnd ParseEnd(string end)
    {
        switch (end)
        {
            case "loop": return SequenceEnd.Loop;
            case "return": return SequenceEnd.Return;
            default: return SequenceEnd.Hold;
        }
    }

    static Vector3 ToVector3(float[] values)
    {
        return values != null && values.Length == 3 ? new Vector3(values[0], values[1], values[2]) : Vector3.zero;
    }
}
