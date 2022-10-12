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

    public GPUInstancerPrefab[] PartPrefabs { get { return partPrefabs; } }

    public string BondName { get { return bondName; } }

    public void Initialize()
    {
        blocks = new Transform[bondBlocks.Length][];

        for (int i = 0; i < bondBlocks.Length; ++i)
            blocks[i] = bondBlocks[i].blocks;
    }

    public Matrix4x4 GetClosestBlock(Vector3 position, out int blockType)
    {
        Vector3 bondBase = GetBondBase(position);
        Vector3 bondPosition = position - bondBase;

        Transform best = blocks[0][0];
        int bestType = 0;

        for (int i = 0; i < blocks.Length; ++i)
            for (int j = 0; j < blocks[i].Length; ++j)
                if (Vector3.Distance(bondPosition, best.position) > Vector3.Distance(bondPosition, blocks[i][j].position))
                { 
                    best = blocks[i][j];
                    bestType = i;
                }

        blockType = bestType;

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

    public Matrix4x4[][] GetBlocksInBounds(Vector3 min, Vector3 max)
    {
        List<Matrix4x4>[] blockTransforms = new List<Matrix4x4>[blocks.Length];

        for (int i = 0; i < blockTransforms.Length; ++i)
            blockTransforms[i] = new List<Matrix4x4>();

        for (float x = min.x; x < max.x; x += GameManager.Bond.xSize * GameManager.Bond.XSpacing)
        {
            for (float y = min.y; y < max.y; y += GameManager.Bond.ySize * GameManager.Bond.YSpacing)
                for (float z = min.z; z < max.z; z += GameManager.Bond.zSize * GameManager.Bond.ZSpacing)
                {
                    for (int i = 0; i < blocks.Length; ++i)
                    {
                        for (int j = 0; j < blocks[i].Length; ++j)
                        {
                            blockTransforms[i].Add(Matrix4x4.TRS(blocks[i][j].localPosition + new Vector3(x, y, z), blocks[i][j].localRotation, Vector3.one));
                        }
                    }
                }
        }

        Matrix4x4[][] _blockTransforms = new Matrix4x4[blockTransforms.Length][];

        for (int i = 0; i < blockTransforms.Length; ++i)
            _blockTransforms[i] = blockTransforms[i].ToArray();

        return _blockTransforms;
    }

    public Matrix4x4[][] GetBlocksInBox(Vector3 min, Vector3 max, out PositionKey[][] _blockDictsKeys)
    {
        Vector3Int minInt = GetBondBaseInt(min);
        Vector3 maxScaled = max * 10000;

        List<Matrix4x4>[] blockTransforms = new List<Matrix4x4>[blocks.Length];
        List<PositionKey>[] blockDictKeys = new List<PositionKey>[blocks.Length];

        for (int i = 0; i < blockTransforms.Length; ++i)
        {
            blockTransforms[i] = new List<Matrix4x4>();
            blockDictKeys[i] = new List<PositionKey>();
        }

        for (int x = minInt.x; x < maxScaled.x; x += GameManager.Bond.xSize * GameManager.Bond.XIntSpacing)
        {
            for (int y = minInt.y; y < maxScaled.y; y += GameManager.Bond.ySize * GameManager.Bond.YIntSpacing)
            {
                for (int z = minInt.z; z < maxScaled.z; z += GameManager.Bond.zSize * GameManager.Bond.ZIntSpacing)
                {
                    for (int i = 0; i < blocks.Length; ++i)
                    {
                        for (int j = 0; j < blocks[i].Length; ++j)
                        {
                            Transform block = blocks[i][j].transform;
                            Vector3 blockPosition = block.localPosition + new Vector3Int(x, y, z).Divide(10000);
                            if (blockPosition.x >= min.x && blockPosition.y >= min.y && blockPosition.z >= min.z)
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

        Vector3Int min = GetBondBaseInt(position - Vector3.one * radius);
        Vector3 max = (position + Vector3.one * radius) * 10000;

        for (int i = 0; i < blockTransforms.Length; ++i)
        {
            blockTransforms[i] = new List<Matrix4x4>();
            blockDictKeys[i] = new List<PositionKey>();
        }

        for (int x = min.x; x < max.x; x += GameManager.Bond.xSize * GameManager.Bond.XIntSpacing)
        {
            for (int y = min.y; y < max.y; y += GameManager.Bond.ySize * GameManager.Bond.YIntSpacing)
            {
                for (int z = min.z; z < max.z; z += GameManager.Bond.zSize * GameManager.Bond.ZIntSpacing)
                {
                    for (int i = 0; i < blocks.Length; ++i)
                    {
                        for (int j = 0; j < blocks[i].Length; ++j)
                        {
                            if (Vector3.Distance(blocks[i][j].localPosition + new Vector3Int(x, y, z).Divide(10000), position) < radius)
                            {
                                Transform block = blocks[i][j].transform;
                                blockTransforms[i].Add(Matrix4x4.TRS(block.localPosition + new Vector3Int(x, y, z).Divide(10000), block.localRotation, Vector3.one));
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

    public Matrix4x4[][] GetBlocksInCylinder(Vector3 position, float radius, float height, out PositionKey[][] _blockDictKeys)
    {
        Vector3 min = position - new Vector3(radius, height / 2, radius);
        Vector3 max = position + new Vector3(radius, height / 2, radius);

        Vector3Int minInt = GetBondBaseInt(min);
        Vector3 maxScaled = max * 10000;

        List<Matrix4x4>[] blockTransforms = new List<Matrix4x4>[blocks.Length];
        List<PositionKey>[] blockDictKeys = new List<PositionKey>[blocks.Length];

        for (int i = 0; i < blockTransforms.Length; ++i)
        {
            blockTransforms[i] = new List<Matrix4x4>();
            blockDictKeys[i] = new List<PositionKey>();
        }

        for (int x = minInt.x; x < maxScaled.x; x += GameManager.Bond.xSize * GameManager.Bond.XIntSpacing)
        {
            for (int y = minInt.y; y < maxScaled.y; y += GameManager.Bond.ySize * GameManager.Bond.YIntSpacing)
            {
                for (int z = minInt.z; z < maxScaled.z; z += GameManager.Bond.zSize * GameManager.Bond.ZIntSpacing)
                {
                    for (int i = 0; i < blocks.Length; ++i)
                    {
                        for (int j = 0; j < blocks[i].Length; ++j)
                        {
                            Transform block = blocks[i][j];
                            Vector3 blockPosition = block.localPosition + new Vector3Int(x, y, z).Divide(10000);

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

    public Matrix4x4[][] GetBlocksInMesh(Collider mesh, out PositionKey[][] _blockDictKeys)
    {
        Vector3Int minInt = GetBondBaseInt(mesh.bounds.min);
        Vector3 maxScaled = mesh.bounds.max * 10000;
        float rayLength = mesh.bounds.max.x - mesh.bounds.min.x + 0.1f;

        List<Matrix4x4>[] blockTransforms = new List<Matrix4x4>[blocks.Length];
        List<PositionKey>[] blockDictKeys = new List<PositionKey>[blocks.Length];

        for (int i = 0; i < blockTransforms.Length; ++i)
        {
            blockTransforms[i] = new List<Matrix4x4>();
            blockDictKeys[i] = new List<PositionKey>();
        }

        for (int x = minInt.x; x < maxScaled.x; x += GameManager.Bond.xSize * GameManager.Bond.XIntSpacing)
        {
            for (int y = minInt.y; y < maxScaled.y; y += GameManager.Bond.ySize * GameManager.Bond.YIntSpacing)
            {
                for (int z = minInt.z; z < maxScaled.z; z += GameManager.Bond.zSize * GameManager.Bond.ZIntSpacing)
                {
                    for (int i = 0; i < blocks.Length; ++i)
                    {
                        for (int j = 0; j < blocks[i].Length; ++j)
                        {
                            Transform block = blocks[i][j];
                            Vector3 blockPosition = block.localPosition + new Vector3Int(x, y, z).Divide(10000);

                            if (PointInMesh(blockPosition, rayLength))
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
}

[Serializable]
public struct PartTypeArray
{ 
    [SerializeField]
    public Transform[] blocks;
}
