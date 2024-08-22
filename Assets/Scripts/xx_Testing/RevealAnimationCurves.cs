using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RevealAnimationCurves : MonoBehaviour
{
    [Header("Hips")]
    public AnimationCurve hips_rotationX;
    public AnimationCurve hips_rotationY;
    public AnimationCurve hips_rotationZ;
    public AnimationCurve hips_rotationW;

    [Space]
    [Header("LeftUpLeg")]
    public AnimationCurve leftUpLeg_rotationX;
    public AnimationCurve leftUpLeg_rotationY;
    public AnimationCurve leftUpLeg_rotationZ;
    public AnimationCurve leftUpLeg_rotationW;

    [Header("LeftLeg")]
    public AnimationCurve leftLeg_rotationX;
    public AnimationCurve leftLeg_rotationY;
    public AnimationCurve leftLeg_rotationZ;
    public AnimationCurve leftLeg_rotationW;

    [Header("LeftFoot")]
    public AnimationCurve leftFoot_rotationX;
    public AnimationCurve leftFoot_rotationY;
    public AnimationCurve leftFoot_rotationZ;
    public AnimationCurve leftFoot_rotationW;

    [Header("LeftToeBase")]
    public AnimationCurve leftToeBase_rotationX;
    public AnimationCurve leftToeBase_rotationY;
    public AnimationCurve leftToeBase_rotationZ;
    public AnimationCurve leftToeBase_rotationW;

    [Space]
    [Header("RightUpLeg")]
    public AnimationCurve rightUpLeg_rotationX;
    public AnimationCurve rightUpLeg_rotationY;
    public AnimationCurve rightUpLeg_rotationZ;
    public AnimationCurve rightUpLeg_rotationW;

    [Header("LeftLeg")]
    public AnimationCurve rightLeg_rotationX;
    public AnimationCurve rightLeg_rotationY;
    public AnimationCurve rightLeg_rotationZ;
    public AnimationCurve rightLeg_rotationW;

    [Header("LeftFoot")]
    public AnimationCurve rightFoot_rotationX;
    public AnimationCurve rightFoot_rotationY;
    public AnimationCurve rightFoot_rotationZ;
    public AnimationCurve rightFoot_rotationW;

    [Header("LeftToeBase")]
    public AnimationCurve rightToeBase_rotationX;
    public AnimationCurve rightToeBase_rotationY;
    public AnimationCurve rightToeBase_rotationZ;
    public AnimationCurve rightToeBase_rotationW;

    [Space]
    [Header("Spine")]
    public AnimationCurve spine_rotationX;
    public AnimationCurve spine_rotationY;
    public AnimationCurve spine_rotationZ;
    public AnimationCurve spine_rotationW;

    [Header("Spine1")]
    public AnimationCurve spine1_rotationX;
    public AnimationCurve spine1_rotationY;
    public AnimationCurve spine1_rotationZ;
    public AnimationCurve spine1_rotationW;

    [Header("Spine2")]
    public AnimationCurve spine2_rotationX;
    public AnimationCurve spine2_rotationY;
    public AnimationCurve spine2_rotationZ;
    public AnimationCurve spine2_rotationW;

    [Header("Spine3")]
    public AnimationCurve spine3_rotationX;
    public AnimationCurve spine3_rotationY;
    public AnimationCurve spine3_rotationZ;
    public AnimationCurve spine3_rotationW;

    [Space]
    [Header("LeftShoulder")]
    public AnimationCurve leftShoulder_rotationX;
    public AnimationCurve leftShoulder_rotationY;
    public AnimationCurve leftShoulder_rotationZ;
    public AnimationCurve leftShoulder_rotationW;

    [Header("LeftArm")]
    public AnimationCurve leftArm_rotationX;
    public AnimationCurve leftArm_rotationY;
    public AnimationCurve leftArm_rotationZ;
    public AnimationCurve leftArm_rotationW;

    [Header("LeftForeArm")]
    public AnimationCurve leftForeArm_rotationX;
    public AnimationCurve leftForeArm_rotationY;
    public AnimationCurve leftForeArm_rotationZ;
    public AnimationCurve leftForeArm_rotationW;

    [Header("LeftArm")]
    public AnimationCurve leftHand_rotationX;
    public AnimationCurve leftHand_rotationY;
    public AnimationCurve leftHand_rotationZ;
    public AnimationCurve leftHand_rotationW;

    [Space]
    [Header("RightShoulder")]
    public AnimationCurve rightShoulder_rotationX;
    public AnimationCurve rightShoulder_rotationY;
    public AnimationCurve rightShoulder_rotationZ;
    public AnimationCurve rightShoulder_rotationW;

    [Header("LeftArm")]
    public AnimationCurve rightArm_rotationX;
    public AnimationCurve rightArm_rotationY;
    public AnimationCurve rightArm_rotationZ;
    public AnimationCurve rightArm_rotationW;

    [Header("LeftForeArm")]
    public AnimationCurve rightForeArm_rotationX;
    public AnimationCurve rightForeArm_rotationY;
    public AnimationCurve rightForeArm_rotationZ;
    public AnimationCurve rightForeArm_rotationW;

    [Header("LeftArm")]
    public AnimationCurve rightHand_rotationX;
    public AnimationCurve rightHand_rotationY;
    public AnimationCurve rightHand_rotationZ;
    public AnimationCurve rightHand_rotationW;

    [Space]
    [Header("Neck")]
    public AnimationCurve neck_rotationX;
    public AnimationCurve neck_rotationY;
    public AnimationCurve neck_rotationZ;
    public AnimationCurve neck_rotationW;

    [Header("Head")]
    public AnimationCurve head_rotationX;
    public AnimationCurve head_rotationY;
    public AnimationCurve head_rotationZ;
    public AnimationCurve head_rotationW;


    void Start()
    {
        Dictionary<string, Dictionary<string, AnimationCurve>> animation = GetComponent<CustomAnimationPlayer>().Animation;

        hips_rotationX = animation["Hips"]["localRotation.x"];
        hips_rotationY = animation["Hips"]["localRotation.y"];
        hips_rotationZ = animation["Hips"]["localRotation.z"];
        hips_rotationW = animation["Hips"]["localRotation.w"];


        leftUpLeg_rotationX = animation["LeftUpLeg"]["localRotation.x"];
        leftUpLeg_rotationY = animation["LeftUpLeg"]["localRotation.y"];
        leftUpLeg_rotationZ = animation["LeftUpLeg"]["localRotation.z"];
        leftUpLeg_rotationW = animation["LeftUpLeg"]["localRotation.w"];

        leftLeg_rotationX = animation["LeftLeg"]["localRotation.x"];
        leftLeg_rotationY = animation["LeftLeg"]["localRotation.y"];
        leftLeg_rotationZ = animation["LeftLeg"]["localRotation.z"];
        leftLeg_rotationW = animation["LeftLeg"]["localRotation.w"];

        leftFoot_rotationX = animation["LeftFoot"]["localRotation.x"];
        leftFoot_rotationY = animation["LeftFoot"]["localRotation.y"];
        leftFoot_rotationZ = animation["LeftFoot"]["localRotation.z"];
        leftFoot_rotationW = animation["LeftFoot"]["localRotation.w"];

        leftToeBase_rotationX = animation["LeftToeBase"]["localRotation.x"];
        leftToeBase_rotationY = animation["LeftToeBase"]["localRotation.y"];
        leftToeBase_rotationZ = animation["LeftToeBase"]["localRotation.z"];
        leftToeBase_rotationW = animation["LeftToeBase"]["localRotation.w"];


        rightUpLeg_rotationX = animation["RightUpLeg"]["localRotation.x"];
        rightUpLeg_rotationY = animation["RightUpLeg"]["localRotation.y"];
        rightUpLeg_rotationZ = animation["RightUpLeg"]["localRotation.z"];
        rightUpLeg_rotationW = animation["RightUpLeg"]["localRotation.w"];

        rightLeg_rotationX = animation["RightLeg"]["localRotation.x"];
        rightLeg_rotationY = animation["RightLeg"]["localRotation.y"];
        rightLeg_rotationZ = animation["RightLeg"]["localRotation.z"];
        rightLeg_rotationW = animation["RightLeg"]["localRotation.w"];

        rightFoot_rotationX = animation["RightFoot"]["localRotation.x"];
        rightFoot_rotationY = animation["RightFoot"]["localRotation.y"];
        rightFoot_rotationZ = animation["RightFoot"]["localRotation.z"];
        rightFoot_rotationW = animation["RightFoot"]["localRotation.w"];

        rightToeBase_rotationX = animation["RightToeBase"]["localRotation.x"];
        rightToeBase_rotationY = animation["RightToeBase"]["localRotation.y"];
        rightToeBase_rotationZ = animation["RightToeBase"]["localRotation.z"];
        rightToeBase_rotationW = animation["RightToeBase"]["localRotation.w"];


        spine_rotationX = animation["Spine"]["localRotation.x"];
        spine_rotationY = animation["Spine"]["localRotation.y"];
        spine_rotationZ = animation["Spine"]["localRotation.z"];
        spine_rotationW = animation["Spine"]["localRotation.w"];

        spine1_rotationX = animation["Spine1"]["localRotation.x"];
        spine1_rotationY = animation["Spine1"]["localRotation.y"];
        spine1_rotationZ = animation["Spine1"]["localRotation.z"];
        spine1_rotationW = animation["Spine1"]["localRotation.w"];

        spine2_rotationX = animation["Spine2"]["localRotation.x"];
        spine2_rotationY = animation["Spine2"]["localRotation.y"];
        spine2_rotationZ = animation["Spine2"]["localRotation.z"];
        spine2_rotationW = animation["Spine2"]["localRotation.w"];

        spine3_rotationX = animation["Spine3"]["localRotation.x"];
        spine3_rotationY = animation["Spine3"]["localRotation.y"];
        spine3_rotationZ = animation["Spine3"]["localRotation.z"];
        spine3_rotationW = animation["Spine3"]["localRotation.w"];


        leftShoulder_rotationX = animation["LeftShoulder"]["localRotation.x"];
        leftShoulder_rotationY = animation["LeftShoulder"]["localRotation.y"];
        leftShoulder_rotationZ = animation["LeftShoulder"]["localRotation.z"];
        leftShoulder_rotationW = animation["LeftShoulder"]["localRotation.w"];

        leftArm_rotationX = animation["LeftArm"]["localRotation.x"];
        leftArm_rotationY = animation["LeftArm"]["localRotation.y"];
        leftArm_rotationZ = animation["LeftArm"]["localRotation.z"];
        leftArm_rotationW = animation["LeftArm"]["localRotation.w"];

        leftForeArm_rotationX = animation["LeftForeArm"]["localRotation.x"];
        leftForeArm_rotationY = animation["LeftForeArm"]["localRotation.y"];
        leftForeArm_rotationZ = animation["LeftForeArm"]["localRotation.z"];
        leftForeArm_rotationW = animation["LeftForeArm"]["localRotation.w"];

        leftHand_rotationX = animation["LeftHand"]["localRotation.x"];
        leftHand_rotationY = animation["LeftHand"]["localRotation.y"];
        leftHand_rotationZ = animation["LeftHand"]["localRotation.z"];
        leftHand_rotationW = animation["LeftHand"]["localRotation.w"];


        rightShoulder_rotationX = animation["RightShoulder"]["localRotation.x"];
        rightShoulder_rotationY = animation["RightShoulder"]["localRotation.y"];
        rightShoulder_rotationZ = animation["RightShoulder"]["localRotation.z"];
        rightShoulder_rotationW = animation["RightShoulder"]["localRotation.w"];

        rightArm_rotationX = animation["RightArm"]["localRotation.x"];
        rightArm_rotationY = animation["RightArm"]["localRotation.y"];
        rightArm_rotationZ = animation["RightArm"]["localRotation.z"];
        rightArm_rotationW = animation["RightArm"]["localRotation.w"];

        rightForeArm_rotationX = animation["RightForeArm"]["localRotation.x"];
        rightForeArm_rotationY = animation["RightForeArm"]["localRotation.y"];
        rightForeArm_rotationZ = animation["RightForeArm"]["localRotation.z"];
        rightForeArm_rotationW = animation["RightForeArm"]["localRotation.w"];

        rightHand_rotationX = animation["RightHand"]["localRotation.x"];
        rightHand_rotationY = animation["RightHand"]["localRotation.y"];
        rightHand_rotationZ = animation["RightHand"]["localRotation.z"];
        rightHand_rotationW = animation["RightHand"]["localRotation.w"];

        neck_rotationX = animation["Neck"]["localRotation.x"];
        neck_rotationY = animation["Neck"]["localRotation.y"];
        neck_rotationZ = animation["Neck"]["localRotation.z"];
        neck_rotationW = animation["Neck"]["localRotation.w"];

        head_rotationX = animation["Head"]["localRotation.x"];
        head_rotationY = animation["Head"]["localRotation.y"];
        head_rotationZ = animation["Head"]["localRotation.z"];
        head_rotationW = animation["Head"]["localRotation.w"];
    }
}
