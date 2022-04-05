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
    List<Transform> bricks = new List<Transform>();

    [Space]
    [Header("Offset")]
    [SerializeField]
    int xSize;
    [SerializeField]
    int ySize;
    [SerializeField]
    int zSize;

    public int XSize { get { return xSize; } }
    public int YSize { get { return ySize; } }
    public int ZSize { get { return zSize; } }

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
        int legoX = Mathf.FloorToInt(position.x / GameManager.xzSpacing);
        int legoY = Mathf.FloorToInt(position.y / GameManager.ySpacing);
        int legoZ = Mathf.FloorToInt(position.z / GameManager.xzSpacing);

        legoX /= XSize;
        legoY /= YSize;
        legoZ /= ZSize;
        legoX *= XSize;
        legoY *= YSize;
        legoZ *= ZSize;

        Vector3 bondBase = new Vector3(legoX * GameManager.xzSpacing, legoY * GameManager.ySpacing, legoZ * GameManager.xzSpacing);
        return bondBase;
    }

    public Matrix4x4[] GetBricksInSphere(Vector3 position, float radius)
    {
        List<Matrix4x4> bricks = new List<Matrix4x4>();
        Vector3 min = GetBondBase(position - Vector3.one * radius);
        Vector3 max = position + Vector3.one * radius;

        for (float x = min.x; x < max.x; x += GameManager.Bond.xSize * GameManager.xzSpacing)
        {
            for (float y = min.y; y < max.y; y += GameManager.Bond.ySize * GameManager.ySpacing)
                for (float z = min.z; z < max.z; z += GameManager.Bond.zSize * GameManager.xzSpacing)
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

        for (float x = min.x; x < max.x; x += GameManager.Bond.xSize * GameManager.xzSpacing)
        {
            for (float y = min.y; y < max.y; y += GameManager.Bond.ySize * GameManager.ySpacing)
                for (float z = min.z; z < max.z; z += GameManager.Bond.zSize * GameManager.xzSpacing)
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

        for (float x = min.x; x < max.x; x += GameManager.Bond.xSize * GameManager.xzSpacing)
        {
            for (float y = min.y; y < max.y; y += GameManager.Bond.ySize * GameManager.ySpacing)
                for (float z = min.z; z < max.z; z += GameManager.Bond.zSize * GameManager.xzSpacing)
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
