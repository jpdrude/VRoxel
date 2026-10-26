using System;
using System.Collections.Generic;
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
    float crossfadeDuration = 0.3f;
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

    // Humanoid places the hips for the native leg length, ignoring CharacterModel's bone scaling.
    // The offset is the change in hip-to-sole distance in the rest pose (T-pose) and is added after every evaluation.
    Transform hips;
    Transform[] restPoseBones;
    Quaternion[] restPoseRotations;
    float nativeHipToSole;
    public float FloorOffset { get; private set; }

    public AnimationClip CurrentClip { get; private set; }
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
        nativeHipToSole = MeasureHipToSole();

        // Manual time update so playback can follow AnimationIO.constantAnimationTime (recording)
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

        float frameTime = AnimationIO.constantAnimationTime ? 1f / AnimationIO.animationFrameRate : Time.deltaTime;
        float deltaTime = frameTime * speed;

        UpdateCrossfade(deltaTime);
        graph.Evaluate(deltaTime);
        hips.position += transform.up * FloorOffset;

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

        currentPlayable = AnimationClipPlayable.Create(graph, clip);
        currentPlayable.SetApplyFootIK(footIK);
        graph.Connect(currentPlayable, 0, mixer, 0);

        fadeDuration = previousPlayable.IsValid() ? fade : 0;
        fadeTimer = 0;
        ApplyFadeWeights();

        CurrentClip = clip;
    }

    // Shows one frame of a clip and pauses there
    public void ShowPose(AnimationClip clip, float time)
    {
        Play(clip, 0);
        currentPlayable.SetTime(time);
        graph.Evaluate(0);
        hips.position += transform.up * FloorOffset;

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
    public void UpdateFloorOffset()
    {
        if (hips == null) return;

        Quaternion[] animatedRotations = new Quaternion[restPoseBones.Length];
        for (int i = 0; i < restPoseBones.Length; i++)
        {
            animatedRotations[i] = restPoseBones[i].localRotation;
            restPoseBones[i].localRotation = restPoseRotations[i];
        }

        FloorOffset = MeasureHipToSole() - nativeHipToSole;

        for (int i = 0; i < restPoseBones.Length; i++)
            restPoseBones[i].localRotation = animatedRotations[i];
    }

    // Height of the hips above the lowest skinned vertex, in the avatar's root space
    float MeasureHipToSole()
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
