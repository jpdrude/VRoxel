using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using GPUInstancer;

using System;

public class Bond : MonoBehaviour
{
    [Header("BondInfo")]
    [SerializeField]
    string bondName;

    [Header("Parts")]
    [SerializeField]
    GPUInstancerPrefab[] partPrefabs;
    
    [SerializeField]
    PartTypeArray[] bondBlocks;

    Transform[][] blocks;

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

    [Space]
    [Header("Global Offset")]
    [SerializeField]
    Vector3 defaultOffset;

    [Space]
    [Header("Extend")]
    [SerializeField]
    Vector3Int minExtend = new Vector3Int();
    [SerializeField]
    Vector3Int maxExtend = new Vector3Int();

    int layerMask = 1 << 8;

    public float XSpacing { get { return xSpacing; } }
    public float YSpacing { get { return ySpacing; } }
    public float ZSpacing { get { return zSpacing; } }

    public int XIntSpacing { get { return Mathf.RoundToInt(xSpacing * 10000); } }
    public int YIntSpacing { get { return Mathf.RoundToInt(ySpacing * 10000); } }
    public int ZIntSpacing { get { return Mathf.RoundToInt(zSpacing * 10000); } }

    public GPUInstancerPrefab[] PartPrefabs { get { return partPrefabs; } }

    public string BondName { get { return bondName; } }

    Vector3 offset;
    public Vector3 Offset 
    { 
        get { return offset; } 
        set
        {
            BlockContainer.Container.RecalculateOffset(value, offset);
            offset = value;
        }
    }

    public static MeshBrushLevels meshBrushLevel = MeshBrushLevels.fine;

    public void Initialize()
    {
        blocks = new Transform[bondBlocks.Length][];

        for (int i = 0; i < bondBlocks.Length; ++i)
            blocks[i] = bondBlocks[i].blocks;

        offset = defaultOffset;
    }

    public Vector3 GetBondBase(Vector3 position)
    {
        int x = Mathf.FloorToInt((position.x - offset.x) / xSpacing);
        int y = Mathf.FloorToInt((position.y - offset.y) / ySpacing);
        int z = Mathf.FloorToInt((position.z - offset.z) / zSpacing);

        x /= xSize;
        y /= ySize;
        z /= zSize;
        x *= xSize;
        y *= ySize;
        z *= zSize;

        x += xSize * minExtend.x;
        y += ySize * minExtend.y;
        z += zSize * minExtend.z;

        Vector3 bondBase = new Vector3(x * xSpacing, y * ySpacing, z * zSpacing);
        return bondBase;
    }

    public Vector3Int GetBondBaseInt(Vector3 position)
    {
        int x = Mathf.FloorToInt((position.x - offset.x) * 10000) / XIntSpacing;
        int y = Mathf.FloorToInt((position.y - offset.y) * 10000) / YIntSpacing;
        int z = Mathf.FloorToInt((position.z - offset.z) * 10000) / ZIntSpacing;

        x /= xSize;
        y /= ySize;
        z /= zSize;
        x *= xSize;
        y *= ySize;
        z *= zSize;

        x += xSize * minExtend.x;
        y += ySize * minExtend.y;
        z += zSize * minExtend.z;

        Vector3Int bondBase = new Vector3Int(x * XIntSpacing, y * YIntSpacing, z * ZIntSpacing);
        return bondBase;
    }

    public Matrix4x4 GetBlockFromPosition(Vector3 position, int type)
    {
        position -= offset;

        int xInt = Mathf.RoundToInt(position.x * 10000);
        int yInt = Mathf.RoundToInt(position.y * 10000);
        int zInt = Mathf.RoundToInt(position.z * 10000);

        int xBondSize = XIntSpacing * xSize;
        int yBondSize = YIntSpacing * ySize;
        int zBondSize = ZIntSpacing * zSize;

        Transform fit = null;

        foreach(Transform t in blocks[type])
        {
            int xBondInt = Mathf.RoundToInt(t.position.x * 10000);
            int yBondInt = Mathf.RoundToInt(t.position.y * 10000);
            int zBondInt = Mathf.RoundToInt(t.position.z * 10000);

            if ((xInt - xBondInt) % xBondSize == 0 && (yInt - yBondInt) % yBondSize == 0 && (zInt - zBondInt) % zBondSize == 0)
                fit = t;
        }

        return Matrix4x4.TRS(position + offset, fit.localRotation, fit.localScale);
    }

    //Doesn't work yet!
    public Matrix4x4 GetClosestBlockFromPosition(Vector3 position, int type)
    {
        int xInt = Mathf.RoundToInt(position.x * 10000);
        int yInt = Mathf.RoundToInt(position.y * 10000);
        int zInt = Mathf.RoundToInt(position.z * 10000);

        int xBondSize = XIntSpacing * xSize;
        int yBondSize = YIntSpacing * ySize;
        int zBondSize = ZIntSpacing * zSize;

        Transform fit = null;
        int bestError = int.MaxValue;

        foreach (Transform t in blocks[type])
        {
            int xBondInt = Mathf.RoundToInt(t.position.x * 10000);
            int yBondInt = Mathf.RoundToInt(t.position.y * 10000);
            int zBondInt = Mathf.RoundToInt(t.position.z * 10000);

            int error = (xInt - xBondInt) % xBondSize + (yInt - yBondInt) % yBondSize + (zInt - zBondInt) % zBondSize;

            if (error < bestError)
            {
                fit = t;
                bestError = error;
            }  
        }

        xInt = xInt / xBondSize;
        yInt = yInt / yBondSize;
        zInt = zInt / zBondSize;

        xInt *= xBondSize;
        yInt *= yBondSize;
        zInt *= zBondSize;

        position = new Vector3(xInt / 10000.0f, yInt / 10000.0f, zInt / 10000.0f);

        return Matrix4x4.TRS(position, fit.localRotation, fit.localScale);
    }

    public Matrix4x4[][] GetBlocksInBox(Vector3 min, Vector3 max, out PositionKey[][] _blockDictsKeys)
    {
        /*  
         *  offset is used for representational 
         *  geometry only.
         *  Dictionary keys are not offset!
         */

        Vector3Int minInt = GetBondBaseInt(min);
        Vector3 maxScaled = (max - offset) * 10000;

        if (maxScaled.x - minInt.x < xSize * XIntSpacing)
            maxScaled.x = minInt.x + xSize * XIntSpacing * 1.1f;
        if (maxScaled.y - minInt.y < ySize * YIntSpacing)
            maxScaled.y = minInt.y + ySize * YIntSpacing * 1.1f;
        if (maxScaled.z - minInt.z < zSize * ZIntSpacing)
            maxScaled.z = minInt.z + zSize * ZIntSpacing * 1.1f;

        maxScaled += new Vector3(xSize * XIntSpacing * maxExtend.x, ySize * YIntSpacing * maxExtend.y, zSize * ZIntSpacing * maxExtend.z);

        List<Matrix4x4>[] blockTransforms = new List<Matrix4x4>[blocks.Length];
        List<PositionKey>[] blockDictKeys = new List<PositionKey>[blocks.Length];

        for (int i = 0; i < blockTransforms.Length; ++i)
        {
            blockTransforms[i] = new List<Matrix4x4>();
            blockDictKeys[i] = new List<PositionKey>();
        }

        for (int x = minInt.x; x < maxScaled.x; x += xSize * XIntSpacing)
        {
            for (int y = minInt.y; y < maxScaled.y; y += ySize * YIntSpacing)
            {
                for (int z = minInt.z; z < maxScaled.z; z += zSize * ZIntSpacing)
                {
                    for (int i = 0; i < blocks.Length; ++i)
                    {
                        for (int j = 0; j < blocks[i].Length; ++j)
                        {
                            Transform block = blocks[i][j].transform;
                            Vector3 blockPosition = block.localPosition + new Vector3Int(x, y, z).Divide(10000) + offset;
                            if (blockPosition.x >= min.x && blockPosition.y >= min.y && blockPosition.z >= min.z && blockPosition.x <= max.x && blockPosition.y <= max.y && blockPosition.z <= max.z)
                            {
                                blockTransforms[i].Add(Matrix4x4.TRS(blockPosition, block.localRotation, Vector3.one));
                                Vector3Int localIntPosition = new Vector3Int(Mathf.RoundToInt(block.localPosition.x * 10000), Mathf.RoundToInt(block.localPosition.y * 10000), Mathf.RoundToInt(block.localPosition.z * 10000));
                                blockDictKeys[i].Add(new PositionKey(localIntPosition + new Vector3Int(x, y, z)));
                            }
                        }
                    }
                }
            }
        }

        Matrix4x4[][] _blockTransforms = new Matrix4x4[blocks.Length][];
        _blockDictsKeys = new PositionKey[blocks.Length][];

        for (int i = 0; i < blockTransforms.Length; ++i)
        {
            _blockTransforms[i] = blockTransforms[i].ToArray();
            _blockDictsKeys[i] = blockDictKeys[i].ToArray();
        }

        return _blockTransforms;
    }

    public Matrix4x4[][] GetBlocksInSphere(Vector3 position, float radius, out PositionKey[][] _blockDictKeys)
    {
        List<Matrix4x4>[] blockTransforms = new List<Matrix4x4>[blocks.Length];
        List<PositionKey>[] blockDictKeys = new List<PositionKey>[blocks.Length];

        int blockCounter = 0;

        Vector3Int min = GetBondBaseInt(position - Vector3.one * radius);
        Vector3 max = (position - offset + Vector3.one * radius) * 10000;

        if (max.x - min.x < xSize * XIntSpacing)
            max.x = min.x + xSize * XIntSpacing * 1.1f;
        if (max.y - min.y < ySize * YIntSpacing)
            max.y = min.y + ySize * YIntSpacing * 1.1f;
        if (max.z - min.z < zSize * ZIntSpacing)
            max.z = min.z + zSize * ZIntSpacing * 1.1f;

        max += new Vector3(xSize * XIntSpacing * maxExtend.x, ySize * YIntSpacing * maxExtend.y, zSize * ZIntSpacing * maxExtend.z);

        for (int i = 0; i < blockTransforms.Length; ++i)
        {
            blockTransforms[i] = new List<Matrix4x4>();
            blockDictKeys[i] = new List<PositionKey>();
        }

        for (int x = min.x; x < max.x; x += xSize * XIntSpacing)
        {
            for (int y = min.y; y < max.y; y += ySize * YIntSpacing)
            {
                for (int z = min.z; z < max.z; z += zSize * ZIntSpacing)
                {
                    for (int i = 0; i < blocks.Length; ++i)
                    {
                        for (int j = 0; j < blocks[i].Length; ++j)
                        {
                            if (Vector3.Distance(blocks[i][j].localPosition + new Vector3Int(x, y, z).Divide(10000), position - offset) < radius)
                            {
                                
                                Transform block = blocks[i][j].transform;
                                blockTransforms[i].Add(Matrix4x4.TRS(block.localPosition + new Vector3Int(x, y, z).Divide(10000) + offset, block.localRotation, Vector3.one));
                                Vector3Int localIntPosition = new Vector3Int(Mathf.RoundToInt(block.localPosition.x * 10000), Mathf.RoundToInt(block.localPosition.y * 10000), Mathf.RoundToInt(block.localPosition.z * 10000));
                                blockDictKeys[i].Add(new PositionKey(localIntPosition + new Vector3Int(x, y, z)));
                                ++blockCounter;
                                
                            }
                        }
                    }
                }
            }
        }

        /*
         * Not yet working!
        if (blockCounter == 0)
        {
            KeyValuePair<int, Matrix4x4> bestBlock = new KeyValuePair<int, Matrix4x4>(0, new Matrix4x4());
            float bestDistance = float.MaxValue;

            for (int i = 0; i < blocks.Length; ++i)
            {
                Matrix4x4 b = GetClosestBlockFromPosition(position, i);
                float dist = Vector3.Distance(b.GetPosition(), position);
                if (dist < bestDistance)// && dist < 0.02f)
                {
                    bestDistance = dist;
                    bestBlock = new KeyValuePair<int, Matrix4x4>(i, b);
                }
            }
             
            if (bestDistance != float.MaxValue)
            {
                blockTransforms[bestBlock.Key].Add(bestBlock.Value);
                blockDictKeys[bestBlock.Key].Add(new PositionKey(bestBlock.Value));
            }
        }
        */

        Matrix4x4[][] _blockTransforms = new Matrix4x4[blocks.Length][];
        _blockDictKeys = new PositionKey[blocks.Length][];

        for (int i = 0; i < blockTransforms.Length; ++i)
        {
            _blockTransforms[i] = blockTransforms[i].ToArray();
            _blockDictKeys[i] = blockDictKeys[i].ToArray();
        }

        return _blockTransforms;
    }

    public Matrix4x4[][] GetBlocksInCylinder(Vector3 position, float radius, float height, out PositionKey[][] _blockDictKeys)
    {
        Vector3 min = position - new Vector3(radius, height / 2, radius);
        Vector3 max = position + new Vector3(radius, height / 2, radius);

        Vector3Int minInt = GetBondBaseInt(min);
        Vector3 maxScaled = (max - offset) * 10000;

        if (maxScaled.x - minInt.x < xSize * XIntSpacing)
            maxScaled.x = minInt.x + xSize * XIntSpacing * 1.1f;
        if (maxScaled.y - minInt.y < ySize * YIntSpacing)
            maxScaled.y = minInt.y + ySize * YIntSpacing * 1.1f;
        if (maxScaled.z - minInt.z < zSize * ZIntSpacing)
            maxScaled.z = minInt.z + zSize * ZIntSpacing * 1.1f;

        maxScaled += new Vector3(xSize * XIntSpacing * maxExtend.x, ySize * YIntSpacing * maxExtend.y, zSize * ZIntSpacing * maxExtend.z);

        List<Matrix4x4>[] blockTransforms = new List<Matrix4x4>[blocks.Length];
        List<PositionKey>[] blockDictKeys = new List<PositionKey>[blocks.Length];

        for (int i = 0; i < blockTransforms.Length; ++i)
        {
            blockTransforms[i] = new List<Matrix4x4>();
            blockDictKeys[i] = new List<PositionKey>();
        }

        for (int x = minInt.x; x < maxScaled.x; x += xSize * XIntSpacing)
        {
            for (int y = minInt.y; y < maxScaled.y; y += ySize * YIntSpacing)
            {
                for (int z = minInt.z; z < maxScaled.z; z += zSize * ZIntSpacing)
                {
                    for (int i = 0; i < blocks.Length; ++i)
                    {
                        for (int j = 0; j < blocks[i].Length; ++j)
                        {
                            Transform block = blocks[i][j];
                            Vector3 blockPosition = block.localPosition + offset + new Vector3Int(x, y, z).Divide(10000);

                            bool inRadius = Vector3.Distance(new Vector3(position.x, 0, position.z), new Vector3(blockPosition.x, 0, blockPosition.z)) < radius;

                            if (inRadius && blockPosition.y <= position.y + height / 2 && blockPosition.y >= position.y - height / 2)
                            {
                                blockTransforms[i].Add(Matrix4x4.TRS(blockPosition, block.localRotation, Vector3.one));
                                Vector3Int localIntPosition = new Vector3Int(Mathf.RoundToInt(block.localPosition.x * 10000), Mathf.RoundToInt(block.localPosition.y * 10000), Mathf.RoundToInt(block.localPosition.z * 10000));
                                blockDictKeys[i].Add(new PositionKey(localIntPosition + new Vector3Int(x, y, z)));
                            }
                        }
                    }
                }
            }
        }        

        Matrix4x4[][] _blockTransforms = new Matrix4x4[blocks.Length][];
        _blockDictKeys = new PositionKey[blocks.Length][];

        for (int i = 0; i < blockTransforms.Length; ++i)
        {
            _blockTransforms[i] = blockTransforms[i].ToArray();
            _blockDictKeys[i] = blockDictKeys[i].ToArray();
        }

        return _blockTransforms;
    }

    public Matrix4x4[][] GetBlocksInCapsule(Vector3 positionA, Vector3 positionB, float radius, out PositionKey[][] _blockDictKeys)
    {
        Vector3 min = positionA;
        if (positionB.x < min.x) min.x = positionB.x;
        if (positionB.y < min.y) min.y = positionB.y;
        if (positionB.z < min.z) min.z = positionB.z;
        min -= Vector3.one * radius;

        Vector3 max = positionB;
        if (positionA.x > max.x) max.x = positionA.x;
        if (positionA.y > max.y) max.y = positionA.y;
        if (positionA.z > max.z) max.z = positionA.z;
        max += Vector3.one * radius;

        Vector3Int minInt = GetBondBaseInt(min);
        Vector3 maxScaled = (max - offset) * 10000;

        if (maxScaled.x - minInt.x < xSize * XIntSpacing)
            maxScaled.x = minInt.x + xSize * XIntSpacing * 1.1f;
        if (maxScaled.y - minInt.y < ySize * YIntSpacing)
            maxScaled.y = minInt.y + ySize * YIntSpacing * 1.1f;
        if (maxScaled.z - minInt.z < zSize * ZIntSpacing)
            maxScaled.z = minInt.z + zSize * ZIntSpacing * 1.1f;

        maxScaled += new Vector3(xSize * XIntSpacing * maxExtend.x, ySize * YIntSpacing * maxExtend.y, zSize * ZIntSpacing * maxExtend.z);

        List<Matrix4x4>[] blockTransforms = new List<Matrix4x4>[blocks.Length];
        List<PositionKey>[] blockDictKeys = new List<PositionKey>[blocks.Length];

        for (int i = 0; i < blockTransforms.Length; ++i)
        {
            blockTransforms[i] = new List<Matrix4x4>();
            blockDictKeys[i] = new List<PositionKey>();
        }

        for (int x = minInt.x; x < maxScaled.x; x += xSize * XIntSpacing)
        {
            for (int y = minInt.y; y < maxScaled.y; y += ySize * YIntSpacing)
            {
                for (int z = minInt.z; z < maxScaled.z; z += zSize * ZIntSpacing)
                {
                    for (int i = 0; i < blocks.Length; ++i)
                    {
                        for (int j = 0; j < blocks[i].Length; ++j)
                        {
                            Transform block = blocks[i][j];
                            Vector3 blockPosition = block.localPosition + offset + new Vector3Int(x, y, z).Divide(10000);

                            bool inCylinder = Vector3.Distance(blockPosition, NearestPointOnLine(positionA, positionB, blockPosition)) < radius;

                            if (inCylinder)
                            {
                                blockTransforms[i].Add(Matrix4x4.TRS(blockPosition, block.localRotation, Vector3.one));
                                Vector3Int localIntPosition = new Vector3Int(Mathf.RoundToInt(block.localPosition.x * 10000), Mathf.RoundToInt(block.localPosition.y * 10000), Mathf.RoundToInt(block.localPosition.z * 10000));
                                blockDictKeys[i].Add(new PositionKey(localIntPosition + new Vector3Int(x, y, z)));
                            }
                        }
                    }
                }
            }
        }

        Matrix4x4[][] _blockTransforms = new Matrix4x4[blocks.Length][];
        _blockDictKeys = new PositionKey[blocks.Length][];

        for (int i = 0; i < blockTransforms.Length; ++i)
        {
            _blockTransforms[i] = blockTransforms[i].ToArray();
            _blockDictKeys[i] = blockDictKeys[i].ToArray();
        }

        return _blockTransforms;
    }

    public static Vector3 NearestPointOnLine(Vector3 lineStart, Vector3 lineEnd, Vector3 pnt)
    {
        Vector3 lineDir = lineEnd - lineStart;
        lineDir.Normalize();
        var v = pnt - lineStart;
        var d = Vector3.Dot(v, lineDir);

        Vector3 cp = lineStart + lineDir * d;

        if (Vector3.Distance(lineStart, lineEnd) < Vector3.Distance(lineStart, cp))
            cp = lineEnd;

        if (Vector3.Distance(lineEnd, lineStart) < Vector3.Distance(lineEnd, cp))
            cp = lineStart;

        return cp;
    }

    public Matrix4x4[][] GetBlocksInMesh(Collider mesh, out PositionKey[][] _blockDictKeys)
    {
        Vector3Int minInt = GetBondBaseInt(mesh.bounds.min);
        Vector3 maxScaled = (mesh.bounds.max - offset) * 10000;
        float rayLength = mesh.bounds.max.x - mesh.bounds.min.x + 0.1f;

        if (maxScaled.x - minInt.x < xSize * XIntSpacing)
            maxScaled.x = minInt.x + xSize * XIntSpacing * 1.1f;
        if (maxScaled.y - minInt.y < ySize * YIntSpacing)
            maxScaled.y = minInt.y + ySize * YIntSpacing * 1.1f;
        if (maxScaled.z - minInt.z < zSize * ZIntSpacing)
            maxScaled.z = minInt.z + zSize * ZIntSpacing * 1.1f;

        maxScaled += new Vector3(xSize * XIntSpacing * maxExtend.x, ySize * YIntSpacing * maxExtend.y, zSize * ZIntSpacing * maxExtend.z);

        List<Matrix4x4>[] blockTransforms = new List<Matrix4x4>[blocks.Length];
        List<PositionKey>[] blockDictKeys = new List<PositionKey>[blocks.Length];

        for (int i = 0; i < blockTransforms.Length; ++i)
        {
            blockTransforms[i] = new List<Matrix4x4>();
            blockDictKeys[i] = new List<PositionKey>();
        }

        for (int x = minInt.x; x < maxScaled.x; x += xSize * XIntSpacing)
        {
            for (int y = minInt.y; y < maxScaled.y; y += ySize * YIntSpacing)
            {
                for (int z = minInt.z; z < maxScaled.z; z += zSize * ZIntSpacing)
                {
                    for (int i = 0; i < blocks.Length; ++i)
                    {
                        for (int j = 0; j < blocks[i].Length; ++j)
                        {
                            Transform block = blocks[i][j];
                            Vector3 blockPosition = block.localPosition + offset + new Vector3Int(x, y, z).Divide(10000);

                            bool pointInMesh = false;

                            if (meshBrushLevel == MeshBrushLevels.rough)
                                pointInMesh = PointInMesh(blockPosition, rayLength, Vector3.right);
                            else
                                pointInMesh = PointInMesh(blockPosition, rayLength, Vector3.right) && PointInMesh(blockPosition, rayLength, Vector3.forward) && PointInMesh(blockPosition, rayLength, Vector3.up);

                            if (pointInMesh)
                            {
                                blockTransforms[i].Add(Matrix4x4.TRS(blockPosition, block.localRotation, Vector3.one));
                                Vector3Int localIntPosition = new Vector3Int(Mathf.RoundToInt(block.localPosition.x * 10000), Mathf.RoundToInt(block.localPosition.y * 10000), Mathf.RoundToInt(block.localPosition.z * 10000));
                                blockDictKeys[i].Add(new PositionKey(localIntPosition + new Vector3Int(x, y, z)));
                            }
                        }
                    }
                }
            }
        }

        Matrix4x4[][] _blockTransforms = new Matrix4x4[blocks.Length][];
        _blockDictKeys = new PositionKey[blocks.Length][];

        for (int i = 0; i < blockTransforms.Length; ++i)
        {
            _blockTransforms[i] = blockTransforms[i].ToArray();
            _blockDictKeys[i] = blockDictKeys[i].ToArray();
        }

        return _blockTransforms;
    }

    bool PointInMesh(Vector3 pos, float rayLength, Vector3 rayDirection)
    {
        Physics.queriesHitBackfaces = true;
        Ray ray = new Ray(pos, rayDirection);
        RaycastHit hit;
        int hits = 0;

        while (true)
        {
            Physics.Raycast(ray, out hit, rayLength, layerMask);
            if (hit.collider == null)
                break;
            else if (hit.collider != null && hit.collider.gameObject.tag != "MeshSpawner")
            {
                Debug.Log("Found stray collider, name: " + hit.collider.gameObject.name);
            }
  
            ++hits;
            ray = new Ray(hit.point + rayDirection * 0.0001f, rayDirection);
        }


        if (hits % 2 == 1)
        {
            return true;
        }
        else
            return false;
    }
}

public enum MeshBrushLevels
{
    rough,
    fine
}

[Serializable]
public struct PartTypeArray
{ 
    [SerializeField]
    public Transform[] blocks;
}
