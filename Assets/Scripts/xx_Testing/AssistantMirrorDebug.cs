using System.Linq;
using System.Reflection;
using UnityEngine;

// Test code: the bridge command debug_assistant_mirror reports what AssistantTerminalMirror and its StretchableFrame
// are doing (registered at runtime, not part of the MCP server)
public static class AssistantMirrorDebug
{
    const BindingFlags Private = BindingFlags.NonPublic | BindingFlags.Instance;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void RegisterCommands()
    {
        McpBridge bridge = Object.FindObjectOfType<McpBridge>();
        if (bridge == null) return;

        bridge.Register("debug_assistant_mirror", args =>
        {
            AssistantTerminalMirror[] mirrors = Resources.FindObjectsOfTypeAll<AssistantTerminalMirror>()
                .Where(m => m.gameObject.scene.IsValid()).ToArray();
            Camera camera = Camera.main;

            return new
            {
                steamvr_active = Valve.VR.SteamVR.active,
                installed = AssistantTerminal.Installed,
                mirror_in_vr = AssistantTerminal.Current.mirrorInVR,
                window_open = AssistantTerminal.IsOpen,
                camera = camera != null ? new { name = camera.name, position = V(camera.transform.position), forward = V(camera.transform.forward) } : null,
                mirrors = mirrors.Select(m => Describe(m, camera)).ToArray()
            };
        });
    }

    static object Describe(AssistantTerminalMirror mirror, Camera camera)
    {
        StretchableFrame frame = (StretchableFrame)typeof(AssistantTerminalMirror).GetField("stretchableFrame", Private).GetValue(mirror);
        GameObject preview = (GameObject)typeof(AssistantTerminalMirror).GetField("preview", Private).GetValue(mirror);
        Texture2D texture = (Texture2D)typeof(AssistantTerminalMirror).GetField("texture", Private).GetValue(mirror);
        Material material = (Material)typeof(AssistantTerminalMirror).GetField("previewMaterial", Private).GetValue(mirror);

        object previewInfo = null;
        if (preview != null)
        {
            Transform t = preview.transform;
            previewInfo = new
            {
                active = preview.activeInHierarchy,
                position = V(t.position),
                rotation = V(t.eulerAngles),
                scale = V(t.lossyScale),
                // Negative: the quad's visible side (-Z) faces the camera
                facing_camera = camera != null ? Vector3.Dot(t.forward, camera.transform.position - t.position) : 0,
                shader = material != null && material.shader != null ? material.shader.name : null,
                has_texture = material != null && material.mainTexture != null,
                layer = LayerMask.LayerToName(preview.layer),
                camera_sees_layer = camera != null && (camera.cullingMask & (1 << preview.layer)) != 0
            };
        }

        object textureInfo = null;
        if (texture != null)
        {
            Color[] samples = { texture.GetPixelBilinear(0.5f, 0.5f), texture.GetPixelBilinear(0.1f, 0.9f), texture.GetPixelBilinear(0.9f, 0.1f), texture.GetPixelBilinear(0.3f, 0.7f) };
            textureInfo = new
            {
                width = texture.width,
                height = texture.height,
                samples = samples.Select(c => new[] { R(c.r), R(c.g), R(c.b), R(c.a) }).ToArray()
            };
        }

        return new
        {
            name = mirror.name,
            active = mirror.isActiveAndEnabled,
            frame = frame != null ? new
            {
                name = frame.name,
                active = frame.gameObject.activeInHierarchy,
                has_sizes = frame.HasSizes,
                orientation = frame.HasSizes ? frame.Orientation : null,
                modelled_gap = new[] { R(frame.ModelledGap.x), R(frame.ModelledGap.y) },
                modelled_gap_world = new[] { R(frame.ModelledGapWorld.x), R(frame.ModelledGapWorld.y) },
                bottom_border_world = R(frame.BottomBorderWorld),
                position = V(frame.transform.position),
                rotation = V(frame.transform.eulerAngles),
                lossy_scale = V(frame.transform.lossyScale),
                parent = frame.transform.parent != null ? frame.transform.parent.name : null
            } : null,
            preview = previewInfo,
            texture = textureInfo
        };
    }

    static float R(float v)
    {
        return Mathf.Round(v * 10000) / 10000;
    }

    static float[] V(Vector3 v)
    {
        return new[] { R(v.x), R(v.y), R(v.z) };
    }
}
