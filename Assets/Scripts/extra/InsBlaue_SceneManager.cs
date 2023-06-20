using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Valve.VR;

public class InsBlaue_SceneManager : MonoBehaviour
{
    [SerializeField]
    GraphicsSettingsMenu graphicsSettings;

    [Space]
    [Header("VR")]
    [SerializeField]
    GameObject vrController;

    [SerializeField]
    SteamVR_ActionSet actionSet;

    [SerializeField]
    List<SteamVR_Action_Boolean> inputActions;

    [SerializeField]
    SteamVR_Input_Sources inputSources;

    [Space]
    [Header("Scene Data")]
    [SerializeField]
    List<InsBlaueSceneData> sceneData;
    int sceneCounter = 0;
    Stack<Transform> teleportStack;

    // Start is called before the first frame update
    void Start()
    {
        graphicsSettings.CustomGraphics(1, false, false, 1);

        GameManager.Manager.SetBond(GameManager.AvailableBonds["LegoBond_porous"]);

        PointCloudIO.DisplayPointCloud("InsBlaue/Apartment_0.005.xyz.bin");

        ChangeScene();

        actionSet.Activate();
    }

    // Update is called once per frame
    void Update()
    {
        bool change = false;

        if (Input.GetMouseButtonDown(0))
            change = true;

        foreach (SteamVR_Action_Boolean action in inputActions)
            if (action.GetStateDown(inputSources))
                change = true;

        if (change)
        {
            if (teleportStack.Count == 0)
            {
                ChangeScene();
                Teleport();
            }
            else
                Teleport();
        }    
    }

    void ChangeScene()
    {
        if (BlockContainer.Container.GetCount() > 0)
        GameManager.Manager.ClearAssembly();

        AssemblyIO.LoadAssemblyLayout(System.IO.Path.Combine(Application.streamingAssetsPath, "InsBlaue/VRoxelFiles", sceneData[sceneCounter].fileName));

        sceneData[sceneCounter].teleportPositions.Reverse();
        teleportStack = new Stack<Transform>(sceneData[sceneCounter].teleportPositions);
        sceneData[sceneCounter].teleportPositions.Reverse();

        HideNames();
        sceneData[sceneCounter].nameObject.SetActive(true);

        ++sceneCounter;
        if (sceneCounter >= sceneData.Count)
            sceneCounter = 0;
    }

    void Teleport()
    {
        if (teleportStack.Count == 0) return;

        Transform newPos = teleportStack.Pop();
        vrController.transform.position = newPos.position;
        vrController.transform.rotation = newPos.rotation;
    }

    void HideNames()
    {
        foreach (InsBlaueSceneData data in sceneData)
        {
            data.nameObject.SetActive(false);
        }
    }
}

[System.Serializable]
public class InsBlaueSceneData
{
    public string fileName;
    public GameObject nameObject;
    public List<Transform> teleportPositions;
}