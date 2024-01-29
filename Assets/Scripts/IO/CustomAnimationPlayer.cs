using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class CustomAnimationPlayer : MonoBehaviour
{
    [Header("Bone Mapping")]
    [SerializeField] 
    Transform Hips;

    [Space]
    [SerializeField] 
    Transform RightUpLeg;
    [SerializeField] 
    Transform RightLeg;
    [SerializeField]
    Transform RightFoot;
    [SerializeField]
    Transform RightToeBase;

    [Space]
    [SerializeField]
    Transform LeftUpLeg;
    [SerializeField]
    Transform LeftLeg;
    [SerializeField]
    Transform LeftFoot;
    [SerializeField]
    Transform LeftToeBase;

    [Space]
    [SerializeField]
    Transform Spine;
    [SerializeField]
    Transform Spine1;
    [SerializeField]
    Transform Spine2;

    [Space]
    [SerializeField]
    Transform LeftShoulder;
    [SerializeField]
    Transform LeftArm;
    [SerializeField]
    Transform LeftForeArm;
    [SerializeField]
    Transform LeftHand;

    [Space]
    [SerializeField]
    Transform RightShoulder;
    [SerializeField]
    Transform RightArm;
    [SerializeField]
    Transform RightForeArm;
    [SerializeField]
    Transform RightHand;

    [Space]
    [SerializeField]
    Transform Neck;
    [SerializeField]
    Transform Head;


    [Space]
    [Header("Offset")]
    [SerializeField]
    Vector3 positionOffset;
    public Vector3 PositionOffset { get { return positionOffset; } set { positionOffset = value; } }
    [SerializeField]
    float rotationOffset;
    public float RotationOffset { get { return rotationOffset; } set { rotationOffset = value; } }
    [SerializeField]
    bool pauseAnimation;
    public bool PauseAnimationState { get { return pauseAnimation; } }
    
    [Space]
    [Header("Trails")]
    [SerializeField]
    float startFade = 0.5f;
    bool shadowTrails = true;
    public bool ShadowTrails { get { return shadowTrails; } set { shadowTrails = value; } }
    int fadeSteps = 5;
    public int FadeSteps
    {
        get { return fadeSteps; }
        set
        {
            fadeSteps = value;
            if (fadeSteps <= 0)
            {
                fadeSteps = 0;
                shadowTrails = false;
            }
            else if (fadeSteps > 25 && fadeTime > 0.1f)
            {
                fadeSteps = 25;
            }
        }
    }
    float fadeTime = 10;
    public float FadeTime
    {
        get { return fadeTime; }
        set
        {
            fadeTime = value;
            if (fadeTime < 0)
            {
                fadeTime = 0;
                shadowTrails = false;
            }
        }
    }
    public Material FadeMaterial { get; set; }

    [Space]
    [Header("Trim Animation")]
    [SerializeField]
    float animationStart;
    public float AnimationStart 
    { 
        get { return animationStart; } 
        set 
        {
            if (value < animation["Hips"]["localRotation.x"].length / frameRate)
            {
                animationStart = value;

                if (timer < animationStart)
                    timer = animationStart;
            }
            else if (value < 1.0f / frameRate)
                animationStart = 1.0f / frameRate;
        } 
    }
    [SerializeField]
    float animationEnd;
    public float AnimationEnd 
    { 
        get { return animationEnd; }
        set 
        { 
            animationEnd = value;

            if (animationEnd > animation["Hips"]["localRotation.x"].length / frameRate)
                animationEnd = animation["Hips"]["localRotation.x"].length / frameRate;

            if (timer >= animationEnd)
                timer = animationStart;
        } 
    }

    [Space]
    [Header("Animation Speed")]
    [SerializeField]
    float speed = 1;
    public float Speed
    {
        get { return speed; }
        set
        {
            speed = value;
            if (speed < 0)
                speed = 0;
            else if (speed > 10)
            {
                speed = 10;
            }
        }
    }
    [SerializeField]
    bool useDamping = false;
    [SerializeField]
    float dampingFactor = 0.5f;
    [Header("Character Model")]
    [SerializeField]
    CharacterModel characterModel;

    [Space]
    [Header("Events")]
    [SerializeField]
    UnityEvent OnLoopBegin = new UnityEvent();
    [SerializeField]
    UnityEvent<bool> OnPause = new UnityEvent<bool>();

    Dictionary<string, Dictionary<string, AnimationCurve>> animation = null;
    float frameRate = 0;

    float timer = 0.1f;
    public float Timer { get { return timer; } }
    float trailTimer = 0;

    float unitFactor = 1;
    public float UnitFactor { get { return unitFactor; } set { unitFactor = value; } }

    Queue<GameObject> trailObjects = new Queue<GameObject>();

    public void SetAnimation(Dictionary<string, Dictionary<string, AnimationCurve>> _animation, float _frameRate)
    {
        animation = _animation;
        frameRate = _frameRate;
        //Debug.Log("Curves Set");

        animationEnd = animation["Hips"]["localRotation.x"].length / frameRate;
        animationStart = 1.0f / frameRate;

        StraightenTangents();
    }

    public void SetTrails(bool _trails, int _fadeSteps, float _fadeTime)
    {
        shadowTrails = _trails;
        fadeSteps = _fadeSteps;
        fadeTime = _fadeTime;
    }

    public void PauseAnimation(bool state)
    {
        pauseAnimation = state;
        OnPause.Invoke(state);
    }


    public void SetOffset(Vector3 newPos, float angle)
    {
        rotationOffset = angle;
        positionOffset = Vector3.zero;

        SetRootTransform(timer);

        positionOffset = newPos - transform.position;

        SetRootTransform(timer);

        pauseAnimation = false;
    }

    protected virtual void Update()
    {
        if (animation == null)
        {
            Debug.LogError("Can't play animation. No curves given");
            return;
        }

        if (pauseAnimation)
        {
            return;
        }

        TransformJoints(timer);

        CheckTrails();

        if (!AnimationIO.constantAnimationTime)
        {
            timer += Time.deltaTime * speed;
            trailTimer += Time.deltaTime;
        }
        else
        {
            timer += 1f / AnimationIO.animationFrameRate * speed;
            trailTimer += 1f / AnimationIO.animationFrameRate;
        }

        if (timer > animationEnd)
        {
            timer = animationStart;
            OnLoopBegin.Invoke();
        }   
    }

    public void CheckTrails()
    {
        if (!shadowTrails) return;

        if (trailTimer < fadeTime / fadeSteps) return;

        GameObject trailObject = Instantiate(gameObject);
        CustomAnimationPlayer player = trailObject.GetComponent<CustomAnimationPlayer>();
        CharacterModel model = trailObject.GetComponent<CharacterModel>();
        player.UnitFactor = unitFactor;
        player.SetAnimation(animation, frameRate);
        player.TransformJoints(timer);
        var behaviour = trailObject.AddComponent<TrailBehaviour>();
        behaviour.Initialize(model.Body, model.Joints, FadeMaterial, startFade, fadeTime);
        Destroy(player);

        foreach (Collider collider in trailObject.GetComponentsInChildren<Collider>())
            Destroy(collider);

        trailTimer = 0;
    }

    public void TransformJoints(float time)
    {
        
        SetJointTransform(Hips, "Hips", time);

        SetJointTransform(RightUpLeg, "RightUpLeg", time);
        SetJointTransform(RightLeg, "RightLeg", time);
        SetJointTransform(RightFoot, "RightFoot", time);
        SetJointTransform(RightToeBase, "RightToeBase", time);

        SetJointTransform(LeftUpLeg, "LeftUpLeg", time);
        SetJointTransform(LeftLeg, "LeftLeg", time);
        SetJointTransform(LeftFoot, "LeftFoot", time);
        SetJointTransform(LeftToeBase, "LeftToeBase", time);

        SetJointTransform(Spine, "Spine", time);
        SetJointTransform(Spine1, "Spine1", time);
        SetJointTransform(Spine2, "Spine2", time);
        MoveJointTransform(Spine2, "Spine3", time);

        SetJointTransform(LeftShoulder, "LeftShoulder", time);
        SetJointTransform(LeftArm, "LeftArm", time);
        SetJointTransform(LeftForeArm, "LeftForeArm", time);
        SetJointTransform(LeftHand, "LeftHand", time);

        SetJointTransform(RightShoulder, "RightShoulder", time);
        SetJointTransform(RightArm, "RightArm", time);
        SetJointTransform(RightForeArm, "RightForeArm", time);
        SetJointTransform(RightHand, "RightHand", time);

        SetJointTransform(Neck, "Neck", time);
        SetJointTransform(Head, "Head", time);
        
        SetRootTransform(time);
    }

    void SetJointTransform(Transform jointTransform, string jointName, float time)
    {
        

        Quaternion localRot = new Quaternion();
        localRot.x = animation[jointName]["localRotation.x"].Evaluate(time);
        localRot.y = animation[jointName]["localRotation.y"].Evaluate(time);
        localRot.z = animation[jointName]["localRotation.z"].Evaluate(time);
        localRot.w = animation[jointName]["localRotation.w"].Evaluate(time);

        localRot.Normalize();

        if (useDamping)
            jointTransform.localRotation = Quaternion.Slerp(jointTransform.localRotation.normalized, localRot, dampingFactor);
        else
            jointTransform.localRotation = localRot;
    }

    void SetRootTransform(float time)
    {
        Vector3 pos = new Vector3();
        Quaternion rot = Quaternion.Euler(0, rotationOffset, 0);

        pos.x = animation["Hips"]["localPosition.x"].Evaluate(time) - animation["Hips"]["localPosition.x"].Evaluate(0);
        pos.y = animation["Hips"]["localPosition.y"].Evaluate(time) - animation["Hips"]["localPosition.y"].Evaluate(0);
        pos.z = animation["Hips"]["localPosition.z"].Evaluate(time) - animation["Hips"]["localPosition.z"].Evaluate(0);

        pos *= 100 * unitFactor;

        /*
        transform.position = rot * (pos + positionOffset);
        transform.rotation = rot;
        */
        if (useDamping)
        {
            transform.position = Vector3.Lerp(transform.position, (rot * pos) + positionOffset, dampingFactor);
            transform.rotation = Quaternion.Slerp(transform.rotation, rot, dampingFactor);
        }
        else
        {
            transform.position = (rot * pos) + positionOffset;
            transform.rotation = rot;
        }
    }

    void MoveJointTransform(Transform jointTransform, string jointName, float time)
    {
        Quaternion localRot = new Quaternion();
        localRot.x = animation[jointName]["localRotation.x"].Evaluate(time);
        localRot.y = animation[jointName]["localRotation.y"].Evaluate(time);
        localRot.z = animation[jointName]["localRotation.z"].Evaluate(time);
        localRot.w = animation[jointName]["localRotation.w"].Evaluate(time);

        jointTransform.Rotate(localRot.eulerAngles.x, localRot.eulerAngles.y, localRot.eulerAngles.z);
    }

    public (Transform rightUpLeg, Transform rightLeg, Transform rightFoot, Transform rightToesBase) GetRightLeg()
    {
        (Transform rightUpLeg, Transform rightLeg, Transform rightFoot, Transform rightToesBase) leg;

        leg.rightUpLeg = RightUpLeg;
        leg.rightLeg = RightLeg;
        leg.rightFoot = RightFoot;
        leg.rightToesBase = RightToeBase;

        return leg;
    }

    public (Transform leftUpLeg, Transform leftLeg, Transform leftFoot, Transform leftToesBase) GetLeftLeg()
    {
        (Transform leftUpLeg, Transform leftLeg, Transform leftFoot, Transform leftToesBase) leg;

        leg.leftUpLeg = LeftUpLeg;
        leg.leftLeg = LeftLeg;
        leg.leftFoot = LeftFoot;
        leg.leftToesBase = LeftToeBase;

        return leg;
    }

    public Transform GetHips()
    {
        return Hips;
    }

    public (Vector3 posOffset, float rotOffset) GetOffset()
    {
        return new(positionOffset, rotationOffset);
    }

    public float RestartAnimation()
    {
        timer = animationStart;
        OnLoopBegin.Invoke();

        return animationEnd - animationStart;
    }

    List<List<AnimationCurve>> ListCurves()
    {
        var animationCurves = new List<List<AnimationCurve>>();

        string[] joints = new string[] {"Hips", "RightUpLeg", "RightLeg", "RightFoot", "RightToeBase", "LeftUpLeg", "LeftLeg", "LeftFoot",
        "LeftToeBase", "Spine", "Spine1", "Spine2", "Spine3", "LeftShoulder", "LeftArm", "LeftForeArm", "LeftHand", "RightShoulder", "RightArm",
        "RightForeArm", "RightHand", "Neck", "Head"};

        foreach (string joint in joints)
        {
            var jointCurve = new List<AnimationCurve>();
            jointCurve.Add(animation[joint]["localRotation.x"]);
            jointCurve.Add(animation[joint]["localRotation.y"]);
            jointCurve.Add(animation[joint]["localRotation.z"]);
            jointCurve.Add(animation[joint]["localRotation.w"]);

            animationCurves.Add(jointCurve);
        }

        return animationCurves;
    }

    void StraightenTangents()
    {
        foreach (string joint in animation.Keys)
        {
            foreach (string identifier in animation[joint].Keys)
            {
                AnimationCurve curve = animation[joint][identifier];

                Keyframe[] keyframes = curve.keys;

                for (int i = 0; i < keyframes.Length; ++i)
                {
                    keyframes[i].inTangent = 0;
                    keyframes[i].outTangent = 0;
                }

                curve.keys = keyframes;
            }
        }
    }

    //Doesn't work great and is very slow!
    void SmoothenAnimationCurves()
    {
        foreach (string joint in animation.Keys)
        {
            int keyCount = animation[joint]["localRotation.x"].keys.Length;
            Quaternion[] smoothRot = new Quaternion[keyCount];

            for (int i = 0; i < keyCount; ++i)
            {
                float x = animation[joint]["localRotation.x"].keys[i].value;
                float y = animation[joint]["localRotation.y"].keys[i].value;
                float z = animation[joint]["localRotation.z"].keys[i].value;
                float w = animation[joint]["localRotation.w"].keys[i].value;

                smoothRot[i] = new Quaternion(x, y, z, w);
            }

            smoothRot = ApplySlerpSmoothing(smoothRot, 5);

            Keyframe[] curveX = animation[joint]["localRotation.x"].keys;
            Keyframe[] curveY = animation[joint]["localRotation.y"].keys;
            Keyframe[] curveZ = animation[joint]["localRotation.z"].keys;
            Keyframe[] curveW = animation[joint]["localRotation.w"].keys;


            for (int i = 0; i < keyCount; ++i)
            {
                curveX[i].value = smoothRot[i].x;
                curveX[i].inTangent = 0;
                curveX[i].outTangent = 0;

                curveY[i].value = smoothRot[i].y;
                curveY[i].inTangent = 0;
                curveY[i].outTangent = 0;

                curveZ[i].value = smoothRot[i].z;
                curveZ[i].inTangent = 0;
                curveZ[i].outTangent = 0;

                curveW[i].value = smoothRot[i].w;
                curveW[i].inTangent = 0;
                curveW[i].outTangent = 0;
            }

            animation[joint]["localRotation.x"].keys = curveX;
            animation[joint]["localRotation.y"].keys = curveY;
            animation[joint]["localRotation.z"].keys = curveZ;
            animation[joint]["localRotation.w"].keys = curveW;
        }
    }

    Quaternion[] ApplySlerpSmoothing(Quaternion[] quaternions, int windowSize)
    {
        Quaternion[] smoothedQuaternions = new Quaternion[quaternions.Length];

        for (int i = 0; i < quaternions.Length; i++)
        {
            int startIndex = Mathf.Max(0, i - windowSize / 2);
            int endIndex = Mathf.Min(quaternions.Length - 1, i + windowSize / 2);

            Quaternion averageQuaternion = Quaternion.identity;

            for (int j = startIndex; j <= endIndex; j++)
            {
                averageQuaternion = Quaternion.Slerp(averageQuaternion, quaternions[j], 1.0f / (endIndex - startIndex + 1));
            }

            smoothedQuaternions[i] = averageQuaternion;
        }

        return smoothedQuaternions;
    }

    //Jolts around sometimes!
    void SmoothenAnimationCurvesSimple()
    {
        foreach (string joint in animation.Keys)
        {
            foreach (string identifier in animation[joint].Keys)
            {
                AnimationCurve curve = animation[joint][identifier];

                Keyframe[] keyframes = curve.keys;
                float[] values = new float[keyframes.Length];

                for (int i = 0; i < keyframes.Length; ++i)
                {
                    values[i] = keyframes[i].value;
                }

                values = AverageCurveValues(values, 5);

                for (int i = 0; i < keyframes.Length; ++i)
                {
                    keyframes[i].value = values[i];
                    keyframes[i].inTangent = 0;
                    keyframes[i].outTangent = 0;
                }

                curve.keys = keyframes;

            }
        }
    }

    float[] AverageCurveValues(float[] values, int windowSize)
    {
        float[] smoothedAngles = new float[values.Length];

        for (int i = 0; i < values.Length; i++)
        {
            int startIndex = Mathf.Max(0, i - windowSize / 2);
            int endIndex = Mathf.Min(values.Length - 1, i + windowSize / 2);

            float sum = 0;
            for (int j = startIndex; j <= endIndex; j++)
            {
                sum += values[j];
            }

            smoothedAngles[i] = sum / (endIndex - startIndex + 1);
        }

        return smoothedAngles;
    }
}
