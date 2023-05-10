using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SteamVR_CharacterPointer : SpawnerActions
{
    [Space]
    [Header("Pointer")]
    [SerializeField]
    GameObject pointer;


    CharacterPointerBehaviour pointerBehaviour;

    SteamVR_AssemblySpawner assemblySpawner;
    SteamVR_PointCloudSelector cloudSelector;

    private void Awake()
    {
        pointerBehaviour = pointer.GetComponent<CharacterPointerBehaviour>();

        assemblySpawner = GetComponent<SteamVR_AssemblySpawner>();
        cloudSelector = GetComponent<SteamVR_PointCloudSelector>();
    }

    public override void Disable()
    {
        enabled = false;
        pointer.SetActive(false);
    }

    public void Enable()
    {
        Debug.Log("Should enable ANimationPointer");
        assemblySpawner.Disable();
        cloudSelector.Disable();

        enabled = true;
        pointer.SetActive(true);
    }

    public override void OnTriggerDown()
    {
        pointerBehaviour.OnTriggerDown();
    }
}
