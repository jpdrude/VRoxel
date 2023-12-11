using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DoFromPointCloud : DoAction
{
    List<PositionKey>[] keys = null;
    Dictionary<PositionKey, Matrix4x4> transforms = null;

    PositionKey[][] sortedKeys = null;
    PositionKey[] allKeys = null;

    public int Count
    {
        get 
        {
            if (transforms == null)
                return 0;
            else
                return transforms.Count; 
        } 
    }

    public DoFromPointCloud(DoType type) : base(type)
    {

    }

    public void AppendKeys(PositionKey[][] _keys, Dictionary<PositionKey, Matrix4x4> _transforms)
    {
        if (keys == null)
        {
            keys = new List<PositionKey>[_keys.Length];

            transforms = _transforms;

            for (int i = 0; i < keys.Length; ++i)
            {
                keys[i] = new List<PositionKey>();
                keys[i].AddRange(_keys[i]);
            }

            return;
        }

        for (int i = 0; i < keys.Length; ++i)
        {
            keys[i].AddRange(_keys[i]);
        }

        foreach (KeyValuePair<PositionKey, Matrix4x4> pair in _transforms)
        {
            try
            {
                transforms.Add(pair.Key, pair.Value);
            }
            catch (System.Exception e)
            {
                Debug.LogError("Key already in Dictionary? \n" + e.Message);
            }
        }
    }


    public override bool Undo()
    {
        if (keys == null || keys.Length == 0) return false;

        if (sortedKeys == null)
        {
            sortedKeys = new PositionKey[keys.Length][];

            for (int i = 0; i < keys.Length; ++i)
            {
                for (int j = keys[i].Count - 1; j >= 0; --j)
                {
                    if (!transforms.ContainsKey(keys[i][j]))
                        keys[i].RemoveAt(j);
                }

                sortedKeys[i] = keys[i].ToArray();
            }
        }

        if (allKeys == null)
        {
            allKeys = new PositionKey[transforms.Count];
            int i = 0;

            foreach (PositionKey key in transforms.Keys)
            {
                allKeys[i] = key;
                ++i;
            }
        }

        BlockContainer.Container.HideBlocks(sortedKeys);
        BlockContainer.Container.BlocksToPointCloud(allKeys, transforms);

        return true;
    }

    public override bool Redo()
    {
        BlockContainer.Container.ReinstateBlocks(sortedKeys);
        PointCloudIO.RecalculatePointCloud();

        return true;
    }
}
