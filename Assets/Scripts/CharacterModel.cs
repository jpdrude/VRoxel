using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;

public class CharacterModel : MonoBehaviour
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
    [SerializeField]
    AvatarDimensions bodyDimensions;

    [Space]
    [SerializeField]
    AvatarDimensions avatarDimensions = new AvatarDimensions();
    public AvatarDimensions NativeDimensions { get { return avatarDimensions; } }
    // The dimensions passed to InitializeAvatar
    public AvatarDimensions Dimensions { get { return bodyDimensions; } }

    [Space]
    [Header("Blend Shapes")]
    [SerializeField]
    SkinnedMeshRenderer bottomSkin;
    [SerializeField]
    SkinnedMeshRenderer bellySkin;
    [SerializeField]
    SkinnedMeshRenderer breastSkin;
    [SerializeField]
    SkinnedMeshRenderer chestSkinA;
    [SerializeField]
    SkinnedMeshRenderer chestSkinB;
    [SerializeField]
    SkinnedMeshRenderer neckSkinA;
    [SerializeField]
    SkinnedMeshRenderer neckSkinB;

    [Space]
    [Header("TrailBrushes")]
    [SerializeField]
    List<LineRenderer> trailBrushes;

    public bool StrokeTrails { get; private set; }
    public float StrokeLength { get; private set; }

    [Space]
    [Header("Setup")]
    [SerializeField]
    CustomAnimationPlayer animationPlayer;
    [SerializeField]
    List<Renderer> body;
    public List<Renderer> Body { get { return body; } }
    [SerializeField]
    List<Renderer> joints;
    public List<Renderer> Joints { get { return joints; } }

    public Vector3 HipPosition { get; private set; }

    float avatarSpineHipDelta;
    float hipLegDelta;
    float spineHipDelta;

    Vector3 hipScale;
    Vector3 upperLegScale;
    Vector3 lowerLegScale;
    Vector3 footScale;

    Vector3 backScale;
    Vector3 shoulderScale;
    Vector3 headScale;

    Vector3 upperArmScale;
    Vector3 foreArmScale;
    Vector3 handScale;

    Vector3 torsoNeutralization;

    Vector3 multipliedUpperLegScale;
    Vector3 multipliedLowerLegScale;

    Vector3 multipliedUpperArmScale;
    Vector3 multipliedForeArmScale;

    public CharacterColorSet CurrentColorSet { get; private set; }

    public void InitializeAvatar(AvatarDimensions dimensions, CharacterColorSet colorSet)
    {
        bodyDimensions = dimensions;

        ResetSkeleton();

        SetSkeletonDimensions();

        SetLegMultipliers();

        SetArmMultipliers();

        SetBodyMultipliers();

        // Humanoid playback ignores the rescaled legs when placing the hips, so the Mixamo player scales the body and its travel
        MixamoAnimationPlayer mixamoPlayer = GetComponent<MixamoAnimationPlayer>();
        if (mixamoPlayer != null)
            mixamoPlayer.UpdateBodyScale();

        SetColor(colorSet);
    }

    public void SetColor(CharacterColorSet colorSet)
    {
        foreach (Renderer renderer in body)
            renderer.material = colorSet.mainColor;

        foreach (Renderer renderer in joints)
            renderer.material = colorSet.secondaryColor;

        // Mixamo avatars are driven by MixamoAnimationPlayer and have no CustomAnimationPlayer
        if (animationPlayer != null)
            animationPlayer.FadeMaterial = colorSet.fadeColor;

        foreach (LineRenderer lr in trailBrushes)
            lr.material = colorSet.strokeColor;

        CurrentColorSet = colorSet;
    }

    public void SetTrails(bool trailsActive, float strokeLength)
    {
        if (strokeLength < 0) return;

        StrokeTrails = trailsActive;
        StrokeLength = strokeLength;

        foreach (LineRenderer lr in trailBrushes)
        {
            if (!trailsActive)
                lr.gameObject.SetActive(false);
            else
            {
                lr.gameObject.SetActive(true);
                lr.gameObject.GetComponent<AnimationTrailBrush>().StrokeLength = strokeLength;
            }
        }
    }

    void ResetSkeleton()
    {
        Hips.localScale = Vector3.one;

        RightUpLeg.localScale = Vector3.one;
        RightLeg.localScale = Vector3.one;
        RightFoot.localScale = Vector3.one;
        RightToeBase.localScale = Vector3.one;

        LeftUpLeg.localScale = Vector3.one;
        LeftLeg.localScale = Vector3.one;
        LeftFoot.localScale = Vector3.one;
        LeftToeBase.localScale = Vector3.one;

        Spine.localScale = Vector3.one;
        Spine1.localScale = Vector3.one;
        Spine2.localScale = Vector3.one;

        LeftShoulder.localScale = Vector3.one;
        LeftArm.localScale = Vector3.one;
        LeftForeArm.localScale = Vector3.one;
        LeftHand.localScale = Vector3.one;

        RightShoulder.localScale = Vector3.one;
        RightArm.localScale = Vector3.one;
        RightForeArm.localScale = Vector3.one;
        RightHand.localScale = Vector3.one;

        Neck.localScale = Vector3.one;
        Head.localScale = Vector3.one;
    }

    void SetSkeletonDimensions()
    {
        avatarSpineHipDelta = Vector3.Distance(Hips.position, Spine.position) * 100;

        hipScale = Vector3.one * (bodyDimensions.hipWidth / avatarDimensions.hipWidth);
        Hips.localScale = hipScale;

        Spine.localScale = Spine.localScale.Divide(hipScale);
        LeftUpLeg.localScale = LeftUpLeg.localScale.Divide(hipScale);
        RightUpLeg.localScale = RightUpLeg.localScale.Divide(hipScale);


        hipLegDelta = Mathf.Abs(Hips.position.y - LeftUpLeg.position.y) * 100;
        HipPosition = new Vector3(Hips.localPosition.x, (bodyDimensions.hipHeight + hipLegDelta + bodyDimensions.extraShowSoleThickness) / 100, Hips.localPosition.z);
        Hips.localPosition = HipPosition;


        upperLegScale = Vector3.one * ((bodyDimensions.hipHeight - bodyDimensions.kneeHeight) / (avatarDimensions.hipHeight - avatarDimensions.kneeHeight));
        LeftUpLeg.localScale = LeftUpLeg.localScale.Multiply(upperLegScale);
        RightUpLeg.localScale = RightUpLeg.localScale.Multiply(upperLegScale);

        LeftLeg.localScale = Vector3.one.Divide(upperLegScale);
        RightLeg.localScale = Vector3.one.Divide(upperLegScale);


        lowerLegScale = Vector3.one * ((bodyDimensions.kneeHeight - bodyDimensions.ankleHeight) / (avatarDimensions.kneeHeight - avatarDimensions.ankleHeight));
        LeftLeg.localScale = LeftLeg.localScale.Multiply(lowerLegScale);
        RightLeg.localScale = RightLeg.localScale.Multiply(lowerLegScale);

        LeftFoot.localScale = Vector3.one.Divide(lowerLegScale);
        RightFoot.localScale = Vector3.one.Divide(lowerLegScale);


        footScale = new Vector3(bodyDimensions.footLength / avatarDimensions.footLength, (bodyDimensions.ankleHeight + bodyDimensions.extraShowSoleThickness) / avatarDimensions.ankleHeight, bodyDimensions.footLength / avatarDimensions.footLength);
        LeftFoot.localScale = LeftFoot.localScale.Multiply(footScale);
        RightFoot.localScale = RightFoot.localScale.Multiply(footScale);


        spineHipDelta = Vector3.Distance(Hips.position, Spine.position) * 100;
        backScale = Vector3.one;
        backScale.y = (bodyDimensions.shoulderHeight - bodyDimensions.hipHeight - spineHipDelta) / (avatarDimensions.shoulderHeight - avatarDimensions.hipHeight - avatarSpineHipDelta);
        float lerpFactor = (backScale.y - 1) * 3;
        if (lerpFactor < 0)
            lerpFactor = 0;
        else if (lerpFactor > 1)
            lerpFactor = 1;
        backScale.x = Mathf.Lerp(backScale.y, 1, lerpFactor);
        backScale.z = Mathf.Lerp(backScale.y, 1, lerpFactor);
        Spine.localScale = Spine.localScale.Multiply(backScale);
        
        Vector3 shoulderReset = new Vector3(backScale.x, 1, 1);
        Spine2.localScale = Spine2.localScale.Divide(shoulderReset);

        
        shoulderScale = Vector3.one;
        shoulderScale.x = bodyDimensions.shoulderWidth / avatarDimensions.shoulderWidth;
        Spine2.localScale = Spine2.localScale.Multiply(shoulderScale);

        torsoNeutralization = backScale.Divide(shoulderReset).Multiply(shoulderScale);
        LeftArm.localScale = LeftArm.localScale.Divide(torsoNeutralization);
        RightArm.localScale = RightArm.localScale.Divide(torsoNeutralization);
        Neck.localScale = Neck.localScale.Divide(torsoNeutralization);


        headScale = Vector3.one * ((bodyDimensions.bodyHeight - bodyDimensions.shoulderHeight) / (avatarDimensions.bodyHeight - avatarDimensions.shoulderHeight));
        Neck.localScale = Neck.localScale.Multiply(headScale);


        upperArmScale = Vector3.one * (((bodyDimensions.elbowSpan - bodyDimensions.shoulderWidth) / 2) / ((avatarDimensions.elbowSpan - avatarDimensions.shoulderWidth) / 2));
        LeftArm.localScale = LeftArm.localScale.Multiply(upperArmScale);
        RightArm.localScale = RightArm.localScale.Multiply(upperArmScale);

        LeftForeArm.localScale = LeftForeArm.localScale.Divide(upperArmScale);
        RightForeArm.localScale = RightForeArm.localScale.Divide(upperArmScale);

        
        foreArmScale = Vector3.one * (((bodyDimensions.wristSpan - bodyDimensions.elbowSpan) / 2) / ((avatarDimensions.wristSpan - avatarDimensions.elbowSpan) / 2));
        LeftForeArm.localScale = LeftForeArm.localScale.Multiply(foreArmScale);
        RightForeArm.localScale = RightForeArm.localScale.Multiply(foreArmScale);

        LeftHand.localScale = LeftHand.localScale.Divide(foreArmScale);
        RightHand.localScale = RightHand.localScale.Divide(foreArmScale);


        handScale = Vector3.one * (((bodyDimensions.armSpan - bodyDimensions.wristSpan) / 2) / ((avatarDimensions.armSpan - avatarDimensions.wristSpan) / 2));
        LeftHand.localScale = LeftHand.localScale.Multiply(handScale);
        RightHand.localScale = RightHand.localScale.Multiply(handScale);
        
    }

    void SetLegMultipliers()
    {
        LeftUpLeg.localScale = Vector3.one.Divide(hipScale).Multiply(upperLegScale);
        multipliedUpperLegScale = new Vector3(upperLegScale.x * bodyDimensions.upperLegWidth, upperLegScale.y, upperLegScale.z * bodyDimensions.upperLegWidth);
        LeftUpLeg.localScale = new Vector3(LeftUpLeg.localScale.x * bodyDimensions.upperLegWidth, LeftUpLeg.localScale.y, LeftUpLeg.localScale.z * bodyDimensions.upperLegWidth);
        RightUpLeg.localScale = LeftUpLeg.localScale;

        LeftLeg.localScale = Vector3.one.Divide(multipliedUpperLegScale).Multiply(lowerLegScale);
        multipliedLowerLegScale = new Vector3(lowerLegScale.x * bodyDimensions.lowerLegWidth, lowerLegScale.y, lowerLegScale.z * bodyDimensions.lowerLegWidth);
        LeftLeg.localScale = new Vector3(LeftLeg.localScale.x * bodyDimensions.lowerLegWidth, LeftLeg.localScale.y, LeftLeg.localScale.z * bodyDimensions.lowerLegWidth);
        RightLeg.localScale = LeftLeg.localScale;

        LeftFoot.localScale = Vector3.one.Divide(multipliedLowerLegScale).Multiply(footScale);
        RightFoot.localScale = Vector3.one.Divide(multipliedLowerLegScale).Multiply(footScale);
    }

    void SetArmMultipliers()
    {
        LeftArm.localScale = Vector3.one.Divide(torsoNeutralization).Multiply(upperArmScale);
        multipliedUpperArmScale = new Vector3(upperArmScale.x, upperArmScale.y * bodyDimensions.upperArmWidth, upperArmScale.z * bodyDimensions.upperArmWidth);
        LeftArm.localScale = new Vector3(LeftArm.localScale.x, LeftArm.localScale.y * bodyDimensions.upperArmWidth, LeftArm.localScale.z * bodyDimensions.upperArmWidth);
        RightArm.localScale = LeftArm.localScale;

        LeftForeArm.localScale = Vector3.one.Divide(multipliedUpperArmScale).Multiply(foreArmScale);
        multipliedForeArmScale = new Vector3(foreArmScale.x, foreArmScale.y * bodyDimensions.foreArmWidth, foreArmScale.z * bodyDimensions.foreArmWidth);
        LeftForeArm.localScale = new Vector3(LeftForeArm.localScale.x, LeftForeArm.localScale.y * bodyDimensions.foreArmWidth, LeftForeArm.localScale.z * bodyDimensions.foreArmWidth);
        RightForeArm.localScale = LeftForeArm.localScale;

        LeftHand.localScale = Vector3.one.Divide(multipliedForeArmScale).Multiply(handScale);
        RightHand.localScale = Vector3.one.Divide(multipliedForeArmScale).Multiply(handScale);
    }

    void SetBodyMultipliers()
    {
        float bottomSize = bodyDimensions.bottomSize;
        if (bottomSize >= 0)
        {
            bottomSkin.SetBlendShapeWeight(0, bottomSize * 100);
            bottomSkin.SetBlendShapeWeight(1, 0);
        }
        else
        {
            bottomSize = Mathf.Abs(bottomSize) * 2;
            bottomSkin.SetBlendShapeWeight(0, 0);
            bottomSkin.SetBlendShapeWeight(1, bottomSize * 100);
        }

        float bellySize = bodyDimensions.bellySize;
        if (bellySize >= 0)
        {
            bellySkin.SetBlendShapeWeight(0, bellySize * 100);
            bellySkin.SetBlendShapeWeight(1, 0);
        }
        else
        {
            bellySize = Mathf.Abs(bellySize) * 2;
            bellySkin.SetBlendShapeWeight(0, 0);
            bellySkin.SetBlendShapeWeight(1, bellySize * 100);
        }

        float breastSize = bodyDimensions.breastSize;
        if (breastSize >= 0)
        {
            breastSkin.SetBlendShapeWeight(2, breastSize * 100);
            breastSkin.SetBlendShapeWeight(3, 0);
        }
        else
        {
            breastSize = Mathf.Abs(breastSize) * 2;
            breastSkin.SetBlendShapeWeight(2, 0);
            breastSkin.SetBlendShapeWeight(3, breastSize * 100);
        }

        float chestSize = bodyDimensions.chestSize;
        if (chestSize >= 0)
        {
            chestSkinA.SetBlendShapeWeight(0, chestSize * 100);
            chestSkinB.SetBlendShapeWeight(0, chestSize * 100);
            chestSkinA.SetBlendShapeWeight(1, 0);
            chestSkinB.SetBlendShapeWeight(1, 0);
        }
        else
        {
            chestSize = Mathf.Abs(chestSize) * 2;
            chestSkinA.SetBlendShapeWeight(0, 0);
            chestSkinB.SetBlendShapeWeight(0, 0);
            chestSkinA.SetBlendShapeWeight(1, chestSize * 100);
            chestSkinB.SetBlendShapeWeight(1, chestSize * 100);
        }

        float neckSize = bodyDimensions.neckSize;
        if (neckSize >= 0)
        {
            neckSkinA.SetBlendShapeWeight(0, neckSize * 100);
            neckSkinB.SetBlendShapeWeight(0, neckSize * 100);
            neckSkinA.SetBlendShapeWeight(1, 0);
            neckSkinB.SetBlendShapeWeight(1, 0);
        }
        else
        {
            neckSize = Mathf.Abs(neckSize) * 2;
            neckSkinA.SetBlendShapeWeight(0, 0);
            neckSkinB.SetBlendShapeWeight(0, 0);
            neckSkinA.SetBlendShapeWeight(1, neckSize * 100);
            neckSkinB.SetBlendShapeWeight(1, neckSize * 100);
        }
    }
}

[System.Serializable]
public class AvatarDimensions
{
    public float bodyHeight;
    public float footLength;

    [Space]
    public float shoulderHeight;
    public float shoulderWidth;
    public float elbowSpan;
    public float wristSpan;
    public float armSpan;

    [Space]
    public float hipHeight;
    public float hipWidth;
    public float kneeHeight;
    public float ankleHeight;
    public float extraShowSoleThickness;

    [Space]
    public float upperLegWidth = 1;
    public float lowerLegWidth = 1;
    public float upperArmWidth = 1;
    public float foreArmWidth = 1;

    [Space]
    public float bottomSize = 0;
    public float bellySize = 0;
    public float breastSize = 0;
    public float chestSize = 0;
    public float neckSize = 0;

    // Average proportions for the current bodyHeight
    public void SetProportionsFromBodyHeight()
    {
        footLength = bodyHeight * 0.14f;

        shoulderHeight = bodyHeight * 0.845f;
        shoulderWidth = bodyHeight * 0.229f;
        elbowSpan = bodyHeight * 0.521f;
        wristSpan = bodyHeight * 0.815f;
        armSpan = bodyHeight * 1.059f;

        hipHeight = bodyHeight * 0.54f;
        hipWidth = bodyHeight * 0.157f;
        kneeHeight = bodyHeight * 0.289f;
        ankleHeight = bodyHeight * 0.058f;
        extraShowSoleThickness = 0;
    }

}