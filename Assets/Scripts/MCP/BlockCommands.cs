using System.Linq;
using Newtonsoft.Json.Linq;
using UnityEngine;

// MCP commands that add or remove volumes of building blocks on the active bond's grid. The block count is estimated
// from the volume first; large volumes need the user's confirmation, as GPU instancing and undo get slow with millions.
// Like the VR spawner, the blocks are the user's design edits and go on the user's UndoStack, one entry per volume.
public static class BlockCommands
{
    // Above this the LLM must ask the user before generating; above the second it also advises against it
    const int ConfirmBlocks = 200000;
    const int DiscourageBlocks = 1000000;

    public static void Register(McpBridge bridge)
    {
        bridge.Register("create_block_volume", CreateBlockVolume);
    }

    static object CreateBlockVolume(JObject args)
    {
        Bond bond = GameManager.Bond;
        if (BlockContainer.Container == null || bond == null)
            throw new McpException("No block assembly in this scene");

        string shape = ((string)args["shape"] ?? "").Trim().ToLowerInvariant();
        string mode = ((string)args["mode"] ?? "add").Trim().ToLowerInvariant();
        if (mode != "add" && mode != "remove")
            throw new McpException("mode must be 'add' or 'remove'");

        float volume = Volume(shape, args);
        int estimate = Mathf.RoundToInt(volume * bond.BlocksPerCubicMeter);
        bool confirmed = (bool?)args["confirm"] ?? false;
        bool estimateOnly = (bool?)args["estimate_only"] ?? false;

        if (estimateOnly || (estimate > ConfirmBlocks && !confirmed))
        {
            return new
            {
                done = false,
                estimated_blocks = estimate,
                volume_m3 = AvatarCommands.Round(volume),
                needs_confirmation = estimate > ConfirmBlocks,
                advice = estimate > DiscourageBlocks
                    ? "Over a million blocks: VRoxel may become slow, and saving, loading and undo take long. Advise the user " +
                      "against it and suggest a smaller volume or a coarser bond; only with their explicit go-ahead call again with confirm=true."
                    : estimate > ConfirmBlocks
                        ? "Over 200,000 blocks: ask the user whether to go ahead, and call again with confirm=true only if they agree."
                        : null
            };
        }

        PositionKey[][] keys;
        Matrix4x4[][] blocks = Blocks(shape, args, bond, out keys);

        int changed;
        if (mode == "add")
        {
            bool[][] mask = BlockContainer.Container.AddBlocks(blocks, keys);
            changed = mask.Sum(part => part.Count(added => added));
            BlockContainer.Container.FinishStroke(PlacementModeType.Spawn);
        }
        else
        {
            bool[][] mask = BlockContainer.Container.RemoveBlocks(blocks, keys).blockMask;
            changed = mask.Sum(part => part.Count(removed => removed));
            BlockContainer.Container.FinishStroke(PlacementModeType.Destroy);
        }

        MessageSystem.DisplayMessage((mode == "add" ? "Added " : "Removed ") + changed.ToString("N0") + " blocks");

        return new
        {
            done = true,
            mode = mode,
            // Blocks in the volume, and how many of them were actually added (free cells) or removed (occupied ones)
            blocks_in_volume = blocks.Sum(part => part.Length),
            changed = changed,
            estimated_blocks = estimate
        };
    }

    static float Volume(string shape, JObject args)
    {
        switch (shape)
        {
            case "box":
                Vector3 size = Size(args);
                return size.x * size.y * size.z;
            case "sphere":
                float r = Radius(args);
                return 4f / 3 * Mathf.PI * r * r * r;
            case "cylinder":
                float cr = Radius(args);
                return Mathf.PI * cr * cr * Positive(args, "height");
            case "capsule":
                float pr = Radius(args);
                float length = Vector3.Distance(Point(args, "axis_start"), Point(args, "axis_end"));
                return Mathf.PI * pr * pr * length + 4f / 3 * Mathf.PI * pr * pr * pr;
            default:
                throw new McpException("shape must be 'box', 'sphere', 'cylinder' or 'capsule'");
        }
    }

    static Matrix4x4[][] Blocks(string shape, JObject args, Bond bond, out PositionKey[][] keys)
    {
        switch (shape)
        {
            case "box":
                Vector3 center = Point(args, "center");
                Vector3 half = Size(args) / 2;
                return bond.GetBlocksInBox(center - half, center + half, out keys);
            case "sphere":
                return bond.GetBlocksInSphere(Point(args, "center"), Radius(args), out keys);
            case "cylinder":
                return bond.GetBlocksInCylinder(Point(args, "center"), Radius(args), Positive(args, "height"), out keys);
            default:
                return bond.GetBlocksInCapsule(Point(args, "axis_start"), Point(args, "axis_end"), Radius(args), out keys);
        }
    }

    static Vector3 Point(JObject args, string name)
    {
        if (!(args[name] is JArray point) || point.Count != 3)
            throw new McpException(name + " must be [x, y, z]");
        return new Vector3((float)point[0], (float)point[1], (float)point[2]);
    }

    static Vector3 Size(JObject args)
    {
        Vector3 size = Point(args, "size");
        if (size.x <= 0 || size.y <= 0 || size.z <= 0)
            throw new McpException("size must be positive in x, y and z");
        return size;
    }

    static float Radius(JObject args)
    {
        return Positive(args, "radius");
    }

    static float Positive(JObject args, string name)
    {
        JToken token = args[name];
        if (token == null || token.Type == JTokenType.Null || (float)token <= 0)
            throw new McpException(name + " must be a positive number in meters");
        return (float)token;
    }
}
