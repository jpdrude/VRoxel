using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Valve.VR;

public class SteamVR_AssemblySpawner : MonoBehaviour
{
    [Header("VR-Input")]
    [SerializeField]
    SteamVR_Action_Boolean trigger;

    [SerializeField]
    SteamVR_Action_Boolean up;
    [SerializeField]
    SteamVR_Action_Boolean down;
    [SerializeField]
    SteamVR_Action_Boolean left;
    [SerializeField]
    SteamVR_Action_Boolean right;

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

    VRPlacementMode mode = VRPlacementMode.SpawnInSphere;
    public VRPlacementMode Mode
    {
        get { return mode; }
        set
        {
            mode = value;
            if (mode == VRPlacementMode.SpawnInSphere)
            {
                sphere.SetActive(true);
                sphere.GetComponent<MeshRenderer>().material = spawnMat;
                box.SetActive(false);
                cylinder.SetActive(false);
                UpdateSphereScale();
            }
            else if (mode == VRPlacementMode.DeleteFromSphere)
            {
                sphere.SetActive(true);
                sphere.GetComponent<MeshRenderer>().material = deleteMat;
                box.SetActive(false);
                cylinder.SetActive(false);
                UpdateSphereScale();
            }
            else if (mode == VRPlacementMode.SpawnInBox)
            {
                box.SetActive(true);
                box.GetComponent<MeshRenderer>().material = spawnMat;
                sphere.SetActive(false);
                cylinder.SetActive(false);
                UpdateBoxScale();
            }
            else if (mode == VRPlacementMode.DeleteFromBox)
            {
                box.SetActive(true);
                box.GetComponent<MeshRenderer>().material = deleteMat;
                sphere.SetActive(false);
                cylinder.SetActive(false);
                UpdateBoxScale();
            }
            else if (mode == VRPlacementMode.SpawnInCylinder)
            {
                cylinder.SetActive(true);
                cylinder.GetComponent<MeshRenderer>().material = spawnMat;
                sphere.SetActive(false);
                box.SetActive(false);
                UpdateCylinderScale();
            }
            else if (mode == VRPlacementMode.DeleteFromCylinder)
            {
                cylinder.SetActive(true);
                cylinder.GetComponent<MeshRenderer>().material = deleteMat;
                sphere.SetActive(false);
                box.SetActive(false);
                UpdateCylinderScale();
            }
        }
    }

    float radius = 0.05f;
    Vector3 boxSize = new Vector3(0.1f, 0.1f, 0.1f);
    float cylinderRadius = 0.05f;
    float cylinderHeight = 0.1f;
    SpawnCubeBehaviour cubeBehaviour;
    Vector3 initialOffset = new Vector3(0, 0, 0.05f);
    float scaleSpeed = 0.1f;

    private void Start()
    {
        Mode = VRPlacementMode.SpawnInSphere;
        cubeBehaviour = GetComponentInChildren<SpawnCubeBehaviour>(true);
    }

    // Update is called once per frame
    void Update()
    {
        if (ModuleContainer.Container == null) return;
        CheckTrigger();
        CheckSphereScale();
        CheckBoxScale();
        CheckCylinderScale();
        MessageSystem.DisplayPermanentMessage(ModuleContainer.Container.GetCount() + " Bricks");
    }

    void CheckTrigger()
    {
        if (trigger.GetState(SteamVR_Input_Sources.RightHand))
        {
            if (Mode == VRPlacementMode.SpawnInSphere)
                SpawnBricksAtPosition(transform.position, radius);
            else if (Mode == VRPlacementMode.DeleteFromSphere)
                RemoveBricksAtPosition(transform.position, radius);
            else if (Mode == VRPlacementMode.SpawnInBox)
                SpawnBricksInBox();
            else if (Mode == VRPlacementMode.DeleteFromBox)
                RemoveBricksInBox();
            else if (Mode == VRPlacementMode.SpawnInCylinder)
                SpawnBricksInCylinder();
            else if (Mode == VRPlacementMode.DeleteFromCylinder)
                RemoveBricksInCylinder();
        }
    }

    void CheckSphereScale()
    {
        if (Mode == VRPlacementMode.SpawnInSphere || Mode == VRPlacementMode.DeleteFromSphere)
        {

            if (up.GetState(SteamVR_Input_Sources.RightHand))
            {
                radius += Time.deltaTime * scaleSpeed;
                UpdateSphereScale();
            }
            else if (down.GetState(SteamVR_Input_Sources.RightHand))
            {
                radius -= Time.deltaTime * scaleSpeed;
                if (radius < 0.001f)
                    radius = 0.001f;
                UpdateSphereScale();
            }
        }
    }

    void CheckCylinderScale()
    {
        if (Mode == VRPlacementMode.SpawnInCylinder || Mode == VRPlacementMode.DeleteFromCylinder)
        {
            if (up.GetState(SteamVR_Input_Sources.RightHand))
            {
                cylinderHeight += Time.deltaTime * scaleSpeed;
                UpdateCylinderScale();
            }
            else if (down.GetState(SteamVR_Input_Sources.RightHand))
            {
                cylinderHeight -= Time.deltaTime * scaleSpeed;
                if (cylinderHeight < 0.025f)
                    cylinderHeight = 0.025f;
                UpdateCylinderScale();
            }
            else if (right.GetState(SteamVR_Input_Sources.RightHand))
            {
                cylinderRadius += Time.deltaTime * scaleSpeed;
                UpdateCylinderScale();
            }
            else if (left.GetState(SteamVR_Input_Sources.RightHand))
            {
                cylinderRadius -= Time.deltaTime * scaleSpeed;
                if (cylinderRadius < 0.01f)
                    cylinderRadius = 0.01f;
                UpdateCylinderScale();
            }
        }
    }

    void CheckBoxScale()
    {
        if (Mode == VRPlacementMode.SpawnInBox || Mode == VRPlacementMode.DeleteFromBox)
        {
            if (up.GetState(SteamVR_Input_Sources.RightHand))
            {
                boxSize += Time.deltaTime * scaleSpeed * Vector3.one;
                UpdateBoxScale();
            }
            else if (down.GetState(SteamVR_Input_Sources.RightHand))
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
            else if (right.GetState(SteamVR_Input_Sources.RightHand))
            {
                boxSize += Time.deltaTime * scaleSpeed * new Vector3(1, 1, 0);

                UpdateBoxScale();
            }
            else if (left.GetState(SteamVR_Input_Sources.RightHand))
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
        Matrix4x4[] bricks = GameManager.Bond.GetBricksInCylinder(transform.position, cylinderRadius, cylinderHeight, out dictPositions);
        ModuleContainer.Container.AddBricks(bricks, dictPositions);
    }

    void RemoveBricksInCylinder()
    {
        Vector3Int[] dictPositions;
        Matrix4x4[] bricks = GameManager.Bond.GetBricksInCylinder(transform.position, cylinderRadius, cylinderHeight, out dictPositions);
        ModuleContainer.Container.RemoveBricks(bricks, dictPositions);
    }

    void UpdateSphereScale()
    {
        sphere.transform.localScale = Vector3.one * 2 * radius;
        transform.localPosition = initialOffset + Vector3.forward * radius;
    }

    void UpdateBoxScale()
    {
        box.transform.localScale = boxSize;
        transform.localPosition = initialOffset + Vector3.forward * (boxSize.z / 2);
    }

    void UpdateCylinderScale()
    {
        cylinder.transform.localScale = new Vector3(cylinderRadius * 2, cylinderHeight, cylinderRadius * 2);
        transform.localPosition = initialOffset + Vector3.forward * cylinderRadius;
    }
}

public enum VRPlacementMode
{
    SpawnInSphere,
    DeleteFromSphere,
    SpawnInBox,
    DeleteFromBox,
    SpawnInCylinder,
    DeleteFromCylinder
}
