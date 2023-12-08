using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DoAddStroke : DoAction
{
    List<DoAddPartialStroke> strokeParts = new List<DoAddPartialStroke>();
    public int StrokeLength { get { return strokeParts.Count; } }

    System.DateTime updateTime;
    public System.DateTime UpdateTime { get { return updateTime; } }

    Matrix4x4[][] collectedBlockTransforms = null;
    PositionKey[][] collectedBlockKeys = null;
    uint[][] collectedBlockTimes = null;

    public DoAddStroke(DoType _type) : base(_type)
    {
        updateTime = System.DateTime.Now;
    }

    public void AppendPartialStroke(Matrix4x4[][] blockPositions, PositionKey[][] blockKeys, bool[][] blockMask, uint[][] blockTimes, uint timeStamp)
    {
        strokeParts.Add(new DoAddPartialStroke(blockPositions, blockKeys, blockMask, blockTimes, timeStamp));
        updateTime = System.DateTime.Now;
    }

    public override bool Undo()
    {
        if (strokeParts.Count == 0) return false;

        if (collectedBlockTransforms == null)
        {
            int bondTemplatePartNumber = strokeParts[0].blockPositions.Length;
            List<Matrix4x4>[] blockTransforms = new List<Matrix4x4>[bondTemplatePartNumber];
            List<PositionKey>[] blockKeys = new List<PositionKey>[bondTemplatePartNumber];
            List<uint>[] blockTimes = new List<uint>[bondTemplatePartNumber];

            foreach (DoAddPartialStroke strokePart in strokeParts)
            {
                for (int i = 0; i < bondTemplatePartNumber; ++i)
                {
                    if (blockTransforms[i] == null)
                    {
                        blockTransforms[i] = new List<Matrix4x4>();
                        blockKeys[i] = new List<PositionKey>();
                        blockTimes[i] = new List<uint>();
                    }

                    for (int j = 0; j < strokePart.blockPositions[i].Length; ++j)
                    {
                        if (strokePart.blockMask[i][j])
                        {
                            blockTransforms[i].Add(strokePart.blockPositions[i][j]);
                            blockKeys[i].Add(strokePart.blockKeys[i][j]);
                            blockTimes[i].Add(strokePart.blockTimes[i][j]);
                        }
                    }
                }
            }

            collectedBlockTransforms = new Matrix4x4[bondTemplatePartNumber][];
            collectedBlockKeys = new PositionKey[bondTemplatePartNumber][];
            collectedBlockTimes = new uint[bondTemplatePartNumber][];

            for (int i = 0; i < bondTemplatePartNumber; ++i)
            {
                collectedBlockTransforms[i] = blockTransforms[i].ToArray();
                collectedBlockKeys[i] = blockKeys[i].ToArray();
                collectedBlockTimes[i] = blockTimes[i].ToArray();
            }
        }

        BlockContainer.Container.RemoveBlocks(collectedBlockTransforms, collectedBlockKeys, true);

        return true;
    }

    public override bool Redo()
    {
        BlockContainer.Container.AddBlocks(collectedBlockTransforms, collectedBlockKeys, collectedBlockTimes, false, true);

        return true;
    }

    
}
