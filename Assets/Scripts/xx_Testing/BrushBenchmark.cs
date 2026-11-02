using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using GPUInstancer;
using Newtonsoft.Json.Linq;
using UnityEngine;

// Times character brushing per frame: "old" one query and add / remove per collider, "new" all colliders in one
// Bond.GetBlocksInShapes (Burst), "swept" that plus skipping what the previous frame covered.
// Test code: the bridge commands benchmark_brush / benchmark_brush_stats are registered at runtime, not part of the
// MCP server. Edits made while benchmarking skip the undo stack.
public class BrushBenchmark : MonoBehaviour
{
    public string method = "new";
    public bool add;
    public float size = 1;

    int frames;
    double totalMs;
    double maxMs;
    int changed;

    SphereCollider[] spheres;
    CapsuleCollider[] capsules;
    MixamoAnimationPlayer player;
    readonly List<Bond.BrushShape> shapes = new List<Bond.BrushShape>();
    readonly List<Bond.BrushShape> previous = new List<Bond.BrushShape>();

    static FieldInfo managerField = typeof(BlockContainer).GetField("manager", BindingFlags.NonPublic | BindingFlags.Instance);
    static FieldInfo partsField = typeof(BlockContainer).GetField("parts", BindingFlags.NonPublic | BindingFlags.Instance);
    static FieldInfo transformsField = typeof(BlockContainer).GetField("transformArray", BindingFlags.NonPublic | BindingFlags.Instance);

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void RegisterCommands()
    {
        McpBridge bridge = Object.FindObjectOfType<McpBridge>();
        if (bridge == null) return;

        bridge.Register("benchmark_brush", args =>
        {
            GameObject avatar = AvatarRegistry.Get(AvatarRegistry.Resolve((string)args["avatar_id"]));
            BrushBenchmark bench = avatar.GetComponent<BrushBenchmark>() ?? avatar.AddComponent<BrushBenchmark>();
            string mode = (string)args["mode"] ?? "off";
            bench.enabled = mode != "off";
            bench.add = mode == "add";
            bench.method = (string)args["method"] ?? "new";
            bench.size = (float?)args["size"] ?? 1;
            bench.ResetStats();
            return new { mode, bench.method, bench.size };
        });
        bridge.Register("benchmark_brush_stats", args =>
        {
            BrushBenchmark bench = AvatarRegistry.Get(AvatarRegistry.Resolve((string)args["avatar_id"])).GetComponent<BrushBenchmark>();
            return new
            {
                bench.method,
                bench.size,
                bench.frames,
                mean_ms = bench.frames > 0 ? bench.totalMs / bench.frames : 0,
                max_ms = bench.maxMs,
                bench.changed
            };
        });
    }

    void ResetStats()
    {
        frames = 0;
        previous.Clear();
        totalMs = 0;
        maxMs = 0;
        changed = 0;
    }

    void Start()
    {
        spheres = GetComponentsInChildren<SphereCollider>();
        capsules = GetComponentsInChildren<CapsuleCollider>();
        player = GetComponent<MixamoAnimationPlayer>();
    }

    void LateUpdate()
    {
        if (player != null && !player.Posed) return;

        Stopwatch watch = Stopwatch.StartNew();
        int count = method == "old" ? BrushOld() : BrushNew();
        watch.Stop();

        frames++;
        totalMs += watch.Elapsed.TotalMilliseconds;
        maxMs = System.Math.Max(maxMs, watch.Elapsed.TotalMilliseconds);
        changed += count;
    }

    // As CharacterSpawner did before: one query and one add / remove per collider, each uploading all blocks
    int BrushOld()
    {
        int count = 0;
        foreach (SphereCollider sc in spheres)
        {
            Bond.BrushShape s = Sphere(sc);
            PositionKey[][] keys;
            Matrix4x4[][] blocks = GameManager.Bond.GetBlocksInSphere(s.a, s.radius, out keys);
            count += Apply(blocks, keys);
            UploadAll();
        }
        foreach (CapsuleCollider cc in capsules)
        {
            Bond.BrushShape s = Capsule(cc);
            PositionKey[][] keys;
            Matrix4x4[][] blocks = GameManager.Bond.GetBlocksInCapsule(s.a, s.b, s.radius, out keys);
            count += Apply(blocks, keys);
            UploadAll();
        }
        return count;
    }

    int BrushNew()
    {
        shapes.Clear();
        foreach (SphereCollider sc in spheres)
            shapes.Add(Sphere(sc));
        foreach (CapsuleCollider cc in capsules)
            shapes.Add(Capsule(cc));

        Dictionary<PositionKey, PositionEntry> placed = BlockContainer.Container.BlockDictionary;
        PositionKey[][] keys;
        Matrix4x4[][] blocks = GameManager.Bond.GetBlocksInShapes(shapes, add ? (key => !placed.ContainsKey(key)) : (System.Func<PositionKey, bool>)(key => placed.ContainsKey(key)), out keys,
            method == "swept" ? previous : null);
        previous.Clear();
        previous.AddRange(shapes);
        return blocks.All(part => part.Length == 0) ? 0 : Apply(blocks, keys);
    }

    int Apply(Matrix4x4[][] blocks, PositionKey[][] keys)
    {
        bool[][] mask = add ? BlockContainer.Container.AddBlocks(blocks, keys, null, false, true) : BlockContainer.Container.RemoveBlocks(blocks, keys, true).blockMask;
        return mask.Sum(part => part.Count(done => done));
    }

    // The old BlockContainer uploaded every block type's transforms on every add and remove, changed or not
    void UploadAll()
    {
        BlockContainer container = BlockContainer.Container;
        GPUInstancerPrefabManager manager = (GPUInstancerPrefabManager)managerField.GetValue(container);
        GPUInstancerPrefab[] parts = (GPUInstancerPrefab[])partsField.GetValue(container);
        Matrix4x4[][] transforms = (Matrix4x4[][])transformsField.GetValue(container);
        for (int i = 0; i < parts.Length; ++i)
            GPUInstancerAPI.UpdateVisibilityBufferWithMatrix4x4Array(manager, parts[i].prefabPrototype, transforms[i]);
    }

    // The same shapes as CharacterSpawner builds from the colliders
    Bond.BrushShape Sphere(SphereCollider sc)
    {
        Vector3 scale = sc.transform.lossyScale;
        float maxScale = Mathf.Max(Mathf.Abs(scale.x), Mathf.Abs(scale.y), Mathf.Abs(scale.z));
        return Bond.BrushShape.Sphere(sc.transform.TransformPoint(sc.center), sc.radius * maxScale * size);
    }

    Bond.BrushShape Capsule(CapsuleCollider cc)
    {
        Vector3 direction = cc.direction == 0 ? Vector3.right : cc.direction == 1 ? Vector3.up : Vector3.forward;
        Vector3 a = cc.transform.TransformPoint(cc.center - direction * (cc.height / 2) + direction * cc.radius);
        Vector3 b = cc.transform.TransformPoint(cc.center + direction * (cc.height / 2) - direction * cc.radius);
        Vector3 scale = cc.transform.lossyScale;
        float across = cc.direction == 0 ? Mathf.Max(Mathf.Abs(scale.y), Mathf.Abs(scale.z))
            : cc.direction == 1 ? Mathf.Max(Mathf.Abs(scale.x), Mathf.Abs(scale.z))
            : Mathf.Max(Mathf.Abs(scale.x), Mathf.Abs(scale.y));
        return Bond.BrushShape.Capsule(a, b, cc.radius * across * size);
    }
}
