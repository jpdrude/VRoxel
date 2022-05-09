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

    bool logAddRemove = false;

    public ModuleContainer(GPUInstancerPrefabManager _manager, GPUInstancerPrefab _brick)
    {
        Container = this;
        manager = _manager;
        brick = _brick;

        ResetContainer();
    }

    public Matrix4x4[] ModulesToPC()
    {
        if (pcTransforms == null)
            pcTransforms = transformArray;
        else
        {
            Matrix4x4[] combinedTransforms = new Matrix4x4[transformArray.Length + pcTransforms.Length];
            Array.Copy(pcTransforms, combinedTransforms, pcTransforms.Length);
            Array.Copy(transformArray, 0, combinedTransforms, pcTransforms.Length, transformArray.Length);
            pcTransforms = combinedTransforms;
        }

        var returnArray = transformArray;

        ResetContainer(true);

        return returnArray;
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

    public void AddBrick(Matrix4x4 _transform)
    {
        BrickToArray(_transform);
        GPUInstancerAPI.UpdateVisibilityBufferWithMatrix4x4Array(manager, brick.prefabPrototype, transformArray);
    }

    public void AddBricks(Matrix4x4[] matrices)
    {
        DateTime start = DateTime.Now;
        int counter = 0;

        foreach (Matrix4x4 matrix4x4 in matrices)
            if (BrickToArray(matrix4x4)) ++counter;

        TimeSpan duration = DateTime.Now - start;
        if (logAddRemove) Debug.Log("Added " + counter + " bricks of " + matrices.Length + " possible. Took " + duration.TotalMilliseconds.ToString("F1") + " milliseconds");

        GPUInstancerAPI.UpdateVisibilityBufferWithMatrix4x4Array(manager, brick.prefabPrototype, transformArray);
    }

    public void RemoveBricks(Matrix4x4[] _transforms)
    {
        DateTime start = DateTime.Now;
        int counter = 0;

        foreach (Matrix4x4 _transform in _transforms)
            if (BrickFromArray(_transform)) ++counter;

        TimeSpan duration = DateTime.Now - start;
        if (logAddRemove) Debug.Log("Removed " + counter + " bricks of " + _transforms.Length + " possible. Took " + duration.TotalMilliseconds.ToString("F1") + " milliseconds");

        GPUInstancerAPI.UpdateVisibilityBufferWithMatrix4x4Array(manager, brick.prefabPrototype, transformArray);
    }

    bool BrickToArray(Matrix4x4 _transform)
    {
        if (indexStack.Count == 0)
            NewBatch();

        DictPosition key = new DictPosition(_transform.GetPosition());
        if (!moduleDictionary.ContainsKey(key))
        {
            int idx = indexStack.Pop();
            moduleDictionary.Add(key, idx);
            transformArray[idx] = _transform;
            return true;
        }
        else
            return false;
    }

    void NewBatch()
    {
        Matrix4x4[] tempTransformArray = new Matrix4x4[transformArray.Length + brickBatchSize];

        Array.Copy(transformArray, tempTransformArray, transformArray.Length);

        for (int i = transformArray.Length; i < tempTransformArray.Length; ++i)
        {
            indexStack.Push(i);
            tempTransformArray[i] = Matrix4x4.zero;
        }

        transformArray = tempTransformArray;
        GPUInstancerAPI.InitializeWithMatrix4x4Array(manager, brick.prefabPrototype, transformArray);
    }

    bool BrickFromArray(Matrix4x4 _transform)
    {
        DictPosition key = new DictPosition(_transform.GetPosition());
        if (moduleDictionary.ContainsKey(key))
        {
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
        x = Mathf.RoundToInt(vec.x * 100000);
        y = Mathf.RoundToInt(vec.y * 100000);
        z = Mathf.RoundToInt(vec.z * 100000);
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