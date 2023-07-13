using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SteamVR_CharacterPointer : SpawnerActions
{
    [Header("Pointer")]
    [SerializeField]
    GameObject pointer;


    CharacterPointerBehaviour pointerBehaviour;

    SteamVR_AssemblySpawner assemblySpawner;
    SteamVR_PointCloudSelector cloudSelector;
    SteamVR_GeometryPointer geometryPointer;

    private void Awake()
    {
        pointerBehaviour = pointer.GetComponent<CharacterPointerBehaviour>();

        assemblySpawner = GetComponent<SteamVR_AssemblySpawner>();
        cloudSelector = GetComponent<SteamVR_PointCloudSelector>();
        geometryPointer = GetComponent<SteamVR_GeometryPointer>();
    }

    public override void Disable()
    {
        enabled = false;
        pointer.SetActive(false);
    }

    public void Enable()
    {
        assemblySpawner.Disable();
        cloudSelector.Disable();
        geometryPointer.Disable();

        enabled = true;
        pointer.SetActive(true);
    }



    public override void OnTriggerDown()
    {
        pointerBehaviour.OnTriggerDown();
    }
}
