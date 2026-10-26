using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DoAction
{
    DoType type;
    public DoType Type { get { return type; } }

    public DoAction(DoType _type)
    {
        type = _type;
    }

    public virtual bool Undo() { return false; }

    public virtual bool Redo() { return false; }

    public enum DoType
    {
        AddStrocke,
        RemoveStroke,
        ToPointCloud,
        ToBlocks,
        LoadAsPointCloud,
        ChangeObjectPos,
        ChangeAnimationPos,
        SpawnAvatar,
        RemoveAvatar,
        SetSequence
    }
}
