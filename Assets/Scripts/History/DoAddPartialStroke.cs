using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DoAddPartialStroke
{
    public Matrix4x4[][] blockPositions;
    public PositionKey[][] blockKeys;
    public bool[][] blockMask;
    public uint[][] blockTimes;


    public DoAddPartialStroke(Matrix4x4[][] _blockPositions, PositionKey[][] _blockKeys, bool[][] _blockMask, uint[][] _blockTimes, uint timeStamp)
    {
        blockPositions = _blockPositions;
        blockKeys = _blockKeys;
        blockMask = _blockMask;
        blockTimes = _blockTimes;

        if (blockKeys == null)
        {
            blockKeys = new PositionKey[blockPositions.Length][];
            for (int i = 0; i < blockKeys.Length; ++i)
            {
                blockKeys[i] = new PositionKey[blockPositions[i].Length];

                for (int j = 0; j < blockKeys[i].Length; ++j)
                {
                    blockKeys[i][j] = new PositionKey(blockPositions[i][j]);
                }
            }
        }

        if (blockTimes == null)
        {
            blockTimes = new uint[blockPositions.Length][];
            for (int i = 0; i < blockTimes.Length; ++i)
            {
                blockTimes[i] = new uint[blockPositions[i].Length];

                for (int j = 0; j < blockTimes[i].Length; ++j)
                {
                    blockTimes[i][j] = timeStamp;
                }
            }
        }

        if (blockMask == null)
        {
            blockMask = new bool[blockPositions.Length][];
            for (int i = 0; i < blockMask.Length; ++i)
            {
                blockMask[i] = new bool[blockPositions[i].Length];

                for (int j = 0; j < blockMask[i].Length; ++j)
                {
                    blockMask[i][j] = true;
                }
            }
        }
    }
}
