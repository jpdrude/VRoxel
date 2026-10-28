using System.Collections.Generic;
using UnityEngine;

// Plays a list of clips on a Mixamo avatar, starting from an anchor pose so the sequence can be replayed.
// Steps can walk to a target (steering the root-motion clip) or turn to a facing direction.
// Grounded avatars follow real floors; climbing clips lift them off, and they stay at their height
// (e.g. on stairs that don't exist) until their feet reach a real floor again.
[RequireComponent(typeof(MixamoAnimationPlayer))]
public class AvatarSequencer : MonoBehaviour
{
    [SerializeField]
    float turnSpeed = 180;
    [SerializeField]
    float arriveRadius = 0.2f;
    [Tooltip("Keep grounded avatars on floors found by GroundProbe")]
    [SerializeField]
    bool followGround = true;

    MixamoAnimationPlayer player;

    List<SequenceStep> steps = new List<SequenceStep>();
    SequenceEnd end = SequenceEnd.Hold;
    int stepIndex = -1;
    float stepTime;
    float stepTimeout;

    public Vector3 AnchorPosition { get; private set; }
    public float AnchorYaw { get; private set; }
    public int StepIndex { get { return stepIndex; } }
    public bool IsPlaying { get { return stepIndex >= 0 && stepIndex < steps.Count; } }
    public bool Grounded { get; private set; } = true;

    void Awake()
    {
        player = GetComponent<MixamoAnimationPlayer>();
        player.Evaluated += Tick;

        SetAnchor(transform.position, transform.eulerAngles.y);
        Grounded = GroundProbe.IsOnFloor(transform.position);
    }

    void OnDestroy()
    {
        if (player != null)
            player.Evaluated -= Tick;
    }

    public void SetAnchor(Vector3 position, float yaw)
    {
        AnchorPosition = position;
        AnchorYaw = yaw;
    }

    public SequenceState GetState()
    {
        return new SequenceState
        {
            steps = new List<SequenceStep>(steps),
            end = end,
            anchorPosition = AnchorPosition,
            anchorYaw = AnchorYaw
        };
    }

    // Moves the avatar to the state's anchor and plays its steps from the start.
    // A fade only makes sense when the anchor is the current pose; after a jump the avatar should cut.
    public void Play(SequenceState state, float firstFade = 0)
    {
        steps = new List<SequenceStep>(state.steps);
        end = state.end;
        SetAnchor(state.anchorPosition, state.anchorYaw);

        Restart(firstFade);
    }

    public void Restart(float firstFade = 0)
    {
        MoveToAnchor();

        if (steps.Count == 0)
        {
            stepIndex = -1;
            player.Stop();
            return;
        }

        BeginStep(0, firstFade);
    }

    void MoveToAnchor()
    {
        transform.SetPositionAndRotation(AnchorPosition, Quaternion.Euler(0, AnchorYaw, 0));

        // An anchor in the air (start of a climb, a floor that doesn't exist) stays in the air
        Grounded = GroundProbe.IsOnFloor(AnchorPosition);
    }

    void BeginStep(int index, float fade)
    {
        stepIndex = index;
        stepTime = 0;

        SequenceStep step = steps[index];
        player.Speed = step.speed;
        player.Play(step.clip, fade);
        player.PauseAnimation(false);

        if (step.vertical)
            Grounded = false;

        // Walking steps end on arrival; the timeout catches targets the clip can't reach (blocked, circling)
        if (step.hasTarget)
        {
            float distance = FlatDistance(transform.position, step.target);
            float clipSpeed = Mathf.Max(step.travelSpeed * step.speed, 0.1f);
            stepTimeout = distance / clipSpeed * 2 + 5;
        }
    }

    void Tick(float deltaTime)
    {
        if (followGround)
            FollowGround();

        if (!IsPlaying) return;

        SequenceStep step = steps[stepIndex];
        stepTime += deltaTime;

        bool finished;
        if (step.hasTarget)
            finished = SteerToTarget(step, deltaTime) || stepTime >= stepTimeout;
        else
        {
            if (step.hasFacing)
                TurnTowards(step.facing, deltaTime);

            if (step.duration > 0)
                finished = stepTime >= step.duration;
            else
                finished = player.Timer >= step.clip.length * (step.clip.isLooping ? Mathf.Max(1, step.loops) : 1);
        }

        if (!finished) return;

        int next = stepIndex + 1;
        if (next < steps.Count)
            BeginStep(next, steps[next].fade >= 0 ? steps[next].fade : player.CrossfadeDuration);
        else if (end == SequenceEnd.Loop)
            Restart();
        else if (end == SequenceEnd.Return)
        {
            // Back at the start, showing the first frame, until the sequence is restarted
            MoveToAnchor();
            player.ShowPose(steps[0].clip, 0);
            stepIndex = steps.Count;
        }
        else
            // The last clip keeps looping or holds its final frame
            stepIndex = steps.Count;
    }

    void FollowGround()
    {
        SequenceStep step = IsPlaying ? steps[stepIndex] : null;

        // Climbing clips own the height; after them the avatar lands only where its feet meet a real floor
        if (step != null && step.vertical)
            return;

        if (!Grounded)
        {
            Grounded = GroundProbe.IsOnFloor(transform.position);
            if (!Grounded) return;
        }

        float? ground = GroundProbe.FindStandingHeight(transform.position);
        if (ground.HasValue)
            transform.position = new Vector3(transform.position.x, ground.Value, transform.position.z);
    }

    bool SteerToTarget(SequenceStep step, float deltaTime)
    {
        Vector3 toTarget = step.target - transform.position;
        toTarget.y = 0;

        if (toTarget.magnitude <= arriveRadius)
            return true;

        TurnTowards(Quaternion.LookRotation(toTarget).eulerAngles.y, deltaTime);
        return false;
    }

    void TurnTowards(float yaw, float deltaTime)
    {
        Quaternion target = Quaternion.Euler(0, yaw, 0);
        transform.rotation = Quaternion.RotateTowards(transform.rotation, target, turnSpeed * deltaTime);
    }

    static float FlatDistance(Vector3 a, Vector3 b)
    {
        a.y = 0;
        b.y = 0;
        return Vector3.Distance(a, b);
    }
}

public enum SequenceEnd
{
    // The last clip keeps looping or holds its final frame
    Hold,
    // Start over from the anchor
    Loop,
    // Jump back to the anchor and wait there
    Return
}

[System.Serializable]
public class SequenceStep
{
    public AnimationClip clip;
    // Seconds; 0 = play the clip once, or `loops` times for looping clips
    public float duration;
    public int loops = 1;
    // Crossfade into this step in seconds; negative = the player's default
    public float fade = -1;
    public float speed = 1;
    // Library travel speed in m/s at speed 1 (AnimationLibraryEntry.averageSpeed), for the walk_to timeout
    public float travelSpeed;
    // The clip climbs or descends (AnimationLibraryEntry.vertical), so floor following pauses
    public bool vertical;

    public bool hasTarget;
    public Vector3 target;

    public bool hasFacing;
    public float facing;
}

public class SequenceState
{
    public List<SequenceStep> steps = new List<SequenceStep>();
    public SequenceEnd end = SequenceEnd.Hold;
    public Vector3 anchorPosition;
    public float anchorYaw;
}
