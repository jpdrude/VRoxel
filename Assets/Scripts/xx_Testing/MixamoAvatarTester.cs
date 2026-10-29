using System.Collections.Generic;
using System.IO;
using UnityEngine;
using Valve.Newtonsoft.Json;

// Test harness for MixamoAvatar: cycles body dimension presets (.vrxlbody) and test clips,
// and shows foot / hip heights to check whether Humanoid playback respects the custom proportions.
// Keys: N = next clip, B = next body preset, R = rebind Animator
[RequireComponent(typeof(CharacterModel), typeof(MixamoAnimationPlayer))]
public class MixamoAvatarTester : MonoBehaviour
{
    [Header("Body Presets")]
    [Tooltip("Paths to .vrxlbody files, absolute or relative to the project folder")]
    [SerializeField]
    List<string> presetPaths = new List<string>();
    [SerializeField]
    bool applyFirstPresetOnStart = true;
    [Tooltip("Call Animator.Rebind() after applying a preset, so Humanoid re-reads the scaled skeleton")]
    [SerializeField]
    bool rebindAfterPreset = false;

    [Tooltip("Optional: applies the library's stair descent fixes and height offsets to the test clips")]
    [SerializeField]
    AnimationLibrary animationLibrary;

    [Space]
    [Header("Keys")]
    [SerializeField]
    KeyCode nextClipKey = KeyCode.N;
    [SerializeField]
    KeyCode nextPresetKey = KeyCode.B;
    [SerializeField]
    KeyCode rebindKey = KeyCode.R;

    CharacterModel characterModel;
    MixamoAnimationPlayer player;
    Animator animator;

    // -1 = the model's native proportions
    int presetIndex = -1;
    string presetName = "Prefab (not initialized)";
    CharacterColorSet fallbackColorSet;

    float lowestFootHeight;
    float minLowestFootHeight = float.MaxValue;
    float maxLowestFootHeight = float.MinValue;

    // Before MixamoAnimationPlayer.Start plays the first test clip
    void Awake()
    {
        MixamoAnimationPlayer.RegisterClipFixes(animationLibrary);
    }

    void Start()
    {
        characterModel = GetComponent<CharacterModel>();
        player = GetComponent<MixamoAnimationPlayer>();
        animator = GetComponent<Animator>();

        fallbackColorSet = BuildColorSetFromCurrentMaterials();

        if (applyFirstPresetOnStart && presetPaths.Count > 0)
            ApplyNextPreset();
    }

    void Update()
    {
        if (Input.GetKeyDown(nextClipKey))
            player.PlayNextTestClip();

        if (Input.GetKeyDown(nextPresetKey))
            ApplyNextPreset();

        if (Input.GetKeyDown(rebindKey))
            RebindAnimator();
    }

    void LateUpdate()
    {
        // Measured after the Animator has written the pose
        Transform leftToes = animator.GetBoneTransform(HumanBodyBones.LeftToes);
        Transform rightToes = animator.GetBoneTransform(HumanBodyBones.RightToes);
        Transform leftFoot = animator.GetBoneTransform(HumanBodyBones.LeftFoot);
        Transform rightFoot = animator.GetBoneTransform(HumanBodyBones.RightFoot);

        lowestFootHeight = float.MaxValue;
        foreach (Transform t in new[] { leftToes, rightToes, leftFoot, rightFoot })
            if (t != null)
                lowestFootHeight = Mathf.Min(lowestFootHeight, t.position.y - transform.position.y);

        minLowestFootHeight = Mathf.Min(minLowestFootHeight, lowestFootHeight);
        maxLowestFootHeight = Mathf.Max(maxLowestFootHeight, lowestFootHeight);
    }

    [ContextMenu("Apply Next Preset")]
    public void ApplyNextPreset()
    {
        // Cycles through the presets and back to the native proportions
        presetIndex++;
        if (presetIndex >= presetPaths.Count)
            presetIndex = -1;

        if (presetIndex == -1)
        {
            // The model's native proportions, copied so InitializeAvatar can't alias them
            CharacterOptions native = new CharacterOptions();
            native.bodyDimensions = JsonConvert.DeserializeObject<AvatarDimensions>(JsonConvert.SerializeObject(characterModel.NativeDimensions));
            ApplyOptions(native, "Native proportions");
            return;
        }

        string path = ResolvePath(presetPaths[presetIndex]);
        if (!File.Exists(path))
        {
            Debug.LogError("MixamoAvatarTester: preset not found: " + path);
            return;
        }

        CharacterOptions options = JsonConvert.DeserializeObject<CharacterOptions>(File.ReadAllText(path));
        ApplyOptions(options, Path.GetFileNameWithoutExtension(path));
    }

    void ApplyOptions(CharacterOptions options, string name)
    {
        CharacterColorSet colorSet = fallbackColorSet;
        if (CharacterEditor.ColorSets != null && options.colorSet < CharacterEditor.ColorSets.Count)
            colorSet = CharacterEditor.ColorSets[options.colorSet];

        characterModel.InitializeAvatar(options.bodyDimensions, colorSet);
        characterModel.SetTrails(options.useBrushStrokes, options.strokeLength);

        if (rebindAfterPreset)
            RebindAnimator();

        presetName = name;
        ResetFootStats();
        Debug.Log("MixamoAvatarTester: applied preset " + name + ", CharacterModel hip position " + characterModel.HipPosition);
    }

    [ContextMenu("Rebind Animator")]
    public void RebindAnimator()
    {
        animator.Rebind();
        ResetFootStats();
    }

    void ResetFootStats()
    {
        minLowestFootHeight = float.MaxValue;
        maxLowestFootHeight = float.MinValue;
    }

    CharacterColorSet BuildColorSetFromCurrentMaterials()
    {
        CharacterColorSet colorSet = new CharacterColorSet();

        if (characterModel.Body.Count > 0)
            colorSet.mainColor = characterModel.Body[0].sharedMaterial;
        if (characterModel.Joints.Count > 0)
            colorSet.secondaryColor = characterModel.Joints[0].sharedMaterial;

        LineRenderer[] strokes = GetComponentsInChildren<LineRenderer>(true);
        if (strokes.Length > 0)
            colorSet.strokeColor = strokes[0].sharedMaterial;

        return colorSet;
    }

    static string ResolvePath(string path)
    {
        if (Path.IsPathRooted(path))
            return path;

        return Path.GetFullPath(Path.Combine(Application.dataPath, "..", path));
    }

    void OnGUI()
    {
        Transform hips = animator.GetBoneTransform(HumanBodyBones.Hips);
        string clip = player.CurrentClip != null ? player.CurrentClip.name : "-";

        GUILayout.BeginArea(new Rect(10, 10, 420, 220), GUI.skin.box);
        GUILayout.Label("Preset: " + presetName + "   [" + nextPresetKey + "]");
        GUILayout.Label("Clip: " + clip + "   [" + nextClipKey + "]" + (player.StairFixActive ? "   stair fix ON" : ""));
        GUILayout.Label("Hip height (actual): " + (hips.position.y - transform.position.y).ToString("0.000") + " m");
        GUILayout.Label("Hip height (CharacterModel): " + characterModel.HipPosition.y.ToString("0.000") + " (local)");
        GUILayout.Label("Floor offset (rest pose): " + player.FloorOffset.ToString("0.000") + " m");
        GUILayout.Label("Lowest foot bone: " + lowestFootHeight.ToString("0.000") + " m");
        GUILayout.Label("  min / max since preset: " + minLowestFootHeight.ToString("0.000") + " / " + maxLowestFootHeight.ToString("0.000") + " m");
        GUILayout.Label("Rebind Animator   [" + rebindKey + "]");
        GUILayout.EndArea();
    }
}
