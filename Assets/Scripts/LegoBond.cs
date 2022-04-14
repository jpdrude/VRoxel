using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using GPUInstancer;

public class LegoBond : MonoBehaviour
{
    [Header("BondInfo")]
    [SerializeField]
    string bondName;

    [Header("Bricks")]
    [SerializeField]
    GPUInstancerPrefab brickPrefab;
    [SerializeField]
    List<Transform> bricks = new List<Transform>();

    [Space]
    [Header("Offset")]
    [SerializeField]
    int xSize;
    [SerializeField]
    int ySize;
    [SerializeField]
    int zSize;

    [Space]
    [Header("Units")]
    [SerializeField]
    float xSpacing;
    [SerializeField]
    float ySpacing;
    [SerializeField]
    float zSpacing;

    public int XSize { get { return xSize; } }
    public int YSize { get { return ySize; } }
    public int ZSize { get { return zSize; } }

    public float XSpacing { get { return xSpacing; } }
    public float YSpacing { get { return ySpacing; } }
    public float ZSpacing { get { return zSpacing; } }

    public GPUInstancerPrefab BrickPrefab { get { return brickPrefab; } }

    public Matrix4x4 GetClosestBrick(Vector3 position)
    {
        Vector3 bondBase = GetBondBase(position);
        Vector3 bondPosition = position - bondBase;

        Transform best = bricks[0];
        for (int i = 1; i < bricks.Count; ++i)
            if (Vector3.Distance(bondPosition, best.position) > Vector3.Distance(bondPosition, bricks[i].position))
                best = bricks[i];

        return Matrix4x4.TRS(bondBase + best.localPosition, best.localRotation, Vector3.one);
    }

    public Vector3 GetBondBase(Vector3 position)
    {
        int legoX = Mathf.FloorToInt(position.x / GameManager.Bond.XSpacing);
        int legoY = Mathf.FloorToInt(position.y / GameManager.Bond.YSpacing);
        int legoZ = Mathf.FloorToInt(position.z / GameManager.Bond.ZSpacing);

        legoX /= XSize;
        legoY /= YSize;
        legoZ /= ZSize;
        legoX *= XSize;
        legoY *= YSize;
        legoZ *= ZSize;

        Vector3 bondBase = new Vector3(legoX * GameManager.Bond.XSpacing, legoY * GameManager.Bond.YSpacing, legoZ * GameManager.Bond.ZSpacing);
        return bondBase;
    }

    public Matrix4x4[] GetBricksInSphere(Vector3 position, float radius)
    {
        List<Matrix4x4> bricks = new List<Matrix4x4>();
        Vector3 min = GetBondBase(position - Vector3.one * radius);
        Vector3 max = position + Vector3.one * radius;

        for (float x = min.x; x < max.x; x += GameManager.Bond.xSize * GameManager.Bond.XSpacing)
        {
            for (float y = min.y; y < max.y; y += GameManager.Bond.ySize * GameManager.Bond.YSpacing)
                for (float z = min.z; z < max.z; z += GameManager.Bond.zSize * GameManager.Bond.ZSpacing)
                {
                    foreach (Transform brick in GameManager.Bond.bricks)
                    {
                        if (Vector3.Distance(brick.localPosition + new Vector3(x, y, z), position) < radius)
                            bricks.Add(Matrix4x4.TRS(brick.localPosition + new Vector3(x, y, z), brick.localRotation, Vector3.one));
                    }
                }
        }

        return bricks.ToArray();
    }

    public Matrix4x4[] GetBricksInBounds(Vector3 min, Vector3 max)
    {
        List<Matrix4x4> bricks = new List<Matrix4x4>();

        for (float x = min.x; x < max.x; x += GameManager.Bond.xSize * GameManager.Bond.XSpacing)
        {
            for (float y = min.y; y < max.y; y += GameManager.Bond.ySize * GameManager.Bond.YSpacing)
                for (float z = min.z; z < max.z; z += GameManager.Bond.zSize * GameManager.Bond.ZSpacing)
                {
                    foreach (Transform brick in GameManager.Bond.bricks)
                    {
                        bricks.Add(Matrix4x4.TRS(brick.localPosition + new Vector3(x, y, z), brick.localRotation, Vector3.one));
                    }
                }
        }

        return bricks.ToArray();
    }

    public Matrix4x4[] GetBricksInBox(Vector3 min, Vector3 max)
    {
        Vector3 originalMin = min;
        min = GetBondBase(min);

        List<Matrix4x4> bricks = new List<Matrix4x4>();

        for (float x = min.x; x < max.x; x += GameManager.Bond.xSize * GameManager.Bond.XSpacing)
        {
            for (float y = min.y; y < max.y; y += GameManager.Bond.ySize * GameManager.Bond.YSpacing)
                for (float z = min.z; z < max.z; z += GameManager.Bond.zSize * GameManager.Bond.ZSpacing)
                {
                    foreach (Transform brick in GameManager.Bond.bricks)
                    {
                        Vector3 brickPosition = brick.localPosition + new Vector3(x, y, z);
                        if (brickPosition.x > originalMin.x && brickPosition.y > originalMin.y && brickPosition.z > originalMin.z)
                            bricks.Add(Matrix4x4.TRS(brickPosition, brick.localRotation, Vector3.one));
                    }
                }
        }

        return bricks.ToArray();
    }
}
