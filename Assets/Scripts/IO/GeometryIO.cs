using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TriLibCore;
using System;
using Valve.VR.InteractionSystem;

public class GeometryIO : MonoBehaviour
{
    static bool isImporting = false;
    static DateTime importStep;
    static GameObject nativeTeleport;
    static Material teleportAreaMat;
    static GameObject teleportAreaPrefab;

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

    public static void ImportTeleportArea(string path, GameObject _nativeTeleport, Material _teleportAreaMat, GameObject _teleportAreaPrefab)
    {
        if (isImporting)
        {
            MessageSystem.DisplayWarning("There is already another import in progress");
            return;
        }

        isImporting = true;
        var assetLoaderOptions = AssetLoader.CreateDefaultLoaderOptions();
        importStep = DateTime.Now;
        AssetLoader.LoadModelFromFile(path, OnGeometryImport, OnTeleportAreaImport, OnImportProgress, OnImportError, null, assetLoaderOptions);

        nativeTeleport = _nativeTeleport;
        teleportAreaMat = _teleportAreaMat;
        teleportAreaPrefab = _teleportAreaPrefab;
    }

    static void OnTeleportAreaImport(AssetLoaderContext assetLoaderContext)
    {
        isImporting = false;
        var root = assetLoaderContext.RootGameObject;

        foreach (MeshFilter mf in root.GetComponentsInChildren<MeshFilter>())
        {
            /*
            Debug.Log("Shader: " + teleportAreaMat.shader.ToString());
            mr.material = new Material(Shader.Find("Valve/VR/Highlight"));
            mr.gameObject.layer = 6;
            var ta = mr.gameObject.AddComponent<TeleportArea>();
            ta.Awake();
            ta.Start();
            */

            var area = Instantiate(teleportAreaPrefab);
            area.transform.position = mf.transform.position;
            area.transform.rotation = mf.transform.rotation;
            area.transform.localScale = mf.transform.localScale;
            area.GetComponent<MeshFilter>().sharedMesh = mf.sharedMesh;
            area.GetComponent<MeshCollider>().sharedMesh = mf.sharedMesh;
            area.GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            var mc = mf.gameObject.AddComponent<MeshCollider>();
            mc.sharedMesh = mf.sharedMesh;
            //mf.gameObject.AddComponent<BoxCollider>();
            mf.GetComponent<MeshRenderer>().enabled = false;
            mf.gameObject.layer = 3;
            mf.transform.position = mf.transform.position - Vector3.down * 0.01f;
        }

        MessageSystem.DisplayMessage("Teleport Areas Imported!");

        if (nativeTeleport != null)
            nativeTeleport.SetActive(false);
    }

    static void OnGeometryImport(AssetLoaderContext assetLoaderContext)
    {

    }

    static void OnMaterialsImport(AssetLoaderContext assetLoaderContext)
    {
        isImporting = false;
        MessageSystem.DisplayMessage("Geometry Imported!");
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
