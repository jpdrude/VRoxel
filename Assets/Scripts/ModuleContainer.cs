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

    public void AddBrick(Matrix4x4 _transform, Vector3Int? dictPosition = null)
    {
        if (dictPosition == null)
            BrickToArray(_transform);
        else
            BrickToArray(_transform, (Vector3Int)dictPosition);

        GPUInstancerAPI.UpdateVisibilityBufferWithMatrix4x4Array(manager, brick.prefabPrototype, transformArray);
    }

    public void AddBricks(Matrix4x4[] matrices, Vector3Int[] dictPositions = null)
    {
        DateTime start = DateTime.Now;
        int counter = 0;

        for (int i = 0; i < matrices.Length; ++i)
        {
            var matrix4x4 = matrices[i];

            if (dictPositions == null)
            {
                if (BrickToArray(matrix4x4)) ++counter;
            }
            else
            {
                if (BrickToArray(matrix4x4, dictPositions[i])) ++counter;
            }
        }

        TimeSpan duration = DateTime.Now - start;
        if (logAddRemove) Debug.Log("Added " + counter + " bricks of " + matrices.Length + " possible. Took " + duration.TotalMilliseconds.ToString("F1") + " milliseconds");

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

    bool BrickToArray(Matrix4x4 _transform, Vector3Int? dictPos = null)
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

    bool BrickFromArray(Matrix4x4 _transform, Vector3Int? dictPos = null)
    {
        DictPosition key;
        if (dictPos == null)
            key = new DictPosition(_transform.GetPosition());
        else
            key = new DictPosition((Vector3Int)dictPos);

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

    public DictPosition(Vector3Int vec)
    {
        x = vec.x;
        y = vec.y;
        z = vec.z;
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