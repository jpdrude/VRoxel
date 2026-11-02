using System.Collections.Generic;
using UnityEngine;

// Predicts where an avatar is after each step of a sequence by replaying it frame by frame the way
// MixamoAnimationPlayer and AvatarSequencer do: crossfade weights, root motion blended from the library's baked
// trajectories, floor following, steering toward walk_to targets and the steps' end conditions.
// It runs at a fixed frame rate, so it can differ slightly from the real frame times.
public static class SequencePlanner
{
    const float FrameTime = 1f / 30;
    // Plans stop here, e.g. for a sequence that walks against a target it never reaches
    const float MaxDuration = 900;

    public class StepPlan
    {
        public string animation;
        public float startTime;
        public float endTime;
        public Vector3 startPosition;
        public float startYaw;
        public Vector3 endPosition;
        public float endYaw;
        public bool grounded;
    }

    public class Plan
    {
        public List<StepPlan> steps = new List<StepPlan>();
        public float duration;
        public Vector3 endPosition;
        public float endYaw;
    }

    // A clip playing in the simulated mixer
    class ClipState
    {
        public AnimationLibraryEntry entry;
        public AnimationClip clip;
        public float time;
        public float lastTime;
    }

    // bodyScale is the avatar's MixamoAnimationPlayer.BodyScale: travel of non-climbing clips scales with its legs
    public static Plan Simulate(SequenceState state, AnimationLibrary library, float bodyScale = 1)
    {
        Plan plan = new Plan();
        Vector3 position = state.anchorPosition;
        float yaw = state.anchorYaw;
        float time = 0;
        bool grounded = GroundProbe.IsOnFloor(position);

        ClipState current = null;
        ClipState previous = null;
        float fadeTimer = 0;
        float fadeDuration = 0;

        for (int index = 0; index < state.steps.Count && time < MaxDuration; index++)
        {
            SequenceStep step = state.steps[index];
            StepPlan stepPlan = new StepPlan { animation = step.clip.name, startTime = time, startPosition = position, startYaw = yaw };

            // BeginStep: the previous clip fades out over this step's fade; the first step cuts in
            float fade = index == 0 ? 0 : (step.fade >= 0 ? step.fade : MixamoAnimationPlayer.DefaultCrossfadeDuration);
            AnimationLibraryEntry entry = library.Get(step.clip.name);
            if (current != null)
                fade = AnimationLibraryEntry.Crossfade(current.entry, current.time, entry, fade);
            previous = current;
            current = new ClipState { entry = entry, clip = step.clip };
            fadeDuration = previous != null ? fade : 0;
            fadeTimer = 0;

            if (step.vertical)
                grounded = false;

            float stepTime = 0;
            float timeout = step.hasTarget
                ? FlatDistance(position, step.target) / Mathf.Max(step.travelSpeed * (step.vertical ? 1 : bodyScale) * step.speed, 0.1f) * 2 + 5
                : 0;

            while (time < MaxDuration)
            {
                // MixamoAnimationPlayer.Update: the whole mixer advances at the current step's speed
                float deltaTime = FrameTime * step.speed;

                float weight = 1;
                if (previous != null)
                {
                    fadeTimer += deltaTime;
                    weight = fadeDuration > 0 ? Mathf.Clamp01(fadeTimer / fadeDuration) : 1;
                    if (fadeTimer >= fadeDuration)
                        previous = null;
                }

                current.time += deltaTime;
                if (previous != null)
                    previous.time += deltaTime;

                Vector3 move = Vector3.zero;
                float turn = 0;
                AddRootMotion(current, weight, bodyScale, ref move, ref turn);
                AddRootMotion(previous, 1 - weight, bodyScale, ref move, ref turn);
                position += Quaternion.Euler(0, yaw, 0) * move;
                yaw += turn;

                // AvatarSequencer.Tick: floor following, then the step's steering and end condition
                time += FrameTime;
                stepTime += FrameTime;

                if (!step.vertical)
                {
                    if (!grounded)
                        grounded = GroundProbe.HasLanded(position);

                    float? ground = grounded ? GroundProbe.FindStandingHeight(position) : null;
                    if (ground.HasValue)
                        position.y = ground.Value;
                }

                bool finished;
                if (step.hasTarget)
                {
                    Vector3 toTarget = step.target - position;
                    toTarget.y = 0;

                    finished = toTarget.magnitude <= AvatarSequencer.DefaultArriveRadius;
                    if (!finished)
                        yaw = TurnTowards(yaw, Quaternion.LookRotation(toTarget).eulerAngles.y);

                    finished |= stepTime >= timeout;
                }
                else
                {
                    if (step.hasFacing)
                        yaw = TurnTowards(yaw, step.facing);

                    finished = step.duration > 0
                        ? stepTime >= step.duration
                        : current.time >= step.clip.length * (step.clip.isLooping ? Mathf.Max(1, step.loops) : 1);
                }

                if (finished)
                    break;
            }

            yaw = Mathf.Repeat(yaw, 360);
            stepPlan.endTime = time;
            stepPlan.endPosition = position;
            stepPlan.endYaw = yaw;
            stepPlan.grounded = grounded;
            plan.steps.Add(stepPlan);
        }

        plan.duration = time;
        plan.endPosition = position;
        plan.endYaw = yaw;
        return plan;
    }

    // Same as MixamoAnimationPlayer.AddRootMotion: the clip's baked motion since the last frame, weighted
    static void AddRootMotion(ClipState state, float weight, float bodyScale, ref Vector3 move, ref float turn)
    {
        if (state == null || state.entry == null) return;

        MotionSample delta = state.entry.MotionBetween(state.lastTime, state.time);
        state.lastTime = state.time;

        float scale = state.entry.vertical ? 1 : bodyScale;
        move += new Vector3(delta.right * scale, delta.up, delta.forward * scale) * weight;
        turn += delta.turn * weight;
    }

    static float TurnTowards(float yaw, float targetYaw)
    {
        return Mathf.MoveTowardsAngle(yaw, targetYaw, AvatarSequencer.DefaultTurnSpeed * FrameTime);
    }

    static float FlatDistance(Vector3 a, Vector3 b)
    {
        a.y = 0;
        b.y = 0;
        return Vector3.Distance(a, b);
    }
}
