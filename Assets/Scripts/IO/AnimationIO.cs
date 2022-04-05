using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TriLibCore;
using TriLibCore.Utils;
using TriLibCore.Extensions;
using TriLibCore.Interfaces;
using System;
using UnityEngine.Playables;
using UnityEngine.Animations;

public class AnimationIO : MonoBehaviour
{
    [SerializeField]
    GameObject _avatar;
    static GameObject avatar;

    [SerializeField]
    AssetLoaderOptions _options;
    static AssetLoaderOptions options;

    static DateTime importStep;
    static bool isImporting = false;

    private void Start()
    {
        options = _options;
        avatar = _avatar;
    }

    public static void ImportMotionCapture(string path)
    {
        if (isImporting)
        {
            MessageSystem.DisplayWarning("There is already another import in progress");
            return;
        }

        isImporting = true;

        importStep = DateTime.Now;
        AssetLoader.LoadModelFromFile(path, OnMotionCaptureImportFinished, null, OnImportProgress, OnImportError, null, options);
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
        avatarInstance.GetComponent<CustomAnimationPlayer>().SetAnimation(animation, loadAnimations[0].FrameRate);

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
