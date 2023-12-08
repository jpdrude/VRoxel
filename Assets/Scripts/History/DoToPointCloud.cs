using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DoToPointCloud : DoAction
{
    PositionKey[] keys;
    Dictionary<PositionKey, Matrix4x4> transforms;

    PositionKey[][] sortedKeys = null;

    public DoToPointCloud(DoType type, PositionKey[] _keys, Dictionary<PositionKey, Matrix4x4> _transforms) : base(type) 
    {
        keys = _keys;
        transforms = _transforms;
    }

    public DoToPointCloud(DoType type, List<Matrix4x4> _transforms) : base(type)
    {
        transforms = new Dictionary<PositionKey, Matrix4x4>();
        keys = new PositionKey[_transforms.Count];

        for (int i = 0; i < _transforms.Count; ++i)
        {
            Matrix4x4 matrix = _transforms[i];
            PositionKey key = new PositionKey(matrix);

            transforms.Add(key, matrix);
            keys[i] = key;
        }
    }

    public override bool Undo()
    {
        if (keys == null || keys.Length == 0) return false;

        if (sortedKeys == null)
        {
            sortedKeys = new PositionKey[BlockContainer.Container.PartTypeCount][];

            List<PositionKey>[] tempKeys = new List<PositionKey>[BlockContainer.Container.PartTypeCount];
            for (int i = 0; i < tempKeys.Length; ++i)
            {
                tempKeys[i] = new List<PositionKey>();
            }

            foreach (PositionKey key in keys)
            {
                PositionEntry entry = BlockContainer.Container.BlockDictionary[key];
                tempKeys[entry.blockType].Add(key);
            }

            for (int i = 0; i < sortedKeys.Length; ++i)
            {
                sortedKeys[i] = tempKeys[i].ToArray();
            }
        }

        BlockContainer.Container.ReinstateBlocks(sortedKeys);
        PointCloudIO.RecalculatePointCloud();

        return true;
    }

    public override bool Redo()
    {
        BlockContainer.Container.HideBlocks(sortedKeys);
        BlockContainer.Container.BlocksToPointCloud(keys, transforms);

        return true;
    }
}
