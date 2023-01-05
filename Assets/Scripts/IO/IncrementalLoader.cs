using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class IncrementalLoader : MonoBehaviour
{
    [SerializeField]
    float loadSpeed = 0.1f;
    [SerializeField]
    bool loadAsLayers = false;

    float timer = 0;

    Stack<IncrementalLoadData> loadStack = null;

    public void SetLoadData(Matrix4x4[][] loadContainer, uint[][] loadTimes)
    {
        if (!loadAsLayers)
            SetLoadByTimeStamp(loadContainer, loadTimes);
        else
            SetLoadByLayer(loadContainer, loadTimes);
    }

    void SetLoadByTimeStamp(Matrix4x4[][] loadContainer, uint[][] loadTimes)
    {
        Dictionary<uint, List<Matrix4x4>[]> sortContainer = new Dictionary<uint, List<Matrix4x4>[]>();

        loadStack = new Stack<IncrementalLoadData>();

        for (int i = 0; i < loadContainer.Length; ++i)
        {
            for (int j = 0; j < loadContainer[i].Length; ++j)
            {
                if (!sortContainer.ContainsKey(loadTimes[i][j]))
                {
                    sortContainer.Add(loadTimes[i][j], new List<Matrix4x4>[loadContainer.Length]);
                    for (int ii = 0; ii < loadContainer.Length; ++ii)
                        sortContainer[loadTimes[i][j]][ii] = new List<Matrix4x4>();
                }

                sortContainer[loadTimes[i][j]][i].Add(loadContainer[i][j]);
            }
        }

        uint[] keys = new uint[sortContainer.Keys.Count];
        sortContainer.Keys.CopyTo(keys, 0);
        Array.Sort(keys);
        Array.Reverse(keys);

        foreach (uint key in keys)
        {
            Matrix4x4[][] matrices = new Matrix4x4[loadContainer.Length][];
            uint[][] times = new uint[loadContainer.Length][];
            for (int i = 0; i < loadContainer.Length; ++i)
            {
                matrices[i] = sortContainer[key][i].ToArray();
                times[i] = new uint[matrices[i].Length];

                for (int j = 0; j < matrices[i].Length; ++j)
                    times[i][j] = key;
            }

            loadStack.Push(new IncrementalLoadData(matrices, times));
        }
    }

    void SetLoadByLayer(Matrix4x4[][] loadContainer, uint[][] loadTimes)
    {
        Dictionary<int, List<IncrementalLoadItem>> sortContainer = new Dictionary<int, List<IncrementalLoadItem>>();

        loadStack = new Stack<IncrementalLoadData>();

        for (int i = 0; i < loadContainer.Length; ++i)
        {
            for (int j = 0; j < loadContainer[i].Length; ++j)
            {
                PositionKey posKey = new PositionKey(loadContainer[i][j]);

                if (!sortContainer.ContainsKey(posKey.y))
                {
                    sortContainer.Add(posKey.y, new List<IncrementalLoadItem>());
                }

                sortContainer[posKey.y].Add(new IncrementalLoadItem(loadContainer[i][j], loadTimes[i][j], i));
            }
        }

        int[] keys = new int[sortContainer.Keys.Count];
        sortContainer.Keys.CopyTo(keys, 0);
        Array.Sort(keys);
        Array.Reverse(keys);

        foreach (int key in keys)
        {
            Matrix4x4[][] _matrices = new Matrix4x4[loadContainer.Length][];
            List<Matrix4x4>[] matrices = new List<Matrix4x4>[loadContainer.Length];

            uint[][] _times = new uint[loadContainer.Length][];
            List<uint>[] times = new List<uint>[loadContainer.Length];

            for (int i = 0; i < loadContainer.Length; ++i)
            {
                matrices[i] = new List<Matrix4x4>();
                times[i] = new List<uint>();
            }

            foreach (IncrementalLoadItem item in sortContainer[key])
            {
                matrices[item.blockType].Add(item.transform);
                times[item.blockType].Add(item.loadTime);
            }

            for (int i = 0; i < loadContainer.Length; ++i)
            {
                _matrices[i] = matrices[i].ToArray();
                _times[i] = times[i].ToArray();
            }

            loadStack.Push(new IncrementalLoadData(_matrices, _times));
        }
    }

    void Update()
    {
        if (loadStack == null) 
            return;

        if (loadStack.Count == 0)
        {
            loadStack = null;
            timer = 0;
            return;
        }

        if (timer < loadSpeed)
        {
            timer += Time.deltaTime;
            return;
        }

        timer = 0;
        IncrementalLoadData data = loadStack.Pop();
        BlockContainer.Container.AddBlocks(data.transforms, null, data.loadTimes);
    }
    
    class IncrementalLoadData
    {
        public Matrix4x4[][] transforms;
        public uint[][] loadTimes;

        public IncrementalLoadData(Matrix4x4[][] _transforms, uint[][] _loadTimes)
        {
            transforms = _transforms;
            loadTimes = _loadTimes;
        }
    }

    class IncrementalLoadItem
    {
        public Matrix4x4 transform;
        public uint loadTime;
        public int blockType;

        public IncrementalLoadItem(Matrix4x4 _transform, uint _loadTime, int _blockType)
        {
            transform = _transform;
            loadTime = _loadTime;
            blockType = _blockType;
        }
    }
}
