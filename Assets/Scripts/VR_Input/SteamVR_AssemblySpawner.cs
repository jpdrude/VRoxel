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

    List<GameObject> brushes = new List<GameObject>();
    List<Vector3> brushScales = new List<Vector3>();
    List<Vector3> brushSizes = new List<Vector3>();
    int brushIndex;

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
                DeactivateBrushes();
                UpdateSphereScale();
            }
            else if (mode == VRPlacementMode.DeleteFromSphere)
            {
                sphere.SetActive(true);
                sphere.GetComponent<MeshRenderer>().material = deleteMat;
                box.SetActive(false);
                cylinder.SetActive(false);
                DeactivateBrushes();
                UpdateSphereScale();
            }
            else if (mode == VRPlacementMode.SpawnInBox)
            {
                box.SetActive(true);
                box.GetComponent<MeshRenderer>().material = spawnMat;
                sphere.SetActive(false);
                cylinder.SetActive(false);
                DeactivateBrushes();
                UpdateBoxScale();
            }
            else if (mode == VRPlacementMode.DeleteFromBox)
            {
                box.SetActive(true);
                box.GetComponent<MeshRenderer>().material = deleteMat;
                sphere.SetActive(false);
                cylinder.SetActive(false);
                DeactivateBrushes();
                UpdateBoxScale();
            }
            else if (mode == VRPlacementMode.SpawnInCylinder)
            {
                cylinder.SetActive(true);
                cylinder.GetComponent<MeshRenderer>().material = spawnMat;
                sphere.SetActive(false);
                box.SetActive(false);
                DeactivateBrushes();
                UpdateCylinderScale();
            }
            else if (mode == VRPlacementMode.DeleteFromCylinder)
            {
                cylinder.SetActive(true);
                cylinder.GetComponent<MeshRenderer>().material = deleteMat;
                sphere.SetActive(false);
                box.SetActive(false);
                DeactivateBrushes();
                UpdateCylinderScale();
            }
            else if (mode == VRPlacementMode.SpawnInBrush)
            {
                cylinder.SetActive(false);
                sphere.SetActive(false);
                box.SetActive(false);
                DeactivateBrushes();
                brushes[brushIndex].gameObject.SetActive(true);
                brushes[brushIndex].GetComponent<MeshRenderer>().material = spawnMat;
                UpdateBrushScale();
            }
            else if (mode == VRPlacementMode.DeleteFromBrush)
            {
                cylinder.SetActive(false);
                sphere.SetActive(false);
                box.SetActive(false);
                DeactivateBrushes();
                brushes[brushIndex].gameObject.SetActive(true);
                brushes[brushIndex].GetComponent<MeshRenderer>().material = deleteMat;
                UpdateBrushScale();
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
        DeactivateBrushes();

        enabled = false;
    }

    public int AddBrush(GameObject mesh, Vector3 size)
    {
        brushes.Add(mesh);
        brushScales.Add(Vector3.one);
        brushSizes.Add(size);
        return brushes.Count - 1;
    }

    public void SetBrush(int idx, VRPlacementMode _mode)
    {
        brushIndex = idx;
        Mode = _mode;
    }


    void DeactivateBrushes()
    {
        foreach (GameObject go in brushes)
            go.SetActive(false);
    }

    public override void OnTrigger()
    {
        if (Mode == VRPlacementMode.SpawnInSphere)
            SpawnBlocksAtPosition(sphere.transform.position, radius);
        else if (Mode == VRPlacementMode.DeleteFromSphere)
            RemoveBlocksAtPosition(sphere.transform.position, radius);
        else if (Mode == VRPlacementMode.SpawnInBox)
            SpawnBlocksInBox();
        else if (Mode == VRPlacementMode.DeleteFromBox)
            RemoveBlocksInBox();
        else if (Mode == VRPlacementMode.SpawnInCylinder)
            SpawnBlocksInCylinder();
        else if (Mode == VRPlacementMode.DeleteFromCylinder)
            RemoveBlocksInCylinder();
        else if (Mode == VRPlacementMode.SpawnInBrush)
            SpawnBlocksInBrush();
        else if (Mode == VRPlacementMode.DeleteFromBrush)
            RemoveBlocksInBrush();
    }

    public override void DPadInput(bool _up, bool _down, bool _left, bool _right)
    {
        if (Mode == VRPlacementMode.SpawnInSphere || Mode == VRPlacementMode.DeleteFromSphere)
            CheckSphereScale(_up, _down);
        else if (Mode == VRPlacementMode.SpawnInBox || Mode == VRPlacementMode.DeleteFromBox)
            CheckBoxScale(_up, _down, _left, _right);
        else if (Mode == VRPlacementMode.SpawnInCylinder || Mode == VRPlacementMode.DeleteFromCylinder)
            CheckCylinderScale(_up, _down, _left, _right);
        else if (Mode == VRPlacementMode.SpawnInBrush || Mode == VRPlacementMode.DeleteFromBrush)
            CheckBrushScale(_up, _down);
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

    void CheckBrushScale(bool up, bool down)
    {
        if (Mode == VRPlacementMode.SpawnInBrush || Mode == VRPlacementMode.DeleteFromBrush)
        {
            if (up)
            {
                brushScales[brushIndex] += Time.deltaTime * scaleSpeed * Vector3.one;
                UpdateBrushScale();
            }
            else if (down)
            {
                brushScales[brushIndex] -= Time.deltaTime * scaleSpeed * Vector3.one;
                if (brushScales[brushIndex].x < 0.025f)
                    brushScales[brushIndex] = Vector3.one * 0.025f;

                UpdateBrushScale();
            }
        }
    }

    public void ChangeMode(int _mode)
    {
        Mode = (VRPlacementMode)_mode;
    }

    void SpawnBlocksAtPosition(Vector3 pos, float radius)
    {
        PositionKey[][] blockDictKeys;
        Matrix4x4[][] blocks = GameManager.Bond.GetBlocksInSphere(pos, radius, out blockDictKeys);
        BlockContainer.Container.AddBlocks(blocks, blockDictKeys);
    }

    void RemoveBlocksAtPosition(Vector3 pos, float radius)
    {
        PositionKey[][] blockDictKeys;
        Matrix4x4[][] blocks = GameManager.Bond.GetBlocksInSphere(pos, radius, out blockDictKeys);
        BlockContainer.Container.RemoveBlocks(blocks, blockDictKeys);
    }

    void SpawnBlocksInBox()
    {
        PositionKey[][] blockDictKeys;
        Matrix4x4[][] blocks = GameManager.Bond.GetBlocksInBox(cubeBehaviour.Min, cubeBehaviour.Max, out blockDictKeys);
        BlockContainer.Container.AddBlocks(blocks, blockDictKeys);
    }

    void RemoveBlocksInBox()
    {
        PositionKey[][] blockDictKeys;
        Matrix4x4[][] blocks = GameManager.Bond.GetBlocksInBox(cubeBehaviour.Min, cubeBehaviour.Max, out blockDictKeys);
        BlockContainer.Container.RemoveBlocks(blocks, blockDictKeys);
    }

    void SpawnBlocksInCylinder()
    {
        PositionKey[][] blockDictKeys;
        Matrix4x4[][] blocks = GameManager.Bond.GetBlocksInCylinder(cylinder.transform.position, cylinderRadius, cylinderHeight, out blockDictKeys);
        BlockContainer.Container.AddBlocks(blocks, blockDictKeys);
    }

    void RemoveBlocksInCylinder()
    {
        PositionKey[][] blockDictKeys;
        Matrix4x4[][] blocks = GameManager.Bond.GetBlocksInCylinder(cylinder.transform.position, cylinderRadius, cylinderHeight, out blockDictKeys);
        BlockContainer.Container.RemoveBlocks(blocks, blockDictKeys);
    }

    void SpawnBlocksInBrush()
    {
        PositionKey[][] blockDictKeys;
        Matrix4x4[][] blocks = GameManager.Bond.GetBlocksInMesh(brushes[brushIndex].GetComponent<MeshCollider>(), out blockDictKeys);
        BlockContainer.Container.AddBlocks(blocks, blockDictKeys);
    }

    void RemoveBlocksInBrush()
    {
        PositionKey[][] blockDictKeys;
        Matrix4x4[][] blocks = GameManager.Bond.GetBlocksInMesh(brushes[brushIndex].GetComponent<MeshCollider>(), out blockDictKeys);
        BlockContainer.Container.RemoveBlocks(blocks, blockDictKeys);
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

    public void UpdateBrushScale()
    {
        brushes[brushIndex].transform.localScale = brushScales[brushIndex];
        //brushes[brushIndex].transform.localPosition = Vector3.forward * brushSizes[brushIndex].z * brushScales[brushIndex].z / 2;
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
    SpawnInBrush,
    DeleteFromBrush,
    BlocksToPointCloud,
    PointCloudToBlocks
}
