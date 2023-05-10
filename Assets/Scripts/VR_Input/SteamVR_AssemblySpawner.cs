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

    SteamVR_PointCloudSelector cloudSelector;
    SteamVR_CharacterPointer characterPointer;

    VRPlacementMode mode = VRPlacementMode.SpawnInSphere;
    public VRPlacementMode Mode
    {
        get { return mode; }
        set
        {
            mode = value;
            enabled = true;
            cloudSelector.Disable();
            characterPointer.Disable();

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
        cloudSelector = GetComponent<SteamVR_PointCloudSelector>();
        characterPointer = GetComponent<SteamVR_CharacterPointer>();
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

    public override void DPadInputPressed(bool _up, bool _down, bool _left, bool _right)
    {
        if (Mode == VRPlacementMode.SpawnInSphere || Mode == VRPlacementMode.DeleteFromSphere)
            CheckSphereScale(_up, _down);
        else if (Mode == VRPlacementMode.SpawnInBox || Mode == VRPlacementMode.DeleteFromBox)
            CheckBoxScale(_up, _down, _left, _right);
        else if (Mode == VRPlacementMode.SpawnInCylinder || Mode == VRPlacementMode.DeleteFromCylinder)
            CheckCylinderScale(_up, _down, _left, _right);
        else if (Mode == VRPlacementMode.SpawnInBrush || Mode == VRPlacementMode.DeleteFromBrush)
            CheckBrushScale(_up, _down, _left, _right);
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
                boxSize += Time.deltaTime * scaleSpeed * BrushOptions.GetConstraints();

                UpdateBoxScale();
            }
            else if (left)
            {
                boxSize -= Time.deltaTime * scaleSpeed * BrushOptions.GetConstraints();
                if (boxSize.x < 0.025f)
                    boxSize.x = 0.025f;
                if (boxSize.y < 0.025f)
                    boxSize.y = 0.025f;

                UpdateBoxScale();
            }
        }
    }

    void CheckBrushScale(bool up, bool down, bool left, bool right)
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
            else if (right)
            {
                brushScales[brushIndex] += Time.deltaTime * scaleSpeed * BrushOptions.GetConstraints();
                UpdateBrushScale();
            }
            else if (left)
            {
                brushScales[brushIndex] -= Time.deltaTime * scaleSpeed * BrushOptions.GetConstraints();
                if (brushScales[brushIndex].x < 0.025f)
                    brushScales[brushIndex] = new Vector3(0.025f, brushScales[brushIndex].y, brushScales[brushIndex].z);
                if (brushScales[brushIndex].y < 0.025f)
                    brushScales[brushIndex] = new Vector3(brushScales[brushIndex].x, 0.025f, brushScales[brushIndex].z);
                if (brushScales[brushIndex].z < 0.025f)
                    brushScales[brushIndex] = new Vector3(brushScales[brushIndex].x, brushScales[brushIndex].y, 0.025f);

                UpdateBrushScale();
            }
        }
    }

    public void ResetNUScaling()
    {
        if (Mode == VRPlacementMode.DeleteFromBox || Mode == VRPlacementMode.SpawnInBox)
        {
            float scale = boxSize.x;
            if (boxSize.y < scale) scale = boxSize.y;
            if (boxSize.z < scale) scale = boxSize.z;

            boxSize = Vector3.one * scale;
            UpdateBoxScale();
        }

        if (Mode == VRPlacementMode.DeleteFromBrush || Mode == VRPlacementMode.SpawnInBrush)
        {
            float scale = brushScales[brushIndex].x;
            if (brushScales[brushIndex].y < scale) scale = brushScales[brushIndex].y;
            if (brushScales[brushIndex].z < scale) scale = brushScales[brushIndex].z;

            brushScales[brushIndex] = Vector3.one * scale;
            UpdateBrushScale();
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
        if (brushes.Count > 0 && brushes[brushIndex] != null)
            brushes[brushIndex].transform.localScale = brushScales[brushIndex];
    }

    public void UpdateAllScales()
    {
        UpdateSphereScale();
        UpdateBoxScale();
        UpdateCylinderScale();
        UpdateBrushScale();
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
