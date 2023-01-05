using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BrushOptions
{
    public static ScaleConstraints scaleConstraints { get; set; }

    public enum ScaleConstraints
    {
        xyz,
        xy,
        yz,
        xz,
        x,
        y,
        z
    }

    public static Vector3 GetConstraints()
    {
        switch (scaleConstraints)
        {
            case ScaleConstraints.xyz:
                return Vector3.one;
            case ScaleConstraints.xy:
                return new Vector3(1, 1, 0);
            case ScaleConstraints.yz:
                return new Vector3(0, 1, 1);
            case ScaleConstraints.xz:
                return new Vector3(1, 0, 1);
            case ScaleConstraints.x:
                return Vector3.right;
            case ScaleConstraints.y:
                return Vector3.up;
            case ScaleConstraints.z:
                return Vector3.forward;
            default:
                return Vector3.one;
        }
    }
}
