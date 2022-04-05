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
    [Header("Trail")]
    [SerializeField]
    float startFade = 0.5f;
    [SerializeField]
    Material fadeMaterial;

    [Space]
    [SerializeField]
    Renderer body;
    [SerializeField]
    Renderer joints;

    public static bool trails = true;
    public static int fadeSteps = 5;
    public static float fadeTime = 10;

    Dictionary<string, Dictionary<string, AnimationCurve>> animation = null;
    float frameRate = 0;

    float timer = 0;
    float trailTimer = 0;

    Queue<GameObject> trailObjects = new Queue<GameObject>();

    public void SetAnimation(Dictionary<string, Dictionary<string, AnimationCurve>> _animation, float _frameRate)
    {
        animation = _animation;
        frameRate = _frameRate;
        Debug.Log("Curves Set");
    }

    private void Update()
    {
        if (animation == null)
        {
            Debug.LogError("Can't play animation. No curves given");
            return;
        }

        TransformJoints(timer);

        CheckTrails();

        timer += Time.deltaTime;
        trailTimer += Time.deltaTime;


        if (timer > animation["Hips"]["localRotation.x"].length / frameRate)
            timer = 0;
    }

    public void CheckTrails()
    {
        if (!trails) return;

        if (trailTimer < fadeTime / fadeSteps) return;

        GameObject trailObject = Instantiate(gameObject);
        CustomAnimationPlayer player = trailObject.GetComponent<CustomAnimationPlayer>();
        player.SetAnimation(animation, frameRate);
        player.TransformJoints(timer);
        var behaviour = trailObject.AddComponent<TrailBehaviour>();
        behaviour.Initialize(player.body, player.joints, fadeMaterial, startFade, fadeTime);
        Destroy(player);

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

        SetRootPosition(time);
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

    void SetRootPosition(float time)
    {
        Vector3 pos = new Vector3();

        pos.x = animation["Hips"]["localPosition.x"].Evaluate(time) - animation["Hips"]["localPosition.x"].Evaluate(0);
        pos.y = animation["Hips"]["localPosition.y"].Evaluate(time) - animation["Hips"]["localPosition.y"].Evaluate(0);
        pos.z = animation["Hips"]["localPosition.z"].Evaluate(time) - animation["Hips"]["localPosition.z"].Evaluate(0);

        pos *= 100;

        transform.position = pos;
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
