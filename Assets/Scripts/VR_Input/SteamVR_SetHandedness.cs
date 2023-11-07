using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Valve.VR;

public class SteamVR_SetHandedness : MonoBehaviour
{
    [Header("Hands")]
    [SerializeField]
    Transform leftHand;
    [SerializeField]
    Transform rightHand;

    [Space]
    [Header("Items")]
    [SerializeField]
    GameObject menu;
    Quaternion menuRot;
    Vector3 menuPos;

    [SerializeField]
    GameObject spawner;
    Quaternion spawnerRot;
    Vector3 spawnerPos;

    [SerializeField]
    GameObject menuTrigger;
    Quaternion menuTriggerRot;
    Vector3 menuTriggerPos;

    [SerializeField]
    GameObject menuSelector;
    Quaternion menuSelectorRot;
    Vector3 menuSelectorPos;

    [SerializeField]
    GameObject quickOptionSwitch;
    Quaternion quickOptionSwitchRot;
    Vector3 quickOptionSwitchPos;

    // Start is called before the first frame update
    void Start()
    {
        menuPos = menu.transform.localPosition;
        spawnerPos = spawner.transform.localPosition;
        menuTriggerPos = menuTrigger.transform.localPosition;
        menuSelectorPos = menuSelector.transform.localPosition;
        quickOptionSwitchPos = quickOptionSwitch.transform.localPosition;

        menuRot = menu.transform.localRotation;
        spawnerRot = spawner.transform.localRotation;
        menuTriggerRot = menuTrigger.transform.localRotation;
        menuSelectorRot = menuSelector.transform.localRotation;
        quickOptionSwitchRot = quickOptionSwitch.transform.localRotation;
    }

    void SetHandedness(SteamVR_Input_Sources menuInputSource, SteamVR_Input_Sources xyzConstraintInputSource, Transform menuHand, Transform spawnerHand)
    {
        menu.transform.parent = menuHand;
        menu.transform.localRotation = menuRot;
        menu.transform.localPosition = menuPos;

        spawner.transform.parent = spawnerHand;
        spawner.transform.localRotation = spawnerRot;
        spawner.transform.localPosition = spawnerPos;

        menuTrigger.transform.parent = spawnerHand;
        menuTrigger.transform.localRotation = menuTriggerRot;
        menuTrigger.transform.localPosition = menuTriggerPos;

        menuSelector.transform.parent = spawnerHand;
        menuSelector.transform.localRotation = menuSelectorRot;
        menuSelector.transform.localPosition = menuSelectorPos;

        quickOptionSwitch.transform.parent = spawnerHand;
        quickOptionSwitch.transform.localRotation = quickOptionSwitchRot;
        quickOptionSwitch.transform.localPosition = quickOptionSwitchPos;

        foreach (SpawnerActions actions in leftHand.gameObject.GetComponentsInChildren<SpawnerActions>(true))
            actions.MainInputSources = Valve.VR.SteamVR_Input_Sources.LeftHand;

        foreach (SpawnerActions actions in rightHand.gameObject.GetComponentsInChildren<SpawnerActions>(true))
            actions.MainInputSources = Valve.VR.SteamVR_Input_Sources.RightHand;

        foreach (VRMenuItem menuItem in menu.GetComponentsInChildren<VRMenuItem>(true))
            menuItem.InputSource = menuInputSource;

        XYZConstraint.InputSource = xyzConstraintInputSource;
    }

    public void SetRightHanded()
    {
        SetHandedness(SteamVR_Input_Sources.RightHand, SteamVR_Input_Sources.LeftHand, leftHand, rightHand);
    }

    public void SetLeftHanded()
    {
        SetHandedness(SteamVR_Input_Sources.LeftHand, SteamVR_Input_Sources.RightHand, rightHand, leftHand);
    }
}
