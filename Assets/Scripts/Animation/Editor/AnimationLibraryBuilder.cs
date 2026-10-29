using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

// Keeps the Mixamo AnimationLibrary in sync with the clip folder
public class AnimationLibraryBuilder : AssetPostprocessor
{
    const string ClipFolder = "Assets/Animation/Mixamo/Clips/";
    public const string LibraryPath = "Assets/Animation/Mixamo/MixamoLibrary.asset";
    // Root motion is baked on the avatar that plays the clips, so distances match runtime
    public const string AvatarPath = "Assets/Animation/Characters/MixamoAvatar.prefab";
    // Fine enough to drive the avatar's root from at runtime (MixamoAnimationPlayer)
    const float SampleInterval = 1f / 30;
    // Root height change that marks a clip as climbing
    const float VerticalThreshold = 0.05f;
    // Mixamo stair descents keep the feet on a flat floor and get StairDescentFix
    static readonly Regex stairDescentPattern = new Regex(@"^vert_.*(stairsDown|descend)", RegexOptions.IgnoreCase);
    const float StairFixInterval = 1f / 30;
    static readonly string GeneratedFolder = MixamoManifest.GeneratedFolder.TrimEnd('/');
    // Transitions from one state to another, e.g. floor_standToKneel; loops and start/stop aren't reversible
    static readonly Regex towardPattern = new Regex(@"^([a-z]+)_([a-z][a-zA-Z]*)To([A-Z][a-zA-Z]*)(_once)?$");
    // Pose clips whose body must sit where the transition into or out of them leaves it: Mixamo's lying idle lies on the
    // floor while its lie-down ends on a bed. The idle's body is moved to the transition's end (or start) pose.
    static readonly (string idle, string transition, bool atEnd)[] poseMatches =
    {
        ("bed_lyingIdle", "bed_sitToLie", true),
        ("floor_kneelIdle", "floor_standToKneel", true),
        ("floor_sitGroundIdle", "floor_sitGroundToStand", false),
        // Mixamo's climbing loop climbs 1 m to the side of where its ladder start ends
        ("vert_ladderClimb_loop", "vert_ladderStart", true),
        ("vert_ladderClimbDown_loop", "vert_ladderEnd", false),
    };
    // Clips whose body turns while the avatar should keep facing the same way: Mixamo's ladder start turns the body 180°
    // onto a ladder that its top-out climbs facing the other way. The turn is undone frame by frame (pose yaw), keeping
    // the facing of the clip's standing end: the start, or the end for the generated step off the ladder.
    static readonly (string clip, bool keepEnd)[] keepFacing = { ("vert_ladderStart", false), ("vert_ladderEnd", true) };
    // Clips whose body must face the way a neighbouring clip does at its end (or start): the climbing loops face like
    // the ladder start and step off they blend with
    static readonly (string clip, string other, bool atEnd)[] facingMatches =
    {
        ("vert_ladderClimb_loop", "vert_ladderStart", true),
        ("vert_ladderClimbDown_loop", "vert_ladderEnd", false),
    };
    // Facing differences below this are left alone
    const float FacingMatchThreshold = 5;
    // Clips whose sideways root motion is dropped: Mixamo's ladder start steps 1 m sideways while it turns, which
    // keepFacing undoes, so the avatar steps straight onto a ladder in front of it
    static readonly string[] noSideways = { "vert_ladderStart", "vert_ladderEnd" };
    // Clips resting on furniture, whose height is the same for every avatar: weight at the clip's start and end, first
    // matching name wins. Sitting down moves onto the chair, standing up off it; chair and bed poses stay on it.
    static readonly (Regex name, float start, float end)[] furniturePoses =
    {
        (new Regex(@"^sit_standToSit"), 0, 1),
        (new Regex(@"^sit_sitToStand"), 1, 0),
        (new Regex(@"^(sit|bed)_"), 1, 1),
    };
    // Clips whose body stands or sits away from the root, so crossfading into them slides it over the floor; their body
    // is moved horizontally to the reference clip's (Mixamo's bartender stands 45 cm behind its root). Transitions are
    // matched at their end, phased in over the clip (sitting down ends 15 cm behind where the sitting loops sit).
    static readonly (string clip, string reference, bool atEnd)[] alignMatches =
    {
        ("activity_bartending_loop", "idle_neutral", false),
        ("gesture_cheering", "idle_neutral", false),
        ("interact_leanRailing_loop", "idle_neutral", false),
        ("sit_idle", "sit_talking_loop", false),
        ("sit_writing_loop", "sit_talking_loop", false),
        ("sit_standToSit", "sit_talking_loop", true),
        ("sit_sitToStand", "idle_neutral", true),
    };
    // Clip families that stand lower than every other clip: Mixamo's floor sitting stands up 7 cm low, and its idle and
    // generated sit-down are matched to it. The whole family is raised until the clip's standing end meets the reference.
    static readonly (string clip, string reference, string[] family)[] heightMatches =
    {
        ("floor_sitGroundToStand", "idle_neutral", new[] { "floor_sitGroundToStand", "floor_standToSitGround", "floor_sitGroundIdle" }),
    };
    // Pose differences below this are left alone
    const float PoseMatchThreshold = 0.02f;
    // Actions whose reverse is the other one of the pair; the only way loops get reversed
    static readonly string[][] oppositeActions =
    {
        new[] { "lieDown", "getUp" }, new[] { "pickUp", "putDown" }, new[] { "stepDown", "stepUp" },
        new[] { "ladderStart", "ladderEnd" }, new[] { "ladderTopOut", "ladderTopIn" }, new[] { "ladderClimb_loop", "ladderClimbDown_loop" },
    };

    [InitializeOnLoadMethod]
    static void BuildIfMissing()
    {
        EditorApplication.delayCall += () =>
        {
            if (AssetDatabase.LoadAssetAtPath<AnimationLibrary>(LibraryPath) == null)
                Rebuild();
        };
    }

    static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
    {
        bool clipsChanged = importedAssets.Concat(deletedAssets).Concat(movedAssets).Concat(movedFromAssetPaths)
            .Any(path => path.StartsWith(ClipFolder) && path.EndsWith(".fbx", System.StringComparison.OrdinalIgnoreCase));

        // Deferred, so the library asset isn't created in the middle of an import
        if (clipsChanged)
            EditorApplication.delayCall += Rebuild;
    }

    [MenuItem("VRoxel/Rebuild Mixamo Animation Library")]
    public static void Rebuild()
    {
        AnimationLibrary library = AssetDatabase.LoadAssetAtPath<AnimationLibrary>(LibraryPath);
        if (library == null)
        {
            library = ScriptableObject.CreateInstance<AnimationLibrary>();
            AssetDatabase.CreateAsset(library, LibraryPath);
        }

        // Descriptions are written by hand, so keep them for clips that still exist
        Dictionary<string, string> descriptions = library.Entries
            .Where(entry => entry != null && !string.IsNullOrEmpty(entry.name))
            .GroupBy(entry => entry.name)
            .ToDictionary(group => group.Key, group => group.First().description);

        Dictionary<string, Vector3[]> reversedPoseOffsets;
        HashSet<string> generated = GenerateReversedClips(out reversedPoseOffsets);

        List<AnimationLibraryEntry> entries = new List<AnimationLibraryEntry>();
        GameObject avatarPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(AvatarPath);

        string[] folders = AssetDatabase.IsValidFolder(GeneratedFolder)
            ? new[] { ClipFolder.TrimEnd('/'), GeneratedFolder }
            : new[] { ClipFolder.TrimEnd('/') };
        foreach (string guid in AssetDatabase.FindAssets("t:AnimationClip", folders))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            string name = Path.GetFileNameWithoutExtension(path);

            // Skip the "__preview__" clips Unity adds to model files
            AnimationClip clip = AssetDatabase.LoadAllAssetsAtPath(path).OfType<AnimationClip>()
                .FirstOrDefault(c => !c.name.StartsWith("__preview__"));
            if (clip == null) continue;

            string description;
            descriptions.TryGetValue(name, out description);

            entries.Add(new AnimationLibraryEntry
            {
                name = name,
                category = name.Split('_')[0],
                clip = clip,
                loop = clip.isLooping,
                length = clip.length,
                description = description,
                motion = avatarPrefab != null ? BakeMotion(avatarPrefab, clip) : new List<MotionSample>()
            });
        }

        // Before the pose matches, which meet reversed clips including these and override them for matched loops
        foreach (AnimationLibraryEntry entry in entries)
        {
            Vector3[] offsets;
            if (reversedPoseOffsets.TryGetValue(entry.name, out offsets))
            {
                entry.poseOffsetStart = offsets[0];
                entry.poseOffsetEnd = offsets[1];
            }
        }

        if (avatarPrefab != null)
        {
            foreach (AnimationLibraryEntry entry in entries.Where(entry => stairDescentPattern.IsMatch(entry.name)))
                ApplyStairFix(avatarPrefab, entry);

            MatchPoses(avatarPrefab, entries);
            MatchFacing(avatarPrefab, entries);
        }

        foreach (AnimationLibraryEntry entry in entries.Where(entry => noSideways.Contains(entry.name) && entry.motion.Count > 0))
        {
            Debug.Log("AnimationLibraryBuilder: " + entry.name + " moved " + entry.motion[entry.motion.Count - 1].right.ToString("+0.00;-0.00") +
                " m sideways; dropped, so it keeps to a straight line");
            for (int i = 0; i < entry.motion.Count; i++)
            {
                MotionSample sample = entry.motion[i];
                sample.right = 0;
                entry.motion[i] = sample;
            }
        }

        foreach (AnimationLibraryEntry entry in entries)
        {
            entry.vertical = entry.motion.Any(sample => Mathf.Abs(sample.up) > VerticalThreshold);
            entry.averageSpeed = TravelSpeed(entry);

            var furniture = furniturePoses.FirstOrDefault(pose => pose.name.IsMatch(entry.name));
            if (furniture.name != null)
            {
                entry.furnitureStart = furniture.start;
                entry.furnitureEnd = furniture.end;
            }
        }

        // FindAssets returns a clip once per model, but stay safe against duplicates
        entries = entries.GroupBy(entry => entry.name).Select(group => group.First()).OrderBy(entry => entry.name).ToList();

        if (avatarPrefab == null)
            Debug.LogWarning("AnimationLibraryBuilder: no avatar at " + AvatarPath + ", root motion not baked");

        library.SetEntries(entries);
        EditorUtility.SetDirty(library);
        AssetDatabase.SaveAssets();

        CheckManifest(entries.Select(entry => entry.name).Where(name => !generated.Contains(name)));
    }

    // Mixamo has many transitions in one direction only (sitting down on the floor, but not getting up from kneeling).
    // Every transition whose opposite isn't downloaded gets the clip played backwards, written to the Generated folder,
    // so avatars can always leave the state a transition brings them into.
    static HashSet<string> GenerateReversedClips(out Dictionary<string, Vector3[]> poseOffsets)
    {
        poseOffsets = new Dictionary<string, Vector3[]>();
        HashSet<string> local = new HashSet<string>(AssetDatabase.FindAssets("t:AnimationClip", new[] { ClipFolder.TrimEnd('/') })
            .Select(guid => Path.GetFileNameWithoutExtension(AssetDatabase.GUIDToAssetPath(guid))));
        HashSet<string> generated = new HashSet<string>();
        GameObject avatarPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(AvatarPath);

        foreach (string name in local.OrderBy(name => name))
        {
            string reverseName = ReverseName(name);
            if (reverseName == null || local.Contains(reverseName) || generated.Contains(reverseName)) continue;

            AnimationClip source = AssetDatabase.LoadAllAssetsAtPath(ClipFolder + name + ".fbx").OfType<AnimationClip>()
                .FirstOrDefault(c => !c.name.StartsWith("__preview__"));
            // Loops only reverse as named opposite actions (climbing up / down); a reversed walk is a different gait
            if (source == null || (source.isLooping && !oppositeActions.Any(pair => pair.Any(action => name.Contains("_" + action))))) continue;

            AnimationClip reversed = Reverse(source, reverseName);
            if (reversed == null) continue;

            if (avatarPrefab != null)
            {
                MatchReversedHeight(avatarPrefab, source, reversed);
                poseOffsets[reverseName] = ReversedPoseOffsets(avatarPrefab, source, reversed);
            }

            // Overwriting in place keeps the asset's GUID, so references to it survive a rebuild
            string path = GeneratedFolder + "/" + reverseName + ".anim";
            AnimationClip existing = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
            if (existing != null)
            {
                EditorUtility.CopySerialized(reversed, existing);
                Object.DestroyImmediate(reversed);
            }
            else
            {
                if (!AssetDatabase.IsValidFolder(GeneratedFolder))
                    AssetDatabase.CreateFolder(Path.GetDirectoryName(GeneratedFolder).Replace('\\', '/'), Path.GetFileName(GeneratedFolder));
                AssetDatabase.CreateAsset(reversed, path);
            }

            generated.Add(reverseName);
        }

        // Drop generated clips whose source is gone or whose opposite has been downloaded since
        if (AssetDatabase.IsValidFolder(GeneratedFolder))
            foreach (string guid in AssetDatabase.FindAssets("t:AnimationClip", new[] { GeneratedFolder }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (!generated.Contains(Path.GetFileNameWithoutExtension(path)))
                    AssetDatabase.DeleteAsset(path);
            }

        AssetDatabase.SaveAssets();
        if (generated.Count > 0)
            Debug.Log("AnimationLibraryBuilder: " + generated.Count + " reversed transitions generated: " + string.Join(", ", generated.OrderBy(name => name)));
        return generated;
    }

    // Unity places a humanoid clip's body relative to the feet at its first frame. A reversed lie-down starts lying with
    // the feet up on the bed, so the whole clip would drop by the bed height. The clip's Y offset (level) moves its
    // start back to the source's end pose.
    static void MatchReversedHeight(GameObject avatarPrefab, AnimationClip source, AnimationClip reversed)
    {
        float target = HipsLocal(avatarPrefab, source, source.length - 1e-3f).y;
        float error = target - HipsLocal(avatarPrefab, reversed, 0).y;
        if (Mathf.Abs(error) <= PoseMatchThreshold) return;

        AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(reversed);
        float originalLevel = settings.level;
        settings.level = originalLevel + error;
        AnimationUtility.SetAnimationClipSettings(reversed, settings);

        // The sign of level isn't documented; keep whichever direction closes the gap
        float remaining = target - HipsLocal(avatarPrefab, reversed, 0).y;
        if (Mathf.Abs(remaining) > Mathf.Abs(error))
        {
            settings.level = originalLevel - error;
            AnimationUtility.SetAnimationClipSettings(reversed, settings);
            remaining = target - HipsLocal(avatarPrefab, reversed, 0).y;
        }

        Debug.Log("AnimationLibraryBuilder: " + reversed.name + " started " + Mathf.Abs(error).ToString("0.00") + " m " + (error > 0 ? "below" : "above") +
            " the end of " + source.name + "; Y offset corrected, " + Mathf.Abs(remaining).ToString("0.000") + " m left");
    }

    // A reversed clip's body should be where its source's is at the mirrored time, but Unity places it about the
    // clip's own travel off the root. The difference at both ends becomes the clip's pose offset.
    static Vector3[] ReversedPoseOffsets(GameObject avatarPrefab, AnimationClip source, AnimationClip reversed)
    {
        float end = source.length - 1e-3f;
        Vector3 start = HipsLocal(avatarPrefab, source, end) - HipsLocal(avatarPrefab, reversed, 0);
        Vector3 finish = HipsLocal(avatarPrefab, source, 0) - HipsLocal(avatarPrefab, reversed, end);

        if (start.magnitude <= PoseMatchThreshold && finish.magnitude <= PoseMatchThreshold)
            return new[] { Vector3.zero, Vector3.zero };

        Debug.Log("AnimationLibraryBuilder: " + reversed.name + " body moved by " + FormatOffset(start) + " at its start and " +
            FormatOffset(finish) + " at its end to match " + source.name);
        return new[] { start, finish };
    }

    static string FormatOffset(Vector3 offset)
    {
        return "(right " + offset.x.ToString("+0.00;-0.00") + ", up " + offset.y.ToString("+0.00;-0.00") + ", forward " + offset.z.ToString("+0.00;-0.00") + ") m";
    }

    // The opposite transition's name, or null if the clip isn't a reversible transition:
    // category_aToB -> category_bToA (sit_standToSit -> sit_sitToStand), and lieDown <-> getUp, pickUp <-> putDown
    static string ReverseName(string name)
    {
        Match toward = towardPattern.Match(name);
        if (toward.Success)
            return toward.Groups[1].Value + "_" + LowerFirst(toward.Groups[3].Value) + "To" + UpperFirst(toward.Groups[2].Value) + toward.Groups[4].Value;

        foreach (string[] pair in oppositeActions)
            for (int i = 0; i < 2; i++)
                if (name.Contains("_" + pair[i]))
                    return name.Replace("_" + pair[i], "_" + pair[1 - i]);

        return null;
    }

    static string LowerFirst(string text) { return char.ToLowerInvariant(text[0]) + text.Substring(1); }
    static string UpperFirst(string text) { return char.ToUpperInvariant(text[0]) + text.Substring(1); }

    // Copies the clip's humanoid curves mirrored in time (key at t moves to length - t, tangents swap and flip)
    // and its import settings, so looping and root motion behave like the source
    static AnimationClip Reverse(AnimationClip source, string name)
    {
        EditorCurveBinding[] bindings = AnimationUtility.GetCurveBindings(source);
        if (bindings.Length == 0)
        {
            Debug.LogWarning("AnimationLibraryBuilder: " + source.name + " has no readable curves, can't generate " + name);
            return null;
        }

        AnimationClip reversed = new AnimationClip { name = name, frameRate = source.frameRate };
        float length = source.length;

        foreach (EditorCurveBinding binding in bindings)
        {
            Keyframe[] keys = AnimationUtility.GetEditorCurve(source, binding).keys
                .Select(key => new Keyframe(length - key.time, key.value, -key.outTangent, -key.inTangent))
                .Reverse()
                .ToArray();
            AnimationUtility.SetEditorCurve(reversed, binding, new AnimationCurve(keys));
        }

        AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(source);
        settings.startTime = 0;
        settings.stopTime = length;
        AnimationUtility.SetAnimationClipSettings(reversed, settings);

        if (!reversed.humanMotion)
            Debug.LogWarning("AnimationLibraryBuilder: " + name + " was generated without humanoid curves and won't retarget");

        return reversed;
    }

    // Clips are gitignored, so a fresh clone has the manifest but not the files; say what to download
    static void CheckManifest(IEnumerable<string> localClips)
    {
        HashSet<string> local = new HashSet<string>(localClips);
        List<MixamoManifest.Entry> manifest = MixamoManifest.Load();

        List<string> missing = manifest.Where(entry => !local.Contains(entry.name))
            .Select(entry => entry.name + " (Mixamo: \"" + entry.mixamoTitle + "\")").ToList();
        List<string> unrecorded = local.Where(name => manifest.All(entry => entry.name != name)).ToList();

        if (missing.Count > 0)
            Debug.LogWarning("AnimationLibraryBuilder: " + missing.Count + " clips from " + MixamoManifest.ManifestPath +
                " are not downloaded. Get them via VRoxel > Mixamo Inbox:\n" + string.Join("\n", missing));
        if (unrecorded.Count > 0)
            Debug.LogWarning("AnimationLibraryBuilder: " + unrecorded.Count + " local clips have no Mixamo title in the manifest, " +
                "so others can't reproduce them. Re-download them through VRoxel > Mixamo Inbox:\n" + string.Join("\n", unrecorded));
    }

    static void ApplyStairFix(GameObject avatarPrefab, AnimationLibraryEntry entry)
    {
        StairDescentFix fix = BakeStairFix(avatarPrefab, entry.clip);
        if (!fix.IsValid || fix.CycleDrop < VerticalThreshold)
        {
            Debug.LogWarning("AnimationLibraryBuilder: " + entry.name + " is named as a stair descent, but its hips don't sink " +
                "relative to the root (" + fix.CycleDrop.ToString("0.00") + " m per cycle); playing it unchanged");
            return;
        }

        entry.stairFix = fix;

        // At runtime the root goes down by the sink, so plans and the vertical flag see the descent
        for (int i = 0; i < entry.motion.Count; i++)
        {
            MotionSample sample = entry.motion[i];
            sample.up -= fix.TotalSink(sample.time);
            entry.motion[i] = sample;
        }

        Debug.Log("AnimationLibraryBuilder: " + entry.name + " gets the stair descent fix, " + fix.CycleDrop.ToString("0.00") + " m down per cycle");
    }

    // Records how far the hips sink relative to the root, and spreads one cycle's drop over each foot's swing,
    // proportional to how far the foot travels (a planted foot doesn't move, so it isn't lowered)
    static StairDescentFix BakeStairFix(GameObject avatarPrefab, AnimationClip clip)
    {
        GameObject avatar = BakeInstance(avatarPrefab);

        Animator animator = avatar.GetComponent<Animator>();
        animator.applyRootMotion = true;
        Transform root = avatar.transform;
        Transform hips = animator.GetBoneTransform(HumanBodyBones.Hips);
        Transform leftFoot = animator.GetBoneTransform(HumanBodyBones.LeftFoot);
        Transform rightFoot = animator.GetBoneTransform(HumanBodyBones.RightFoot);

        int count = Mathf.CeilToInt(clip.length / StairFixInterval) + 1;
        float[] sink = new float[count];
        float[] leftTravel = new float[count];
        float[] rightTravel = new float[count];

        PlayableGraph graph = PlayableGraph.Create("StairFixBake");
        try
        {
            graph.SetTimeUpdateMode(DirectorUpdateMode.Manual);
            AnimationPlayableOutput output = AnimationPlayableOutput.Create(graph, "Bake", animator);
            output.SetSourcePlayable(AnimationClipPlayable.Create(graph, clip));
            graph.Play();
            graph.Evaluate(0);

            float startHips = root.InverseTransformPoint(hips.position).y;
            Vector3 lastLeft = leftFoot.position;
            Vector3 lastRight = rightFoot.position;
            float interval = clip.length / (count - 1);
            float lastTime = 0;

            for (int i = 0; i < count; i++)
            {
                // The last sample stops just short of the clip length: a looping clip wraps to frame 0 exactly there
                float time = i < count - 1 ? i * interval : clip.length - 1e-3f;
                if (i > 0)
                    graph.Evaluate(time - lastTime);
                lastTime = time;

                sink[i] = startHips - root.InverseTransformPoint(hips.position).y;
                leftTravel[i] = (i > 0 ? leftTravel[i - 1] : 0) + FlatDistance(leftFoot.position, lastLeft);
                rightTravel[i] = (i > 0 ? rightTravel[i - 1] : 0) + FlatDistance(rightFoot.position, lastRight);
                lastLeft = leftFoot.position;
                lastRight = rightFoot.position;
            }
        }
        finally
        {
            graph.Destroy();
            Object.DestroyImmediate(avatar);
        }

        float cycleDrop = sink[count - 1];
        Debug.Log("AnimationLibraryBuilder: " + clip.name + " hips relative to root over one cycle: sink min " + sink.Min().ToString("0.000") +
            ", max " + sink.Max().ToString("0.000") + ", end " + cycleDrop.ToString("0.000") + " m; feet travel left " +
            leftTravel[count - 1].ToString("0.00") + ", right " + rightTravel[count - 1].ToString("0.00") + " m");
        StairDescentFix fix = new StairDescentFix
        {
            interval = clip.length / (count - 1),
            sink = sink,
            leftFootDrop = DistributeDrop(leftTravel, cycleDrop),
            rightFootDrop = DistributeDrop(rightTravel, cycleDrop)
        };
        return fix;
    }

    static float[] DistributeDrop(float[] travel, float cycleDrop)
    {
        float total = travel[travel.Length - 1];
        return travel.Select(distance => total > 1e-4f ? cycleDrop * distance / total : 0).ToArray();
    }

    static float FlatDistance(Vector3 a, Vector3 b)
    {
        a.y = 0;
        b.y = 0;
        return Vector3.Distance(a, b);
    }

    static void MatchPoses(GameObject avatarPrefab, List<AnimationLibraryEntry> entries)
    {
        foreach (var match in poseMatches)
        {
            AnimationLibraryEntry idle = entries.FirstOrDefault(entry => entry.name == match.idle);
            AnimationLibraryEntry transition = entries.FirstOrDefault(entry => entry.name == match.transition);
            if (idle == null || transition == null) continue;

            // A non-looping clip holds its last frame just before its length
            float transitionTime = match.atEnd ? transition.clip.length - 1e-3f : 0;
            Vector3 offset = HipsLocal(avatarPrefab, transition.clip, transitionTime) + transition.PoseOffsetAt(transitionTime) - HipsLocal(avatarPrefab, idle.clip, 0);
            bool apply = offset.magnitude > PoseMatchThreshold;

            Debug.Log("AnimationLibraryBuilder: " + match.idle + (apply ? " body moved by " + FormatOffset(offset) : " matches") +
                " to meet " + match.transition + " at its " + (match.atEnd ? "end" : "start"));

            idle.poseOffsetStart = apply ? offset : Vector3.zero;
            idle.poseOffsetEnd = idle.poseOffsetStart;
        }

        foreach (var match in alignMatches)
        {
            AnimationLibraryEntry clip = entries.FirstOrDefault(entry => entry.name == match.clip);
            AnimationLibraryEntry reference = entries.FirstOrDefault(entry => entry.name == match.reference);
            if (clip == null || reference == null) continue;

            float time = match.atEnd ? clip.clip.length - 1e-3f : 0;
            Vector3 offset = HipsLocal(avatarPrefab, reference.clip, 0) + reference.PoseOffsetAt(0) - HipsLocal(avatarPrefab, clip.clip, time) - clip.PoseOffsetAt(time);
            offset.y = 0;
            if (offset.magnitude <= PoseMatchThreshold)
            {
                Debug.Log("AnimationLibraryBuilder: " + match.clip + " matches " + match.reference);
                continue;
            }

            Debug.Log("AnimationLibraryBuilder: " + match.clip + " body moved by " + FormatOffset(offset) + (match.atEnd ? " at its end" : "") +
                " to stand where " + match.reference + " does");

            clip.poseOffsetEnd += offset;
            if (!match.atEnd)
                clip.poseOffsetStart += offset;
        }

        // After the matches above, which line the family's clips up with each other
        foreach (var match in heightMatches)
        {
            AnimationLibraryEntry clip = entries.FirstOrDefault(entry => entry.name == match.clip);
            AnimationLibraryEntry reference = entries.FirstOrDefault(entry => entry.name == match.reference);
            if (clip == null || reference == null) continue;

            float end = clip.clip.length - 1e-3f;
            float lift = HipsLocal(avatarPrefab, reference.clip, 0).y + reference.PoseOffsetAt(0).y - HipsLocal(avatarPrefab, clip.clip, end).y - clip.PoseOffsetAt(end).y;
            if (Mathf.Abs(lift) <= PoseMatchThreshold) continue;

            Debug.Log("AnimationLibraryBuilder: " + string.Join(", ", match.family) + " raised by " + lift.ToString("+0.00;-0.00") + " m, so " +
                match.clip + " stands up as high as " + match.reference);

            foreach (AnimationLibraryEntry entry in entries.Where(entry => match.family.Contains(entry.name)))
            {
                entry.poseOffsetStart += Vector3.up * lift;
                entry.poseOffsetEnd += Vector3.up * lift;
            }
        }
    }

    // Facing of the body relative to the root (degrees clockwise, from the hip line, unwrapped) at count evenly spaced
    // moments of a clip, played in small steps as at runtime
    static float[] BodyYaws(GameObject avatarPrefab, AnimationClip clip, int count)
    {
        GameObject avatar = BakeInstance(avatarPrefab);
        Animator animator = avatar.GetComponent<Animator>();
        animator.applyRootMotion = true;
        Transform leftHip = animator.GetBoneTransform(HumanBodyBones.LeftUpperLeg);
        Transform rightHip = animator.GetBoneTransform(HumanBodyBones.RightUpperLeg);
        float[] yaws = new float[count];

        PlayableGraph graph = PlayableGraph.Create("FacingBake");
        try
        {
            graph.SetTimeUpdateMode(DirectorUpdateMode.Manual);
            AnimationPlayableOutput output = AnimationPlayableOutput.Create(graph, "Bake", animator);
            output.SetSourcePlayable(AnimationClipPlayable.Create(graph, clip));
            graph.Play();
            graph.Evaluate(0);

            float interval = count > 1 ? clip.length / (count - 1) : 0;
            float lastTime = 0;
            for (int i = 0; i < count; i++)
            {
                // The last sample stops just short of the clip length, where a looping clip wraps to frame 0
                float time = i < count - 1 || count == 1 ? i * interval : Mathf.Max(0, clip.length - 1e-3f);
                for (float played = lastTime; played < time; played += SampleInterval)
                    graph.Evaluate(Mathf.Min(SampleInterval, time - played));
                lastTime = time;

                Vector3 right = Vector3.ProjectOnPlane(avatar.transform.InverseTransformDirection(rightHip.position - leftHip.position), Vector3.up);
                Vector3 forward = Vector3.Cross(right, Vector3.up);
                float yaw = Mathf.Atan2(forward.x, forward.z) * Mathf.Rad2Deg;
                yaws[i] = i > 0 ? yaws[i - 1] + Mathf.DeltaAngle(yaws[i - 1], yaw) : yaw;
            }
        }
        finally
        {
            graph.Destroy();
            Object.DestroyImmediate(avatar);
        }

        return yaws;
    }

    static void MatchFacing(GameObject avatarPrefab, List<AnimationLibraryEntry> entries)
    {
        foreach (var keep in keepFacing)
        {
            AnimationLibraryEntry entry = entries.FirstOrDefault(candidate => candidate.name == keep.clip);
            if (entry == null) continue;

            int count = Mathf.CeilToInt(entry.clip.length / SampleInterval) + 1;
            float[] yaws = BodyYaws(avatarPrefab, entry.clip, count);
            float kept = keep.keepEnd ? yaws[count - 1] : yaws[0];
            entry.poseYaw = yaws.Select(yaw => kept - yaw).ToArray();
            entry.poseYawInterval = entry.clip.length / (count - 1);

            Debug.Log("AnimationLibraryBuilder: " + entry.name + " turns its body by " + (yaws[count - 1] - yaws[0]).ToString("+0;-0") +
                "°, undone so the avatar keeps facing forward");
        }

        foreach (var match in facingMatches)
        {
            AnimationLibraryEntry clip = entries.FirstOrDefault(entry => entry.name == match.clip);
            AnimationLibraryEntry other = entries.FirstOrDefault(entry => entry.name == match.other);
            if (clip == null || other == null) continue;

            float otherTurn = other.PoseYawAt(match.atEnd ? other.clip.length : 0);
            float otherFacing = (match.atEnd ? BodyYaws(avatarPrefab, other.clip, 2)[1] : BodyYaws(avatarPrefab, other.clip, 1)[0]) + otherTurn;
            float clipStart = BodyYaws(avatarPrefab, clip.clip, 1)[0];
            // Picked closest to the other clip's own turn, so the crossfade between the two blends their turns smoothly
            float turn = Mathf.DeltaAngle(0, otherFacing - clipStart);
            turn += Mathf.Round((otherTurn - turn) / 360) * 360;
            bool apply = Mathf.Abs(Mathf.DeltaAngle(0, turn)) > FacingMatchThreshold;

            Debug.Log("AnimationLibraryBuilder: " + match.clip + (apply ? " body turned by " + turn.ToString("+0;-0") + "°" : " faces the same way") +
                " to meet " + match.other + " at its " + (match.atEnd ? "end" : "start"));

            clip.poseYaw = apply ? new[] { turn } : new float[0];
            clip.poseYawInterval = 0;
        }
    }

    // A hidden avatar at the origin for baking. MixamoAnimationPlayer handles OnAnimatorMove, which stops the
    // Animator from moving the root, and its scripts don't run in the editor; without it Unity applies root motion.
    public static GameObject BakeInstance(GameObject avatarPrefab)
    {
        GameObject avatar = Object.Instantiate(avatarPrefab);
        avatar.hideFlags = HideFlags.HideAndDontSave;
        avatar.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);

        MixamoAnimationPlayer player = avatar.GetComponent<MixamoAnimationPlayer>();
        if (player != null)
            Object.DestroyImmediate(player);

        return avatar;
    }

    // Hips relative to the root (x right, y up, z forward) at one moment of a clip, on a hidden avatar instance.
    // Played up to that time in small steps, so the root has followed the clip's root motion, as at runtime.
    static Vector3 HipsLocal(GameObject avatarPrefab, AnimationClip clip, float time)
    {
        GameObject avatar = BakeInstance(avatarPrefab);
        Animator animator = avatar.GetComponent<Animator>();
        animator.applyRootMotion = true;

        PlayableGraph graph = PlayableGraph.Create("HipsBake");
        try
        {
            graph.SetTimeUpdateMode(DirectorUpdateMode.Manual);
            AnimationPlayableOutput output = AnimationPlayableOutput.Create(graph, "Bake", animator);
            output.SetSourcePlayable(AnimationClipPlayable.Create(graph, clip));
            graph.Play();
            graph.Evaluate(0);

            for (float played = 0; played < time; played += SampleInterval)
                graph.Evaluate(Mathf.Min(SampleInterval, time - played));

            Transform hips = animator.GetBoneTransform(HumanBodyBones.Hips);
            return avatar.transform.InverseTransformPoint(hips.position);
        }
        finally
        {
            graph.Destroy();
            Object.DestroyImmediate(avatar);
        }
    }

    // Horizontal path length of the baked root motion over one cycle, per second. Unity's clip.averageSpeed is 0 for
    // generated clips and measured on the native skeleton, so it is only the fallback when nothing was baked.
    static float TravelSpeed(AnimationLibraryEntry entry)
    {
        if (entry.motion.Count < 2 || entry.length <= 0)
            return new Vector2(entry.clip.averageSpeed.x, entry.clip.averageSpeed.z).magnitude;

        float distance = 0;
        for (int i = 1; i < entry.motion.Count; i++)
            distance += new Vector2(entry.motion[i].right - entry.motion[i - 1].right, entry.motion[i].forward - entry.motion[i - 1].forward).magnitude;

        return distance / entry.length;
    }

    // Plays one cycle on a hidden avatar instance with root motion and records the root relative to its start pose
    static List<MotionSample> BakeMotion(GameObject avatarPrefab, AnimationClip clip)
    {
        List<MotionSample> samples = new List<MotionSample>();

        GameObject avatar = BakeInstance(avatarPrefab);

        Animator animator = avatar.GetComponent<Animator>();
        animator.applyRootMotion = true;

        PlayableGraph graph = PlayableGraph.Create("MotionBake");
        try
        {
            graph.SetTimeUpdateMode(DirectorUpdateMode.Manual);
            AnimationPlayableOutput output = AnimationPlayableOutput.Create(graph, "Bake", animator);
            output.SetSourcePlayable(AnimationClipPlayable.Create(graph, clip));
            graph.Play();
            graph.Evaluate(0);

            Transform root = avatar.transform;
            float unwrappedYaw = 0;
            float lastYaw = root.eulerAngles.y;
            int count = Mathf.CeilToInt(clip.length / SampleInterval);

            for (int i = 0; i <= count; i++)
            {
                float time = Mathf.Min(i * SampleInterval, clip.length);
                if (i > 0)
                    graph.Evaluate(time - samples[i - 1].time);

                // Unwrapped, so turning clips can report more than 360 degrees
                unwrappedYaw += Mathf.DeltaAngle(lastYaw, root.eulerAngles.y);
                lastYaw = root.eulerAngles.y;

                samples.Add(new MotionSample { time = time, right = root.position.x, forward = root.position.z, up = root.position.y, turn = unwrappedYaw });
            }
        }
        finally
        {
            graph.Destroy();
            Object.DestroyImmediate(avatar);
        }

        MotionSample end = samples[samples.Count - 1];
        bool moved = Mathf.Abs(end.right) + Mathf.Abs(end.forward) > 0.01f || Mathf.Abs(end.turn) > 1;
        if (!moved && clip.averageSpeed.magnitude * clip.length > 0.1f)
            Debug.LogWarning("AnimationLibraryBuilder: " + clip.name + " has root motion but the bake recorded none");

        return samples;
    }
}
