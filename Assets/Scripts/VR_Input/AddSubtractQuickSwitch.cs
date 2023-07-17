using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AddSubtractQuickSwitch : SpawnerActions
{
    [SerializeField]
    SteamVR_AssemblySpawner spawner;

    [SerializeField]
    MenuSwitch menu;

    public override void OnQuickSwitchDown()
    {
        if (!spawner.enabled) return;

        if (spawner.Mode == VRPlacementMode.SpawnInSphere || spawner.Mode == VRPlacementMode.SpawnInBox || spawner.Mode == VRPlacementMode.SpawnInCylinder || spawner.Mode == VRPlacementMode.SpawnInBrush)
        {
            menu.GetCurrentRemove().RemoteInvoke();
        }
        else if (spawner.Mode == VRPlacementMode.DeleteFromSphere || spawner.Mode == VRPlacementMode.DeleteFromBox || spawner.Mode == VRPlacementMode.DeleteFromCylinder || spawner.Mode == VRPlacementMode.DeleteFromBrush)
        {
            menu.GetCurrentAdd().RemoteInvoke();
        }
        else if (spawner.Mode == VRPlacementMode.BlocksToPointCloud)
        {
            spawner.ChangeMode((int)VRPlacementMode.PointCloudToBlocks);
        }
        else if (spawner.Mode == VRPlacementMode.PointCloudToBlocks)
        {
            spawner.ChangeMode((int)VRPlacementMode.BlocksToPointCloud);
        }
    }
}
