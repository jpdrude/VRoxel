using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BrowseBrushes : SpawnerActions
{
    [SerializeField]
    MenuSwitch menu;

    [SerializeField]
    SteamVR_AssemblySpawner spawner;

    public override void DPadInput(bool _up, bool _down, bool _left, bool _right)
    {
        if (!spawner.enabled) return;

        if (spawner.Mode == VRPlacementMode.SpawnInSphere || spawner.Mode == VRPlacementMode.SpawnInBox ||
            spawner.Mode == VRPlacementMode.SpawnInCylinder || spawner.Mode == VRPlacementMode.SpawnInCapsule ||
            spawner.Mode == VRPlacementMode.SpawnInBrush)
        {
            if (_left)
            {
                menu.PreviousAdd();
                menu.GetCurrentAdd().RemoteInvoke();
            }
            else if (_right)
            {
                menu.NextAdd();
                menu.GetCurrentAdd().RemoteInvoke();
            }
        }
        else if (spawner.Mode == VRPlacementMode.DeleteFromSphere || spawner.Mode == VRPlacementMode.DeleteFromBox ||
            spawner.Mode == VRPlacementMode.DeleteFromCylinder || spawner.Mode == VRPlacementMode.DeleteFromCapsule ||
            spawner.Mode == VRPlacementMode.DeleteFromBrush)
        {
            if (_left)
            {
                menu.PreviousRemove();
                menu.GetCurrentRemove().RemoteInvoke();
            }
            else if (_right)
            {
                menu.NextRemove();
                menu.GetCurrentRemove().RemoteInvoke();
            }
        }
    }
}
