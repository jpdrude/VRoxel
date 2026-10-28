using System.Collections.Generic;
using UnityEngine;

// Curated Mixamo clips that LLM tools can play by name.
// Rebuilt by AnimationLibraryBuilder whenever the clip folder changes; descriptions are kept across rebuilds.
[CreateAssetMenu(fileName = "MixamoLibrary", menuName = "VRoxel/Animation Library")]
public class AnimationLibrary : ScriptableObject
{
    [SerializeField]
    List<AnimationLibraryEntry> entries = new List<AnimationLibraryEntry>();
    public List<AnimationLibraryEntry> Entries { get { return entries; } }

    Dictionary<string, AnimationLibraryEntry> lookup;

    public AnimationLibraryEntry Get(string name)
    {
        if (lookup == null || lookup.Count != entries.Count)
        {
            lookup = new Dictionary<string, AnimationLibraryEntry>();
            foreach (AnimationLibraryEntry entry in entries)
                lookup[entry.name] = entry;
        }

        AnimationLibraryEntry result;
        lookup.TryGetValue(name, out result);
        return result;
    }

    public void SetEntries(List<AnimationLibraryEntry> newEntries)
    {
        entries = newEntries;
        lookup = null;
    }
}

[System.Serializable]
public class AnimationLibraryEntry
{
    public string name;
    public string category;
    public AnimationClip clip;

    [Space]
    public bool loop;
    public float length;
    // Horizontal root motion in m/s along the baked path, 0 for clips on the spot
    public float averageSpeed;
    // Root moves up or down (vert clips); playing it lifts the avatar off floor following
    public bool vertical;

    [TextArea]
    public string description;

    // Root motion over one clip cycle, baked on MixamoAvatar, in the avatar's frame at clip start
    public List<MotionSample> motion = new List<MotionSample>();

    // Correction for Mixamo stair descents, whose feet are pinned to a flat floor; empty for other clips
    public StairDescentFix stairFix = new StairDescentFix();

    // Pose change after clip time t (seconds, unscaled by playback speed). Looping clips repeat their cycle.
    public MotionSample SampleMotion(float t)
    {
        if (motion.Count == 0 || length <= 0)
            return new MotionSample { time = t };

        if (!loop)
            return Interpolate(Mathf.Clamp(t, 0, length));

        int cycles = Mathf.FloorToInt(t / length);
        MotionSample result = new MotionSample();
        MotionSample cycle = motion[motion.Count - 1];
        for (int i = 0; i < cycles; i++)
            result = result.Then(cycle);

        result = result.Then(Interpolate(t - cycles * length));
        result.time = t;
        return result;
    }

    MotionSample Interpolate(float t)
    {
        int next = motion.FindIndex(sample => sample.time >= t);
        if (next <= 0)
            return next == 0 ? motion[0] : motion[motion.Count - 1];

        MotionSample a = motion[next - 1];
        MotionSample b = motion[next];
        float f = (t - a.time) / Mathf.Max(b.time - a.time, 1e-5f);

        return new MotionSample
        {
            time = t,
            right = Mathf.Lerp(a.right, b.right, f),
            forward = Mathf.Lerp(a.forward, b.forward, f),
            up = Mathf.Lerp(a.up, b.up, f),
            turn = Mathf.LerpAngle(a.turn, b.turn, f)
        };
    }
}

// Mixamo bakes its stair descents onto a flat floor: the feet stay at floor height while the body goes down,
// so the knees fold. Baked per clip: how far the hips sink relative to the root over one cycle, and how far
// each foot should be lowered at each moment (it goes down one cycle drop during its swing).
// MixamoAnimationPlayer lowers the root by the sink, raises the hips by the same amount and moves the feet down with IK.
[System.Serializable]
public class StairDescentFix
{
    public float interval;
    // Sink of the hips relative to the root since clip start; the last sample is the drop per cycle
    public float[] sink = new float[0];
    public float[] leftFootDrop = new float[0];
    public float[] rightFootDrop = new float[0];

    public bool IsValid { get { return sink != null && sink.Length > 1 && interval > 0; } }
    public float CycleDrop { get { return IsValid ? sink[sink.Length - 1] : 0; } }
    public float Length { get { return IsValid ? interval * (sink.Length - 1) : 0; } }

    // Sink accumulated over all cycles played so far; continuous across loops
    public float TotalSink(double time)
    {
        int cycles = Mathf.FloorToInt((float)(time / Length));
        return cycles * CycleDrop + Sample(sink, (float)(time - cycles * Length));
    }

    public float SinkAt(double time) { return Sample(sink, CycleTime(time)); }
    public float LeftFootDropAt(double time) { return Sample(leftFootDrop, CycleTime(time)); }
    public float RightFootDropAt(double time) { return Sample(rightFootDrop, CycleTime(time)); }

    float CycleTime(double time)
    {
        return Mathf.Repeat((float)time, Length);
    }

    float Sample(float[] values, float time)
    {
        float index = Mathf.Clamp(time / interval, 0, values.Length - 1);
        int i = Mathf.Min(Mathf.FloorToInt(index), values.Length - 2);
        return Mathf.Lerp(values[i], values[i + 1], index - i);
    }
}

// Root offset relative to a start pose: meters to the right / forward / up, degrees turned clockwise (seen from above)
[System.Serializable]
public struct MotionSample
{
    public float time;
    public float right;
    public float forward;
    public float up;
    public float turn;

    // This offset followed by another one, expressed in this offset's start frame
    public MotionSample Then(MotionSample next)
    {
        Vector3 offset = Quaternion.Euler(0, turn, 0) * new Vector3(next.right, 0, next.forward);
        return new MotionSample
        {
            time = time + next.time,
            right = right + offset.x,
            forward = forward + offset.z,
            up = up + next.up,
            turn = turn + next.turn
        };
    }
}
