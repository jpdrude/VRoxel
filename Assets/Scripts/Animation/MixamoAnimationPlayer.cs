using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

[RequireComponent(typeof(Animator))]
public class MixamoAnimationPlayer : MonoBehaviour
{
    [Header("Test Clips")]
    [SerializeField]
    List<AnimationClip> testClips = new List<AnimationClip>();
    [SerializeField]
    bool playOnStart = true;
    [SerializeField]
    bool cycleTestClips = true;

    [Space]
    [Header("Playback")]
    [SerializeField]
    float crossfadeDuration = DefaultCrossfadeDuration;
    // SequencePlanner plans with the default
    public const float DefaultCrossfadeDuration = 0.3f;
    public float CrossfadeDuration { get { return crossfadeDuration; } set { crossfadeDuration = Mathf.Max(0, value); } }
    [SerializeField]
    float speed = 1;
    public float Speed
    {
        get { return speed; }
        set { speed = Mathf.Clamp(value, 0, 10); }
    }
    [SerializeField]
    bool pauseAnimation;
    public bool PauseAnimationState { get { return pauseAnimation; } }
    [Tooltip("Off by default: foot IK pins the feet to the native leg length before the floor offset lifts the body")]
    [SerializeField]
    bool footIK = false;

    Animator animator;
    PlayableGraph graph;
    AnimationMixerPlayable mixer;
    AnimationClipPlayable currentPlayable;
    AnimationClipPlayable previousPlayable;

    float fadeTimer = 0;
    float fadeDuration = 0;
    int testClipIndex = -1;

    // Humanoid places the hips for the native leg length, ignoring CharacterModel's bone scaling. The body is scaled
    // about the root by the change in hip-to-sole distance in the rest pose (T-pose), which fits it to this avatar's legs
    // in every pose, and so is the clips' travel: a shorter avatar takes shorter steps.
    Transform hips;
    Transform[] restPoseBones;
    Quaternion[] restPoseRotations;
    float nativeHipToSole;
    // Height of the ankles above the soles in the rest pose; on furniture the feet are kept at least this high
    float footToSole;
    public float BodyScale { get; private set; } = 1;
    // False until a clip has posed the body; before that it stands in its T-pose (CharacterSpawner waits for it)
    public bool Posed { get; private set; }

    // Stair descents corrected at runtime (see StairDescentFix), registered from the AnimationLibrary
    static readonly Dictionary<AnimationClip, StairDescentFix> stairFixes = new Dictionary<AnimationClip, StairDescentFix>();
    // Body shifts and turns that line clips' poses up with their neighbours (AnimationLibraryEntry.poseOffsetStart / End, poseYaw)
    static readonly Dictionary<AnimationClip, AnimationLibraryEntry> poseOffsets = new Dictionary<AnimationClip, AnimationLibraryEntry>();
    AnimationClip previousClip;

    // Root motion of library clips comes from their baked trajectories (AnimationLibraryEntry.motion), not from Unity:
    // during a crossfade Unity blends the clips' root positions, which shifted avatars by up to 0.4 m. Blending each
    // clip's own motion since the last frame, as Animator state machines do, can't jump, and matches SequencePlanner.
    static readonly Dictionary<AnimationClip, AnimationLibraryEntry> motions = new Dictionary<AnimationClip, AnimationLibraryEntry>();

    class RootMotionState
    {
        public AnimationLibraryEntry entry;
        public double lastTime;
    }

    RootMotionState currentMotion;
    RootMotionState previousMotion;

    class FixState
    {
        public StairDescentFix fix;
    }

    FixState currentFix;
    FixState previousFix;
    Transform leftUpperLeg, leftLowerLeg, leftFoot;
    Transform rightUpperLeg, rightLowerLeg, rightFoot;

    public AnimationClip CurrentClip { get; private set; }
    public bool StairFixActive { get { return currentFix != null; } }
    // Raised after every evaluation with the frame's delta time before the speed multiplier
    public event Action<float> Evaluated;
    public float Timer { get { return currentPlayable.IsValid() ? (float)currentPlayable.GetTime() : 0; } }

    void Awake()
    {
        animator = GetComponent<Animator>();

        // Measured before the graph poses the skeleton, while it is still in the prefab's rest pose
        hips = animator.GetBoneTransform(HumanBodyBones.Hips);
        restPoseBones = hips.GetComponentsInChildren<Transform>(true);
        restPoseRotations = new Quaternion[restPoseBones.Length];
        for (int i = 0; i < restPoseBones.Length; i++)
            restPoseRotations[i] = restPoseBones[i].localRotation;
        nativeHipToSole = MeasureHipToSole(out footToSole);

        leftUpperLeg = animator.GetBoneTransform(HumanBodyBones.LeftUpperLeg);
        leftLowerLeg = animator.GetBoneTransform(HumanBodyBones.LeftLowerLeg);
        leftFoot = animator.GetBoneTransform(HumanBodyBones.LeftFoot);
        rightUpperLeg = animator.GetBoneTransform(HumanBodyBones.RightUpperLeg);
        rightLowerLeg = animator.GetBoneTransform(HumanBodyBones.RightLowerLeg);
        rightFoot = animator.GetBoneTransform(HumanBodyBones.RightFoot);

        // Manual time update so playback can follow AnimationIO.FrameTime (recording, brushing)
        graph = PlayableGraph.Create(name + "_MixamoGraph");
        graph.SetTimeUpdateMode(DirectorUpdateMode.Manual);

        var output = AnimationPlayableOutput.Create(graph, "Animation", animator);
        mixer = AnimationMixerPlayable.Create(graph, 2);
        output.SetSourcePlayable(mixer);

        graph.Play();
    }

    void Start()
    {
        if (playOnStart && testClips.Count > 0)
            PlayNextTestClip();
    }

    void Update()
    {
        if (pauseAnimation || CurrentClip == null)
            return;

        float frameTime = AnimationIO.FrameTime;
        float deltaTime = frameTime * speed;

        UpdateCrossfade(deltaTime);
        graph.Evaluate(deltaTime);
        ApplyRootMotion();
        ApplyPoseOffsets();
        ApplyStairFixes();

        Evaluated?.Invoke(frameTime);

        if (cycleTestClips && !CurrentClip.isLooping && Timer >= CurrentClip.length)
            PlayNextTestClip();
    }

    public void Play(AnimationClip clip)
    {
        Play(clip, crossfadeDuration);
    }

    public void Play(AnimationClip clip, float fade)
    {
        if (clip == null) return;

        AnimationLibraryEntry fromEntry, toEntry;
        if (CurrentClip != null && currentPlayable.IsValid() && motions.TryGetValue(CurrentClip, out fromEntry) && motions.TryGetValue(clip, out toEntry))
            fade = AnimationLibraryEntry.Crossfade(fromEntry, (float)currentPlayable.GetTime(), toEntry, fade);

        // Mixer input 0 holds the current clip, input 1 the clip fading out
        if (previousPlayable.IsValid())
        {
            graph.Disconnect(mixer, 1);
            previousPlayable.Destroy();
        }

        if (currentPlayable.IsValid())
        {
            graph.Disconnect(mixer, 0);
            previousPlayable = currentPlayable;
            graph.Connect(previousPlayable, 0, mixer, 1);
        }

        previousClip = CurrentClip;
        currentPlayable = AnimationClipPlayable.Create(graph, clip);
        currentPlayable.SetApplyFootIK(footIK);
        graph.Connect(currentPlayable, 0, mixer, 0);

        previousFix = currentFix;
        StairDescentFix fix;
        currentFix = stairFixes.TryGetValue(clip, out fix) ? new FixState { fix = fix } : null;

        previousMotion = currentMotion;
        AnimationLibraryEntry entry;
        currentMotion = motions.TryGetValue(clip, out entry) ? new RootMotionState { entry = entry } : null;

        fadeDuration = previousPlayable.IsValid() ? fade : 0;
        fadeTimer = 0;
        ApplyFadeWeights();

        CurrentClip = clip;
    }

    // Stair descent fixes and height offsets baked into the library
    public static void RegisterClipFixes(AnimationLibrary library)
    {
        if (library == null) return;

        foreach (AnimationLibraryEntry entry in library.Entries)
        {
            if (entry.clip == null) continue;

            if (entry.stairFix != null && entry.stairFix.IsValid)
                stairFixes[entry.clip] = entry.stairFix;
            if (entry.HasPoseOffset)
                poseOffsets[entry.clip] = entry;
            if (entry.motion != null && entry.motion.Count > 1)
                motions[entry.clip] = entry;
        }

        Debug.Log("MixamoAnimationPlayer: root motion of " + motions.Count + " clips, " + stairFixes.Count + " stair descent fixes (" + string.Join(", ", stairFixes.Keys.Select(clip => clip.name)) +
            "), " + poseOffsets.Count + " pose offsets (" + string.Join(", ", poseOffsets.Keys.Select(clip => clip.name)) + ") registered");
    }

    // Moves the root by each playing clip's baked motion since the last frame, weighted by the crossfade
    void ApplyRootMotion()
    {
        Vector3 move = Vector3.zero;
        float turn = 0;

        AddRootMotion(currentMotion, currentPlayable, mixer.GetInputWeight(0), BodyScale, ref move, ref turn);
        AddRootMotion(previousMotion, previousPlayable, previousPlayable.IsValid() ? mixer.GetInputWeight(1) : 0, BodyScale, ref move, ref turn);

        // The offsets are in the avatar's frame before this frame's turn
        transform.position += transform.rotation * move;
        transform.rotation = Quaternion.AngleAxis(turn, transform.up) * transform.rotation;
    }

    static void AddRootMotion(RootMotionState state, AnimationClipPlayable playable, float weight, float bodyScale, ref Vector3 move, ref float turn)
    {
        if (state == null || !playable.IsValid()) return;

        double time = playable.GetTime();
        MotionSample delta = state.entry.MotionBetween((float)state.lastTime, (float)time);
        state.lastTime = time;

        // Climbing clips keep their path, so a stair or ladder has the same shape for every avatar
        float scale = state.entry.vertical ? 1 : bodyScale;
        move += new Vector3(delta.right * scale, delta.up, delta.forward * scale) * weight;
        turn += delta.turn * weight;
    }

    // Having this callback stops the Animator from moving the root itself. Clips outside the library (e.g. test
    // clips) keep Unity's root motion. The library bake removes this component, as the callback doesn't run in the editor.
    void OnAnimatorMove()
    {
        if (currentMotion == null)
            GetComponent<Animator>().ApplyBuiltinRootMotion();
    }

    // Shifts and turns the body by the current and fading-out clip's pose offsets, weighted by the crossfade, and scales
    // it about the root to this avatar's legs. Furniture has the same height for everyone, so sitting on chairs and lying
    // on beds keep the model's hip height; there a taller avatar's feet are lifted back onto the floor.
    void ApplyPoseOffsets()
    {
        Posed = true;
        float furniture = FurnitureWeight();
        Vector3 body = transform.InverseTransformPoint(hips.position) + PoseOffset();
        body.x *= BodyScale;
        body.y *= Mathf.Lerp(BodyScale, 1, furniture);
        body.z *= BodyScale;
        hips.position = transform.TransformPoint(body);

        float yaw = PoseYaw();
        if (Mathf.Abs(yaw) > 1e-3f)
            hips.rotation = Quaternion.AngleAxis(yaw, transform.up) * hips.rotation;

        if (furniture > 0)
        {
            KeepFootOnFloor(leftUpperLeg, leftLowerLeg, leftFoot, furniture);
            KeepFootOnFloor(rightUpperLeg, rightLowerLeg, rightFoot, furniture);
        }
    }

    void KeepFootOnFloor(Transform upperLeg, Transform lowerLeg, Transform foot, float weight)
    {
        float below = footToSole * BodyScale - transform.InverseTransformPoint(foot.position).y;
        if (below > 0.001f)
            LegIK.Solve(upperLeg, lowerLeg, foot, foot.position + transform.up * below * weight);
    }

    // How much the current and fading-out clip sit or lie on furniture (0-1), weighted by the crossfade
    float FurnitureWeight()
    {
        float weight = 0;
        AnimationLibraryEntry entry;

        if (CurrentClip != null && currentPlayable.IsValid() && motions.TryGetValue(CurrentClip, out entry))
            weight += entry.FurnitureAt((float)currentPlayable.GetTime()) * mixer.GetInputWeight(0);
        if (previousPlayable.IsValid() && previousClip != null && motions.TryGetValue(previousClip, out entry))
            weight += entry.FurnitureAt((float)previousPlayable.GetTime()) * mixer.GetInputWeight(1);

        return weight;
    }

    // Body shift of the current and fading-out clip in the avatar's frame, weighted by the crossfade
    Vector3 PoseOffset()
    {
        Vector3 offset = Vector3.zero;
        AnimationLibraryEntry entry;

        if (CurrentClip != null && currentPlayable.IsValid() && poseOffsets.TryGetValue(CurrentClip, out entry))
            offset += entry.PoseOffsetAt((float)currentPlayable.GetTime()) * mixer.GetInputWeight(0);
        if (previousPlayable.IsValid() && previousClip != null && poseOffsets.TryGetValue(previousClip, out entry))
            offset += entry.PoseOffsetAt((float)previousPlayable.GetTime()) * mixer.GetInputWeight(1);

        return offset;
    }

    // Body turn about the hips of the current and fading-out clip, weighted by the crossfade
    float PoseYaw()
    {
        float yaw = 0;
        AnimationLibraryEntry entry;

        if (CurrentClip != null && currentPlayable.IsValid() && poseOffsets.TryGetValue(CurrentClip, out entry))
            yaw += entry.PoseYawAt((float)currentPlayable.GetTime()) * mixer.GetInputWeight(0);
        if (previousPlayable.IsValid() && previousClip != null && poseOffsets.TryGetValue(previousClip, out entry))
            yaw += entry.PoseYawAt((float)previousPlayable.GetTime()) * mixer.GetInputWeight(1);

        return yaw;
    }

    // The root follows the descent through the baked motion, whose height includes the clip's sink; the hips are
    // raised by the sink so the body keeps its path, then the feet move down the stairs with IK. Weighted by the crossfade.
    void ApplyStairFixes()
    {
        float hipsRaise = 0;
        float leftDrop = 0;
        float rightDrop = 0;

        ApplyStairFix(currentFix, currentPlayable, mixer.GetInputWeight(0), ref hipsRaise, ref leftDrop, ref rightDrop);
        ApplyStairFix(previousFix, previousPlayable, previousPlayable.IsValid() ? mixer.GetInputWeight(1) : 0, ref hipsRaise, ref leftDrop, ref rightDrop);

        if (hipsRaise == 0 && leftDrop == 0 && rightDrop == 0)
            return;

        hips.position += transform.up * hipsRaise;

        if (leftDrop > 0.001f)
            LegIK.Solve(leftUpperLeg, leftLowerLeg, leftFoot, leftFoot.position - transform.up * leftDrop);
        if (rightDrop > 0.001f)
            LegIK.Solve(rightUpperLeg, rightLowerLeg, rightFoot, rightFoot.position - transform.up * rightDrop);
    }

    static void ApplyStairFix(FixState state, AnimationClipPlayable playable, float weight,
        ref float hipsRaise, ref float leftDrop, ref float rightDrop)
    {
        if (state == null || !playable.IsValid()) return;

        double time = playable.GetTime();
        hipsRaise += state.fix.SinkAt(time) * weight;
        leftDrop += state.fix.LeftFootDropAt(time) * weight;
        rightDrop += state.fix.RightFootDropAt(time) * weight;
    }

    // Shows one frame of a clip and pauses there
    public void ShowPose(AnimationClip clip, float time)
    {
        Play(clip, 0);
        currentPlayable.SetTime(time);
        // The pose is shown where the avatar stands; root motion continues from here
        if (currentMotion != null)
            currentMotion.lastTime = time;
        graph.Evaluate(0);
        ApplyPoseOffsets();

        pauseAnimation = true;
    }

    // Stops playback; the avatar keeps its last pose
    public void Stop()
    {
        if (previousPlayable.IsValid())
        {
            graph.Disconnect(mixer, 1);
            previousPlayable.Destroy();
        }

        if (currentPlayable.IsValid())
        {
            graph.Disconnect(mixer, 0);
            currentPlayable.Destroy();
        }

        CurrentClip = null;
        currentMotion = null;
        previousMotion = null;
    }

    void UpdateCrossfade(float deltaTime)
    {
        if (!previousPlayable.IsValid()) return;

        fadeTimer += deltaTime;
        ApplyFadeWeights();

        if (fadeTimer >= fadeDuration)
        {
            graph.Disconnect(mixer, 1);
            previousPlayable.Destroy();
        }
    }

    void ApplyFadeWeights()
    {
        float weight = fadeDuration > 0 ? Mathf.Clamp01(fadeTimer / fadeDuration) : 1;
        mixer.SetInputWeight(0, weight);
        mixer.SetInputWeight(1, 1 - weight);
    }

    // Call after the skeleton is rescaled (CharacterModel.InitializeAvatar)
    public void UpdateBodyScale()
    {
        if (hips == null) return;

        Quaternion[] animatedRotations = new Quaternion[restPoseBones.Length];
        for (int i = 0; i < restPoseBones.Length; i++)
        {
            animatedRotations[i] = restPoseBones[i].localRotation;
            restPoseBones[i].localRotation = restPoseRotations[i];
        }

        BodyScale = MeasureHipToSole(out footToSole) / nativeHipToSole;

        for (int i = 0; i < restPoseBones.Length; i++)
            restPoseBones[i].localRotation = animatedRotations[i];
    }

    // Height of the hips (and the lower ankle) above the lowest skinned vertex, in the avatar's root space
    float MeasureHipToSole(out float ankleToSole)
    {
        float lowest = float.MaxValue;
        Mesh baked = new Mesh();

        foreach (SkinnedMeshRenderer smr in GetComponentsInChildren<SkinnedMeshRenderer>())
        {
            smr.BakeMesh(baked);
            // Baked vertices include the renderer's scale but not its position and rotation
            Matrix4x4 toRoot = transform.worldToLocalMatrix * Matrix4x4.TRS(smr.transform.position, smr.transform.rotation, Vector3.one);
            foreach (Vector3 vertex in baked.vertices)
                lowest = Mathf.Min(lowest, toRoot.MultiplyPoint3x4(vertex).y);
        }

        Destroy(baked);
        float ankle = Mathf.Min(transform.InverseTransformPoint(animator.GetBoneTransform(HumanBodyBones.LeftFoot).position).y, transform.InverseTransformPoint(animator.GetBoneTransform(HumanBodyBones.RightFoot).position).y);
        ankleToSole = ankle - lowest;
        return transform.InverseTransformPoint(hips.position).y - lowest;
    }

    public void PauseAnimation(bool state)
    {
        pauseAnimation = state;
    }

    public float RestartAnimation()
    {
        if (currentPlayable.IsValid())
            currentPlayable.SetTime(0);
        if (currentMotion != null)
            currentMotion.lastTime = 0;

        return 0;
    }

    [ContextMenu("Play Next Test Clip")]
    public void PlayNextTestClip()
    {
        if (testClips.Count == 0) return;

        testClipIndex = (testClipIndex + 1) % testClips.Count;
        Play(testClips[testClipIndex]);
    }

    void OnDestroy()
    {
        if (graph.IsValid())
            graph.Destroy();
    }
}
