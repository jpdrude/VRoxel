using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Newtonsoft.Json.Linq;
using UnityEngine;

// MCP commands for the scene around the avatars: its bounds, and saving and loading the design
public static class SceneCommands
{
    public static void Register(McpBridge bridge)
    {
        bridge.Register("get_assembly_bounds", GetAssemblyBounds);
        bridge.Register("save_scene", SaveScene);
        bridge.Register("load_scene", LoadScene);
        bridge.Register("list_saved_scenes", ListSavedScenes);
    }

    static readonly Regex validName = new Regex(@"^[\w\- ]{1,64}$");

    // Next to the saved animations (AnimationFileCommands.Folder), so a scene and its avatars can share a name
    public static string Folder
    {
        get { return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "VRoxel", "Scenes"); }
    }

    // A scene's file: by name in Folder, or a path
    static string ScenePath(JObject args, string command)
    {
        string name = (string)args["name"];
        string path = (string)args["path"];
        if (!string.IsNullOrEmpty(path))
        {
            path = Path.GetFullPath(Environment.ExpandEnvironmentVariables(path));
            if (!path.EndsWith(".vrxl", StringComparison.OrdinalIgnoreCase) && !path.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
                path += ".vrxl";
            return path;
        }
        if (!string.IsNullOrEmpty(name) && validName.IsMatch(name))
            return Path.Combine(Folder, name + ".vrxl");
        throw new McpException(command + " needs a name (letters, digits, spaces, - and _) or a path");
    }

    static void NeedAssembly(string what)
    {
        if (BlockContainer.Container == null || GameManager.Bond == null)
            throw new McpException("No block assembly in this scene to " + what + " yet; choose a building system in VRoxel first");
    }

    // The design as a .vrxl file, as the Esc menu's Save does: by name in Folder, or to a path. Written in the background
    static object SaveScene(JObject args)
    {
        NeedAssembly("save");
        string path = ScenePath(args, "save_scene");

        if (File.Exists(path) && !((bool?)args["overwrite"] ?? false))
            throw new McpException(path + " exists already; pass overwrite=true to replace it");

        Directory.CreateDirectory(Path.GetDirectoryName(path));
        bool inactive = (bool?)args["include_inactive"] ?? true;
        AssemblyIO.SaveAssemblyLayout(path, inactive);

        return new
        {
            path,
            blocks = BlockContainer.Container.BlockDictionary.Count,
            include_inactive = inactive,
            note = "written in the background; large designs take a few seconds"
        };
    }

    // A .vrxl (or exported .json) design into the running one, as the Esc menu's Load does. replace clears the current
    // design first (and with it the user's undo history), which also lets a file of another building system switch to
    // it; VRoxel switches only while the design has under 1000 blocks. Read in the background, the blocks follow a
    // moment later.
    static object LoadScene(JObject args)
    {
        NeedAssembly("load into");
        string path = ScenePath(args, "load_scene");
        if (!File.Exists(path))
            throw new McpException(path + " doesn't exist; list_saved_scenes shows the saved scenes");

        bool replace = (bool?)args["replace"] ?? true;
        int before = BlockContainer.Container.BlockDictionary.Count;
        if (replace && before > 0)
            GameManager.Manager.ClearAssembly();

        if ((bool?)args["as_point_cloud"] ?? false)
            AssemblyIO.LoadAssemblyAsPointCloud(path);
        else
            AssemblyIO.LoadAssemblyLayout(path);

        return new
        {
            path,
            replaced_blocks = replace ? before : 0,
            building_system = GameManager.Bond.BondName,
            note = "loading in the background; get_assembly_bounds shows the design once it's there. A file of another " +
                "building system replaces the current one only while the design has under 1000 blocks"
        };
    }

    static object ListSavedScenes(JObject args)
    {
        if (!Directory.Exists(Folder))
            return new object[0];

        return Directory.GetFiles(Folder, "*.vrxl").OrderBy(path => path).Select(path =>
        {
            FileInfo file = new FileInfo(path);
            return (object)new
            {
                name = Path.GetFileNameWithoutExtension(path),
                saved = file.LastWriteTime.ToString("yyyy-MM-dd HH:mm"),
                size_kb = file.Length / 1024
            };
        }).ToArray();
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
