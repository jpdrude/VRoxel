using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SteamVR_GeometryPointer : SpawnerActions
{
    [Header("Pointer")]
    [SerializeField]
    GameObject pointer;

    GeometryPointerBehaviour pointerBehaviour;

    SteamVR_AssemblySpawner assemblySpawner;
    SteamVR_PointCloudSelector cloudSelector;
    SteamVR_CharacterPointer characterPointer;

    private void Awake()
    {
        pointerBehaviour = pointer.GetComponent<GeometryPointerBehaviour>();

        assemblySpawner = GetComponent<SteamVR_AssemblySpawner>();
        cloudSelector = GetComponent<SteamVR_PointCloudSelector>();
        characterPointer = GetComponent<SteamVR_CharacterPointer>();
    }

    public override void Disable()
    {
        pointerBehaviour.AbortPlacement();
        enabled = false;
        pointer.SetActive(false);
    }

    public void Enable()
    {
        assemblySpawner.Disable();
        cloudSelector.Disable();
        characterPointer.Disable();

        enabled = true;
        pointer.SetActive(true);
    }

    public override void OnTriggerDown()
    {
        pointerBehaviour.OnTriggerDown();
    }

    public override void DPadInputPressed(bool _up, bool _down, bool _left, bool _right)
    {
        if (_up) pointerBehaviour.DPadUpPressed();
        else if (_down) pointerBehaviour.DPadDownPressed();
        else if (_left) pointerBehaviour.DPadLeftPressed();
        else if (_right) pointerBehaviour.DPadRightPressed();
    }

    public override void DPadInput(bool _up, bool _down, bool _left, bool _right)
    {
        if (_left) pointerBehaviour.DPadLeft();
        else if (_right) pointerBehaviour.DPadRight();
    }

    public override void OnQuickSwitchDown()
    {
        pointerBehaviour.AbortPlacement();
    }
}
