using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR;
using Valve.VR;

// Shows the VR user's view on the desktop window as a red/cyan anaglyph.
// It doesn't render the scene again: it reads the two eye images the headset already received (the OpenVR
// compositor's mirror textures) and composites them in one fullscreen pass, drawn by a camera that renders no geometry.
// Put it on any object in the scene and wire a button to Toggle() (or a UI Toggle to SetAnaglyph); give it the
// button's label (Text or TextMeshPro) and it reads "Enable Anaglyph" or "Disable Anaglyph" to match the state.
public class AnaglyphMirror : MonoBehaviour
{
    public enum AnaglyphMode { Dubois = 0, Color = 1, HalfColor = 2, Gray = 3 }

    [SerializeField, Tooltip("Dubois keeps the most colour with the least ghosting; Gray has no retinal rivalry")]
    AnaglyphMode mode = AnaglyphMode.Dubois;
    [SerializeField, Tooltip("Swap the eyes for cyan/red glasses")]
    bool swapEyes = false;
    [SerializeField, Tooltip("Vertical field of view shown on the screen in degrees; 0 fits the largest view both eyes cover")]
    float verticalFov = 0f;
    [SerializeField, Range(0.5f, 1f), Tooltip("With an automatic field of view, how much of the eye image to keep (the edges are masked by the lens)")]
    float autoFovCrop = 0.85f;
    [SerializeField, Tooltip("Distance in meters at which things lie in the screen plane: nearer pops out, farther sinks in")]
    float convergenceDistance = 2f;
    [SerializeField, Tooltip("Flip the eye images vertically, should they show upside down (with another graphics driver or headset)")]
    bool flipVertical = false;

    [Space]
    [SerializeField, Tooltip("The toggle button's label (Text or TextMeshPro); it names the action the button takes next")]
    Graphic buttonLabel;
    [SerializeField]
    string enableLabel = "Enable Anaglyph";
    [SerializeField]
    string disableLabel = "Disable Anaglyph";

    public AnaglyphMode Mode { get { return mode; } set { mode = value; } }
    public bool SwapEyes { get { return swapEyes; } set { swapEyes = value; } }
    public float ConvergenceDistance { get { return convergenceDistance; } set { convergenceDistance = Mathf.Max(0.1f, value); } }
    public float VerticalFov { get { return verticalFov; } set { verticalFov = Mathf.Clamp(value, 0f, 170f); } }
    public bool IsOn { get { return compositor != null; } }

    static readonly int LeftTexId = Shader.PropertyToID("_LeftTex");
    static readonly int RightTexId = Shader.PropertyToID("_RightTex");
    static readonly int LeftRectId = Shader.PropertyToID("_LeftRect");
    static readonly int RightRectId = Shader.PropertyToID("_RightRect");
    static readonly int ModeId = Shader.PropertyToID("_Mode");
    static readonly int FlipYId = Shader.PropertyToID("_FlipY");

    Compositor compositor;
    Material material;
    Texture2D deviceProbe;
    Texture2D leftTex, rightTex;
    IntPtr leftSrv = IntPtr.Zero, rightSrv = IntPtr.Zero;
    float nextAcquireTime;

    public void Toggle()
    {
        SetAnaglyph(!IsOn);
    }

    public void SetAnaglyph(bool state)
    {
        SwitchAnaglyph(state);
        UpdateLabel();
    }

    void UpdateLabel()
    {
        if (buttonLabel == null) return;
        string text = IsOn ? disableLabel : enableLabel;
        if (buttonLabel is Text uiText) uiText.text = text;
        else if (buttonLabel is TMP_Text tmpText) tmpText.text = text;
    }

    void SwitchAnaglyph(bool state)
    {
        if (state == IsOn) return;
        if (!state)
        {
            Stop();
            MessageSystem.DisplayMessage("Anaglyph view off");
            return;
        }

        string problem = CheckSupport();
        if (problem != null)
        {
            MessageSystem.DisplayWarning(problem);
            return;
        }

        if (material == null)
        {
            Shader shader = Shader.Find("Hidden/VRoxel/Anaglyph");
            if (shader == null)
            {
                MessageSystem.DisplayWarning("Anaglyph shader missing");
                return;
            }
            material = new Material(shader) { hideFlags = HideFlags.HideAndDontSave };
        }

        GameObject go = new GameObject("AnaglyphCompositor") { hideFlags = HideFlags.HideInHierarchy };
        DontDestroyOnLoad(go);
        Camera cam = go.AddComponent<Camera>();
        cam.cullingMask = 0;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = Color.black;
        cam.depth = 100;
        cam.stereoTargetEye = StereoTargetEyeMask.None;
        cam.targetDisplay = 0;
        cam.useOcclusionCulling = false;
        cam.allowHDR = false;
        cam.allowMSAA = false;
        cam.nearClipPlane = 0.01f;
        cam.farClipPlane = 0.02f;
        compositor = go.AddComponent<Compositor>();
        compositor.owner = this;

        nextAcquireTime = 0f;
        MessageSystem.DisplayMessage("Anaglyph view on");
    }

    static string CheckSupport()
    {
        if (!XRSettings.enabled || !XRSettings.isDeviceActive)
            return "Anaglyph view needs the headset running";
        if (SystemInfo.graphicsDeviceType != UnityEngine.Rendering.GraphicsDeviceType.Direct3D11)
            return "Anaglyph view needs Direct3D 11";
        if (OpenVR.Compositor == null || OpenVR.System == null)
            return "Anaglyph view needs SteamVR";
        return null;
    }

    void Start()
    {
        UpdateLabel();
    }

    void OnEnable()
    {
#if UNITY_EDITOR
        UnityEditor.AssemblyReloadEvents.beforeAssemblyReload += Stop;
#endif
    }

    void OnDisable()
    {
#if UNITY_EDITOR
        UnityEditor.AssemblyReloadEvents.beforeAssemblyReload -= Stop;
#endif
        Stop();
        UpdateLabel();
    }

    void OnDestroy()
    {
        if (material != null) Destroy(material);
        if (deviceProbe != null) Destroy(deviceProbe);
    }

    void Stop()
    {
        if (compositor != null) Destroy(compositor.gameObject);
        compositor = null;
        ReleaseMirrorTextures();
    }

    // The compositor publishes its copies of the submitted eye images as shader resource views; they stay valid
    // and are updated every frame, so they are fetched once and only again after a failure.
    bool AcquireMirrorTextures()
    {
        if (leftTex != null && rightTex != null) return true;
        if (Time.unscaledTime < nextAcquireTime) return false;
        nextAcquireTime = Time.unscaledTime + 1f;

        CVRCompositor vrCompositor = OpenVR.Compositor;
        if (vrCompositor == null) return false;
        if (deviceProbe == null)
            deviceProbe = new Texture2D(1, 1) { hideFlags = HideFlags.HideAndDontSave };
        // Any D3D11 resource identifies the device to OpenVR.
        IntPtr device = deviceProbe.GetNativeTexturePtr();

        ReleaseMirrorTextures();
        if (vrCompositor.GetMirrorTextureD3D11(EVREye.Eye_Left, device, ref leftSrv) != EVRCompositorError.None ||
            vrCompositor.GetMirrorTextureD3D11(EVREye.Eye_Right, device, ref rightSrv) != EVRCompositorError.None ||
            leftSrv == IntPtr.Zero || rightSrv == IntPtr.Zero)
        {
            ReleaseMirrorTextures();
            return false;
        }

        int w = Mathf.Max(1, XRSettings.eyeTextureWidth);
        int h = Mathf.Max(1, XRSettings.eyeTextureHeight);
        leftTex = Texture2D.CreateExternalTexture(w, h, TextureFormat.RGBA32, false, false, leftSrv);
        rightTex = Texture2D.CreateExternalTexture(w, h, TextureFormat.RGBA32, false, false, rightSrv);
        leftTex.wrapMode = rightTex.wrapMode = TextureWrapMode.Clamp;
        leftTex.filterMode = rightTex.filterMode = FilterMode.Bilinear;
        return true;
    }

    void ReleaseMirrorTextures()
    {
        if (leftTex != null) Destroy(leftTex);
        if (rightTex != null) Destroy(rightTex);
        leftTex = rightTex = null;
        CVRCompositor vrCompositor = OpenVR.Compositor;
        if (vrCompositor != null)
        {
            if (leftSrv != IntPtr.Zero) vrCompositor.ReleaseMirrorTextureD3D11(leftSrv);
            if (rightSrv != IntPtr.Zero) vrCompositor.ReleaseMirrorTextureD3D11(rightSrv);
        }
        leftSrv = rightSrv = IntPtr.Zero;
    }

    struct EyeFrustum
    {
        public float left, right, top, bottom; // tangents as OpenVR gives them: left and top negative
        public float offsetX;                  // eye position relative to the head centre, meters
    }

    static EyeFrustum GetFrustum(CVRSystem system, EVREye eye)
    {
        EyeFrustum f = new EyeFrustum();
        system.GetProjectionRaw(eye, ref f.left, ref f.right, ref f.top, ref f.bottom);
        f.offsetX = system.GetEyeToHeadTransform(eye).m3;
        return f;
    }

    // Crop of one eye image (uv offset xy, uv scale zw) for a view of half tangents hx, hy centred on the eye's
    // optical axis, shifted so that a point straight ahead at the convergence distance lands on the screen centre.
    Vector4 CropRect(EyeFrustum f, float hx, float hy)
    {
        float shift = -f.offsetX / convergenceDistance;
        float width = f.right - f.left;
        float height = f.bottom - f.top;
        return new Vector4((shift - hx - f.left) / width, (f.bottom - hy) / height, 2f * hx / width, 2f * hy / height);
    }

    void Composite(RenderTexture source, RenderTexture destination)
    {
        CVRSystem system = OpenVR.System;
        if (system == null || !AcquireMirrorTextures())
        {
            Graphics.Blit(Texture2D.blackTexture, destination);
            return;
        }

        EyeFrustum l = GetFrustum(system, EVREye.Eye_Left);
        EyeFrustum r = GetFrustum(system, EVREye.Eye_Right);
        float aspect = (float)Screen.width / Mathf.Max(1, Screen.height);

        float hy;
        if (verticalFov > 0f)
            hy = Mathf.Tan(0.5f * verticalFov * Mathf.Deg2Rad);
        else
        {
            float shift = Mathf.Abs(l.offsetX - r.offsetX) * 0.5f / convergenceDistance;
            float up = Mathf.Min(-l.top, -r.top);
            float down = Mathf.Min(l.bottom, r.bottom);
            float side = Mathf.Min(Mathf.Min(-l.left, l.right), Mathf.Min(-r.left, r.right)) - shift;
            hy = autoFovCrop * Mathf.Min(Mathf.Min(up, down), side / aspect);
        }
        float hx = hy * aspect;

        Texture leftEye = swapEyes ? rightTex : leftTex;
        Texture rightEye = swapEyes ? leftTex : rightTex;
        material.SetTexture(LeftTexId, leftEye);
        material.SetTexture(RightTexId, rightEye);
        material.SetVector(LeftRectId, CropRect(swapEyes ? r : l, hx, hy));
        material.SetVector(RightRectId, CropRect(swapEyes ? l : r, hx, hy));
        material.SetInt(ModeId, (int)mode);
        // The mirror textures hold the eye images top row first (as SteamVR expects them), the opposite of how
        // Unity samples a texture, so they are flipped unless the option asks otherwise.
        material.SetFloat(FlipYId, flipVertical ? 0f : 1f);
        Graphics.Blit(source, destination, material);
    }

    // Sits on the hidden geometry-free camera and replaces its (empty) image with the anaglyph.
    class Compositor : MonoBehaviour
    {
        public AnaglyphMirror owner;

        void OnRenderImage(RenderTexture source, RenderTexture destination)
        {
            if (owner == null) Graphics.Blit(source, destination);
            else owner.Composite(source, destination);
        }
    }
}
