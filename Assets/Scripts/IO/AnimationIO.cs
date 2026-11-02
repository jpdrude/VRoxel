using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TriLibCore;
using TriLibCore.Utils;
using TriLibCore.Extensions;
using TriLibCore.Interfaces;
using TriLibCore.Fbx;
using System;
using UnityEngine.Playables;
using UnityEngine.Animations;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using Valve.Newtonsoft.Json;

public class AnimationIO : MonoBehaviour
{
    static int testIndex = 0;

    [SerializeField]
    GameObject _avatar;
    static GameObject avatar;

    [SerializeField]
    GameObject _wheelchairAvatar;
    static GameObject wheelchairAvatar;

    static CharacterOptions characterOptions;

    [SerializeField]
    AssetLoaderOptions _options;
    static AssetLoaderOptions options;

    [SerializeField]
    GameObject _editAnimationMenu;
    static GameObject editAnimationMenu;

    [SerializeField]
    Transform _editAnimationParent;
    static Transform editAnimationParent;

    [Space]
    [Header("Mapping")]
    [SerializeField]
    AnimationMapping _xsensMapping;
    static AnimationMapping xSensMapping;

    [SerializeField]
    AnimationMapping _rokokoMapping;
    static AnimationMapping rokokoMapping;

    static DateTime importStep;
    static bool isImporting = false;

    // Recording: every frame advances animation by exactly 1 / animationFrameRate
    public static bool constantAnimationTime = false;
    // Character brushes: frames advance in real time, but at most 1 / animationFrameRate, so when brushing drops the
    // frame rate the animation slows down instead of skipping blocks
    public static bool cappedAnimationTime = false;
    public static int animationFrameRate = 30;

    // Animation time this frame advances by, for every player and trail
    public static float FrameTime
    {
        get
        {
            if (constantAnimationTime)
                return 1f / animationFrameRate;
            if (cappedAnimationTime)
                return Mathf.Min(Time.deltaTime, 1f / animationFrameRate);
            return Time.deltaTime;
        }
    }

    static WheelchairMovement wheelchairMovement;

    [Space]
    [Header("Rokoko Transforms")]
    [SerializeField]
    Vector3 _leftUpLegRotation = new Vector3(0, 90, 0);
    [SerializeField]
    Vector3 _leftLegRotation = new Vector3(0, 0, 0);
    [SerializeField]
    Vector3 _leftFootRotation = new Vector3(90, 90, 0);
    [SerializeField]
    Vector3 _leftToeBaseRotation = new Vector3(0, 0, 0);
    [SerializeField]
    Vector3 _rightUpLegRotation = new Vector3(0, -90, 0);
    [SerializeField]
    Vector3 _rightLegRotation = new Vector3(0, 0, 0);
    [SerializeField]
    Vector3 _rightFootRotation = new Vector3(90, -90, 0);
    [SerializeField]
    Vector3 _rightToeBaseRotation = new Vector3(0, 0, 0);
    [SerializeField]
    Vector3 _spineRotation = new Vector3(0, 0, 180);
    [SerializeField]
    Vector3 _spine1Rotation = new Vector3(0, 0, 0);
    [SerializeField]
    Vector3 _spine2Rotation = new Vector3(0, 0, 0);
    [SerializeField]
    Vector3 _spine3Rotation = new Vector3(0, 0, 0);
    [SerializeField]
    Vector3 _leftShoulderRotation = new Vector3(0, 0, 90);
    [SerializeField]
    Vector3 _leftArmRotation = new Vector3(0, 90, 0);
    [SerializeField]
    Vector3 _leftForeArmRotation = new Vector3(0, 0, 0);
    [SerializeField]
    Vector3 _leftHandRotation;
    [SerializeField]
    Vector3 _rightShoulderRotation = new Vector3(0, 0, -90);
    [SerializeField]
    Vector3 _rightArmRotation = new Vector3(0, -90, 0);
    [SerializeField]
    Vector3 _rightForeArmRotation = new Vector3(0, 0, 0);
    [SerializeField]
    Vector3 _rightHandRotation = new Vector3(0, 0, 0);
    [SerializeField]
    Vector3 _neckRotation = new Vector3(0, 0, 0);
    [SerializeField]
    Vector3 _headRotation = new Vector3(0, 0, 0);

    static Vector3 leftUpLegRotation;
    static Vector3 leftLegRotation;
    static Vector3 leftFootRotation;
    static Vector3 leftToeBaseRotation;
    static Vector3 rightUpLegRotation;
    static Vector3 rightLegRotation;
    static Vector3 rightFootRotation;
    static Vector3 rightToeBaseRotation;
    static Vector3 spineRotation;
    static Vector3 spine1Rotation;
    static Vector3 spine2Rotation;
    static Vector3 spine3Rotation;
    static Vector3 leftShoulderRotation;
    static Vector3 leftArmRotation;
    static Vector3 leftForeArmRotation;
    static Vector3 leftHandRotation;
    static Vector3 rightShoulderRotation;
    static Vector3 rightArmRotation;
    static Vector3 rightForeArmRotation;
    static Vector3 rightHandRotation;
    static Vector3 neckRotation;
    static Vector3 headRotation;

    private void Start()
    {
        options = _options;
        avatar = _avatar;
        wheelchairAvatar = _wheelchairAvatar;

        editAnimationMenu = _editAnimationMenu;
        editAnimationParent = _editAnimationParent;

        xSensMapping = _xsensMapping;
        rokokoMapping = _rokokoMapping;
    }

    private void Update()
    {
        leftUpLegRotation = _leftUpLegRotation;
        leftLegRotation = _leftLegRotation;
        leftFootRotation = _leftFootRotation;
        leftToeBaseRotation = _leftToeBaseRotation;
        rightUpLegRotation = _rightUpLegRotation;
        rightLegRotation = _rightLegRotation;
        rightFootRotation = _rightFootRotation;
        rightToeBaseRotation = _rightToeBaseRotation;
        spineRotation = _spineRotation;
        spine1Rotation = _spine1Rotation;
        spine2Rotation = _spine2Rotation;
        spine3Rotation = _spine3Rotation;
        leftShoulderRotation = _leftShoulderRotation;
        leftArmRotation = _leftArmRotation;
        leftForeArmRotation = _leftForeArmRotation;
        leftHandRotation = _leftHandRotation;
        rightShoulderRotation = _rightShoulderRotation;
        rightArmRotation = _rightArmRotation;
        rightForeArmRotation = _rightForeArmRotation;
        neckRotation = _neckRotation;
        headRotation = _headRotation;
    }

    public static void ImportMotionCapture(string path, CharacterOptions _characterOptions, bool wheelchair = false)
    {
        characterOptions = _characterOptions;


        if (isImporting)
        {
            MessageSystem.DisplayWarning("There is already another import in progress");
            return;
        }

        isImporting = true;

        importStep = DateTime.Now;

        if (wheelchair)
            AssetLoader.LoadModelFromFile(path, OnWheelchairMocapImportFinished, null, OnImportProgress, OnImportError, null, options);
        else
            AssetLoader.LoadModelFromFile(path, OnMocapImportFinished, null, OnImportProgress, OnImportError, null, options);

        //GetFBXStartTimeAsync(path);
    }

    static void GetFBXStartTimeAsync(string path)
    {
        GetFBXStartTimeDelegate getFBXDelegate = new GetFBXStartTimeDelegate(GetFBXStartTime);
        getFBXDelegate.BeginInvoke(path, null, null);
    }

    static void GetFBXStartTime(string path)
    {
        if (!File.Exists(path)) return;

        ulong kTimeValue = 0;

        using (var fileStream = File.OpenRead(path))
        using (var streamReader = new StreamReader(fileStream))
        {
            String line = streamReader.ReadLine();

            if (line[0] != ';' || !line.Contains("FBX")) return;

            while ((line = streamReader.ReadLine()) != null)
            {
                if (line.Length > 25 && line.Contains("\"LocalStart\"") && line.Contains("\"KTime\""))
                {
                    Match match = Regex.Match(line, @"\d+");
                    if (match.Success)
                    {
                        ulong number = ulong.Parse(match.Value);
                        if (number > kTimeValue)
                            kTimeValue = number;
                    }
                }
            }
        }

        float timeInSeconds = (float)(kTimeValue / 46186158000.0);
        TimeSpan time = TimeSpan.FromSeconds(timeInSeconds);
        Debug.Log("Start Time of longest Animation: " + time.ToString());
    }

    static void AddAnimation(AssetLoaderContext loadedAnimationContext, bool wheelchair = false)
    {
        var loadAnimations = loadedAnimationContext.RootModel.AllAnimations;
        Dictionary<string, Dictionary<string, AnimationCurve>> animation = new Dictionary<string, Dictionary<string, AnimationCurve>>();

        //Debug.Log("****Loaded Curves****");
        AnimationMappingEnum mapping = AnimationMappingEnum.xSens;

        foreach (var curves in loadAnimations[0].AnimationCurveBindings)
        {
            if (curves.Model.Name == "LeftThigh")
                mapping = AnimationMappingEnum.Rokoko;
        }

        // Iterate over all the joints
        foreach (var joint in loadAnimations[0].AnimationCurveBindings)
        {
            /*
            Debug.Log(curves.Model.Name);

            foreach (var c in curves.AnimationCurves)
            {
                Debug.Log("\t" + c.Property);
            }
            */
            
            //Debug.Log("Model: " + curves.Model.Name);
            Dictionary<string, AnimationCurve> jointCurves = new Dictionary<string, AnimationCurve>();

            if (mapping == AnimationMappingEnum.xSens)
            {
                foreach (var c in joint.AnimationCurves)
                {
                    jointCurves.Add(c.Property, c.AnimationCurve);
                }

                string jointName = GetJointMapping(xSensMapping, joint.Model.Name);
                if (jointName != null)
                    animation.Add(jointName, jointCurves);
            }
            else if (mapping == AnimationMappingEnum.Rokoko)
            {
                // Retrieve joint name using mapping
                string jointName = GetJointMapping(rokokoMapping, joint.Model.Name);
                if (jointName == null) continue;

                int rotationKeyFrameCount = 0;
                int positionKeyFrameCount = 0;

                // Preliminary pass to count rotation keyframes
                foreach (var c in joint.AnimationCurves)
                {
                    if (c.Property == "localRotation.x")
                    {
                        rotationKeyFrameCount = c.AnimationCurve.length;
                    }
                    else if (c.Property == "localPosition.x")
                    {
                        positionKeyFrameCount = c.AnimationCurve.length;
                    }
                }

                //Debug.Log("RokokoMapping: " + jointName + ", positionKeys: " + positionKeyFrameCount + ", rotationKeys: " + rotationKeyFrameCount); ;

                // Allocate arrays to store positions and their corresponding times
                Vector3[] positions = new Vector3[positionKeyFrameCount];
                float[] positionTimes = new float[positionKeyFrameCount];

                // Allocate arrays to store rotations and their corresponding times
                Quaternion[] rotations = new Quaternion[rotationKeyFrameCount];
                float[] rotationTimes = new float[rotationKeyFrameCount];

                // Initialize rotation array with identity quaternions
                for (int i = 0; i < rotationKeyFrameCount; ++i)
                    rotations[i] = Quaternion.identity;

                // Process each component curve for positions (x, y, z) and rotations (x, y, z, w)
                foreach (var c in joint.AnimationCurves)
                {
                    //Debug.Log("\t" + jointName + "_" + c.Property);
                    if (c.Property == "localRotation.x")
                        for (int i = 0; i < rotationKeyFrameCount; ++i)
                        {
                            rotationTimes[i] = c.AnimationCurve.keys[i].time;
                            rotations[i].x = c.AnimationCurve.keys[i].value;
                        }
                    else if (c.Property == "localRotation.y")
                        for (int i = 0; i < rotationKeyFrameCount; ++i)
                        {
                            rotations[i].y = c.AnimationCurve.keys[i].value;
                        }
                    else if (c.Property == "localRotation.z")
                        for (int i = 0; i < rotationKeyFrameCount; ++i)
                        {
                            rotations[i].z = c.AnimationCurve.keys[i].value;
                        }
                    else if (c.Property == "localRotation.w")
                        for (int i = 0; i < rotationKeyFrameCount; ++i)
                        {
                            rotations[i].w = c.AnimationCurve.keys[i].value;
                        }
                    else if (c.Property == "localPosition.x")
                        for (int i = 0; i < positionKeyFrameCount; ++i)
                        {
                            positionTimes[i] = c.AnimationCurve.keys[i].time;
                            positions[i].x = c.AnimationCurve.keys[i].value;
                        }
                    else if (c.Property == "localPosition.y")
                        for (int i = 0; i < positionKeyFrameCount; ++i)
                        {
                            positions[i].y = c.AnimationCurve.keys[i].value;
                        }
                    else if (c.Property == "localPosition.z")
                        for (int i = 0; i < positionKeyFrameCount; ++i)
                        {
                            positions[i].z = c.AnimationCurve.keys[i].value;
                        }
                }

                // Normalize the quaternions to ensure valid rotations
                for (int i = 0; i < rotationKeyFrameCount; ++i)
                    rotations[i].Normalize();

                (Vector3[] positions, Quaternion[] rotations) mappedValues = (positions, rotations);

                Debug.Log(jointName + " rotation 1 = " + mappedValues.rotations[0].eulerAngles.ToString("F2"));

                mappedValues = MapRokokoValues(jointName, positions, rotations);

                AnimationCurve positionX = new AnimationCurve();
                AnimationCurve positionY = new AnimationCurve();
                AnimationCurve positionZ = new AnimationCurve();

                for (int i = 0; i < positions.Length; ++i)
                {
                    positionX.AddKey(positionTimes[i], mappedValues.positions[i].x);
                    positionY.AddKey(positionTimes[i], mappedValues.positions[i].y);
                    positionZ.AddKey(positionTimes[i], mappedValues.positions[i].z);
                }

                AnimationCurve rotationX = new AnimationCurve();
                AnimationCurve rotationY = new AnimationCurve();
                AnimationCurve rotationZ = new AnimationCurve();
                AnimationCurve rotationW = new AnimationCurve();

                for (int i = 0; i < rotations.Length; ++i)
                {
                    rotationX.AddKey(rotationTimes[i], mappedValues.rotations[i].x);
                    rotationY.AddKey(rotationTimes[i], mappedValues.rotations[i].y);
                    rotationZ.AddKey(rotationTimes[i], mappedValues.rotations[i].z);
                    rotationW.AddKey(rotationTimes[i], mappedValues.rotations[i].w);
                }

                if (positionKeyFrameCount > 0)
                {
                    jointCurves.Add("localPosition.x", positionX);
                    jointCurves.Add("localPosition.y", positionY);
                    jointCurves.Add("localPosition.z", positionZ);
                }

                if (rotationKeyFrameCount > 0)
                {
                    jointCurves.Add("localRotation.x", rotationX);
                    jointCurves.Add("localRotation.y", rotationY);
                    jointCurves.Add("localRotation.z", rotationZ);
                    jointCurves.Add("localRotation.w", rotationW);
                }

                
                if (jointName != null)
                    animation.Add(jointName, jointCurves);
            }
        }

        GameObject avatarInstance;

        if (wheelchair)
        {
            avatarInstance = Instantiate(wheelchairAvatar);
            avatarInstance.GetComponent<CustomWheelchairAnimationPlayer>().SetWheelchairCurves(wheelchairMovement);
        }
        else
            avatarInstance = Instantiate(avatar);

        avatarInstance.GetComponent<CharacterModel>().InitializeAvatar(characterOptions.bodyDimensions, CharacterEditor.ColorSets[characterOptions.colorSet]);
        avatarInstance.GetComponent<CharacterModel>().SetTrails(characterOptions.useBrushStrokes, characterOptions.strokeLength);
        avatarInstance.GetComponent<CustomAnimationPlayer>().SetAnimation(animation, loadAnimations[0].FrameRate);
        avatarInstance.GetComponent<CustomAnimationPlayer>().SetTrails(characterOptions.useMotionTrails, characterOptions.trailSteps, characterOptions.trailTime);
        avatarInstance.GetComponent<CustomAnimationPlayer>().UnitFactor = characterOptions.unitFactor;
        avatarInstance.name = loadedAnimationContext.RootGameObject.name;

        GameObject shortMenu = Instantiate(editAnimationMenu, editAnimationParent);
        shortMenu.GetComponent<EditAnimationShortMenu>().SetAnimationCharacter(avatarInstance);

        editAnimationParent.GetComponent<RectTransform>().sizeDelta = new Vector2(0, editAnimationParent.transform.childCount * 80);

        Destroy(loadedAnimationContext.RootGameObject);

        ++testIndex;
    }



    static (Vector3[] positions, Quaternion[] rotations) MapRokokoValues(string jointName, Vector3[] rokokoPositions, Quaternion[] rokokoRotations)
    {
        Vector3[] mappedPositions = new Vector3[rokokoPositions.Length];
        Quaternion[] mappedRotations = new Quaternion[rokokoRotations.Length];
        Quaternion qXsens = Quaternion.Euler(0, 0, 0);
        Quaternion qRokoko = Quaternion.Euler(0, 0, 0);

        /*
        if (jointName == "LeftUpLeg" || jointName == "LeftLeg")
            qRokoko = Quaternion.Euler(0, 90, 0);
        else if (jointName == "LeftFoot" || jointName == "LeftToeBase")
            qRokoko = Quaternion.Euler(90, 90, 0);
        else if (jointName == "RightUpLeg" || jointName == "RightLeg")
            qRokoko = Quaternion.Euler(0, -90, 0);
        else if (jointName == "RightFoot" || jointName == "RightToeBase")
            qRokoko = Quaternion.Euler(90, -90, 0);
        else if (jointName == "Spine" || jointName == "Spine1" || jointName == "Spine2" || jointName == "Spine3"
            || jointName == "Neck" || jointName == "Head")
            qRokoko = Quaternion.Euler(0, 0, 180);
        else if (jointName == "LeftShoulder")
            qRokoko = Quaternion.Euler(0, 0, 90);
        else if (jointName == "LeftArm" || jointName == "LeftForeArm" || jointName == "LeftHand")
            qRokoko = Quaternion.Euler(0, 90, 0);
        else if (jointName == "RightShoulder")
            qRokoko = Quaternion.Euler(0, 0, -90);
        else if (jointName == "RightArm" || jointName == "RightForeArm" || jointName == "RightHand")
            qRokoko = Quaternion.Euler(0, -90, 0);
        */
        
        if (jointName == "LeftUpLeg")
            qRokoko = Quaternion.Euler(leftUpLegRotation);
        else if (jointName == "LeftLeg")
            qRokoko = Quaternion.Euler(leftLegRotation);
        else if (jointName == "LeftFoot")
            qRokoko = Quaternion.Euler(leftFootRotation);
        else if (jointName == "LeftToeBase")
            qRokoko = Quaternion.Euler(leftToeBaseRotation);
        else if (jointName == "RightUpLeg")
            qRokoko = Quaternion.Euler(rightUpLegRotation);
        else if (jointName == "RightLeg")
            qRokoko = Quaternion.Euler(rightLegRotation);
        else if (jointName == "RightFoot")
            qRokoko = Quaternion.Euler(rightFootRotation);
        else if (jointName == "RightToeBase")
            qRokoko = Quaternion.Euler(rightToeBaseRotation);
        else if (jointName == "Spine")
            qRokoko = Quaternion.Euler(spineRotation);
        else if (jointName == "Spine1")
            qRokoko = Quaternion.Euler(spine1Rotation);
        else if (jointName == "Spine2")
            qRokoko = Quaternion.Euler(spine2Rotation);
        else if (jointName == "Spine3")
            qRokoko = Quaternion.Euler(spine3Rotation);
        else if (jointName == "LeftShoulder")
            qRokoko = Quaternion.Euler(leftShoulderRotation);
        else if (jointName == "LeftArm")
            qRokoko = Quaternion.Euler(leftArmRotation);
        else if (jointName == "LeftForeArm")
            qRokoko = Quaternion.Euler(leftForeArmRotation);
        else if (jointName == "LeftHand")
            qRokoko = Quaternion.Euler(leftHandRotation);
        else if (jointName == "RightShoulder")
            qRokoko = Quaternion.Euler(rightShoulderRotation);
        else if (jointName == "RightArm")
            qRokoko = Quaternion.Euler(rightArmRotation);
        else if (jointName == "RightForeArm")
            qRokoko = Quaternion.Euler(rightForeArmRotation);
        else if (jointName == "RightHand")
            qRokoko = Quaternion.Euler(rightHandRotation);
        else if (jointName == "Neck")
            qRokoko = Quaternion.Euler(neckRotation);
        else if (jointName == "Head")
            qRokoko = Quaternion.Euler(headRotation);

        Quaternion rotationDifference = Quaternion.Inverse(qRokoko);

        for (int i = 0; i < rokokoRotations.Length; ++i)
        {
            mappedRotations[i] = rotationDifference * rokokoRotations[i];
        }

        for (int i = 0; i < rokokoPositions.Length; ++i)
        {
            mappedPositions[i] = rotationDifference * rokokoPositions[i];
        }

        return (mappedPositions, mappedRotations);
    }

    static (Vector3[] positions, Quaternion[] rotations) MapRokokoValuesMatrix(string jointName, Vector3[] rokokoPositions, Quaternion[] rokokoRotations)
    {
        Vector3[] mappedPositions = new Vector3[rokokoPositions.Length];
        Quaternion[] mappedRotations = new Quaternion[rokokoRotations.Length];
        Matrix4x4 matrix = Matrix4x4.identity;


        if (jointName == "LeftUpLeg")
        {
            matrix.m00 = 0;
            matrix.m02 = -1;
            matrix.m20 = 1;
            matrix.m22 = 0;
        }
        else if (jointName == "LeftFoot")
        {
            matrix.m00 = 0;
            matrix.m02 = -1;
            matrix.m10 = 1;
            matrix.m11 = 0;
            matrix.m21 = -1;
            matrix.m22 = 0;
        }
        else if (jointName == "RightUpLeg")
        {
            matrix.m00 = 0;
            matrix.m02 = 1;
            matrix.m20 = -1;
            matrix.m22 = 0;
        }
        else if (jointName == "RightFoot")
        {
            matrix.m00 = 0;
            matrix.m02 = 1;
            matrix.m10 = -1;
            matrix.m11 = 0;
            matrix.m21 = -1;
            matrix.m22 = 0;
        }
        else if (jointName == "Spine")
        {
            matrix.m00 = -1;
            matrix.m11 = -1;
        }
        else if (jointName == "LeftShoulder")
        {
            matrix.m00 = 0;
            matrix.m01 = 1;
            matrix.m10 = -1;
            matrix.m11 = 0;
        }
        else if (jointName == "LeftArm")
        {
            matrix.m00 = 0;
            matrix.m02 = -1;
            matrix.m20 = 1;
            matrix.m22 = 0;
        }
        else if (jointName == "RightShoulder")
        {
            matrix.m00 = 0;
            matrix.m01 = -1;
            matrix.m10 = 1;
            matrix.m11 = 0;
        }
        else if (jointName == "RightArm")
        {
            matrix.m00 = 0;
            matrix.m02 = 1;
            matrix.m20 = -1;
            matrix.m22 = 0;
        }


        for (int i = 0; i < rokokoRotations.Length; ++i)
        {
            mappedRotations[i] = matrix.GetRotation() * rokokoRotations[i];
        }

        for (int i = 0; i < rokokoPositions.Length; ++i)
        {
            mappedPositions[i] = matrix.GetRotation() * rokokoPositions[i];
        }

        return (mappedPositions, mappedRotations);
    }

    static Vector3 OrientVector(Vector3 v, Quaternion qA, Quaternion qB)
    {
        Quaternion qAInverse = Quaternion.Inverse(qA);
        Quaternion qTransform = qB * qAInverse;

        return qTransform * v;
    }

    static Quaternion OrientQuaternion(Quaternion q, Quaternion qA, Quaternion qB)
    {
        Quaternion qAInverse = Quaternion.Inverse(qA);
        Quaternion qTransform = qB * qAInverse;

        Quaternion qTransformInverse = Quaternion.Inverse(qTransform);

        Quaternion qMapped = qTransform * q * qTransformInverse;

        return qMapped;
    }

    static AnimationCurve FlipCurve(AnimationCurve curve)
    {
        AnimationCurve newCurve = new AnimationCurve();

        foreach (var keyFrame in curve.keys)
        {
            newCurve.AddKey(keyFrame.time, -keyFrame.value);
        }
        return newCurve;
    }

    static string GetJointMapping(AnimationMapping mapping, string jointName)
    {
        if (jointName == mapping.hips)
            return "Hips";

        else if (jointName == mapping.rightUpLeg)
            return "RightUpLeg";
        else if (jointName == mapping.rightLeg)
            return "RightLeg";
        else if (jointName == mapping.rightFoot)
            return "RightFoot";
        else if (jointName == mapping.rightToeBase)
            return "RightToeBase";

        else if (jointName == mapping.leftUpLeg)
            return "LeftUpLeg";
        else if (jointName == mapping.leftLeg)
            return "LeftLeg";
        else if (jointName == mapping.leftFoot)
            return "LeftFoot";
        else if (jointName == mapping.leftToeBase)
            return "LeftToeBase";

        else if (jointName == mapping.spine)
            return "Spine";
        else if (jointName == mapping.spine1)
            return "Spine1";
        else if (jointName == mapping.spine2)
            return "Spine2";
        else if (jointName == mapping.spine3)
            return "Spine3";

        else if (jointName == mapping.leftShoulder)
            return "LeftShoulder";
        else if (jointName == mapping.leftArm)
            return "LeftArm";
        else if (jointName == mapping.leftForeArm)
            return "LeftForeArm";
        else if (jointName == mapping.leftHand)
            return "LeftHand";

        else if (jointName == mapping.rightShoulder)
            return "RightShoulder";
        else if (jointName == mapping.rightArm)
            return "RightArm";
        else if (jointName == mapping.rightForeArm)
            return "RightForeArm";
        else if (jointName == mapping.rightHand)
            return "RightHand";

        else if (jointName == mapping.neck)
            return "Neck";
        else if (jointName == mapping.head)
            return "Head";

        else
        {
            Debug.LogWarning("No Mapping Found! Mapping Name missing is: " + jointName);
            //MessageSystem.DisplayWarning("No Mapping Found! Mapping Name missing is: " + jointName);
            return null;
        }
    }
    

    static void OnMocapImportFinished(AssetLoaderContext assetLoaderContext)
    {
        isImporting = false;

        AddAnimation(assetLoaderContext);

    }

    static void OnWheelchairMocapImportFinished(AssetLoaderContext assetLoaderContext)
    {
        isImporting = false;

        AddAnimation(assetLoaderContext, true);

    }

    static void OnImportProgress(AssetLoaderContext assetLoaderContext, float progress)
    {
        if ((DateTime.Now - importStep).TotalMilliseconds > 100)
        {
            Debug.Log("Import: " + (int)(progress * 100) + "%");
            MessageSystem.DisplayMessage("Import: " + (int)(progress * 100) + "%");
            importStep = DateTime.Now;
        }
    }

    static void OnImportError(IContextualizedError contextualizedError)
    {
        MessageSystem.DisplayWarning("Import was interrupted due to an error");
        Debug.LogError("Import was interrupted due to an error. " + contextualizedError.GetInnerException().Message + ", " + contextualizedError.GetInnerException().StackTrace);
    }

    public static void ImportWheelchairMovement(string path)
    {
        using (StreamReader file = File.OpenText(path))
        {
            JsonSerializer serializer = new JsonSerializer();
            wheelchairMovement = (WheelchairMovement)serializer.Deserialize(file, typeof(WheelchairMovement));
        }

        wheelchairMovement.BuildCurves();
    }
}

public enum AnimationMappingEnum
{
    xSens,
    Rokoko
}

public delegate void GetFBXStartTimeDelegate(string path);

public class WheelchairMovement
{
    public int startHours;
    public int startMinutes;
    public int startSeconds;
    public int startMilliseconds;

    public int mvnStartHours;
    public int mvnStartMinutes;
    public int mvnStartSeconds;
    public int mvnStartMilliseconds;

    public float[] keyFrameTimes;
    public float[] posXValues;
    public float[] posYValues;
    public float[] posZValues;
    public float[] rotXValues;
    public float[] rotYValues;
    public float[] rotZValues;

    public AnimationCurve posXCurve;
    public AnimationCurve posYCurve;
    public AnimationCurve posZCurve;

    public AnimationCurve rotXCurve;
    public AnimationCurve rotYCurve;
    public AnimationCurve rotZCurve;

    public void BuildCurves()
    {
        List<Keyframe> posXKeys = new List<Keyframe>();
        List<Keyframe> posYKeys = new List<Keyframe>();
        List<Keyframe> posZKeys = new List<Keyframe>();

        List<Keyframe> rotXKeys = new List<Keyframe>();
        List<Keyframe> rotYKeys = new List<Keyframe>();
        List<Keyframe> rotZKeys = new List<Keyframe>();

        for (int i = 0; i < keyFrameTimes.Length; ++i)
        {
            if (CheckOutlier(posXValues, 1, i, 3))
                posXKeys.Add(new Keyframe(keyFrameTimes[i], posXValues[i]));
            if (CheckOutlier(posYValues, 1, i, 3))
                posYKeys.Add(new Keyframe(keyFrameTimes[i], posYValues[i]));
            if (CheckOutlier(posZValues, 1, i, 3))
                posZKeys.Add(new Keyframe(keyFrameTimes[i], posZValues[i]));

            if (CheckOutlier(rotXValues, 5, i, 5, true))
                rotXKeys.Add(new Keyframe(keyFrameTimes[i], rotXValues[i]));
            if (CheckOutlier(rotYValues, 5, i, 5, true))
                rotYKeys.Add(new Keyframe(keyFrameTimes[i], rotYValues[i]));
            if (CheckOutlier(rotZValues, 5, i, 5, true))
                rotZKeys.Add(new Keyframe(keyFrameTimes[i], rotZValues[i]));
        }

        posXCurve = new AnimationCurve(posXKeys.ToArray());
        posYCurve = new AnimationCurve(posYKeys.ToArray());
        posZCurve = new AnimationCurve(posZKeys.ToArray());

        rotXCurve = new AnimationCurve(rotXKeys.ToArray());
        rotYCurve = new AnimationCurve(rotYKeys.ToArray());
        rotZCurve = new AnimationCurve(rotZKeys.ToArray());
    }

    bool CheckOutlier(float[] values, float epsilon, int idx, int width, bool rotation = false)
    {
        int count = 0;
        float average = 0;

        for (int i = idx - width; i < idx + width; ++i)
        {
            if (i < 0 || i >= values.Length)
                continue;

            ++count;

            if (!rotation)
                average += values[i];
            else
                average += Mathf.Abs(values[i] - 180);
        }

        average /= count;

        if (!rotation)
        {
            if (Mathf.Abs(values[idx] - average) < epsilon)
                return true;
            else
                return false;
        }
        else
        {
            if (Mathf.Abs(Mathf.Abs(values[idx] - 180) - average) < epsilon)
                return true;
            else
                return false;
        }
    }
}