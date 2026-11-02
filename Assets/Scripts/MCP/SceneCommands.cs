using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using UnityEngine;

// MCP commands that describe the scene around the avatars
public static class SceneCommands
{
    public static void Register(McpBridge bridge)
    {
        bridge.Register("get_assembly_bounds", GetAssemblyBounds);
    }

    // Axis-aligned bounds of all placed blocks in world space. Grid positions are block centers,
    // so the box is padded by half a grid spacing; parts larger than their grid cell can stick out slightly.
    static object GetAssemblyBounds(JObject args)
    {
        BlockContainer container = BlockContainer.Container;
        Bond bond = GameManager.Bond;
        if (container == null || bond == null)
            throw new McpException("No block assembly in this scene");

        Dictionary<PositionKey, PositionEntry> blocks = container.BlockDictionary;
        if (blocks == null || blocks.Count == 0)
            return new { block_count = 0 };

        Vector3Int min = new Vector3Int(int.MaxValue, int.MaxValue, int.MaxValue);
        Vector3Int max = new Vector3Int(int.MinValue, int.MinValue, int.MinValue);
        foreach (PositionKey key in blocks.Keys)
        {
            min = Vector3Int.Min(min, new Vector3Int(key.x, key.y, key.z));
            max = Vector3Int.Max(max, new Vector3Int(key.x, key.y, key.z));
        }

        float res = bond.ContainerRes;
        Vector3 halfCell = new Vector3(bond.XSpacing, bond.YSpacing, bond.ZSpacing) / 2;
        Vector3 worldMin = (Vector3)min / res + bond.Offset - halfCell;
        Vector3 worldMax = (Vector3)max / res + bond.Offset + halfCell;

        return new
        {
            block_count = blocks.Count,
            min = ToArray(worldMin),
            max = ToArray(worldMax),
            center = ToArray((worldMin + worldMax) / 2),
            size = ToArray(worldMax - worldMin),
            grid_spacing = ToArray(halfCell * 2)
        };
    }

    static float[] ToArray(Vector3 v)
    {
        return new[] { Round(v.x), Round(v.y), Round(v.z) };
    }

    static float Round(float value)
    {
        return Mathf.Round(value * 1000) / 1000;
    }
}
