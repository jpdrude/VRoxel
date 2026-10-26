using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

// Keeps the Mixamo AnimationLibrary in sync with the clip folder
public class AnimationLibraryBuilder : AssetPostprocessor
{
    const string ClipFolder = "Assets/Animation/Mixamo/Clips/";
    const string LibraryPath = "Assets/Animation/Mixamo/MixamoLibrary.asset";
    // Root motion is baked on the avatar that plays the clips, so distances match runtime
    const string AvatarPath = "Assets/Animation/Characters/MixamoAvatar.prefab";
    const float SampleInterval = 0.1f;
    // Root height change that marks a clip as climbing
    const float VerticalThreshold = 0.05f;

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

        List<AnimationLibraryEntry> entries = new List<AnimationLibraryEntry>();
        GameObject avatarPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(AvatarPath);

        foreach (string guid in AssetDatabase.FindAssets("t:AnimationClip", new[] { ClipFolder.TrimEnd('/') }))
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
                averageSpeed = new Vector2(clip.averageSpeed.x, clip.averageSpeed.z).magnitude,
                description = description,
                motion = avatarPrefab != null ? BakeMotion(avatarPrefab, clip) : new List<MotionSample>()
            });
        }

        // FindAssets returns a clip once per model, but stay safe against duplicates
        foreach (AnimationLibraryEntry entry in entries)
            entry.vertical = entry.motion.Any(sample => Mathf.Abs(sample.up) > VerticalThreshold);

        entries = entries.GroupBy(entry => entry.name).Select(group => group.First()).OrderBy(entry => entry.name).ToList();

        if (avatarPrefab == null)
            Debug.LogWarning("AnimationLibraryBuilder: no avatar at " + AvatarPath + ", root motion not baked");

        library.SetEntries(entries);
        EditorUtility.SetDirty(library);
        AssetDatabase.SaveAssets();

        CheckManifest(entries.Select(entry => entry.name));
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

    // Plays one cycle on a hidden avatar instance with root motion and records the root relative to its start pose
    static List<MotionSample> BakeMotion(GameObject avatarPrefab, AnimationClip clip)
    {
        List<MotionSample> samples = new List<MotionSample>();

        GameObject avatar = Object.Instantiate(avatarPrefab);
        avatar.hideFlags = HideFlags.HideAndDontSave;
        avatar.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);

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
