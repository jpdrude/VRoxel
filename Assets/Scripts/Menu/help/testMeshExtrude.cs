using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class testMeshExtrude : MonoBehaviour
{
    [SerializeField]
    Vector3[] clockwisePolygon;
    [SerializeField]
    Material mat;

    private void Start()
    {
        var mr = gameObject.AddComponent<MeshRenderer>();
        mr.material = mat;

        var mf = gameObject.AddComponent<MeshFilter>();
        Mesh mesh = PlatformMesh.ExtrudeMeshFromPoints(clockwisePolygon);
        mf.sharedMesh = mesh;
    }
}
