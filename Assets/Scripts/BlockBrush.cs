using System.Collections.Generic;
using UnityEngine;

// Adds or removes blocks in a brush volume frame after frame while a trigger or key is held (VR and desktop spawners).
// Through Bond.GetBlocksInShapes a stroke only processes the volume the brush newly sweeps, and only blocks that change;
// the blocks go on the undo stack as before, closed by BlockContainer.FinishStroke.
public class BlockBrush
{
    readonly List<Bond.BrushShape> shapes = new List<Bond.BrushShape>(1);
    readonly List<Bond.BrushShape> previous = new List<Bond.BrushShape>(1);

    public void Add(Bond.BrushShape shape)
    {
        Brush(shape, true);
    }

    public void Remove(Bond.BrushShape shape)
    {
        Brush(shape, false);
    }

    // When the trigger or key is released, so the next stroke processes its whole first volume
    public void EndStroke()
    {
        previous.Clear();
    }

    void Brush(Bond.BrushShape shape, bool add)
    {
        shapes.Clear();
        shapes.Add(shape);

        Dictionary<PositionKey, PositionEntry> placed = BlockContainer.Container.BlockDictionary;
        PositionKey[][] blockDictKeys;
        Matrix4x4[][] blocks = GameManager.Bond.GetBlocksInShapes(shapes, add ? (key => !placed.ContainsKey(key)) : (System.Func<PositionKey, bool>)(key => placed.ContainsKey(key)), out blockDictKeys, previous);

        previous.Clear();
        previous.Add(shape);

        bool any = false;
        foreach (Matrix4x4[] part in blocks)
            any |= part.Length > 0;
        if (!any) return;

        if (add)
            BlockContainer.Container.AddBlocks(blocks, blockDictKeys);
        else
            BlockContainer.Container.RemoveBlocks(blocks, blockDictKeys);
    }
}
