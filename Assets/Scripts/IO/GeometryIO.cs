using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TriLibCore;
using System;
using Valve.VR.InteractionSystem;

public class GeometryIO : MonoBehaviour
{
    static Dictionary<string, DateTime> importSteps = new Dictionary<string, DateTime>();

    [Header("Defaults")]
    [SerializeField]
    Material _defaultMaterial;

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

    [Space]
    [Header("Management")]
    [SerializeField]
    Transform _importGeometryParent;
    [SerializeField]
    GameObject _menuEntryPrefab;
    [SerializeField]
    Transform _menuEntryParent;

    static Material defaultMaterial;

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

    //fix transform variables
    static bool fixGeometryTransform = false;
    static bool fixBrushTransform = false;
    static bool fixTeleportTransform = false;

    //Management Variables
    static Transform importGeometryParent;
    static GameObject menuEntryPrefab = null;
    static Transform menuEntryParent = null;

    private void Start()
    {
        defaultMaterial = _defaultMaterial;

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

        importGeometryParent = _importGeometryParent;
        menuEntryPrefab = _menuEntryPrefab;
        menuEntryParent = _menuEntryParent;
    }

    public static void ImportGeometry(string path, bool fixTransform)
    {
        AssetLoader.LoadModelFromFile(path, OnGeometryImport, OnMaterialsImport, OnImportProgress, OnGeometryImportError, null, options);
        fixGeometryTransform = fixTransform;
    }

    public static void ImportKitBashPalette(string path)
    {
        AssetLoader.LoadModelFromFile(path, OnGeometryImport, OnKitBashPaletteImport, OnImportProgress, OnImportError, null, options);
    }

    public static void ImportTeleportArea(string path, bool fixTransform)
    {
        AssetLoader.LoadModelFromFile(path, OnGeometryImport, OnTeleportAreaImport, OnImportProgress, OnTeleportImportError, null, options);
        fixTeleportTransform = fixTransform;
    }

    public static void ImportBrushes(string path, bool fixTransform)
    {
        AssetLoader.LoadModelFromFile(path, OnGeometryImport, OnBrushImport, OnImportProgress, OnBrushImportError, null, options);
        fixBrushTransform = fixTransform;
    }

    static void OnTeleportAreaImport(AssetLoaderContext assetLoaderContext)
    {
        if (importSteps.ContainsKey(assetLoaderContext.Filename))
            importSteps.Remove(assetLoaderContext.Filename);

        var root = assetLoaderContext.RootGameObject;

        if (fixTeleportTransform)
        {
            fixTeleportTransform = false;
            root.transform.Rotate(Vector3.up, 180);
        }

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

        //if (nativeTeleport != null)
            //nativeTeleport.SetActive(false);
    }

    static void OnBrushImport(AssetLoaderContext assetLoaderContext)
    {
        if (importSteps.ContainsKey(assetLoaderContext.Filename))
            importSteps.Remove(assetLoaderContext.Filename);

        if (fixBrushTransform)
        {
            fixBrushTransform = false;
            assetLoaderContext.RootGameObject.transform.Rotate(Vector3.up, 180);
        }

        Debug.Log("Brush file imported");
        MeshFilter[] meshFilters = assetLoaderContext.RootGameObject.GetComponentsInChildren<MeshFilter>();

        foreach (MeshFilter mf in meshFilters)
        {
            Debug.Log("Brush added");
            Vector3 size = ComputeMeshSize(mf.sharedMesh);
            
            int  index = spawner.AddBrush(BuildSpawnerMesh(mf.sharedMesh, spawner.transform), size);

            fpsSpawner.AddBrush(BuildSpawnerMesh(mf.sharedMesh, fpsSpawner.transform));
            
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

        XYZConstraint constraint = spawnMesh.AddComponent<XYZConstraint>();
        constraint.Spawner = spawner;

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

        BuildBrushButton(mesh, addButton.transform, size, addMaterial, index, VRPlacementMode.SpawnInBrush);
        BuildBrushButton(mesh, removeButton.transform, size, removeMaterial, index, VRPlacementMode.DeleteFromBrush);

        buttonMenu.AddButtons.Add(addButton);
        buttonMenu.RemoveButtons.Add(removeButton);
    }

    static void BuildBrushButton(Mesh mesh, Transform parent, Vector3 size, Material mat, int index, VRPlacementMode mode)
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
        menuItem.Action.AddListener(delegate { spawner.SetBrush(index, mode); });

        parent.gameObject.SetActive(false);
    }

    static void OnGeometryImport(AssetLoaderContext assetLoaderContext)
    {

    }

    static void OnMaterialsImport(AssetLoaderContext assetLoaderContext)
    {
        if (importSteps.ContainsKey(assetLoaderContext.Filename))
            importSteps.Remove(assetLoaderContext.Filename);

        assetLoaderContext.RootGameObject.layer = 11;

        foreach (MeshRenderer mr in assetLoaderContext.RootGameObject.GetComponentsInChildren<MeshRenderer>())
        {
            if (mr.material == null)
                mr.material = defaultMaterial;
        }

        if (fixGeometryTransform)
        {
            assetLoaderContext.RootGameObject.transform.Rotate(Vector3.up, 180);
            fixGeometryTransform = false;
        }

        CreateGeometryMenuEntries(assetLoaderContext.RootGameObject);

        MessageSystem.DisplayMessage("Geometry Imported!");
    }

    static void CreateGeometryMenuEntries(GameObject root)
    {
        for (int i = root.transform.childCount - 1; i >= 0; --i)
        {
            GameObject child = root.transform.GetChild(i).gameObject;

            Vector3 center = Vector3.zero;
            int childMeshCount = 0;
            GameObject centerObj = new GameObject(child.name);
            centerObj.layer = 11;

            var itemRoot = centerObj.AddComponent<GeometryItemRoot>();
            itemRoot.enabled = false;

            foreach (MeshFilter mf in child.GetComponentsInChildren<MeshFilter>())
            {
                var col = mf.gameObject.AddComponent<MeshCollider>();
                col.sharedMesh = mf.sharedMesh;

                Vector3 size = mf.gameObject.GetComponent<MeshRenderer>().bounds.size;
                if (size.x * size.y * size.z > 5)
                    centerObj.layer = 2;

                center += mf.gameObject.GetComponent<MeshRenderer>().bounds.center;
                ++childMeshCount;
            }

            center /= childMeshCount;

            centerObj.transform.position = center;
            centerObj.transform.parent = importGeometryParent;

            for (int j = child.transform.childCount - 1; j >= 0; --j)
            {
                child.transform.GetChild(j).parent = centerObj.transform;
            }

            Destroy(child);

            GameObject menuEntryGo = Instantiate(menuEntryPrefab, menuEntryParent);
            menuEntryGo.GetComponent<EditGeometryMenu>().InitializeModel(centerObj);

            menuEntryParent.GetComponent<RectTransform>().sizeDelta = new Vector2(0, menuEntryParent.transform.childCount * 80);
        }
    }

    static void OnKitBashPaletteImport(AssetLoaderContext assetLoaderContext)
    {
        KitBashIO.SetPalette(assetLoaderContext.RootGameObject);
    }

    static void OnImportProgress(AssetLoaderContext assetLoaderContext, float progress)
    {
        string path = assetLoaderContext.Filename;

        if (!importSteps.ContainsKey(path))
        {
            importSteps.Add(path, DateTime.Now);
            return;
        }

        if ((DateTime.Now - importSteps[path]).TotalMilliseconds > 500)
        {
            string message = System.IO.Path.GetFileName(assetLoaderContext.Filename) + " import: " + (int)(progress * 100) + "%";
            Debug.Log(message);
            MessageSystem.DisplayMessage(message);
            importSteps[path] = DateTime.Now;
        }
    }

    static void OnImportError(IContextualizedError contextualizedError)
    {
        MessageSystem.DisplayWarning("Import was interrupted due to an error");
    }

    static void OnGeometryImportError(IContextualizedError contextualizedError)
    {
        OnImportError(contextualizedError);
        fixGeometryTransform = false;
    }

    static void OnTeleportImportError(IContextualizedError contextualizedError)
    {
        OnImportError(contextualizedError);
        fixTeleportTransform = false;
    }

    static void OnBrushImportError(IContextualizedError contextualizedError)
    {
        OnImportError(contextualizedError);
        fixBrushTransform = false;
    }
}
