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

    static DateTime importStep;
    static bool isImporting = false;

    public static bool constantAnimationTime = false;
    public static int animationFrameRate = 30;

    static WheelchairMovement wheelchairMovement;
         
    private void Start()
    {
        options = _options;
        avatar = _avatar;
        wheelchairAvatar = _wheelchairAvatar;

        editAnimationMenu = _editAnimationMenu;
        editAnimationParent = _editAnimationParent;
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
        foreach (var curves in loadAnimations[0].AnimationCurveBindings)
        {
            //Debug.Log("Model: " + curves.Model.Name);
            Dictionary<string, AnimationCurve> jointCurves = new Dictionary<string, AnimationCurve>();
            foreach (var c in curves.AnimationCurves)
            {
                //Debug.Log("    Property: " + c.Property + ", Keyframes: " + c.AnimationCurve.length);
                jointCurves.Add(c.Property, c.AnimationCurve);
            }
            animation.Add(curves.Model.Name, jointCurves);
            
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

            Destroy(loadedAnimationContext.RootGameObject);
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
        Debug.LogError("Import was interrupted due to an error");
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