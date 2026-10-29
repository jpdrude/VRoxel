using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

// Finds clip pairs whose crossfade would make the body jump, like the reversed lie-down did before pose offsets.
// Measures every library clip's first and last pose on the baked avatar (body relative to the root, including the
// clip's pose offset and stair fix), sorts the poses into groups (standing, sitting, lying...), and compares every
// pair where one clip ends in the group the next one starts in. Writes Reports~/TransitionReport.md and ClipPoses.csv.
public static class TransitionAudit
{
    const string ReportFolder = "Assets/Animation/Mixamo/Reports~/";
    const float SampleInterval = 1f / 30;
    // Root speed at a clip's start / end is measured over this window
    const float VelocityWindow = 0.1f;

    // Pair thresholds: body position (m) and facing (degrees)
    const float BodyJump = 0.08f;
    const float YawJump = 30;
    // A one-shot clip that starts or ends faster than this changes speed abruptly at the crossfade
    const float MovingSpeed = 0.3f;
    const float TurningRate = 60;

    // Poses whose measures overlap other groups' are named explicitly: on the ladder the thighs are raised like
    // kneeling or crouching, but the knees are off the floor
    static readonly (Regex clip, string frame, string group)[] namedPoses =
    {
        (new Regex("^vert_ladderClimb"), "both", "on ladder"),
        (new Regex("^vert_ladderStart"), "end", "on ladder"),
        (new Regex("^vert_ladderTopOut"), "start", "on ladder"),
    };

    public class Pose
    {
        // Relative to the root: x right, y up, z forward
        public Vector3 hips;
        public float head;
        public float lowestKnee;
        public float lowestFoot;
        // How far the hips are above the lower knee (the standing leg): about 0.45 m standing, 0.1 m sitting on a chair
        public float hipsAboveKnees;
        // Horizontal distance from the hips to the knees' midpoint: long when sitting on a chair
        public float kneeReach;
        // Body facing relative to the root, degrees clockwise
        public float yaw;
        // Angle of the hips-to-head line from vertical: 0 upright, 90 lying
        public float spineTilt;
        // Root speed in the root's frame (m/s) and turn rate (degrees/s)
        public Vector2 velocity;
        public float turnRate;
        public string group;
        public string reason;
    }

    class ClipPoses
    {
        public AnimationLibraryEntry entry;
        public Pose start;
        public Pose end;
    }

    [MenuItem("VRoxel/Check Animation Transitions")]
    public static void Run()
    {
        AnimationLibrary library = AssetDatabase.LoadAssetAtPath<AnimationLibrary>(AnimationLibraryBuilder.LibraryPath);
        GameObject avatarPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(AnimationLibraryBuilder.AvatarPath);
        if (library == null || avatarPrefab == null)
        {
            Debug.LogWarning("TransitionAudit: needs the animation library and " + AnimationLibraryBuilder.AvatarPath);
            return;
        }

        List<ClipPoses> clips = new List<ClipPoses>();
        try
        {
            List<AnimationLibraryEntry> entries = library.Entries.Where(entry => entry.clip != null).ToList();
            for (int i = 0; i < entries.Count; i++)
            {
                EditorUtility.DisplayProgressBar("Check Animation Transitions", entries[i].name, (float)i / entries.Count);
                clips.Add(Measure(avatarPrefab, entries[i]));
            }
        }
        finally
        {
            EditorUtility.ClearProgressBar();
        }

        foreach (ClipPoses clip in clips)
        {
            Classify(clip.start, clip.entry, "start");
            Classify(clip.end, clip.entry, "end");
        }

        Directory.CreateDirectory(ReportFolder);
        File.WriteAllText(ReportFolder + "ClipPoses.csv", Csv(clips));
        string report = Report(clips);
        File.WriteAllText(ReportFolder + "TransitionReport.md", report);

        Debug.Log("TransitionAudit: report written to " + ReportFolder + "TransitionReport.md");
    }

    // First and last pose of one clip, played from the start in small steps so the root follows its root motion.
    // A loop can be left at any moment of its cycle; its start pose stands for both ends.
    static ClipPoses Measure(GameObject avatarPrefab, AnimationLibraryEntry entry)
    {
        GameObject avatar = AnimationLibraryBuilder.BakeInstance(avatarPrefab);
        Animator animator = avatar.GetComponent<Animator>();
        animator.applyRootMotion = true;

        PlayableGraph graph = PlayableGraph.Create("TransitionAudit");
        try
        {
            graph.SetTimeUpdateMode(DirectorUpdateMode.Manual);
            AnimationPlayableOutput output = AnimationPlayableOutput.Create(graph, "Audit", animator);
            output.SetSourcePlayable(AnimationClipPlayable.Create(graph, entry.clip));
            graph.Play();
            graph.Evaluate(0);

            Pose start = Sample(avatar.transform, animator, entry, 0);
            if (entry.loop)
                return new ClipPoses { entry = entry, start = start, end = Sample(avatar.transform, animator, entry, 0) };

            float end = entry.clip.length - 1e-3f;
            for (float played = 0; played < end; played += SampleInterval)
                graph.Evaluate(Mathf.Min(SampleInterval, end - played));

            return new ClipPoses { entry = entry, start = start, end = Sample(avatar.transform, animator, entry, end) };
        }
        finally
        {
            graph.Destroy();
            Object.DestroyImmediate(avatar);
        }
    }

    static Pose Sample(Transform root, Animator animator, AnimationLibraryEntry entry, float time)
    {
        // The runtime turns the body about its hips by the clip's pose yaw, shifts it by its pose offset and raises it by
        // a stair descent's sink
        Vector3 offset = entry.PoseOffsetAt(time);
        Quaternion turn = Quaternion.Euler(0, entry.PoseYawAt(time), 0);
        Vector3 rawHips = root.InverseTransformPoint(animator.GetBoneTransform(HumanBodyBones.Hips).position);
        Vector3 lift = entry.stairFix != null && entry.stairFix.IsValid ? Vector3.up * entry.stairFix.SinkAt(time) : Vector3.zero;
        System.Func<HumanBodyBones, Vector3> local = bone => rawHips + turn * (root.InverseTransformPoint(animator.GetBoneTransform(bone).position) - rawHips) + offset;

        Vector3 hips = local(HumanBodyBones.Hips) + lift;
        Vector3 head = local(HumanBodyBones.Head) + lift;
        Vector3 leftHip = local(HumanBodyBones.LeftUpperLeg);
        Vector3 rightHip = local(HumanBodyBones.RightUpperLeg);
        Vector3 leftKnee = local(HumanBodyBones.LeftLowerLeg) + lift;
        Vector3 rightKnee = local(HumanBodyBones.RightLowerLeg) + lift;
        Vector3 leftFoot = local(HumanBodyBones.LeftFoot);
        Vector3 rightFoot = local(HumanBodyBones.RightFoot);

        Vector3 right = Vector3.ProjectOnPlane(rightHip - leftHip, Vector3.up);
        Vector3 forward = Vector3.Cross(right, Vector3.up);
        Vector3 knees = (leftKnee + rightKnee) / 2;

        // Root speed from the baked motion, over a short window inside the clip
        float from = Mathf.Clamp(time - VelocityWindow / 2, 0, Mathf.Max(0, entry.length - VelocityWindow));
        MotionSample delta = entry.MotionBetween(from, from + VelocityWindow);

        return new Pose
        {
            hips = hips,
            head = head.y,
            lowestKnee = Mathf.Min(leftKnee.y, rightKnee.y),
            lowestFoot = Mathf.Min(leftFoot.y, rightFoot.y),
            hipsAboveKnees = hips.y - Mathf.Min(leftKnee.y, rightKnee.y),
            kneeReach = Vector2.Distance(new Vector2(hips.x, hips.z), new Vector2(knees.x, knees.z)),
            yaw = forward.sqrMagnitude > 1e-6f ? Mathf.Atan2(forward.x, forward.z) * Mathf.Rad2Deg : 0,
            spineTilt = Vector3.Angle(head - hips, Vector3.up),
            velocity = new Vector2(delta.right, delta.forward) / VelocityWindow,
            turnRate = delta.turn / VelocityWindow
        };
    }

    // Groups from body measures on the 181 cm model, in this order:
    //   lying          spine more than 60° from vertical (bed: 91-96°)
    //   floor sitting  hips below 0.25 m (0.07 m)
    //   kneeling       a knee within 0.15 m of the floor (0.04-0.08 m), hips below 0.7 m (0.40 m)
    //   sitting        hips 0.5-0.8 m (0.57-0.68 m) and at most 0.25 m above the lower knee (0.04-0.16 m): thighs level
    //   standing       hips above 0.75 m (0.81-1.04 m) and at least 0.3 m above the lower knee (0.38-0.51 m)
    // Anything else is "other" with its measures, so it shows up in the report instead of being misfiled
    static void Classify(Pose pose, AnimationLibraryEntry entry, string frame)
    {
        foreach (var named in namedPoses)
            if (named.clip.IsMatch(entry.name) && (named.frame == "both" || named.frame == frame))
            {
                pose.group = named.group;
                pose.reason = "named";
                return;
            }

        if (pose.spineTilt > 60)
        {
            pose.group = "lying";
            pose.reason = "spine " + F(pose.spineTilt, "0") + "°";
        }
        else if (pose.hips.y < 0.25f)
        {
            pose.group = "floor sitting";
            pose.reason = "hips " + F(pose.hips.y) + " m";
        }
        else if (pose.lowestKnee < 0.15f && pose.hips.y < 0.7f)
        {
            pose.group = "kneeling";
            pose.reason = "knee " + F(pose.lowestKnee) + " m";
        }
        else if (pose.hips.y > 0.5f && pose.hips.y < 0.8f && pose.hipsAboveKnees < 0.25f)
        {
            pose.group = "sitting";
            pose.reason = "hips " + F(pose.hips.y) + " m, " + F(pose.hipsAboveKnees) + " m above the lower knee";
        }
        else if (pose.hips.y > 0.75f && pose.hipsAboveKnees > 0.3f)
        {
            pose.group = "standing";
            pose.reason = "hips " + F(pose.hips.y) + " m, " + F(pose.hipsAboveKnees) + " m above the lower knee";
        }
        else
        {
            pose.group = "other";
            pose.reason = "hips " + F(pose.hips.y) + " m, " + F(pose.hipsAboveKnees) + " m above the lower knee, knee " + F(pose.lowestKnee) +
                " m, spine " + F(pose.spineTilt, "0") + "°";
        }
    }

    static string Report(List<ClipPoses> clips)
    {
        StringBuilder md = new StringBuilder();
        md.AppendLine("# Transition report");
        md.AppendLine();
        md.AppendLine("Generated by VRoxel > Check Animation Transitions from " + clips.Count + " clips. Positions are the body (hips) relative to the");
        md.AppendLine("avatar's root, in meters (right, up, forward), including pose offsets and the stair fix. A transition is any pair");
        md.AppendLine("where the first clip ends in the pose group the second starts in.");
        md.AppendLine();

        List<string> groups = clips.SelectMany(c => new[] { c.start.group, c.end.group }).Distinct().OrderBy(g => g).ToList();
        Dictionary<string, Vector3> typical = groups.ToDictionary(g => g, g => Median(clips.SelectMany(c => new[] { c.start, c.end }).Where(p => p.group == g)));
        Dictionary<string, float> typicalYaw = groups.ToDictionary(g => g, g => MedianYaw(clips.SelectMany(c => new[] { c.start, c.end }).Where(p => p.group == g)));

        // ---- groups
        md.AppendLine("## Pose groups");
        md.AppendLine();
        md.AppendLine("| Group | Typical body position | Clips starting here | Clips ending here |");
        md.AppendLine("|---|---|---|---|");
        foreach (string g in groups)
            md.AppendLine("| " + g + " | " + V(typical[g]) + ", facing " + F(typicalYaw[g], "0") + "° | " +
                string.Join(", ", clips.Where(c => c.start.group == g).Select(c => c.entry.name)) + " | " +
                string.Join(", ", clips.Where(c => c.end.group == g).Select(c => c.entry.name)) + " |");
        md.AppendLine();

        md.AppendLine("<details><summary>Why each pose is in its group</summary>");
        md.AppendLine();
        foreach (ClipPoses c in clips)
            md.AppendLine("- " + c.entry.name + ": starts " + c.start.group + " (" + c.start.reason + ")" +
                (c.entry.loop ? ", loop" : ", ends " + c.end.group + " (" + c.end.reason + ")"));
        md.AppendLine();
        md.AppendLine("</details>");
        md.AppendLine();

        // ---- clips off their group's typical pose: the likely cause of a jump
        md.AppendLine("## Poses away from their group's typical pose");
        md.AppendLine();
        md.AppendLine("A crossfade between a clip listed here and an ordinary clip of the same group moves the body by about this much.");
        md.AppendLine();
        List<(float size, string line)> outliers = new List<(float, string)>();
        foreach (ClipPoses c in clips)
            foreach (var frame in c.entry.loop ? new[] { ("pose", c.start) } : new[] { ("start", c.start), ("end", c.end) })
            {
                Vector3 off = frame.Item2.hips - typical[frame.Item2.group];
                float yawOff = Mathf.DeltaAngle(typicalYaw[frame.Item2.group], frame.Item2.yaw);
                if (off.magnitude > BodyJump || Mathf.Abs(yawOff) > YawJump)
                    outliers.Add((off.magnitude + Mathf.Abs(yawOff) / 360,
                        "- " + c.entry.name + " " + frame.Item1 + " (" + frame.Item2.group + "): body " + V(off) + " = " + F(off.magnitude) + " m off, facing " +
                        F(yawOff, "+0;-0") + "°"));
            }
        foreach (var o in outliers.OrderByDescending(o => o.size))
            md.AppendLine(o.line);
        if (outliers.Count == 0)
            md.AppendLine("None.");
        md.AppendLine();

        // ---- pairs
        List<(float size, string line)> jumps = new List<(float, string)>();
        int pairs = 0;
        foreach (ClipPoses a in clips)
            foreach (ClipPoses b in clips)
            {
                if (a.end.group != b.start.group || (a == b && a.entry.loop)) continue;
                pairs++;

                float body = Vector3.Distance(a.end.hips, b.start.hips);
                float yaw = Mathf.Abs(Mathf.DeltaAngle(a.end.yaw, b.start.yaw));
                if (body > BodyJump || yaw > YawJump)
                    jumps.Add((body + yaw / 360, "| " + a.entry.name + " | " + b.entry.name + " | " + a.end.group + " | " + F(body) + " | " + F(yaw, "0") + " |"));
            }

        md.AppendLine("## Transitions with a body jump");
        md.AppendLine();
        md.AppendLine(jumps.Count + " of " + pairs + " transitions move the body more than " + F(BodyJump) + " m or turn it more than " + F(YawJump, "0") +
            "° during the crossfade. Most come from the poses above; the 150 largest:");
        md.AppendLine();
        md.AppendLine("| From | To | Group | Body (m) | Facing (°) |");
        md.AppendLine("|---|---|---|---|---|");
        foreach (var j in jumps.OrderByDescending(j => j.size).Take(150))
            md.AppendLine(j.line);
        md.AppendLine();

        // ---- dead ends
        md.AppendLine("## Pose groups without a way in or out");
        md.AppendLine();
        bool any = false;
        foreach (string g in groups)
        {
            bool reached = clips.Any(c => c.end.group == g && c.start.group != g);
            bool left = clips.Any(c => c.start.group == g && c.end.group != g);
            bool standing = g == "standing";
            if (!standing && (!reached || !left))
            {
                any = true;
                md.AppendLine("- **" + g + "**: " + (!reached ? "no clip leads into it from another group" : "") + (!reached && !left ? "; " : "") +
                    (!left ? "no clip leads out of it to another group" : ""));
            }
        }
        if (!any)
            md.AppendLine("None: every group can be reached and left.");
        md.AppendLine();

        // ---- speed
        md.AppendLine("## One-shot clips that start or end moving");
        md.AppendLine();
        md.AppendLine("Next to a still clip, these change speed abruptly at the crossfade (> " + F(MovingSpeed) + " m/s or > " + F(TurningRate, "0") + "°/s).");
        md.AppendLine("Expected for walking transitions (start walking ends at walking speed); worth a look for the others.");
        md.AppendLine();
        foreach (ClipPoses c in clips.Where(c => !c.entry.loop))
            foreach (var frame in new[] { ("starts", c.start), ("ends", c.end) })
                if (frame.Item2.velocity.magnitude > MovingSpeed || Mathf.Abs(frame.Item2.turnRate) > TurningRate)
                    md.AppendLine("- " + c.entry.name + " " + frame.Item1 + " at " + F(frame.Item2.velocity.magnitude) + " m/s, turning " +
                        F(frame.Item2.turnRate, "0") + "°/s");

        return md.ToString();
    }

    static Vector3 Median(IEnumerable<Pose> poses)
    {
        List<Vector3> list = poses.Select(p => p.hips).ToList();
        return new Vector3(Median(list.Select(v => v.x)), Median(list.Select(v => v.y)), Median(list.Select(v => v.z)));
    }

    static float MedianYaw(IEnumerable<Pose> poses)
    {
        return Median(poses.Select(p => p.yaw));
    }

    static float Median(IEnumerable<float> values)
    {
        List<float> sorted = values.OrderBy(v => v).ToList();
        return sorted.Count == 0 ? 0 : sorted[sorted.Count / 2];
    }

    static string F(float value, string format = "0.00")
    {
        return value.ToString(format, CultureInfo.InvariantCulture);
    }

    static string V(Vector3 v)
    {
        return "(" + F(v.x) + ", " + F(v.y) + ", " + F(v.z) + ")";
    }

    static string Csv(List<ClipPoses> clips)
    {
        StringBuilder csv = new StringBuilder("clip,loop,frame,group,hips_x,hips_y,hips_z,head_y,knee_y,foot_y,hips_above_knees,knee_reach,yaw,spine_tilt,v_right,v_forward,turn_rate\n");
        foreach (ClipPoses c in clips)
            foreach (var frame in new[] { ("start", c.start), ("end", c.end) })
            {
                Pose p = frame.Item2;
                csv.AppendLine(string.Join(",", new[] { c.entry.name, c.entry.loop ? "1" : "0", frame.Item1, p.group }
                    .Concat(new[] { p.hips.x, p.hips.y, p.hips.z, p.head, p.lowestKnee, p.lowestFoot, p.hipsAboveKnees, p.kneeReach, p.yaw, p.spineTilt, p.velocity.x, p.velocity.y, p.turnRate }
                        .Select(v => F(v, "0.000")))));
            }

        return csv.ToString();
    }
}
