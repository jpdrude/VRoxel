using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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
    public bool PauseAnimationState { get { return pauseAnimation; } set { pauseAnimation = value; } }
    
    [Space]
    [Header("Trails")]
    [SerializeField]
    float startFade = 0.5f;
    bool trails = true;
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
                trails = false;
            }
            else if (fadeSteps > 25 && fadeTime > 0.1f)
            {
                fadeSteps = 25;
                trails = true;
            }
            else if (fadeTime > 0.1f)
                trails = true;
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
                trails = false;
            }
            else if (fadeSteps > 0)
            {
                trails = true;
            }
        }
    }
    public Material FadeMaterial { get; set; }

    [Space]
    [Header("Trim Animation")]
    [SerializeField]
    float animationStart = 0;
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
    [Header("Animation Spped")]
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
    [Header("Character Model")]
    [SerializeField]
    CharacterModel characterModel;

    Dictionary<string, Dictionary<string, AnimationCurve>> animation = null;
    float frameRate = 0;

    float timer = 0;
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
    }

    public void SetTrails(bool _trails, int _fadeSteps, float _fadeTime)
    {
        trails = _trails;
        fadeSteps = _fadeSteps;
        fadeTime = _fadeTime;
    }

    public void PauseAnimation()
    {
        pauseAnimation = true;
    }

    public void ResumeAnimation()
    {
        pauseAnimation = false;
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

    private void Update()
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

        timer += Time.deltaTime * speed;
        trailTimer += Time.deltaTime;


        if (timer > animationEnd)
            timer = animationStart;
    }

    public void CheckTrails()
    {
        if (!trails) return;

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
        transform.position = (rot * pos) + positionOffset;
        transform.rotation = rot;

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
}
