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

    [Header("Teleporting")]
    [SerializeField]
    GameObject _nativeTeleport;
    [SerializeField]
    GameObject _teleportAreaPrefab;

    [Space]
    [Header("MeshSpawner")]
    [SerializeField]
    SteamVR_AssemblySpawner _spawner;
    [SerializeField]
    GameObject _rightHand;
    [SerializeField]
    TargetSpawner _fpsSpawner;   

    [Space]
    [Header("MeshSpawnButtons")]
    [SerializeField]
    Transform _spawnAddButtonParent;
    [SerializeField]
    Transform _spawnRemoveButtonParent;
    [SerializeField]
    GameObject _buttonPrefab;
    [SerializeField]
    Material _addMaterial;
    [SerializeField]
    Material _removeMaterial;
    [SerializeField]
    MenuSwitch _buttonMenu;

    [Space]
    [Header("TriLibCore Import")]
    [SerializeField]
    AssetLoaderOptions _options;


    static GameObject nativeTeleport;
    static GameObject teleportAreaPrefab;

    static SteamVR_AssemblySpawner spawner;
    static GameObject rightHand;
    static TargetSpawner fpsSpawner;

    static Transform spawnAddButtonParent;
    static Transform spawnRemoveButtonParent;
    static GameObject buttonPrefab;
    static Material addMaterial;
    static Material removeMaterial;
    static MenuSwitch buttonMenu;

    static AssetLoaderOptions options;

    private void Start()
    {
        nativeTeleport = _nativeTeleport;
        teleportAreaPrefab = _teleportAreaPrefab;

        spawner = _spawner;
        rightHand = _rightHand;
        fpsSpawner = _fpsSpawner;

        spawnAddButtonParent = _spawnAddButtonParent;
        spawnRemoveButtonParent = _spawnRemoveButtonParent;
        buttonPrefab = _buttonPrefab;
        addMaterial = _addMaterial;
        removeMaterial = _removeMaterial;
        buttonMenu = _buttonMenu;

        options = _options;
    }

    public static void ImportGeometry(string path)
    {
        if (isImporting)
        {
            MessageSystem.DisplayWarning("There is already another import in progress");
            return;
        }

        isImporting = true;
        importStep = DateTime.Now;
        AssetLoader.LoadModelFromFile(path, OnGeometryImport, OnMaterialsImport, OnImportProgress, OnImportError, null, options);
    }

    public static void ImportTeleportArea(string path)
    {
        if (isImporting)
        {
            MessageSystem.DisplayWarning("There is already another import in progress");
            return;
        }

        isImporting = true;
        importStep = DateTime.Now;
        AssetLoader.LoadModelFromFile(path, OnGeometryImport, OnTeleportAreaImport, OnImportProgress, OnImportError, null, options);

    }

    public static void ImportSpawnShape(string path)
    {
        if (isImporting)
        {
            MessageSystem.DisplayWarning("There is already another import in progress");
            return;
        }

        isImporting = true;
        importStep = DateTime.Now;
        AssetLoader.LoadModelFromFile(path, OnGeometryImport, OnSpawnShapeImport, OnImportProgress, OnImportError, null, options);
    }

    static void OnTeleportAreaImport(AssetLoaderContext assetLoaderContext)
    {
        isImporting = false;
        var root = assetLoaderContext.RootGameObject;

        foreach (MeshFilter mf in root.GetComponentsInChildren<MeshFilter>())
        {
            var area = Instantiate(teleportAreaPrefab);
            area.transform.position = mf.transform.position;
            area.transform.rotation = mf.transform.rotation;
            area.transform.localScale = mf.transform.localScale;
            area.GetComponent<MeshFilter>().sharedMesh = mf.sharedMesh;
            area.GetComponent<MeshCollider>().sharedMesh = mf.sharedMesh;
            area.GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            var mc = mf.gameObject.AddComponent<MeshCollider>();
            mc.sharedMesh = mf.sharedMesh;
            mf.GetComponent<MeshRenderer>().enabled = false;
            mf.gameObject.layer = 3;
            mf.transform.position = mf.transform.position - Vector3.down * 0.01f;
        }

        MessageSystem.DisplayMessage("Teleport Areas Imported!");

        if (nativeTeleport != null)
            nativeTeleport.SetActive(false);
    }

    static void OnSpawnShapeImport(AssetLoaderContext assetLoaderContext)
    {
        Debug.Log("SpawnMesh file imported");
        MeshFilter[] meshFilters = assetLoaderContext.RootGameObject.GetComponentsInChildren<MeshFilter>();

        foreach (MeshFilter mf in meshFilters)
        {
            Debug.Log("SpawnMesh added");
            Vector3 size = ComputeMeshSize(mf.sharedMesh);
            
            int  index = spawner.AddSpawnMesh(BuildSpawnerMesh(mf.sharedMesh, spawner.transform), size);

            fpsSpawner.AddSpawnMesh(BuildSpawnerMesh(mf.sharedMesh, fpsSpawner.transform));
            
            BuildSpawnerButtons(mf.sharedMesh, index, size);
        }

        assetLoaderContext.RootGameObject.SetActive(false);
    }

    static GameObject BuildSpawnerMesh(Mesh mesh, Transform parent)
    {
        GameObject spawnMesh = new GameObject("MeshSpawner");
        spawnMesh.transform.parent = parent;
        spawnMesh.transform.localPosition = Vector3.zero;
        MeshFilter filter = spawnMesh.AddComponent<MeshFilter>();
        filter.sharedMesh = mesh;
        MeshRenderer renderer = spawnMesh.AddComponent<MeshRenderer>();
        MeshCollider collider = spawnMesh.AddComponent<MeshCollider>();
        collider.sharedMesh = mesh;
        SpawnCubeBehaviour cubeBehaviour = spawnMesh.AddComponent<SpawnMeshBehaviour>();
        cubeBehaviour.Parent = rightHand;
        spawnMesh.layer = 8;
        spawnMesh.tag = "MeshSpawner";
        spawnMesh.SetActive(false);
        Debug.Log("SpawnerMesh built");
        return spawnMesh;
    }

    static Vector3 ComputeMeshSize(Mesh mesh)
    {
        Vector3 min = Vector3.one * float.MaxValue;
        Vector3 max = Vector3.one * float.MinValue;

        foreach(Vector3 vertex in mesh.vertices)
        {
            if (vertex.x < min.x) min.x = vertex.x;
            if (vertex.x > max.x) max.x = vertex.x;

            if (vertex.y < min.y) min.y = vertex.y;
            if (vertex.y > max.y) max.y = vertex.y;

            if (vertex.z < min.z) min.z = vertex.z;
            if (vertex.z > max.z) max.z = vertex.z;
        }

        return new Vector3(max.x - min.x, max.y - min.y, max.z - min.z);
    }

    static void BuildSpawnerButtons(Mesh mesh, int index, Vector3 size)
    {
        GameObject addButton = Instantiate(buttonPrefab, spawnAddButtonParent);
        GameObject removeButton = Instantiate(buttonPrefab, spawnRemoveButtonParent);

        BuildButtonMesh(mesh, addButton.transform, size, addMaterial, index, VRPlacementMode.SpawnInMesh);
        BuildButtonMesh(mesh, removeButton.transform, size, removeMaterial, index, VRPlacementMode.DeleteFromMesh);

        buttonMenu.AddButtons.Add(addButton);
        buttonMenu.RemoveButtons.Add(removeButton);
    }

    static void BuildButtonMesh(Mesh mesh, Transform parent, Vector3 size, Material mat, int index, VRPlacementMode mode)
    {
        GameObject buttonMesh = new GameObject("AddMesh");
        buttonMesh.transform.parent = parent;
        buttonMesh.transform.localPosition = Vector3.zero;
        MeshFilter mf = buttonMesh.AddComponent<MeshFilter>();
        mf.sharedMesh = mesh;
        MeshRenderer mr = buttonMesh.AddComponent<MeshRenderer>();
        mr.material = mat;
        MeshCollider mc = buttonMesh.AddComponent<MeshCollider>();
        mc.sharedMesh = mesh;
        Destroy(mc);

        float longestSide = size.x;
        if (size.z > longestSide)
            longestSide = size.z;
        buttonMesh.transform.localScale = 0.04f / longestSide * Vector3.one;

        VRMenuItem menuItem = parent.GetComponent<VRMenuItem>();
        menuItem.Action.AddListener(delegate { spawner.SetSpawnMesh(index, mode); });

        parent.gameObject.SetActive(false);
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
