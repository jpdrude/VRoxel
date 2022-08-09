using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using GPUInstancer;
using System;

public class ModuleContainer
{
    public static ModuleContainer Container { get; set; }

    public static readonly int brickBatchSize = 100000;

    GPUInstancerPrefabManager manager;
    GPUInstancerPrefab brick;

    Stack<int> indexStack;
    Matrix4x4[] transformArray;
    public Matrix4x4[] TransformArray { get { return transformArray; } }

    Matrix4x4[] pcTransforms;
    public Matrix4x4[] PcTransforms { get { return pcTransforms; } }

    Dictionary<DictPosition, int> moduleDictionary;
    public Dictionary<DictPosition, int> ModuleDictionary { get { return moduleDictionary; } }

    //Dictionary<DictPosition, Matrix4x4> 

    bool logAddRemove = false;

    public ModuleContainer(GPUInstancerPrefabManager _manager, GPUInstancerPrefab _brick)
    {
        Container = this;
        manager = _manager;
        brick = _brick;

        ResetContainer();
    }

    public void ModulesToPointCloud()
    {
        List<Matrix4x4> transformList = new List<Matrix4x4>();
        foreach(Matrix4x4 matrix4x4 in transformArray)
        {
            if (matrix4x4 != Matrix4x4.zero)
            {
                transformList.Add(matrix4x4);
                var dictPosition = new DictPosition(matrix4x4);
                indexStack.Push(moduleDictionary[dictPosition]);
                moduleDictionary[dictPosition] = -1;
            }
        }


        Matrix4x4[] returnArray = transformList.ToArray();

        PointCloudIO.ModulesToCloud(AddPCTransforms(returnArray));

        ResetContainer(true);
    }

    public void ModulesToPointCloud(DictPosition[] brickEntries)
    {
        List<Matrix4x4> brickTransforms = new List<Matrix4x4>();
        int brickCounter = 0;
        int pcLength = 0;
        if (pcTransforms != null)
            pcLength = pcTransforms.Length;

        for (int i = 0; i < brickEntries.Length; ++i)
        {
            if (!moduleDictionary.ContainsKey(brickEntries[i])) continue;

            int brickIndex = moduleDictionary[brickEntries[i]];
            if (brickIndex <= 0) continue;

            brickTransforms.Add(transformArray[brickIndex]);
            indexStack.Push(brickIndex);
            transformArray[brickIndex] = Matrix4x4.zero;
            moduleDictionary[brickEntries[i]] = -pcLength - brickCounter;
            ++brickCounter;
        }

        PointCloudIO.ModulesToCloud(AddPCTransforms(brickTransforms.ToArray()));

        GPUInstancerAPI.UpdateVisibilityBufferWithMatrix4x4Array(manager, brick.prefabPrototype, transformArray);
    }

    public Matrix4x4[] AddPCTransforms(Matrix4x4[] transforms)
    {
        if (pcTransforms == null)
            pcTransforms = transforms;
        else
        {
            Matrix4x4[] combinedTransforms = new Matrix4x4[transforms.Length + pcTransforms.Length];
            Array.Copy(pcTransforms, combinedTransforms, pcTransforms.Length);
            Array.Copy(transforms, 0, combinedTransforms, pcTransforms.Length, transforms.Length);
            pcTransforms = combinedTransforms;
        }

        return transforms;
    }

    public void ResetContainer(bool keepDictionary = false)
    {
        transformArray = new Matrix4x4[brickBatchSize];
        indexStack = new Stack<int>();
        
        if (!keepDictionary)
            moduleDictionary = new Dictionary<DictPosition, int>();


        for (int i = brickBatchSize - 1; i >= 0; --i)
        {
            transformArray[i] = Matrix4x4.zero;
            indexStack.Push(i);
        }

        GPUInstancerAPI.InitializeWithMatrix4x4Array(manager, brick.prefabPrototype, transformArray);
    }

    public void AddBrick(Matrix4x4 _transform, Vector3Int? dictPosition = null)
    {
        if (dictPosition == null)
            BrickToArray(_transform);
        else
            BrickToArray(_transform, (Vector3Int)dictPosition);

        GPUInstancerAPI.UpdateVisibilityBufferWithMatrix4x4Array(manager, brick.prefabPrototype, transformArray);
    }

    public void AddBricks(Matrix4x4[] matrices, Vector3Int[] dictPositions = null, bool asPointCloud = false)
    {
        DateTime start = DateTime.Now;
        int counter = 0;

        for (int i = 0; i < matrices.Length; ++i)
        {
            var matrix4x4 = matrices[i];

            if (dictPositions == null)
            {
                if (!asPointCloud)
                {
                    if (BrickToArray(matrix4x4)) ++counter;
                }
                else
                {
                    if (BrickToArray(matrix4x4, null, true)) ++counter;
                }
            }
            else
            {
                if (!asPointCloud)
                {
                    if (BrickToArray(matrix4x4, dictPositions[i])) ++counter;
                }
                else
                {
                    if (BrickToArray(matrix4x4, dictPositions[i], true)) ++counter;
                }
            }
        }

        TimeSpan duration = DateTime.Now - start;
        if (logAddRemove) Debug.Log("Added " + counter + " bricks of " + matrices.Length + " possible. Took " + duration.TotalMilliseconds.ToString("F1") + " milliseconds");

        if (!asPointCloud)
            GPUInstancerAPI.UpdateVisibilityBufferWithMatrix4x4Array(manager, brick.prefabPrototype, transformArray);
        else
            PointCloudIO.ModulesToCloud(AddPCTransforms(matrices));
    }

    public void ReinstateBricks(Vector3Int[] brickEntries)
    {
        foreach (Vector3Int entry in brickEntries)
        {
            DictPosition dictEntry = new DictPosition(entry);
            if (!moduleDictionary.ContainsKey(dictEntry) || moduleDictionary[dictEntry] > 0)
                continue;

            int pcIndex = -moduleDictionary[dictEntry];

            if (indexStack.Count < 1) NewBatch();

            int brickIndex = indexStack.Pop();
            transformArray[brickIndex] = pcTransforms[pcIndex];
            moduleDictionary[dictEntry] = brickIndex;
            pcTransforms[pcIndex] = Matrix4x4.zero;
        }

        GPUInstancerAPI.UpdateVisibilityBufferWithMatrix4x4Array(manager, brick.prefabPrototype, transformArray);
    }

    public void RemoveBricks(Matrix4x4[] matrices, Vector3Int[] dictPositions = null)
    {
        DateTime start = DateTime.Now;
        int counter = 0;

        for (int i = 0; i < matrices.Length; ++i)
        {
            var matrix4x4 = matrices[i];

            if (dictPositions == null)
            {
                if (BrickFromArray(matrix4x4)) ++counter;
            }
            else
            {
                if (BrickFromArray(matrix4x4, dictPositions[i])) ++counter;
            }
        }

        TimeSpan duration = DateTime.Now - start;
        if (logAddRemove) Debug.Log("Removed " + counter + " bricks of " + matrices.Length + " possible. Took " + duration.TotalMilliseconds.ToString("F1") + " milliseconds");

        GPUInstancerAPI.UpdateVisibilityBufferWithMatrix4x4Array(manager, brick.prefabPrototype, transformArray);
    }

    bool BrickToArray(Matrix4x4 _transform, Vector3Int? dictPos = null, bool skipGPUInstancer = false)
    {
        if (indexStack.Count == 0)
            NewBatch();

        DictPosition key;
        if (dictPos == null)
            key = new DictPosition(_transform.GetPosition());
        else
            key = new DictPosition((Vector3Int)dictPos);

        if (!moduleDictionary.ContainsKey(key))
        {
            if (!skipGPUInstancer)
            {
                int idx = indexStack.Pop();
                moduleDictionary.Add(key, idx);
                transformArray[idx] = _transform;
                return true;
            }
            else
            {
                moduleDictionary.Add(key, -1);
                return true;
            }
        }
        else
            return false;
    }

    void NewBatch()
    {
        Matrix4x4[] tempTransformArray = new Matrix4x4[transformArray.Length + brickBatchSize];

        Array.Copy(transformArray, tempTransformArray, transformArray.Length);

        for (int i = transformArray.Length + 1; i < tempTransformArray.Length; ++i)
        {
            indexStack.Push(i);
            tempTransformArray[i] = Matrix4x4.zero;
        }

        transformArray = tempTransformArray;
        GPUInstancerAPI.InitializeWithMatrix4x4Array(manager, brick.prefabPrototype, transformArray);
    }

    bool BrickFromArray(Matrix4x4 _transform, Vector3Int? dictPos = null)
    {
        
        DictPosition key;
        if (dictPos == null)
            key = new DictPosition(_transform.GetPosition());
        else
            key = new DictPosition((Vector3Int)dictPos);

        if (moduleDictionary.ContainsKey(key))
        {
            if (moduleDictionary[key] <= 0) return false;
            
            transformArray[moduleDictionary[key]] = Matrix4x4.zero;
            indexStack.Push(moduleDictionary[key]);
            moduleDictionary.Remove(key);
            return true;
        }
        else
            return false;
    }

    public int GetCount()
    {
        return moduleDictionary.Count;
    }
}

public struct DictPosition
{
    public int x;
    public int y;
    public int z;

    public DictPosition(Vector3 vec)
    {
        x = Mathf.RoundToInt(vec.x * 10000);
        y = Mathf.RoundToInt(vec.y * 10000);
        z = Mathf.RoundToInt(vec.z * 10000);
    }

    public DictPosition(Vector3Int vec)
    {
        x = vec.x;
        y = vec.y;
        z = vec.z;
    }

    public DictPosition(Matrix4x4 matrix)
    {
        Vector3 vec = matrix.GetPosition();
        x = Mathf.RoundToInt(vec.x * 10000);
        y = Mathf.RoundToInt(vec.y * 10000);
        z = Mathf.RoundToInt(vec.z * 10000);
    }

    public override bool Equals(object obj)
    {
        if (typeof(DictPosition) != obj.GetType())
            return false;
        DictPosition other = (DictPosition)obj;
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