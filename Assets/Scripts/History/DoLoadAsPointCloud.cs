using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DoLoadAsPointCloud : DoAction
{
    Matrix4x4[][] transforms;
    bool[][] mask;
    uint[][] times;

    List<PositionKey> keys;

    public DoLoadAsPointCloud(DoType type, Matrix4x4[][] _transforms, bool[][] _mask, uint[][] _times) : base(type)
    {
        transforms = _transforms;
        mask = _mask;
        times = _times;

        keys = new List<PositionKey>();

        for (int i = 0; i < transforms.Length; ++i)
        {
            for (int j = 0; j < transforms[i].Length; ++j)
            {
                if (mask[i][j])
                    keys.Add(new PositionKey(transforms[i][j]));
            }
        }
    }

    public override bool Undo()
    {
        BlockContainer.Container.RemoveCloudBlocks(keys);
        return true;
    }

    public override bool Redo()
    {
        BlockContainer.Container.AddBlocks(transforms, null, times, true);
        return true;
    }
}
