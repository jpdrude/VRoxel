using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Valve.VR;
using UnityEngine.ProBuilder;
using UnityEngine.ProBuilder.MeshOperations;

public static class PlatformMesh
{
    public static GameObject GeneratePlatformMesh(Material platformMaterial)
    {
        var platform = new GameObject();
        platform.name = "ElevatorPlatform";

        HmdQuad_t[] collisionBounds;
        OpenVR.ChaperoneSetup.GetWorkingCollisionBoundsInfo(out collisionBounds);
        List<Vector3> boundaryList = new List<Vector3>();

        foreach (var bound in collisionBounds)
        {
            boundaryList.Add(new Vector3(bound.vCorners0.v0, 0, -bound.vCorners0.v2));
        }
        //boundaryList.Reverse();

        GameObject p1 = new GameObject();
        p1.transform.parent = platform.transform;

        var mr = p1.AddComponent<MeshRenderer>();
        var mf = p1.AddComponent<MeshFilter>();

        mr.material = platformMaterial;
        mf.sharedMesh = ExtrudeMeshFromPoints(boundaryList.ToArray());

        boundaryList.Reverse();

        GameObject p2 = new GameObject();
        p2.transform.parent = platform.transform;

        mr = p2.AddComponent<MeshRenderer>();
        mf = p2.AddComponent<MeshFilter>();

        mr.material = platformMaterial;
        mf.sharedMesh = ExtrudeMeshFromPoints(boundaryList.ToArray());

        platform.SetActive(false);

        return platform;
    }

    public static GameObject GeneratePlatformMesh(Material platformMaterial, GameObject template)
    {
        GameObject platform = MonoBehaviour.Instantiate(template);
        platform.transform.position = Vector3.zero;
        platform.transform.rotation = Quaternion.identity;
        platform.GetComponent<MeshRenderer>().material = platformMaterial;

        float sizeX = 0;
        float sizeY = 0;
        OpenVR.ChaperoneSetup.GetWorkingPlayAreaSize(ref sizeX, ref sizeY);

        Vector3 scale = Vector3.one;
        if (sizeX > sizeY)
        {
            scale.x = sizeX;
            scale.z = sizeX;
        }
        else
        {
            scale.x = sizeY;
            scale.z = sizeY;
        }

        Debug.Log("Game Area is: " + sizeX + "m x " + sizeY + "m");

        platform.transform.localScale = scale;
        platform.SetActive(false);

        return platform;
    }


    public static Mesh ExtrudeMeshFromPoints(Vector3[] floorVertices)
    {
        Mesh mesh = new Mesh();

        Vector3[] vertices = new Vector3[floorVertices.Length * 2];
        List<Vector3> vertexList = new List<Vector3>();
        List<Face> faceList = new List<Face>();

        for (int i = 0; i < floorVertices.Length; ++i)
        {
            vertices[i] = floorVertices[i];
            vertices[i].y = 0;
            vertices[i + floorVertices.Length] = floorVertices[i] + Vector3.up;
        }

        foreach (Vector3 vertexPos in vertices)
        {
            vertexList.Add(vertexPos);
        }

        for (int i = 0; i < floorVertices.Length; ++i)
        {
            int nextFloor = i + 1;
            if (nextFloor >= floorVertices.Length)
                nextFloor = 0;

            int nextCeil = i + floorVertices.Length + 1;
            if (nextCeil >= floorVertices.Length * 2)
                nextCeil = floorVertices.Length;

            int[] triA = new int[3];
            int[] triB = new int[3];

            triA[0] = i;
            triA[1] = nextFloor;
            triA[2] = i + floorVertices.Length;

            triB[0] = nextFloor;
            triB[1] = nextCeil;
            triB[2] = i + floorVertices.Length;

            faceList.Add(new Face(triA));
            faceList.Add(new Face(triB));
        }

        var proMesh = ProBuilderMesh.Create(vertexList, faceList);
        proMesh.gameObject.SetActive(false);

        int[] bottomIndeces = new int[floorVertices.Length];
        for (int i = 0; i < bottomIndeces.Length; ++i)
            bottomIndeces[i] = bottomIndeces.Length - 1 - i;

        int[] topIndeces = new int[floorVertices.Length];
        for (int i = 0; i < topIndeces.Length; ++i)
            topIndeces[i] = topIndeces.Length + i;

        proMesh.CreatePolygon(bottomIndeces, false);
        proMesh.CreatePolygon(topIndeces, false);

        proMesh.ToMesh();
        proMesh.Refresh();

        MeshUtility.Compile(proMesh, mesh);

        mesh.RecalculateBounds();
        mesh.RecalculateNormals();

        return mesh;
    }
}
