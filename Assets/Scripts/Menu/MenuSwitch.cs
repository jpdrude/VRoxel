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
    SteamVR_PlayArea playArea;

    float playerHeight = 0;
    GameObject platform = null;

    private void Start()
    {
        activeAggregationTool.SetActive(true);
        menuSelector.SetActive(false);

        reallyLoad.SetActive(false);
        reallyNew.SetActive(false);

        Teleport.Player.Listen(PlayerTeleported);
        platform = PlatformMesh.GeneratePlatformMesh(platformMaterial);
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
        PointCloudIO.ModulesToCloud(ModuleContainer.Container.ModulesToPC());
    }

    void SetPlayerHeight()
    {
        Vector3 currentPos = vrPlayer.transform.position;
        currentPos.y = playerHeight;
        vrPlayer.transform.position = currentPos;

        platform.transform.localScale = new Vector3(1, playerHeight, 1);
        platform.transform.position = new Vector3(vrPlayer.transform.position.x, 0, vrPlayer.transform.position.z);

        if (playerHeight == 0)
            platform.SetActive(false);
        else
            platform.SetActive(true);
    }

    void PlayerTeleported(TeleportMarkerBase marker)
    {
        SetPlayerHeight();
    } 


}
