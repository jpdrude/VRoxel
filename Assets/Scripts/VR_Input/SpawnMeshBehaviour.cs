using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpawnMeshBehaviour : SpawnCubeBehaviour
{
    static bool alignToWorld = true;
    public static bool AlignToWorld
    {
        get { return alignToWorld; }
        set { alignToWorld = value; }
    }

    protected override void Update()
    {
        if (alignToWorld)
            base.Update();
        else
            transform.localRotation = Quaternion.identity;
    }
}
