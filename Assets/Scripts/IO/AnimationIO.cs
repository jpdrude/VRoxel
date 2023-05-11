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

public class AnimationIO : MonoBehaviour
{
    [SerializeField]
    GameObject _avatar;
    static GameObject avatar;

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
         
    private void Start()
    {
        options = _options;
        avatar = _avatar;

        editAnimationMenu = _editAnimationMenu;
        editAnimationParent = _editAnimationParent;
    }

    public static void ImportMotionCapture(string path, CharacterOptions _characterOptions)
    {
        characterOptions = _characterOptions;


        if (isImporting)
        {
            MessageSystem.DisplayWarning("There is already another import in progress");
            return;
        }

        isImporting = true;

        importStep = DateTime.Now;
        AssetLoader.LoadModelFromFile(path, OnMotionCaptureImportFinished, null, OnImportProgress, OnImportError, null, options);

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

    static void AddAnimation(AssetLoaderContext loadedAnimationContext)
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

        var avatarInstance = Instantiate(avatar);
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

    static void OnMotionCaptureImportFinished(AssetLoaderContext assetLoaderContext)
    {
        isImporting = false;

        AddAnimation(assetLoaderContext);
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
    }
}

public delegate void GetFBXStartTimeDelegate(string path);