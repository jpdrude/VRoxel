using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class ExtensionMethods
{
    public static Vector3 Divide(this Vector3Int vec, float div)
    {
        return new Vector3(vec.x / div, vec.y / div, vec.z / div);
    }
}
