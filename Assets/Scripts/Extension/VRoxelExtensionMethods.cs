using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class VRoxelExtensionMethods
{
    public static Vector3 Divide(this Vector3 vector, Vector3 other)
    {
        return new Vector3(vector.x / other.x, vector.y / other.y, vector.z / other.z);
    }

    public static Vector3 Multiply(this Vector3 vector, Vector3 other)
    {
        return new Vector3(vector.x * other.x, vector.y * other.y, vector.z * other.z);
    }
}
