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

    Stack<int>[] cloudIndexStack;
    Matrix4x4[][] cloudTransformArray = null;
    public Matrix4x4[][] CloudTransformArray { get { return cloudTransformArray; } }

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
        ResetCloudContainer();
    }

    public void BlocksToPointCloud()
    {
        List<Matrix4x4> toCloudTransforms = new List<Matrix4x4>();

        for (int i = 0; i < transformArray.Length; ++i)
        {
            for (int j = 0; j < transformArray[i].Length; ++j)
            {
                Matrix4x4 matrix4x4 = transformArray[i][j];
                if (matrix4x4 != Matrix4x4.zero)
                {
                    toCloudTransforms.Add(matrix4x4);

                    var blockDictKey = new PositionKey(matrix4x4);
                    indexStack[i].Push(blockDictionary[blockDictKey].item);

                    if (cloudIndexStack[i].Count <= 0)
                        NewCloudBatch(i);

                    int cloudIndex = cloudIndexStack[i].Pop();

                    cloudTransformArray[i][cloudIndex] = matrix4x4;

                    uint timeStamp = blockDictionary[blockDictKey].time;

                    blockDictionary[blockDictKey] = new PositionEntry(i, -cloudIndex, timeStamp);
                }
            }
        }

        PointCloudIO.ModulesToCloud(toCloudTransforms);

        ResetContainer(true);
    }

    public void BlocksToPointCloud(PositionKey[] blockKeys, Dictionary<PositionKey, Matrix4x4> blockTransforms)
    {
        List<Matrix4x4> toCloudTransforms = new List<Matrix4x4>();

        for (int i = 0; i < blockKeys.Length; ++i)
        {
            if (!blockDictionary.ContainsKey(blockKeys[i])) continue;

            PositionEntry blockIndex = blockDictionary[blockKeys[i]];
            if (blockIndex.item <= 0) continue;

            indexStack[blockIndex.blockType].Push(blockIndex.item);

            if (cloudIndexStack[blockIndex.blockType].Count <= 0)
                NewCloudBatch(blockIndex.blockType);

            int cloudIndex = cloudIndexStack[blockIndex.blockType].Pop();

            cloudTransformArray[blockIndex.blockType][cloudIndex] = blockTransforms[blockKeys[i]];

            uint timeStamp = blockDictionary[blockKeys[i]].time;

            blockDictionary[blockKeys[i]] = new PositionEntry(blockIndex.blockType, -cloudIndex, timeStamp);

            toCloudTransforms.Add(blockTransforms[blockKeys[i]]);
        }

        PointCloudIO.ModulesToCloud(toCloudTransforms);
    }

    public Dictionary<PositionKey, Matrix4x4> HideBlocks(PositionKey[][] blockKeys)
    {
        Dictionary<PositionKey, Matrix4x4> blockTransforms = new Dictionary<PositionKey, Matrix4x4>();

        for (int i = 0; i < blockKeys.Length; ++i)
        {
            for (int j = 0; j < blockKeys[i].Length; ++j)
            {
                if (!blockDictionary.ContainsKey(blockKeys[i][j])) continue;

                PositionEntry blockIndex = blockDictionary[blockKeys[i][j]];
                if (blockIndex.item <= 0) continue;

                if (transformArray[blockIndex.blockType][blockIndex.item] == Matrix4x4.zero) continue;

                blockTransforms.Add(blockKeys[i][j], transformArray[blockIndex.blockType][blockIndex.item]);
                transformArray[blockIndex.blockType][blockIndex.item] = Matrix4x4.zero;
            }
        }

        for (int i = 0; i < parts.Length; ++i)
            GPUInstancerAPI.UpdateVisibilityBufferWithMatrix4x4Array(manager, parts[i].prefabPrototype, transformArray[i]);

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
        {
            blockDictionary = new Dictionary<PositionKey, PositionEntry>();
            ResetCloudContainer();
        }    

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

    public void ResetCloudContainer()
    {
        cloudTransformArray = new Matrix4x4[parts.Length][];

        cloudIndexStack = new Stack<int>[parts.Length];

        for (int i = 0; i < parts.Length; ++i)
        {
            cloudTransformArray[i] = new Matrix4x4[batchSize];

            cloudIndexStack[i] = new Stack<int>();
        }

        for (int i = 0; i < parts.Length; ++i)
        {
            for (int j = batchSize - 1; j >= 0; --j)
            {
                cloudTransformArray[i][j] = Matrix4x4.zero;
                cloudIndexStack[i].Push(j);
            }
        }
    }

    public void AddBlock(Matrix4x4 _transform, int partType, PositionKey? blockDictKey = null)
    {
        uint timeStamp = (uint)(GameManager.gameTime * 10);

        if (blockDictKey == null)
            BlockToArray(_transform, partType, timeStamp);
        else
            BlockToArray(_transform, partType, timeStamp, blockDictKey);

        GPUInstancerAPI.UpdateVisibilityBufferWithMatrix4x4Array(manager, parts[partType].prefabPrototype, transformArray[partType]);
    }

    //Needs Change
    public void AddBlocks(Matrix4x4[][] blockTransforms, PositionKey[][] blockDictKeys = null, uint[][] times = null, bool asPointCloud = false)
    {
        List<Matrix4x4> toCloudTransforms = new List<Matrix4x4>();

        uint timeStamp = (uint)(GameManager.gameTime * 10);

        for (int i = 0; i < blockTransforms.Length; ++i)
        {
            for (int j = 0; j < blockTransforms[i].Length; ++j)
            {
                var matrix4x4 = blockTransforms[i][j];

                if (times != null)
                    timeStamp = times[i][j];

                if (blockDictKeys == null)
                {
                    if (!asPointCloud)
                    {
                        BlockToArray(matrix4x4, i, timeStamp);
                    }
                    else
                    {
                        BlockToArray(matrix4x4, i, timeStamp, null, true);
                        toCloudTransforms.Add(matrix4x4);
                    }
                }
                else
                {
                    if (!asPointCloud)
                    {
                        BlockToArray(matrix4x4, i, timeStamp, blockDictKeys[i][j]);
                    }
                    else
                    {
                        BlockToArray(matrix4x4, i, timeStamp, blockDictKeys[i][j], true);
                        toCloudTransforms.Add(matrix4x4);
                    }
                }
            }
        }

        if (!asPointCloud)
            for (int i = 0; i < parts.Length; ++i)
                GPUInstancerAPI.UpdateVisibilityBufferWithMatrix4x4Array(manager, parts[i].prefabPrototype, transformArray[i]);
        else
            PointCloudIO.ModulesToCloud(toCloudTransforms);
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
                transformArray[entry.blockType][blockIndex] = cloudTransformArray[entry.blockType][pcIndex];
                blockDictionary[dictKey] = new PositionEntry(entry.blockType, blockIndex, entry.time);
                cloudTransformArray[entry.blockType][pcIndex] = Matrix4x4.zero;

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

    bool BlockToArray(Matrix4x4 _transform, int blockType, uint timeStamp, PositionKey? blockKey = null, bool skipGPUInstancer = false)
    {
        PositionKey key;
        if (blockKey == null)
            key = new PositionKey(_transform.GetPosition());
        else
            key = (PositionKey)blockKey;

        if (!blockDictionary.ContainsKey(key))
        {
            if (!skipGPUInstancer)
            {
                if (indexStack[blockType].Count == 0)
                    NewBatch(blockType);

                int idx = indexStack[blockType].Pop();
                blockDictionary.Add(key, new PositionEntry(blockType, idx, timeStamp));
                transformArray[blockType][idx] = _transform;
                return true;
            }
            else
            {
                if (cloudIndexStack[blockType].Count == 0)
                    NewCloudBatch(blockType);

                int idx = cloudIndexStack[blockType].Pop();
                blockDictionary.Add(key, new PositionEntry(blockType, -idx, timeStamp));
                cloudTransformArray[blockType][idx] = _transform;
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

    void NewCloudBatch(int blockType)
    {
        Matrix4x4[] tempCloudTransformArray = new Matrix4x4[cloudTransformArray[blockType].Length + batchSize];

        Array.Copy(cloudTransformArray[blockType], tempCloudTransformArray, cloudTransformArray[blockType].Length);

        for (int i = cloudTransformArray[blockType].Length + 1; i < tempCloudTransformArray.Length; ++i)
        {
            if (i == 0) continue;
            cloudIndexStack[blockType].Push(i);
            tempCloudTransformArray[i] = Matrix4x4.zero;
        }

        cloudTransformArray[blockType] = tempCloudTransformArray;
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
    public uint time; //Time in 1/10 sec

    public PositionEntry(int _blockType, int _item, uint _time)
    {
        blockType = _blockType;
        item = _item;
        time = _time;
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