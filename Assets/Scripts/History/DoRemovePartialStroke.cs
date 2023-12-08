using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DoRemovePartialStroke
{
    public Matrix4x4[][] blockPositions;
    public PositionKey[][] blockKeys;
    public bool[][] blockMask;
    public uint[][] blockTimes;


    public DoRemovePartialStroke(Matrix4x4[][] _blockPositions, PositionKey[][] _blockKeys, bool[][] _blockMask, uint[][] _blockTimes)
    {
        this.blockPositions = _blockPositions;
        blockKeys = _blockKeys;
        blockMask = _blockMask;
        blockTimes = _blockTimes;
    }
}
