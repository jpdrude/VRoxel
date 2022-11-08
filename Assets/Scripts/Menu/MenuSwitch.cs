using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Valve.VR.InteractionSystem;
using Valve.VR;
using UnityEngine.XR;
using UnityEngine.UI;
using UnityEngine.ProBuilder;
using UnityEngine.ProBuilder.MeshOperations;

public class MenuSwitch : MonoBehaviour
{
    [Header("Menu Selection")]
    [SerializeField]
    GameObject activeAggregationTool;

    [SerializeField]
    GameObject menuSelector;

    [Space]
    [Header("Menu Options")]
    [SerializeField]
    GameObject icons;
    [SerializeField]
    GameObject reallyNew;
    [SerializeField]
    GameObject reallyLoad;

    [Space]
    [Header("VR Player Elevation")]
    [SerializeField]
    GameObject vrPlayer;
    [SerializeField]
    float elevationSpeed = 0.5f;
    [SerializeField]
    Material platformMaterial;
    [SerializeField]
    GameObject platformTemplate;
    [SerializeField]
    SteamVR_PlayArea playArea;

    [Space]
    [Header("Add/Remove Buttons")]
    [SerializeField]
    SteamVR_AssemblySpawner spawner;
    [SerializeField]
    List<GameObject> addButtons;
    public List<GameObject> AddButtons { get { return addButtons; } }
    [SerializeField]
    List<GameObject> removeButtons;
    public List<GameObject> RemoveButtons { get { return removeButtons; } }

    [Space]
    [Header("Lock Ortho")]
    [SerializeField]
    GameObject lockOrthoSymbol;
    [SerializeField]
    GameObject unlockOrthoSymbol;

    float playerHeight = 0;
    GameObject platform = null;
    Vector3 basePos;
    int addIndex = 0;
    int removeIndex = 0;

    private void Start()
    {
        activeAggregationTool.SetActive(true);
        menuSelector.SetActive(false);

        reallyLoad.SetActive(false);
        reallyNew.SetActive(false);

        Teleport.Player.Listen(PlayerTeleported);
        //platform = PlatformMesh.GeneratePlatformMesh(platformMaterial);
        platform = PlatformMesh.GeneratePlatformMesh(platformMaterial, platformTemplate);

        SetAddButton(0);
        SetRemoveButton(0);
    }

    void SetAddButton(int index)
    {
        foreach (GameObject add in addButtons)
            add.SetActive(false);

        addButtons[index].SetActive(true);
    }

    void SetRemoveButton(int index)
    {
        foreach (GameObject remove in removeButtons)
            remove.SetActive(false);

        removeButtons[index].SetActive(true);
    }


    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.tag != "MenuTrigger") return;
        Debug.Log("Should switch to selection mode");
        activeAggregationTool.SetActive(false);
        menuSelector.SetActive(true);
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.gameObject.tag != "MenuTrigger") return;
        Debug.Log("Should switch to placement mode");
        activeAggregationTool.SetActive(true);
        menuSelector.SetActive(false);
    }

    public void ReallyLoad()
    {
        icons.SetActive(false);
        reallyLoad.SetActive(true);
    }

    public void ReallyNew()
    {
        icons.SetActive(false);
        reallyNew.SetActive(true);
    }

    public void RestoreMenu()
    {
        icons.SetActive(true);
        reallyNew.SetActive(false);
        reallyLoad.SetActive(false);
    }

    public void RaisePlayer()
    {
        playerHeight += Time.deltaTime * elevationSpeed;

        SetPlayerHeight();
    }

    public void LowerPlayer()
    {
        playerHeight -= Time.deltaTime * elevationSpeed;
        if (playerHeight < 0) playerHeight = 0;

        SetPlayerHeight();
    }

    public void ModulesToPointCloud()
    {
        BlockContainer.Container.BlocksToPointCloud();
    }

    public void NextAdd()
    {
        ++addIndex;
        if (addIndex >= addButtons.Count)
            addIndex = 0;

        SetAddButton(addIndex);
    }

    public void NextRemove()
    {
        ++removeIndex;
        if (removeIndex >= removeButtons.Count)
            removeIndex = 0;

        SetRemoveButton(removeIndex);
    }

    public void PreviousAdd()
    {
        --addIndex;
        if (addIndex < 0)
            addIndex = addButtons.Count - 1;

        SetAddButton(addIndex);
    }

    public void PreviousRemove()
    {
        --removeIndex;
        if (removeIndex < 0)
            removeIndex = removeButtons.Count - 1;

        SetRemoveButton(removeIndex);
    }

    public void ToggleMeshSpawnerOrtho()
    {
        if (!SpawnMeshBehaviour.AlignToWorld)
        {
            lockOrthoSymbol.SetActive(true);
            unlockOrthoSymbol.SetActive(false);
        }
        else
        {
            lockOrthoSymbol.SetActive(false);
            unlockOrthoSymbol.SetActive(true);
        }

        SpawnMeshBehaviour.AlignToWorld = !SpawnMeshBehaviour.AlignToWorld;
        spawner.UpdateBrushScale();
    }

    public VRMenuItem GetCurrentAdd()
    {
        return AddButtons[addIndex].GetComponent<VRMenuItem>();
    }

    public VRMenuItem GetCurrentRemove()
    {
        return RemoveButtons[removeIndex].GetComponent<VRMenuItem>();
    }

    void SetPlayerHeight()
    {
        Vector3 currentPos = vrPlayer.transform.position;
        currentPos.y = playerHeight + basePos.y;
        vrPlayer.transform.position = currentPos;

        Vector3 platformScale = platform.transform.localScale;
        platform.transform.localScale = new Vector3(platformScale.x, playerHeight, platformScale.z);
        platform.transform.position = basePos;

        if (playerHeight == 0)
            platform.SetActive(false);
        else
            platform.SetActive(true);
    }

    void PlayerTeleported(TeleportMarkerBase marker)
    {
        basePos = vrPlayer.transform.position;
        SetPlayerHeight();
    } 


}
