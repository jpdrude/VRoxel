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
                UpdateSphereScale();
            }
            else if (mode == VRPlacementMode.DeleteFromSphere)
            {
                sphere.SetActive(true);
                sphere.GetComponent<MeshRenderer>().material = deleteMat;
                box.SetActive(false);
                UpdateSphereScale();
            }
            else if (mode == VRPlacementMode.SpawnInBox)
            {
                box.SetActive(true);
                box.GetComponent<MeshRenderer>().material = spawnMat;
                sphere.SetActive(false);
                UpdateBoxScale();
            }
            else if (mode == VRPlacementMode.DeleteFromBox)
            {
                box.SetActive(true);
                box.GetComponent<MeshRenderer>().material = deleteMat;
                sphere.SetActive(false);
                UpdateBoxScale();
            }
        }  
    }

    float radius = 0.05f;
    Vector3 boxSize = new Vector3(0.1f, 0.1f, 0.1f);
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
        }
    }

    void CheckSphereScale()
    {
        if (Mode == VRPlacementMode.SpawnInBox || Mode == VRPlacementMode.DeleteFromBox)
            return;

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

    void CheckBoxScale()
    {
        if (Mode == VRPlacementMode.SpawnInSphere || Mode == VRPlacementMode.DeleteFromSphere)
            return;

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

    public void ChangeMode(int _mode)
    {
        Mode = (VRPlacementMode)_mode;
    }

    void SpawnBricksAtPosition(Vector3 pos, float radius)
    {
        ModuleContainer.Container.AddBricks(GameManager.Bond.GetBricksInSphere(pos, radius));
    }

    void RemoveBricksAtPosition(Vector3 pos, float radius)
    {
        ModuleContainer.Container.RemoveBricks(GameManager.Bond.GetBricksInSphere(pos, radius));
    }

    void SpawnBricksInBox()
    {
        ModuleContainer.Container.AddBricks(GameManager.Bond.GetBricksInBox(cubeBehaviour.Min, cubeBehaviour.Max));
    }

    void RemoveBricksInBox()
    {
        ModuleContainer.Container.RemoveBricks(GameManager.Bond.GetBricksInBox(cubeBehaviour.Min, cubeBehaviour.Max));
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
}

public enum VRPlacementMode
{
    SpawnInSphere,
    DeleteFromSphere,
    SpawnInBox,
    DeleteFromBox
}
