using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using UnityEngine;

// MCP commands for spawning Mixamo avatars and playing library clips.
// Every state change goes through McpHistory, separate from the user's UndoStack.
public static class AvatarCommands
{
    static McpBridge bridge;

    public static void Register(McpBridge mcpBridge)
    {
        bridge = mcpBridge;

        bridge.Register("list_animations", ListAnimations);
        bridge.Register("spawn_avatar", SpawnAvatar);
        bridge.Register("play_animation", PlayAnimation);
        bridge.Register("set_sequence", SetSequence);
        bridge.Register("plan_sequence", PlanSequence);
        bridge.Register("restart_sequences", RestartSequences);
        bridge.Register("list_avatars", ListAvatars);
        bridge.Register("remove_avatar", RemoveAvatar);
        bridge.Register("set_avatar_color", SetAvatarColor);
        bridge.Register("set_avatar_body", SetAvatarBody);
        bridge.Register("set_avatar_brush", SetAvatarBrush);
        bridge.Register("get_animation_motion", GetAnimationMotion);
        bridge.Register("get_ground_height", GetGroundHeight);
        bridge.Register("undo", Undo);
        bridge.Register("redo", Redo);
    }

    static object ListAnimations(JObject args)
    {
        string category = (string)args["category"];

        return Library().Entries
            .Where(entry => string.IsNullOrEmpty(category) || entry.category == category)
            .Select(entry => new
            {
                name = entry.name,
                category = entry.category,
                loop = entry.loop,
                length = Round(entry.length),
                speed = Round(entry.averageSpeed),
                vertical = entry.vertical,
                // Root displacement over one cycle, in the avatar's frame at clip start
                cycle_motion = DescribeMotion(entry.SampleMotion(entry.length)),
                description = entry.description
            })
            .ToList();
    }

    // Root motion trajectory of one clip cycle, for precise placement (e.g. how far sitting down moves back)
    static object GetAnimationMotion(JObject args)
    {
        string name = (string)args["animation"];
        AnimationLibraryEntry entry = GetEntry(name);

        float interval = Mathf.Max(0.05f, (float?)args["interval"] ?? 0.25f);
        List<object> samples = new List<object>();
        for (float t = 0; t < entry.length; t += interval)
            samples.Add(DescribeMotion(entry.SampleMotion(t)));
        samples.Add(DescribeMotion(entry.SampleMotion(entry.length)));

        return new { animation = name, loop = entry.loop, length = Round(entry.length), samples = samples };
    }

    // Floor heights at (x, z), top to bottom; several values mean several storeys
    static object GetGroundHeight(JObject args)
    {
        JToken x = args["x"];
        JToken z = args["z"];
        if (x == null || z == null)
            throw new McpException("x and z are required");

        return new { heights = GroundProbe.FindAll((float)x, (float)z).Select(Round).ToList() };
    }

    static object Undo(JObject args)
    {
        DoAction action = McpHistory.Undo();
        return new { undone = action != null ? action.Type.ToString() : null, undo_left = McpHistory.UndoCount, redo_left = McpHistory.RedoCount };
    }

    static object Redo(JObject args)
    {
        DoAction action = McpHistory.Redo();
        return new { redone = action != null ? action.Type.ToString() : null, undo_left = McpHistory.UndoCount, redo_left = McpHistory.RedoCount };
    }

    static object SpawnAvatar(JObject args)
    {
        if (bridge.MixamoAvatar == null)
            throw new McpException("McpBridge has no avatar prefab assigned");

        Vector3 position = ReadVector3(args["position"], Vector3.zero);
        float yaw = (float?)args["yaw"] ?? 0;
        AvatarBodies.Parameters body = AvatarBodies.Read(args, null);
        AvatarDimensions dimensions = AvatarBodies.Build(body, bridge.MixamoAvatar.GetComponent<CharacterModel>().NativeDimensions);

        AvatarColors.Entry color = ReadColor(args["color"], null) ?? AvatarColors.PickUnused(AvatarRegistry.UsedColors());

        string animation = (string)args["animation"];
        SequenceStep firstStep = string.IsNullOrEmpty(animation) ? null : NewStep(GetEntry(animation));

        // Snap to the floor near the requested height; the LLM finds storeys with get_ground_height.
        // on_floor false places the avatar exactly at y, e.g. on a floor that doesn't exist.
        if ((bool?)args["on_floor"] ?? true)
        {
            float? ground = GroundProbe.FindStandingHeight(position);
            if (ground.HasValue)
                position.y = ground.Value;
        }

        string id;
        GameObject avatar = Spawn(position, yaw, dimensions, color, (string)args["name"], out id);
        AvatarBodies.Remember(avatar, body);

        // The spawn pose becomes the anchor that sequences replay from
        AvatarSequencer sequencer = GetSequencer(avatar);
        if (firstStep != null)
        {
            SequenceState state = sequencer.GetState();
            state.steps.Add(firstStep);
            sequencer.Play(state);
        }

        McpHistory.Push(new DoSpawnAvatar(DoAction.DoType.SpawnAvatar, avatar));

        return DescribeAvatar(id, avatar);
    }

    // Instantiates and registers an avatar without recording history; null dimensions keep the model's own
    internal static GameObject Spawn(Vector3 position, float yaw, AvatarDimensions dimensions, AvatarColors.Entry color, string name, out string id)
    {
        if (bridge.MixamoAvatar == null)
            throw new McpException("McpBridge has no avatar prefab assigned");

        GameObject avatar = Object.Instantiate(bridge.MixamoAvatar, position, Quaternion.Euler(0, yaw, 0));
        CharacterModel model = avatar.GetComponent<CharacterModel>();

        // Copied, so InitializeAvatar can't alias the prefab's native dimensions
        if (dimensions == null)
            dimensions = model.NativeDimensions;
        dimensions = AvatarBodies.Copy(dimensions);

        model.InitializeAvatar(dimensions, AvatarColors.Build(GetColorSet(model, 0), color));

        id = AvatarRegistry.Register(avatar);
        AvatarRegistry.ApplyColor(avatar, color.name);
        avatar.name = name ?? id;

        return avatar;
    }

    // A one-step sequence from the avatar's current pose, crossfading from what it plays now
    static object PlayAnimation(JObject args)
    {
        string id = ResolveId(args["avatar_id"]);
        GameObject avatar = GetAvatar(id);

        SequenceStep step = NewStep(GetEntry((string)args["animation"]));
        step.speed = ReadSpeed(args["speed"]);

        SequenceState state = new SequenceState
        {
            steps = new List<SequenceStep> { step },
            anchorPosition = avatar.transform.position,
            anchorYaw = avatar.transform.eulerAngles.y
        };

        float fade = (float?)args["fade"] ?? avatar.GetComponent<MixamoAnimationPlayer>().CrossfadeDuration;
        ApplySequence(GetSequencer(avatar), state, fade);

        return DescribeAvatar(id, avatar);
    }

    static object SetSequence(JObject args)
    {
        string id = ResolveId(args["avatar_id"]);
        GameObject avatar = GetAvatar(id);

        float fade;
        SequenceState state = ReadSequenceState(args, avatar, out fade);
        ApplySequence(GetSequencer(avatar), state, fade);

        return new { avatar = DescribeAvatar(id, avatar), plan = DescribePlan(state, BodyScale(avatar)) };
    }

    // Dry run: where each step would start and end, without changing the scene
    static object PlanSequence(JObject args)
    {
        string id = ResolveId(args["avatar_id"]);
        GameObject avatar = id != null ? GetAvatar(id) : null;
        if (avatar == null && args["start_position"] == null)
            throw new McpException("Give avatar_id or start_position");

        float fade;
        return DescribePlan(ReadSequenceState(args, avatar, out fade), BodyScale(avatar));
    }

    // Steps, loop flag and anchor from set_sequence / plan_sequence arguments; avatar may be null for plans
    static SequenceState ReadSequenceState(JObject args, GameObject avatar, out float fade)
    {
        JArray stepArgs = args["steps"] as JArray;
        if (stepArgs == null || stepArgs.Count == 0)
            throw new McpException("steps must be a non-empty list");

        SequenceState state = new SequenceState { end = ReadEnd(args["end"]) };
        for (int i = 0; i < stepArgs.Count; i++)
            state.steps.Add(ReadStep(stepArgs[i] as JObject, i));

        AvatarSequencer sequencer = avatar != null ? GetSequencer(avatar) : null;
        Vector3 anchorPosition = sequencer != null ? sequencer.AnchorPosition : Vector3.zero;
        float anchorYaw = sequencer != null ? sequencer.AnchorYaw : 0;

        // "anchor" replays from the avatar's start pose, "current" continues from where it stands now
        string start = (string)args["start"] ?? "anchor";
        fade = 0;
        if (args["start_position"] != null || args["start_yaw"] != null)
        {
            state.anchorPosition = ReadVector3(args["start_position"], anchorPosition);
            state.anchorYaw = (float?)args["start_yaw"] ?? anchorYaw;
        }
        else if (start == "current")
        {
            if (avatar == null)
                throw new McpException("start 'current' needs an avatar_id");

            state.anchorPosition = avatar.transform.position;
            state.anchorYaw = avatar.transform.eulerAngles.y;
            fade = avatar.GetComponent<MixamoAnimationPlayer>().CrossfadeDuration;
        }
        else if (start == "anchor")
        {
            state.anchorPosition = anchorPosition;
            state.anchorYaw = anchorYaw;
        }
        else
            throw new McpException("start must be 'anchor' or 'current'");

        return state;
    }

    // Travel of non-climbing clips scales with the avatar's legs; plans without an avatar use the model's
    static float BodyScale(GameObject avatar)
    {
        return avatar != null ? avatar.GetComponent<MixamoAnimationPlayer>().BodyScale : 1;
    }

    static object DescribePlan(SequenceState state, float bodyScale)
    {
        SequencePlanner.Plan plan = SequencePlanner.Simulate(state, Library(), bodyScale);
        float gap = Vector3.Distance(plan.endPosition, state.anchorPosition);

        return new
        {
            duration = Round(plan.duration),
            steps = plan.steps.Select(step => new
            {
                animation = step.animation,
                start_time = Round(step.startTime),
                end_time = Round(step.endTime),
                start = ToArray(step.startPosition),
                start_yaw = Round(step.startYaw),
                end = ToArray(step.endPosition),
                end_yaw = Round(step.endYaw),
                // False after climbing until the feet reach a real floor; airborne avatars keep their height
                grounded = step.grounded
            }).ToList(),
            end_mode = state.end.ToString().ToLowerInvariant(),
            end_position = ToArray(plan.endPosition),
            end_yaw = Round(plan.endYaw),
            // How far the last step ends from the anchor; 'loop' and 'return' jump back by this much
            loop_gap = Round(gap),
            loop_gap_yaw = Round(Mathf.Abs(Mathf.DeltaAngle(plan.endYaw, state.anchorYaw)))
        };
    }

    static object DescribeMotion(MotionSample sample)
    {
        return new { time = Round(sample.time), right = Round(sample.right), forward = Round(sample.forward), up = Round(sample.up), turn = Round(sample.turn) };
    }

    // Replays sequences from their anchors, so several avatars start in sync
    static object RestartSequences(JObject args)
    {
        string id = ResolveId(args["avatar_id"]);
        IEnumerable<KeyValuePair<string, GameObject>> targets = id == null
            ? AvatarRegistry.All()
            : new[] { new KeyValuePair<string, GameObject>(id, GetAvatar(id)) };

        List<object> restarted = new List<object>();
        foreach (KeyValuePair<string, GameObject> entry in targets)
        {
            GetSequencer(entry.Value).Restart();
            restarted.Add(DescribeAvatar(entry.Key, entry.Value));
        }

        return restarted;
    }

    static object ListAvatars(JObject args)
    {
        return AvatarRegistry.All().Select(entry => DescribeAvatar(entry.Key, entry.Value)).ToList();
    }

    static object RemoveAvatar(JObject args)
    {
        string id = ResolveId(args["avatar_id"]);
        GameObject avatar = GetAvatar(id);

        // Deactivated rather than destroyed, so undo can restore it
        avatar.SetActive(false);
        McpHistory.Push(new DoSpawnAvatar(DoAction.DoType.RemoveAvatar, avatar));

        return new { removed = id };
    }

    static object SetAvatarColor(JObject args)
    {
        string id = ResolveId(args["avatar_id"]);
        GameObject avatar = GetAvatar(id);

        string oldColor = AvatarRegistry.GetColor(avatar);
        AvatarColors.Entry color = ReadColor(args["color"], avatar);
        if (color == null)
            throw new McpException("color is required. Free colours: " + string.Join(", ", FreeColors(avatar)));

        AvatarRegistry.ApplyColor(avatar, color.name);
        McpHistory.Push(new DoSetAvatarColor(DoAction.DoType.SetAvatarColor, avatar, oldColor, color.name));

        return DescribeAvatar(id, avatar);
    }

    // Changes height, weight, sex, muscularity or measurements; arguments left out keep their current value
    static object SetAvatarBody(JObject args)
    {
        string id = ResolveId(args["avatar_id"]);
        GameObject avatar = GetAvatar(id);
        CharacterModel model = avatar.GetComponent<CharacterModel>();

        AvatarBodies.Parameters oldBody = AvatarBodies.Get(avatar) ?? new AvatarBodies.Parameters();
        AvatarDimensions oldDimensions = AvatarBodies.Copy(model.Dimensions);
        AvatarBodies.Parameters body = AvatarBodies.Read(args, oldBody);
        AvatarDimensions dimensions = AvatarBodies.Build(body, model.NativeDimensions);

        ApplyBody(avatar, dimensions, body);
        McpHistory.Push(new DoSetAvatarBody(DoAction.DoType.SetAvatarBody, avatar, oldDimensions, oldBody, AvatarBodies.Copy(dimensions), body.Copy()));

        return DescribeAvatar(id, avatar);
    }

    // Rebuilds the body; its colour and sequence stay
    internal static void ApplyBody(GameObject avatar, AvatarDimensions dimensions, AvatarBodies.Parameters body)
    {
        CharacterModel model = avatar.GetComponent<CharacterModel>();
        model.InitializeAvatar(AvatarBodies.Copy(dimensions), model.CurrentColorSet);
        AvatarRegistry.ApplyColor(avatar, AvatarRegistry.GetColor(avatar));
        AvatarBodies.Remember(avatar, body);
    }

    // The avatar's body adds or removes blocks wherever it moves (CharacterSpawner), or stops
    static object SetAvatarBrush(JObject args)
    {
        string id = ResolveId(args["avatar_id"]);
        GameObject avatar = GetAvatar(id);

        string oldMode = BrushMode(avatar);
        float oldSize = BrushSize(avatar);

        string mode = ((string)args["mode"] ?? oldMode).Trim().ToLowerInvariant();
        if (mode != "add" && mode != "remove" && mode != "off")
            throw new McpException("mode must be 'add', 'remove' or 'off'");

        float size = (float?)args["size"] ?? oldSize;
        if (size < 0.25f || size > 5)
            throw new McpException("size must be between 0.25 and 5 (1 is the body itself)");

        ApplyBrush(avatar, mode, size);
        McpHistory.Push(new DoSetAvatarBrush(DoAction.DoType.SetAvatarBrush, avatar, oldMode, oldSize, mode, size));

        return DescribeAvatar(id, avatar);
    }

    internal static void ApplyBrush(GameObject avatar, string mode, float size)
    {
        CharacterSpawner spawner = avatar.GetComponent<CharacterSpawner>();
        if (spawner == null)
            throw new McpException("This avatar has no CharacterSpawner, so it can't brush blocks");

        spawner.BrushSize = size;

        if (mode == "add")
            spawner.AddCharacterBrush();
        else if (mode == "remove")
            spawner.RemoveCharacterBrush();
        else
            spawner.DisableCharacterBrush();
    }

    static float BrushSize(GameObject avatar)
    {
        CharacterSpawner spawner = avatar.GetComponent<CharacterSpawner>();
        return spawner != null ? spawner.BrushSize : 1;
    }

    static string BrushMode(GameObject avatar)
    {
        CharacterSpawner spawner = avatar.GetComponent<CharacterSpawner>();
        return spawner == null || !spawner.Brushing ? "off" : spawner.AddBlocks ? "add" : "remove";
    }

    // The body as set and as built: all lengths in cm, so a change like "longer legs" can start from the current values
    static object DescribeBody(GameObject avatar)
    {
        CharacterModel model = avatar.GetComponent<CharacterModel>();
        AvatarDimensions d = model.Dimensions;
        AvatarBodies.Parameters body = AvatarBodies.Get(avatar) ?? new AvatarBodies.Parameters();
        float? bmi = AvatarBodies.Bmi(body, d);

        return new
        {
            height_cm = Round(d.bodyHeight),
            weight_kg = body.weightKg,
            bmi = bmi.HasValue ? Round(bmi.Value) : (float?)null,
            sex = body.sex.HasValue ? Round(body.sex.Value) : (float?)null,
            muscularity = Round(body.muscularity),
            body_file = body.bodyFile,
            measurements = new
            {
                footLength = Round(d.footLength),
                shoulderHeight = Round(d.shoulderHeight),
                shoulderWidth = Round(d.shoulderWidth),
                elbowSpan = Round(d.elbowSpan),
                wristSpan = Round(d.wristSpan),
                armSpan = Round(d.armSpan),
                hipHeight = Round(d.hipHeight),
                hipWidth = Round(d.hipWidth),
                kneeHeight = Round(d.kneeHeight),
                ankleHeight = Round(d.ankleHeight),
                extraShowSoleThickness = Round(d.extraShowSoleThickness)
            },
            // Measurements given explicitly; they stay when height, weight, sex or muscularity change
            fixed_measurements = body.measurements.Keys.ToList()
        };
    }

    // A palette colour no other avatar wears, so colour names stay unambiguous; null if none was given
    static AvatarColors.Entry ReadColor(JToken token, GameObject avatar)
    {
        string name = (string)token;
        if (string.IsNullOrWhiteSpace(name))
            return null;

        AvatarColors.Entry color = AvatarColors.Find(name);
        if (color == null)
            throw new McpException("Unknown color " + name + ". Colours: " + string.Join(", ", AvatarColors.Names));

        if (AvatarRegistry.UsedColors(avatar).Contains(color.name))
            throw new McpException("Another avatar is already " + color.name + ". Free colours: " + string.Join(", ", FreeColors(avatar)));

        return color;
    }

    static IEnumerable<string> FreeColors(GameObject avatar)
    {
        HashSet<string> used = AvatarRegistry.UsedColors(avatar);
        return AvatarColors.Names.Where(name => !used.Contains(name));
    }

    static void ApplySequence(AvatarSequencer sequencer, SequenceState state, float fade)
    {
        SequenceState oldState = sequencer.GetState();
        sequencer.Play(state, fade);
        McpHistory.Push(new DoSetSequence(DoAction.DoType.SetSequence, sequencer, oldState, state));
    }

    static SequenceStep ReadStep(JObject stepArgs, int index)
    {
        if (stepArgs == null)
            throw new McpException("steps[" + index + "] must be an object");

        string animation = (string)stepArgs["animation"];
        AnimationLibraryEntry entry = Library().Get(animation ?? "");
        if (entry == null || entry.clip == null)
            throw new McpException("steps[" + index + "]: unknown animation " + animation + ". Use list_animations for valid names");

        SequenceStep step = NewStep(entry);
        step.duration = Mathf.Max(0, (float?)stepArgs["duration"] ?? 0);
        step.loops = Mathf.Max(1, (int?)stepArgs["loops"] ?? 1);
        step.fade = (float?)stepArgs["fade"] ?? -1;
        step.speed = ReadSpeed(stepArgs["speed"]);

        if (stepArgs["walk_to"] != null)
        {
            // Steering only works for clips that travel
            if (entry.averageSpeed < 0.05f)
                throw new McpException("steps[" + index + "]: " + animation + " stays on the spot and can't walk_to. Use a clip with speed > 0");

            step.hasTarget = true;
            step.target = ReadVector3(stepArgs["walk_to"], Vector3.zero);
        }

        if (stepArgs["face"] != null)
        {
            step.hasFacing = true;
            step.facing = (float)stepArgs["face"];
        }

        return step;
    }

    internal static object DescribeAvatar(string id, GameObject avatar)
    {
        MixamoAnimationPlayer player = avatar.GetComponent<MixamoAnimationPlayer>();
        AvatarSequencer sequencer = GetSequencer(avatar);
        SequenceState state = sequencer.GetState();
        Animator animator = avatar.GetComponent<Animator>();

        // The body faces away from its hip line, which can differ from the root's yaw while a clip turns the body
        Vector3 hipLine = animator.GetBoneTransform(HumanBodyBones.RightUpperLeg).position - animator.GetBoneTransform(HumanBodyBones.LeftUpperLeg).position;
        Vector3 bodyForward = Vector3.Cross(Vector3.ProjectOnPlane(hipLine, Vector3.up), Vector3.up);

        return new
        {
            id = id,
            color = AvatarRegistry.GetColor(avatar),
            name = avatar.name,
            position = ToArray(avatar.transform.position),
            // Where the body is, e.g. on a bed while the avatar's root stays on the floor beside it
            body_position = ToArray(animator.GetBoneTransform(HumanBodyBones.Hips).position),
            body_yaw = Round(Mathf.Atan2(bodyForward.x, bodyForward.z) * Mathf.Rad2Deg),
            body = DescribeBody(avatar),
            // add / remove: its body adds or removes blocks wherever it moves
            brush = BrushMode(avatar),
            brush_size = Round(BrushSize(avatar)),
            // Leg length relative to the 181 cm model: steps, and so cycle_motion and speed of non-climbing clips, scale by it
            body_scale = Round(player.BodyScale),
            yaw = Round(avatar.transform.eulerAngles.y),
            animation = player.CurrentClip != null ? player.CurrentClip.name : null,
            sequence = state.steps.Select(step => step.clip.name).ToList(),
            sequence_step = sequencer.IsPlaying ? sequencer.StepIndex : (int?)null,
            end = state.end.ToString().ToLowerInvariant(),
            grounded = sequencer.Grounded,
            anchor = ToArray(state.anchorPosition),
            anchor_yaw = Round(state.anchorYaw)
        };
    }

    internal static AnimationLibrary Library()
    {
        if (bridge.AnimationLibrary == null)
            throw new McpException("McpBridge has no animation library assigned");

        return bridge.AnimationLibrary;
    }

    internal static AnimationLibraryEntry GetEntry(string name)
    {
        AnimationLibraryEntry entry = Library().Get(name ?? "");
        if (entry == null || entry.clip == null)
            throw new McpException("Unknown animation: " + name + ". Use list_animations for valid names");

        return entry;
    }

    internal static SequenceStep NewStep(AnimationLibraryEntry entry)
    {
        return new SequenceStep { clip = entry.clip, vertical = entry.vertical, travelSpeed = entry.averageSpeed };
    }

    static SequenceEnd ReadEnd(JToken token)
    {
        switch ((string)token ?? "hold")
        {
            case "hold": return SequenceEnd.Hold;
            case "loop": return SequenceEnd.Loop;
            case "return": return SequenceEnd.Return;
            default: throw new McpException("end must be 'hold', 'loop' or 'return'");
        }
    }

    // avatar_id accepts the id ("avatar_1") or the avatar's colour ("teal"); null when not given
    internal static string ResolveId(JToken token)
    {
        string reference = (string)token;
        if (string.IsNullOrWhiteSpace(reference))
            return null;

        string id = AvatarRegistry.Resolve(reference);
        if (id == null)
            throw new McpException("No avatar " + reference + ". Use list_avatars for ids and colours");

        return id;
    }

    internal static GameObject GetAvatar(string id)
    {
        GameObject avatar = AvatarRegistry.Get(id);
        if (avatar == null)
            throw new McpException("Unknown avatar_id: " + id + ". Use list_avatars for valid ids");

        return avatar;
    }

    internal static AvatarSequencer GetSequencer(GameObject avatar)
    {
        AvatarSequencer sequencer = avatar.GetComponent<AvatarSequencer>();
        return sequencer != null ? sequencer : avatar.AddComponent<AvatarSequencer>();
    }

    static CharacterColorSet GetColorSet(CharacterModel model, int index)
    {
        List<CharacterColorSet> colorSets = CharacterEditor.ColorSets;
        if (colorSets != null && colorSets.Count > 0)
            return colorSets[Mathf.Clamp(index, 0, colorSets.Count - 1)];

        // Scenes without a CharacterEditor: keep the prefab's materials
        CharacterColorSet colorSet = new CharacterColorSet();
        if (model.Body.Count > 0)
            colorSet.mainColor = model.Body[0].sharedMaterial;
        if (model.Joints.Count > 0)
            colorSet.secondaryColor = model.Joints[0].sharedMaterial;

        LineRenderer[] strokes = model.GetComponentsInChildren<LineRenderer>(true);
        if (strokes.Length > 0)
            colorSet.strokeColor = strokes[0].sharedMaterial;

        return colorSet;
    }

    static float ReadSpeed(JToken token)
    {
        float speed = (float?)token ?? 1;
        if (speed < 0 || speed > 10)
            throw new McpException("speed must be between 0 and 10");

        return speed;
    }

    static Vector3 ReadVector3(JToken token, Vector3 fallback)
    {
        if (token == null || token.Type == JTokenType.Null)
            return fallback;

        JArray array = token as JArray;
        if (array == null || array.Count != 3)
            throw new McpException("Expected [x, y, z], got " + token.ToString(Newtonsoft.Json.Formatting.None));

        return new Vector3((float)array[0], (float)array[1], (float)array[2]);
    }

    internal static float[] ToArray(Vector3 v)
    {
        return new[] { Round(v.x), Round(v.y), Round(v.z) };
    }

    internal static float Round(float value)
    {
        return Mathf.Round(value * 1000) / 1000;
    }
}
