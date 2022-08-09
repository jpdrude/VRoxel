using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SteamVR_AssemblySpawner : SpawnerActions
{
    [Header("Spawn Sphere")]
    [SerializeField]
    GameObject sphere;

    [Header("Spawn Box")]
    [SerializeField]
    GameObject box;

    [Header("Spawn Cylinder")]
    [SerializeField]
    GameObject cylinder;

    [Header("Materials")]
    [SerializeField]
    Material spawnMat;
    [SerializeField]
    Material deleteMat;

    List<GameObject> spawnMeshes = new List<GameObject>();
    List<Vector3> meshScales = new List<Vector3>();
    List<Vector3> meshSizes = new List<Vector3>();
    int spawnMeshIndex;

    float radius = 0.05f;
    Vector3 boxSize = new Vector3(0.1f, 0.1f, 0.1f);
    float cylinderRadius = 0.05f;
    float cylinderHeight = 0.1f;
    SpawnCubeBehaviour cubeBehaviour;
    float scaleSpeed = 0.1f;

    SteamVR_PointCloudSelector selector;

    VRPlacementMode mode = VRPlacementMode.SpawnInSphere;
    public VRPlacementMode Mode
    {
        get { return mode; }
        set
        {
            mode = value;
            enabled = true;
            selector.Disable();

            if (mode == VRPlacementMode.SpawnInSphere)
            {
                sphere.SetActive(true);
                sphere.GetComponent<MeshRenderer>().material = spawnMat;
                box.SetActive(false);
                cylinder.SetActive(false);
                DeactivateSpawnMeshes();
                UpdateSphereScale();
            }
            else if (mode == VRPlacementMode.DeleteFromSphere)
            {
                sphere.SetActive(true);
                sphere.GetComponent<MeshRenderer>().material = deleteMat;
                box.SetActive(false);
                cylinder.SetActive(false);
                DeactivateSpawnMeshes();
                UpdateSphereScale();
            }
            else if (mode == VRPlacementMode.SpawnInBox)
            {
                box.SetActive(true);
                box.GetComponent<MeshRenderer>().material = spawnMat;
                sphere.SetActive(false);
                cylinder.SetActive(false);
                DeactivateSpawnMeshes();
                UpdateBoxScale();
            }
            else if (mode == VRPlacementMode.DeleteFromBox)
            {
                box.SetActive(true);
                box.GetComponent<MeshRenderer>().material = deleteMat;
                sphere.SetActive(false);
                cylinder.SetActive(false);
                DeactivateSpawnMeshes();
                UpdateBoxScale();
            }
            else if (mode == VRPlacementMode.SpawnInCylinder)
            {
                cylinder.SetActive(true);
                cylinder.GetComponent<MeshRenderer>().material = spawnMat;
                sphere.SetActive(false);
                box.SetActive(false);
                DeactivateSpawnMeshes();
                UpdateCylinderScale();
            }
            else if (mode == VRPlacementMode.DeleteFromCylinder)
            {
                cylinder.SetActive(true);
                cylinder.GetComponent<MeshRenderer>().material = deleteMat;
                sphere.SetActive(false);
                box.SetActive(false);
                DeactivateSpawnMeshes();
                UpdateCylinderScale();
            }
            else if (mode == VRPlacementMode.SpawnInMesh)
            {
                cylinder.SetActive(false);
                sphere.SetActive(false);
                box.SetActive(false);
                DeactivateSpawnMeshes();
                spawnMeshes[spawnMeshIndex].gameObject.SetActive(true);
                spawnMeshes[spawnMeshIndex].GetComponent<MeshRenderer>().material = spawnMat;
                UpdateMeshScale();
            }
            else if (mode == VRPlacementMode.DeleteFromMesh)
            {
                cylinder.SetActive(false);
                sphere.SetActive(false);
                box.SetActive(false);
                DeactivateSpawnMeshes();
                spawnMeshes[spawnMeshIndex].gameObject.SetActive(true);
                spawnMeshes[spawnMeshIndex].GetComponent<MeshRenderer>().material = deleteMat;
                UpdateMeshScale();
            }
        }
    }

    private void Awake()
    {
        selector = GetComponent<SteamVR_PointCloudSelector>();
        cubeBehaviour = GetComponentInChildren<SpawnCubeBehaviour>(true);
    }

    private void Start()
    {
        Mode = VRPlacementMode.SpawnInSphere;
    }

    public override void Disable()
    {
        sphere.SetActive(false);
        box.SetActive(false);
        cylinder.SetActive(false);
        DeactivateSpawnMeshes();

        enabled = false;
    }

    public int AddSpawnMesh(GameObject mesh, Vector3 size)
    {
        spawnMeshes.Add(mesh);
        meshScales.Add(Vector3.one);
        meshSizes.Add(size);
        return spawnMeshes.Count - 1;
    }

    public void SetSpawnMesh(int idx, VRPlacementMode _mode)
    {
        spawnMeshIndex = idx;
        Mode = _mode;
    }


    void DeactivateSpawnMeshes()
    {
        foreach (GameObject go in spawnMeshes)
            go.SetActive(false);
    }

    public override void OnTrigger()
    {
        if (Mode == VRPlacementMode.SpawnInSphere)
            SpawnBricksAtPosition(sphere.transform.position, radius);
        else if (Mode == VRPlacementMode.DeleteFromSphere)
            RemoveBricksAtPosition(sphere.transform.position, radius);
        else if (Mode == VRPlacementMode.SpawnInBox)
            SpawnBricksInBox();
        else if (Mode == VRPlacementMode.DeleteFromBox)
            RemoveBricksInBox();
        else if (Mode == VRPlacementMode.SpawnInCylinder)
            SpawnBricksInCylinder();
        else if (Mode == VRPlacementMode.DeleteFromCylinder)
            RemoveBricksInCylinder();
        else if (Mode == VRPlacementMode.SpawnInMesh)
            SpawnBricksInMesh();
        else if (Mode == VRPlacementMode.DeleteFromMesh)
            RemoveBricksInMesh();
    }

    public override void DPadInput(bool _up, bool _down, bool _left, bool _right)
    {
        if (Mode == VRPlacementMode.SpawnInSphere || Mode == VRPlacementMode.DeleteFromSphere)
            CheckSphereScale(_up, _down);
        else if (Mode == VRPlacementMode.SpawnInBox || Mode == VRPlacementMode.DeleteFromBox)
            CheckBoxScale(_up, _down, _left, _right);
        else if (Mode == VRPlacementMode.SpawnInCylinder || Mode == VRPlacementMode.DeleteFromCylinder)
            CheckCylinderScale(_up, _down, _left, _right);
        else if (Mode == VRPlacementMode.SpawnInMesh || Mode == VRPlacementMode.DeleteFromMesh)
            CheckMeshScale(_up, _down);
    }

    void CheckSphereScale(bool up, bool down)
    {
        if (Mode == VRPlacementMode.SpawnInSphere || Mode == VRPlacementMode.DeleteFromSphere)
        {

            if (up)
            {
                radius += Time.deltaTime * scaleSpeed;
                UpdateSphereScale();
            }
            else if (down)
            {
                radius -= Time.deltaTime * scaleSpeed;
                if (radius < 0.001f)
                    radius = 0.001f;
                UpdateSphereScale();
            }
        }
    }

    void CheckCylinderScale(bool up, bool down, bool left, bool right)
    {
        if (Mode == VRPlacementMode.SpawnInCylinder || Mode == VRPlacementMode.DeleteFromCylinder)
        {
            if (up)
            {
                cylinderHeight += Time.deltaTime * scaleSpeed;
                UpdateCylinderScale();
            }
            else if (down)
            {
                cylinderHeight -= Time.deltaTime * scaleSpeed;
                if (cylinderHeight < 0.025f)
                    cylinderHeight = 0.025f;
                UpdateCylinderScale();
            }
            else if (right)
            {
                cylinderRadius += Time.deltaTime * scaleSpeed;
                UpdateCylinderScale();
            }
            else if (left)
            {
                cylinderRadius -= Time.deltaTime * scaleSpeed;
                if (cylinderRadius < 0.01f)
                    cylinderRadius = 0.01f;
                UpdateCylinderScale();
            }
        }
    }

    void CheckBoxScale(bool up, bool down, bool left, bool right)
    {
        if (Mode == VRPlacementMode.SpawnInBox || Mode == VRPlacementMode.DeleteFromBox)
        {
            if (up)
            {
                boxSize += Time.deltaTime * scaleSpeed * Vector3.one;
                UpdateBoxScale();
            }
            else if (down)
            {
                boxSize -= Time.deltaTime * scaleSpeed * Vector3.one;
                if (boxSize.x < 0.025f)
                    boxSize.x = 0.025f;
                if (boxSize.y < 0.025f)
                    boxSize.y = 0.025f;
                if (boxSize.z < 0.025f)
                    boxSize.z = 0.025f;

                UpdateBoxScale();
            }
            else if (right)
            {
                boxSize += Time.deltaTime * scaleSpeed * new Vector3(1, 1, 0);

                UpdateBoxScale();
            }
            else if (left)
            {
                boxSize -= Time.deltaTime * scaleSpeed * new Vector3(1, 1, 0);
                if (boxSize.x < 0.025f)
                    boxSize.x = 0.025f;
                if (boxSize.y < 0.025f)
                    boxSize.y = 0.025f;

                UpdateBoxScale();
            }
        }
    }

    void CheckMeshScale(bool up, bool down)
    {
        if (Mode == VRPlacementMode.SpawnInMesh || Mode == VRPlacementMode.DeleteFromMesh)
        {
            if (up)
            {
                meshScales[spawnMeshIndex] += Time.deltaTime * scaleSpeed * Vector3.one;
                UpdateMeshScale();
            }
            else if (down)
            {
                meshScales[spawnMeshIndex] -= Time.deltaTime * scaleSpeed * Vector3.one;
                if (meshScales[spawnMeshIndex].x < 0.025f)
                    meshScales[spawnMeshIndex] = Vector3.one * 0.025f;

                UpdateMeshScale();
            }
        }
    }

    public void ChangeMode(int _mode)
    {
        Mode = (VRPlacementMode)_mode;
    }

    void SpawnBricksAtPosition(Vector3 pos, float radius)
    {
        Vector3Int[] dictPositions;
        Matrix4x4[] bricks = GameManager.Bond.GetBricksInSphere(pos, radius, out dictPositions);
        ModuleContainer.Container.AddBricks(bricks, dictPositions);
    }

    void RemoveBricksAtPosition(Vector3 pos, float radius)
    {
        Vector3Int[] dictPositions;
        Matrix4x4[] bricks = GameManager.Bond.GetBricksInSphere(pos, radius, out dictPositions);
        ModuleContainer.Container.RemoveBricks(bricks, dictPositions);
    }

    void SpawnBricksInBox()
    {
        Vector3Int[] dictPositions;
        Matrix4x4[] bricks = GameManager.Bond.GetBricksInBox(cubeBehaviour.Min, cubeBehaviour.Max, out dictPositions);
        ModuleContainer.Container.AddBricks(bricks, dictPositions);
    }

    void RemoveBricksInBox()
    {
        Vector3Int[] dictPositions;
        Matrix4x4[] bricks = GameManager.Bond.GetBricksInBox(cubeBehaviour.Min, cubeBehaviour.Max, out dictPositions);
        ModuleContainer.Container.RemoveBricks(bricks, dictPositions);
    }

    void SpawnBricksInCylinder()
    {
        Vector3Int[] dictPositions;
        Matrix4x4[] bricks = GameManager.Bond.GetBricksInCylinder(cylinder.transform.position, cylinderRadius, cylinderHeight, out dictPositions);
        ModuleContainer.Container.AddBricks(bricks, dictPositions);
    }

    void RemoveBricksInCylinder()
    {
        Vector3Int[] dictPositions;
        Matrix4x4[] bricks = GameManager.Bond.GetBricksInCylinder(cylinder.transform.position, cylinderRadius, cylinderHeight, out dictPositions);
        ModuleContainer.Container.RemoveBricks(bricks, dictPositions);
    }

    void SpawnBricksInMesh()
    {
        Vector3Int[] dictPositions;
        Matrix4x4[] bricks = GameManager.Bond.GetBricksInMesh(spawnMeshes[spawnMeshIndex].GetComponent<MeshCollider>(), out dictPositions);
        ModuleContainer.Container.AddBricks(bricks, dictPositions);
    }

    void RemoveBricksInMesh()
    {
        Vector3Int[] dictPositions;
        Matrix4x4[] bricks = GameManager.Bond.GetBricksInMesh(spawnMeshes[spawnMeshIndex].GetComponent<MeshCollider>(), out dictPositions);
        ModuleContainer.Container.RemoveBricks(bricks, dictPositions);
    }

    void UpdateSphereScale()
    {
        sphere.transform.localScale = Vector3.one * 2 * radius;
        sphere.transform.localPosition = Vector3.forward * radius;
    }

    void UpdateBoxScale()
    {
        box.transform.localScale = boxSize;
        box.transform.localPosition = Vector3.forward * (boxSize.z / 2);
    }

    void UpdateCylinderScale()
    {
        cylinder.transform.localScale = new Vector3(cylinderRadius * 2, cylinderHeight, cylinderRadius * 2);
        cylinder.transform.localPosition = Vector3.forward * cylinderRadius;
    }

    public void UpdateMeshScale()
    {
        spawnMeshes[spawnMeshIndex].transform.localScale = meshScales[spawnMeshIndex];
        spawnMeshes[spawnMeshIndex].transform.localPosition = Vector3.forward * meshSizes[spawnMeshIndex].z * meshScales[spawnMeshIndex].z / 2;
    }
}

public enum VRPlacementMode
{
    SpawnInSphere,
    DeleteFromSphere,
    SpawnInBox,
    DeleteFromBox,
    SpawnInCylinder,
    DeleteFromCylinder,
    SpawnInMesh,
    DeleteFromMesh,
    BricksToPointCloud,
    PointCloudToBricks
}
