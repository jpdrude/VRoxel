using System.Collections.Generic;
using UnityEngine;

// Predicts where an avatar is after each step of a sequence, from the root motion baked into the library.
// Mirrors AvatarSequencer's step timing; walk_to steps are approximated as a straight walk at the clip's speed,
// and heights only follow the floor at step ends (a wall or gap mid-step is not detected).
public static class SequencePlanner
{
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

    public static Plan Simulate(SequenceState state, AnimationLibrary library)
    {
        Plan plan = new Plan();
        Vector3 position = state.anchorPosition;
        float yaw = state.anchorYaw;
        float time = 0;
        bool grounded = GroundProbe.IsOnFloor(position);

        foreach (SequenceStep step in state.steps)
        {
            AnimationLibraryEntry entry = library.Get(step.clip.name);
            StepPlan stepPlan = new StepPlan { animation = step.clip.name, startTime = time, startPosition = position, startYaw = yaw };

            if (step.hasTarget)
            {
                Vector3 toTarget = step.target - position;
                toTarget.y = 0;

                float speed = Mathf.Max((entry != null ? entry.averageSpeed : 0) * step.speed, 0.1f);
                float walkTime = toTarget.magnitude / speed;
                time += walkTime;

                if (toTarget.magnitude > 0.001f)
                    yaw = Quaternion.LookRotation(toTarget).eulerAngles.y;

                // Walking up or down stairs: the clip's climb rate over the walk; the target's y is ignored
                float climb = step.vertical && entry != null ? entry.SampleMotion(walkTime * step.speed).up : 0;
                position = new Vector3(step.target.x, position.y + climb, step.target.z);

                if (step.vertical)
                    grounded = false;
            }
            else
            {
                if (step.vertical)
                    grounded = false;

                float clipTime = ClipTime(step);
                time += step.duration > 0 ? step.duration : (step.speed > 0 ? clipTime / step.speed : 0);

                if (entry != null)
                {
                    MotionSample motion = entry.SampleMotion(clipTime);
                    position += Quaternion.Euler(0, yaw, 0) * new Vector3(motion.right, 0, motion.forward);
                    // Height only changes for climbing clips; the others bake it into the pose
                    if (step.vertical)
                        position.y += motion.up;
                    yaw += motion.turn;
                }

                // Turning toward `face` overrides the clip's own rotation by the end of the step
                if (step.hasFacing)
                    yaw = step.facing;
            }

            // Same floor following as AvatarSequencer: airborne avatars land only where their feet meet a floor
            if (!step.vertical)
            {
                if (!grounded)
                    grounded = GroundProbe.IsOnFloor(position);

                float? ground = grounded ? GroundProbe.FindStandingHeight(position) : null;
                if (ground.HasValue)
                    position.y = ground.Value;
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

    // Clip time a step plays, matching AvatarSequencer's end condition
    static float ClipTime(SequenceStep step)
    {
        if (step.duration > 0)
            return step.duration * step.speed;

        return step.clip.length * (step.clip.isLooping ? Mathf.Max(1, step.loops) : 1);
    }
}
