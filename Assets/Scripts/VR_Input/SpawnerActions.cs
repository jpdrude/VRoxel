using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Valve.VR;

public class SpawnerActions : MonoBehaviour
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

    public SteamVR_Action_Boolean Trigger { get { return trigger; } }
    public SteamVR_Action_Boolean Up { get { return up; } }
    public SteamVR_Action_Boolean Down { get { return down; } }
    public SteamVR_Action_Boolean Left { get { return left; } }
    public SteamVR_Action_Boolean Right { get { return right; } }
   

    private void Update()
    {
        if (ModuleContainer.Container == null) return;

        if (trigger.GetState(SteamVR_Input_Sources.RightHand))
            OnTrigger();

        if (trigger.GetStateUp(SteamVR_Input_Sources.RightHand))
            OnTriggerUp();

        bool _up = up.GetState(SteamVR_Input_Sources.RightHand);
        bool _down = down.GetState(SteamVR_Input_Sources.RightHand);
        bool _left = left.GetState(SteamVR_Input_Sources.RightHand);
        bool _right = right.GetState(SteamVR_Input_Sources.RightHand);

        if (_up || _down || _left || _right)
            DPadInput(_up, _down, _left, _right);

        MessageSystem.DisplayPermanentMessage(ModuleContainer.Container.GetCount() + " Units");
    }

    public virtual void OnTrigger() { }
    public virtual void OnTriggerUp() { }
    public virtual void DPadInput(bool _up, bool _down, bool _left, bool _right) { }
    public virtual void Disable() { }
}
