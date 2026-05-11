using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Valve.VR;

public class SpawnerActions : MonoBehaviour
{
    [Header("VR-Input")]
    [SerializeField]
    SteamVR_Action_Boolean trigger;

    [Space]
    [SerializeField]
    SteamVR_Action_Boolean up;
    [SerializeField]
    SteamVR_Action_Boolean down;
    [SerializeField]
    SteamVR_Action_Boolean left;
    [SerializeField]
    SteamVR_Action_Boolean right;

    [Space]
    [SerializeField]
    SteamVR_Action_Boolean quickOptionSwitch;

    [Space]
    [SerializeField]
    SteamVR_Input_Sources mainInputSource;
    public SteamVR_Input_Sources MainInputSources { get { return mainInputSource; } set { mainInputSource = value; } }


    public SteamVR_Action_Boolean Trigger { get { return trigger; } }
    public SteamVR_Action_Boolean Up { get { return up; } }
    public SteamVR_Action_Boolean Down { get { return down; } }
    public SteamVR_Action_Boolean Left { get { return left; } }
    public SteamVR_Action_Boolean Right { get { return right; } }
    public SteamVR_Action_Boolean QuickOptionSwitch { get { return quickOptionSwitch; } }

    private int _lastDisplayedCount = -1;

    private void Update()
    {
        if (BlockContainer.Container == null) return;

        if (trigger.GetStateDown(mainInputSource))
            OnTriggerDown();

        if (trigger.GetState(mainInputSource))
            OnTrigger();

        if (trigger.GetStateUp(mainInputSource))
            OnTriggerUp();

        if (quickOptionSwitch.GetStateDown(SteamVR_Input_Sources.Any))
            OnQuickSwitchDown();

        bool _up = up.GetStateDown(mainInputSource);
        bool _down = down.GetStateDown(mainInputSource);
        bool _left = left.GetStateDown(mainInputSource);
        bool _right = right.GetStateDown(mainInputSource);

        if (_up || _down || _left || _right)
            DPadInput(_up, _down, _left, _right);

        _up = up.GetState(mainInputSource);
        _down = down.GetState(mainInputSource);
        _left = left.GetState(mainInputSource);
        _right = right.GetState(mainInputSource);

        if (_up || _down || _left || _right)
            DPadInputPressed(_up, _down, _left, _right);

        int count = BlockContainer.Container.GetCount();
        if (count != _lastDisplayedCount)
        {
            _lastDisplayedCount = count;
            MessageSystem.DisplayPermanentMessage(count + " Units");
        }
    }

    public virtual void OnTrigger() { }
    public virtual void OnTriggerDown() { }
    public virtual void OnTriggerUp() { }
    public virtual void OnQuickSwitchDown() { }
    public virtual void DPadInputPressed(bool _up, bool _down, bool _left, bool _right) { }
    public virtual void DPadInput(bool _up, bool _down, bool _left, bool _right) { }
    public virtual void Disable() { }
}
