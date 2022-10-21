using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using GPUInstancer;
using System;

public class BlockContainer
{
    public static BlockContainer Container { get; set; }

    public static readonly int batchSize = 100000;

    GPUInstancerPrefabManager manager;
    GPUInstancerPrefab[] parts;

    Stack<int>[] indexStack;
    Matrix4x4[][] transformArray;
    public Matrix4x4[][] TransformArray { get { return transformArray; } }

    Matrix4x4[][] pcTransformArray;
    public Matrix4x4[][] PcTransformArray { get { return pcTransformArray; } }

    Dictionary<PositionKey, PositionEntry> blockDictionary;
    public Dictionary<PositionKey, PositionEntry> BlockDictionary { get { return blockDictionary; } }
    public int PartTypeCount { get { return parts.Length; } }

    bool logAddRemove = false;

    public BlockContainer(GPUInstancerPrefabManager _manager, GPUInstancerPrefab[] _parts)
    {
        Container = this;
        manager = _manager;
        parts = _parts;

        ResetContainer();
    }

    public void BlocksToPointCloud()
    {
        List<Matrix4x4>[] blockTransforms = new List<Matrix4x4>[parts.Length];

        for (int i = 0; i < transformArray.Length; ++i)
        {
            blockTransforms[i] = new List<Matrix4x4>();
            for (int j = 0; j < transformArray[i].Length; ++j)
            {
                {
                    Matrix4x4 matrix4x4 = transformArray[i][j];
                    if (matrix4x4 != Matrix4x4.zero)
                    {
                        blockTransforms[i].Add(matrix4x4);
                        var blockDictKey = new PositionKey(matrix4x4);
                        indexStack[i].Push(blockDictionary[blockDictKey].item);
                        blockDictionary[blockDictKey] = new PositionEntry(i, -1);
                    }
                }
            }
        }


        Matrix4x4[][] _blockTransforms = new Matrix4x4[parts.Length][];
        for (int i = 0; i < parts.Length; ++i)
            _blockTransforms[i] = blockTransforms[i].ToArray();

        PointCloudIO.ModulesToCloud(AddPCTransforms(_blockTransforms));

        ResetContainer(true);
    }

    public void BlocksToPointCloud(PositionKey[] blockKeys)
    {
        List<Matrix4x4>[] blockTransforms = new List<Matrix4x4>[parts.Length];
        for (int i = 0; i < parts.Length; ++i)
            blockTransforms[i] = new List<Matrix4x4>();

        int[] blockCounter = new int[parts.Length];
        int pcLength = 0;
        if (pcTransformArray != null)
            pcLength = pcTransformArray.Length;

        for (int i = 0; i < blockKeys.Length; ++i)
        {
            if (!blockDictionary.ContainsKey(blockKeys[i])) continue;

            PositionEntry blockIndex = blockDictionary[blockKeys[i]];
            if (blockIndex.item <= 0) continue;

            blockTransforms[blockIndex.blockType].Add(transformArray[blockIndex.blockType][blockIndex.item]);
            indexStack[blockIndex.blockType].Push(blockIndex.item);
            transformArray[blockIndex.blockType][blockIndex.item] = Matrix4x4.zero;
            blockDictionary[blockKeys[i]] = new PositionEntry(blockIndex.blockType, -pcLength - blockCounter[blockIndex.blockType]);
            ++blockCounter[blockIndex.blockType];
        }

        Matrix4x4[][] _blockTransforms = new Matrix4x4[parts.Length][];
        for (int i = 0; i < _blockTransforms.Length; ++i)
            _blockTransforms[i] = blockTransforms[i].ToArray();

        PointCloudIO.ModulesToCloud(AddPCTransforms(_blockTransforms));

        for (int i = 0; i < parts.Length; ++i)
            GPUInstancerAPI.UpdateVisibilityBufferWithMatrix4x4Array(manager, parts[i].prefabPrototype, transformArray[i]);
    }

    public Matrix4x4[][] AddPCTransforms(Matrix4x4[][] blockTransforms)
    {
        if (pcTransformArray == null)
            pcTransformArray = blockTransforms;
        else
        {
            for (int i = 0; i < parts.Length; ++i)
            {
                Matrix4x4[] combinedTransforms = new Matrix4x4[blockTransforms[i].Length + pcTransformArray[i].Length];
                Array.Copy(pcTransformArray[i], combinedTransforms, pcTransformArray[i].Length);
                Array.Copy(blockTransforms[i], 0, combinedTransforms, pcTransformArray[i].Length, blockTransforms[i].Length);
                pcTransformArray[i] = combinedTransforms;
            }
        }

        return blockTransforms;
    }

    public void ResetContainer(bool keepDictionary = false)
    {
        transformArray = new Matrix4x4[parts.Length][];
        indexStack = new Stack<int>[parts.Length];
        for (int i = 0; i < parts.Length; ++i)
        {
            transformArray[i] = new Matrix4x4[batchSize];
            indexStack[i] = new Stack<int>();
        }        
        
        if (!keepDictionary)
            blockDictionary = new Dictionary<PositionKey, PositionEntry>();

        for (int i = 0; i < parts.Length; ++i)
        {
            for (int j = batchSize - 1; j >= 0; --j)
            {
                transformArray[i][j] = Matrix4x4.zero;
                indexStack[i].Push(j);
            }
        }

        for (int i = 0; i < parts.Length; ++i)
            GPUInstancerAPI.InitializeWithMatrix4x4Array(manager, parts[i].prefabPrototype, transformArray[i]);
    }

    public void AddBlock(Matrix4x4 _transform, int partType, PositionKey? blockDictKey = null)
    {
        if (blockDictKey == null)
            BlockToArray(_transform, partType);
        else
            BlockToArray(_transform, partType, blockDictKey);

        GPUInstancerAPI.UpdateVisibilityBufferWithMatrix4x4Array(manager, parts[partType].prefabPrototype, transformArray[partType]);
    }

    public void AddBlocks(Matrix4x4[][] blockTransforms, PositionKey[][] blockDictKeys = null, bool asPointCloud = false)
    {
        DateTime start = DateTime.Now;
        int counter = 0;

        for (int i = 0; i < blockTransforms.Length; ++i)
        {
            for (int j = 0; j < blockTransforms[i].Length; ++j)
            {
                var matrix4x4 = blockTransforms[i][j];

                if (blockDictKeys == null)
                {
                    if (!asPointCloud)
                    {
                        if (BlockToArray(matrix4x4, i)) ++counter;
                    }
                    else
                    {
                        if (BlockToArray(matrix4x4, i, null, true)) ++counter;
                    }
                }
                else
                {
                    if (!asPointCloud)
                    {
                        if (BlockToArray(matrix4x4, i, blockDictKeys[i][j])) ++counter;
                    }
                    else
                    {
                        if (BlockToArray(matrix4x4, i, blockDictKeys[i][j], true)) ++counter;
                    }
                }
            }
        }

        TimeSpan duration = DateTime.Now - start;
        if (logAddRemove) Debug.Log("Added " + counter + " blocks of " + blockTransforms.Length + " possible. Took " + duration.TotalMilliseconds.ToString("F1") + " milliseconds");

        if (!asPointCloud)
            for (int i = 0; i < parts.Length; ++i)
                GPUInstancerAPI.UpdateVisibilityBufferWithMatrix4x4Array(manager, parts[i].prefabPrototype, transformArray[i]);
        else
            PointCloudIO.ModulesToCloud(AddPCTransforms(blockTransforms));
    }

    public void ReinstateBlocks(PositionKey[][] blockDictPositions)
    {
        for (int i = 0; i < blockDictPositions.Length; ++i)
        {
            foreach (PositionKey dictKey in blockDictPositions[i])
            {
                if (!blockDictionary.ContainsKey(dictKey) || blockDictionary[dictKey].item > 0)
                    continue;

                PositionEntry entry = blockDictionary[dictKey];

                int pcIndex = -entry.item;

                if (indexStack[entry.blockType].Count < 1) NewBatch(entry.blockType);

                int blockIndex = indexStack[entry.blockType].Pop();
                transformArray[entry.blockType][blockIndex] = pcTransformArray[entry.blockType][pcIndex];
                blockDictionary[dictKey] = new PositionEntry(entry.blockType, blockIndex);
                pcTransformArray[entry.blockType][pcIndex] = Matrix4x4.zero;
            }
        }

        for (int i = 0; i < parts.Length; ++i)
            GPUInstancerAPI.UpdateVisibilityBufferWithMatrix4x4Array(manager, parts[i].prefabPrototype, transformArray[i]);
    }

    public void RemoveBlocks(Matrix4x4[][] blockTransforms, PositionKey[][] blockDictKeys = null)
    {
        DateTime start = DateTime.Now;
        int counter = 0;

        for (int i = 0; i < blockTransforms.Length; ++i)
        {
            for (int j = 0; j < blockTransforms[i].Length; ++j)
            {
                var matrix4x4 = blockTransforms[i][j];

                if (blockDictKeys == null)
                {
                    if (BlockFromArray(matrix4x4)) ++counter;
                }
                else
                {
                    if (BlockFromArray(matrix4x4, blockDictKeys[i][j])) ++counter;
                }
            }
        }

        TimeSpan duration = DateTime.Now - start;
        if (logAddRemove) Debug.Log("Removed " + counter + " blocks of " + blockTransforms.Length + " possible. Took " + duration.TotalMilliseconds.ToString("F1") + " milliseconds");

        for (int i = 0; i < parts.Length; ++i)
            GPUInstancerAPI.UpdateVisibilityBufferWithMatrix4x4Array(manager, parts[i].prefabPrototype, transformArray[i]);
    }

    bool BlockToArray(Matrix4x4 _transform, int blockType, PositionKey? blockKey = null, bool skipGPUInstancer = false)
    {
        if (indexStack[blockType].Count == 0)
            NewBatch(blockType);

        PositionKey key;
        if (blockKey == null)
            key = new PositionKey(_transform.GetPosition());
        else
            key = (PositionKey)blockKey;

        if (!blockDictionary.ContainsKey(key))
        {
            if (!skipGPUInstancer)
            {
                int idx = indexStack[blockType].Pop();
                blockDictionary.Add(key, new PositionEntry(blockType, idx));
                transformArray[blockType][idx] = _transform;
                return true;
            }
            else
            {
                blockDictionary.Add(key, new PositionEntry(blockType, -1));
                return true;
            }
        }
        else
            return false;
    }

    bool BlockFromArray(Matrix4x4 _transform, PositionKey? blockKey = null)
    {
        
        PositionKey key;
        if (blockKey == null)
            key = new PositionKey(_transform.GetPosition());
        else
            key = (PositionKey)blockKey;

        if (blockDictionary.ContainsKey(key))
        {
            if (blockDictionary[key].item < 0) return false;

            PositionEntry entry = blockDictionary[key];

            transformArray[entry.blockType][entry.item] = Matrix4x4.zero;
            indexStack[entry.blockType].Push(entry.item);
            blockDictionary.Remove(key);
            return true;
        }
        else
            return false;
    }

    void NewBatch(int blockType)
    {
        Matrix4x4[] tempTransformArray = new Matrix4x4[transformArray[blockType].Length + batchSize];

        Array.Copy(transformArray[blockType], tempTransformArray, transformArray[blockType].Length);

        for (int i = transformArray[blockType].Length + 1; i < tempTransformArray.Length; ++i)
        {
            indexStack[blockType].Push(i);
            tempTransformArray[i] = Matrix4x4.zero;
        }

        transformArray[blockType] = tempTransformArray;
        GPUInstancerAPI.InitializeWithMatrix4x4Array(manager, parts[blockType].prefabPrototype, transformArray[blockType]);
    }

    public int GetCount()
    {
        return blockDictionary.Count;
    }
}

public struct PositionEntry
{
    public int blockType;
    public int item;

    public PositionEntry(int _blockType, int _item)
    {
        blockType = _blockType;
        item = _item;
    }
}
public struct PositionKey
{
    public int x;
    public int y;
    public int z;

    public PositionKey(Vector3 vec)
    {
        x = Mathf.RoundToInt(vec.x * 10000);
        y = Mathf.RoundToInt(vec.y * 10000);
        z = Mathf.RoundToInt(vec.z * 10000);
    }

    public PositionKey(Vector3Int vec)
    {
        x = vec.x;
        y = vec.y;
        z = vec.z;
    }

    public PositionKey(Matrix4x4 matrix)
    {
        Vector3 vec = matrix.GetPosition();
        x = Mathf.RoundToInt(vec.x * 10000);
        y = Mathf.RoundToInt(vec.y * 10000);
        z = Mathf.RoundToInt(vec.z * 10000);
    }

    public override bool Equals(object obj)
    {
        if (typeof(PositionKey) != obj.GetType())
            return false;
        PositionKey other = (PositionKey)obj;
        if (other.x == x && other.y == y && other.z == z)
            return true;
        else
            return false;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(x, y, z);
    }
}