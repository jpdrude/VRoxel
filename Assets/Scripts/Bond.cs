using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using GPUInstancer;

using UnityEngine.Jobs;
using Unity.Mathematics;
using Unity.Collections;
using Unity.Burst;

public class Bond : MonoBehaviour
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

    int layerMask = 1 << 8;

    public int XSize { get { return xSize; } }
    public int YSize { get { return ySize; } }
    public int ZSize { get { return zSize; } }

    public float XSpacing { get { return xSpacing; } }
    public float YSpacing { get { return ySpacing; } }
    public float ZSpacing { get { return zSpacing; } }

    public int XIntSpacing { get { return Mathf.RoundToInt(xSpacing * 10000); } }
    public int YIntSpacing { get { return Mathf.RoundToInt(ySpacing * 10000); } }
    public int ZIntSpacing { get { return Mathf.RoundToInt(zSpacing * 10000); } }

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
        int x = Mathf.FloorToInt(position.x / GameManager.Bond.XSpacing);
        int y = Mathf.FloorToInt(position.y / GameManager.Bond.YSpacing);
        int z = Mathf.FloorToInt(position.z / GameManager.Bond.ZSpacing);

        x /= XSize;
        y /= YSize;
        z /= ZSize;
        x *= XSize;
        y *= YSize;
        z *= ZSize;

        Vector3 bondBase = new Vector3(x * GameManager.Bond.XSpacing, y * GameManager.Bond.YSpacing, z * GameManager.Bond.ZSpacing);
        return bondBase;
    }

    public Vector3Int GetBondBaseInt(Vector3 position)
    {
        int x = Mathf.FloorToInt(position.x * 10000) / GameManager.Bond.XIntSpacing;
        int y = Mathf.FloorToInt(position.y * 10000) / GameManager.Bond.YIntSpacing;
        int z = Mathf.FloorToInt(position.z * 10000) / GameManager.Bond.ZIntSpacing;

        x /= XSize;
        y /= YSize;
        z /= ZSize;
        x *= XSize;
        y *= YSize;
        z *= ZSize;

        Vector3Int bondBase = new Vector3Int(x * GameManager.Bond.XIntSpacing, y * GameManager.Bond.YIntSpacing, z * GameManager.Bond.ZIntSpacing);
        return bondBase;
    }

    public Matrix4x4[] GetBricksInSphere(Vector3 position, float radius, out Vector3Int[] dictPositions)
    {
        List<Matrix4x4> bricks = new List<Matrix4x4>();
        List<Vector3Int> intPositions = new List<Vector3Int>();

        Vector3Int min = GetBondBaseInt(position - Vector3.one * radius);
        Vector3 max = (position + Vector3.one * radius) * 10000;

        for (int x = min.x; x < max.x; x += GameManager.Bond.xSize * GameManager.Bond.XIntSpacing)
        {
            for (int y = min.y; y < max.y; y += GameManager.Bond.ySize * GameManager.Bond.YIntSpacing)
            {
                for (int z = min.z; z < max.z; z += GameManager.Bond.zSize * GameManager.Bond.ZIntSpacing)
                {
                    foreach (Transform brick in GameManager.Bond.bricks)
                    {
                        if (Vector3.Distance(brick.localPosition + new Vector3Int(x, y, z).Divide(10000), position) < radius)
                        {
                            bricks.Add(Matrix4x4.TRS(brick.localPosition + new Vector3Int(x, y, z).Divide(10000), brick.localRotation, Vector3.one));
                            Vector3Int localIntPosition = new Vector3Int(Mathf.RoundToInt(brick.localPosition.x * 10000), Mathf.RoundToInt(brick.localPosition.y * 10000), Mathf.RoundToInt(brick.localPosition.z * 10000));
                            intPositions.Add(localIntPosition + new Vector3Int(x, y, z));
                            //Debug.Log("Brick added. Position: " + (brick.localPosition + new Vector3Int(x, y, z).Divide(10000)).ToString("F4") + ", intPosition: " + (localIntPosition + new Vector3Int(x, y, z)).ToString());
                        }
                    }
                }
            }
        }

        dictPositions = intPositions.ToArray();
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

    public Matrix4x4[] GetBricksInBox(Vector3 min, Vector3 max, out Vector3Int[] dictPositions)
    {
        Vector3Int minInt = GetBondBaseInt(min);
        Vector3 maxScaled = max * 10000;

        List<Matrix4x4> bricks = new List<Matrix4x4>();
        List<Vector3Int> intPositions = new List<Vector3Int>();

        for (int x = minInt.x; x < maxScaled.x; x += GameManager.Bond.xSize * GameManager.Bond.XIntSpacing)
        {
            for (int y = minInt.y; y < maxScaled.y; y += GameManager.Bond.ySize * GameManager.Bond.YIntSpacing)
            {
                for (int z = minInt.z; z < maxScaled.z; z += GameManager.Bond.zSize * GameManager.Bond.ZIntSpacing)
                {
                    foreach (Transform brick in GameManager.Bond.bricks)
                    {
                        Vector3 brickPosition = brick.localPosition + new Vector3Int(x, y, z).Divide(10000);
                        if (brickPosition.x >= min.x && brickPosition.y >= min.y && brickPosition.z >= min.z)
                        {
                            bricks.Add(Matrix4x4.TRS(brickPosition, brick.localRotation, Vector3.one));
                            Vector3Int localIntPosition = new Vector3Int(Mathf.RoundToInt(brick.localPosition.x * 10000), Mathf.RoundToInt(brick.localPosition.y * 10000), Mathf.RoundToInt(brick.localPosition.z * 10000));
                            intPositions.Add(localIntPosition + new Vector3Int(x, y, z));
                            //Debug.Log("Brick added. Position: " + (brick.localPosition + new Vector3Int(x, y, z).Divide(10000)).ToString("F4") + ", intPosition: " + (localIntPosition + new Vector3Int(x, y, z)).ToString());
                        }
                    }
                }
            }
        }

        dictPositions = intPositions.ToArray();
        return bricks.ToArray();
    }

    public Matrix4x4[] GetBricksInCylinder(Vector3 position, float radius, float height, out Vector3Int[] dictPositions)
    {
        Vector3 min = position - new Vector3(radius, height / 2, radius);
        Vector3 max = position + new Vector3(radius, height / 2, radius);

        Vector3Int minInt = GetBondBaseInt(min);
        Vector3 maxScaled = max * 10000;

        List<Matrix4x4> bricks = new List<Matrix4x4>();
        List<Vector3Int> intPositions = new List<Vector3Int>();

        for (int x = minInt.x; x < maxScaled.x; x += GameManager.Bond.xSize * GameManager.Bond.XIntSpacing)
        {
            for (int y = minInt.y; y < maxScaled.y; y += GameManager.Bond.ySize * GameManager.Bond.YIntSpacing)
            {
                for (int z = minInt.z; z < maxScaled.z; z += GameManager.Bond.zSize * GameManager.Bond.ZIntSpacing)
                {
                    foreach (Transform brick in GameManager.Bond.bricks)
                    {
                        Vector3 brickPosition = brick.localPosition + new Vector3Int(x, y, z).Divide(10000);

                        bool inRadius = Vector3.Distance(new Vector3(position.x, 0, position.z), new Vector3(brickPosition.x, 0, brickPosition.z)) < radius;

                        if (inRadius && brickPosition.y <= position.y + height / 2 && brickPosition.y >= position.y - height / 2)
                        {
                            bricks.Add(Matrix4x4.TRS(brickPosition, brick.localRotation, Vector3.one));
                            Vector3Int localIntPosition = new Vector3Int(Mathf.RoundToInt(brick.localPosition.x * 10000), Mathf.RoundToInt(brick.localPosition.y * 10000), Mathf.RoundToInt(brick.localPosition.z * 10000));
                            intPositions.Add(localIntPosition + new Vector3Int(x, y, z));
                            //Debug.Log("Brick added. Position: " + (brick.localPosition + new Vector3Int(x, y, z).Divide(10000)).ToString("F4") + ", intPosition: " + (localIntPosition + new Vector3Int(x, y, z)).ToString());
                        }
                    }
                }
            }
        }

        dictPositions = intPositions.ToArray();
        return bricks.ToArray();
    }

    public Matrix4x4[] GetBricksInMesh(Collider mesh, out Vector3Int[] dictPositions)
    {
        /*
        GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cube.transform.localScale = mesh.bounds.size;
        cube.transform.position = mesh.bounds.center;
        */

        Vector3Int minInt = GetBondBaseInt(mesh.bounds.min);
        Vector3 maxScaled = mesh.bounds.max * 10000;
        float rayLength = mesh.bounds.max.x - mesh.bounds.min.x + 0.1f;

        List<Matrix4x4> bricks = new List<Matrix4x4>();
        List<Vector3Int> intPositions = new List<Vector3Int>();

        for (int x = minInt.x; x < maxScaled.x; x += GameManager.Bond.xSize * GameManager.Bond.XIntSpacing)
        {
            for (int y = minInt.y; y < maxScaled.y; y += GameManager.Bond.ySize * GameManager.Bond.YIntSpacing)
            {
                for (int z = minInt.z; z < maxScaled.z; z += GameManager.Bond.zSize * GameManager.Bond.ZIntSpacing)
                {
                    
                    foreach (Transform brick in GameManager.Bond.bricks)
                    {
                        Vector3 brickPosition = brick.localPosition + new Vector3Int(x, y, z).Divide(10000);

                        if (PointInMesh(brickPosition, rayLength))
                        {
                            bricks.Add(Matrix4x4.TRS(brickPosition, brick.localRotation, Vector3.one));
                            Vector3Int localIntPosition = new Vector3Int(Mathf.RoundToInt(brick.localPosition.x * 10000), Mathf.RoundToInt(brick.localPosition.y * 10000), Mathf.RoundToInt(brick.localPosition.z * 10000));
                            intPositions.Add(localIntPosition + new Vector3Int(x, y, z));
                        }
                    }
                    

                    /*
                    Vector3[] pos = new Vector3[GameManager.Bond.bricks.Count];
                    for(int i = 0; i < pos.Length; ++i)
                    {
                        pos[i] = GameManager.Bond.bricks[i].localPosition + new Vector3Int(x, y, z).Divide(10000);
                    }

                    bool[] results = PointInMesh(pos);
                    for (int i = 0; i < pos.Length; ++i)
                    {
                        if (results[i])
                        {
                            Transform brick = GameManager.Bond.bricks[i];
                            bricks.Add(Matrix4x4.TRS(pos[i], brick.localRotation, Vector3.one));
                            Vector3Int localIntPosition = new Vector3Int(Mathf.RoundToInt(brick.localPosition.x * 10000), Mathf.RoundToInt(brick.localPosition.y * 10000), Mathf.RoundToInt(brick.localPosition.z * 10000));
                            intPositions.Add(localIntPosition + new Vector3Int(x, y, z));
                        }
                    }   
                    */
                }
            }
        }

        dictPositions = intPositions.ToArray();
        return bricks.ToArray();
    }

    bool PointInMesh(Vector3 pos, float rayLength)
    {
        Physics.queriesHitBackfaces = true;
        Ray ray = new Ray(pos, Vector3.right);
        RaycastHit hit;
        int hits = 0;

        while (true)
        {
            Physics.Raycast(ray, out hit, rayLength, layerMask);
            if (hit.collider == null || hit.collider.gameObject.tag != "MeshSpawner")
                break;
            else if (hit.collider != null && hit.collider.gameObject.tag != "MeshSpawner")
            {
                Debug.Log("Found stray collider, name: " + hit.collider.gameObject.name);
            }
  
            ++hits;
            ray = new Ray(hit.point + Vector3.right * 0.0001f, Vector3.right);
        }


        if (hits % 2 == 1)
        {
            return true;
        }
        else
            return false;
    }

    /*
    [BurstCompile]
    bool[] PointInMesh(Vector3[] pos)
    {
        int numCollision = 4;
        Physics.queriesHitBackfaces = true;
        var hitResults = new NativeArray<RaycastHit>(pos.Length * numCollision, Allocator.TempJob);
        var commands = new NativeArray<RaycastCommand>(pos.Length, Allocator.TempJob);
        bool[] results = new bool[pos.Length];
        int firstPositive = 0;

        for (int i = 0; i < pos.Length; ++i)
        {
            commands[i] = new RaycastCommand(pos[i], Vector3.right, Mathf.Infinity, layerMask, numCollision);
        }

        var handle = RaycastCommand.ScheduleBatch(commands, hitResults, 1, default(Unity.Jobs.JobHandle));
        handle.Complete();

        for (int i = 0; i < pos.Length; ++i)
        {
            int hitCount = 0;
            for (int j = 0; j < numCollision; ++j)
            {
                if (hitResults[i * numCollision + j].collider != null)
                    ++hitCount;
            }

            if (hitCount % 2 == 1)
            {
                results[i] = true;
                ++firstPositive;            
            }
        }

        Vector3[] hitPos = new Vector3[firstPositive];
        int count = 0;

        for (int i = 0; i < results.Length; ++i)
        {
            if (results[i])
            {
                hitPos[count] = hitResults[i * numCollision].point - Vector3.right * 0.001f;
                ++count;
            }
        }

        hitResults = new NativeArray<RaycastHit>(hitPos.Length * numCollision, Allocator.TempJob);
        commands = new NativeArray<RaycastCommand>(hitPos.Length, Allocator.TempJob);

        for (int i = 0; i < hitPos.Length; ++i)
        {
            commands[i] = new RaycastCommand(hitPos[i], Vector3.left, Mathf.Infinity, layerMask, numCollision);
        }

        handle = RaycastCommand.ScheduleBatch(commands, hitResults, 1, default(Unity.Jobs.JobHandle));
        handle.Complete();

        count = 0;

        for (int i = 0; i < pos.Length; ++i)
        {
            if (results[i])
            {
                int hitCount = 0;
                for (int j = 0; j < numCollision; ++j)
                {
                    if (hitResults[count * numCollision + j].collider != null)
                        ++hitCount;
                }

                if (hitCount % 2 != 1)
                {
                    results[i] = false;
                }

                ++count;
            }
        }

        hitResults.Dispose();
        commands.Dispose();

        return results;
    }
    */
}
