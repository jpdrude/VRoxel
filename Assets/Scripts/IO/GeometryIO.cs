using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TriLibCore;
using System;

public class GeometryIO : MonoBehaviour
{
    static bool isImporting = false;
    static DateTime importStep;

    public static void ImportGeometry(string path)
    {
        if (isImporting)
        {
            MessageSystem.DisplayWarning("There is already another import in progress");
            return;
        }

        isImporting = true;
        var assetLoaderOptions = AssetLoader.CreateDefaultLoaderOptions();
        importStep = DateTime.Now;
        AssetLoader.LoadModelFromFile(path, OnGeometryImport, OnMaterialsImport, OnImportProgress, OnImportError, null, assetLoaderOptions);
    }

    static void OnGeometryImport(AssetLoaderContext assetLoaderContext)
    {

    }

    static void OnMaterialsImport(AssetLoaderContext assetLoaderContext)
    {
        isImporting = false;
    }

    static void OnImportProgress(AssetLoaderContext assetLoaderContext, float progress)
    {
        if ((DateTime.Now - importStep).TotalMilliseconds > 1000)
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
