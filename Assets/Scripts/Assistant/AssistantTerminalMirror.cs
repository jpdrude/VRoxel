using System;
using System.Runtime.InteropServices;
using System.Threading;
using UnityEngine;
using Valve.VR;

// Shows the assistant terminal's window in VR as a SteamVR overlay set into a StretchableFrame. Put it on the VR menu
// (it is then only active in VR) and place the frame where the mirror should be, e.g. above the menu. Everything else
// comes from the frame, read at start as it is in the scene (Read Sizes From Bounds): the mirror is as wide as the
// space between the frame's corners less framePadding on each side, lies in the frame's plane facing the way the
// frame faces (flipSide turns it to the other side), depthOffset in front of its centre piece, framePadding above the
// frame's bottom row. Its height follows the terminal window's shape; the frame keeps its width and is laid out every
// frame to reach framePadding above the mirror. The frame is shown and hidden with the mirror.
// The overlay is attached to the controller the frame is under, so SteamVR moves it with the latest controller pose.
// Without SteamVR (e.g. the player's fallback without a headset), desktopPreview shows the terminal on a quad in its
// place instead, to check the setup in the Game and Scene view.
// It is hidden while the terminal isn't open or is minimized. The window is captured on a background thread
// (mirrorFramesPerSecond in OptionalFeatures), scaled down to mirrorMaxWidth, and only handed to the main thread when
// enough of it changed (mirrorMinChangedTiles), as uploading it is what costs VRoxel time. The mirror only shows the
// terminal, typing happens on the keyboard.
public class AssistantTerminalMirror : MonoBehaviour
{
    [SerializeField]
    StretchableFrame stretchableFrame;
    [Tooltip("Space between the mirror and the frame, in meters: x to the corners on the left and right, y to the " +
        "frame's bottom and top rows")]
    [SerializeField]
    Vector2 framePadding = new Vector2(0.005f, 0.005f);
    [Tooltip("How far the mirror sits in front of the frame's centre piece (its origin plane without one), along its " +
        "depth axis, in meters")]
    [SerializeField]
    float depthOffset = 0.002f;
    [Tooltip("Shows the mirror on the frame's other side, facing the other way: if it appears behind or below the frame")]
    [SerializeField]
    bool flipSide = false;
    [Tooltip("Without SteamVR (no headset), shows the terminal on a quad where the mirror would be")]
    [SerializeField]
    bool desktopPreview = true;

    ulong handle = OpenVR.k_ulOverlayHandleInvalid;
    Texture2D texture;
    GameObject preview;
    Material previewMaterial;
    SteamVR_Behaviour_Pose pose;
    Transform poseOf;

    // Captured on the capture thread, uploaded on the main thread
    readonly object frameLock = new object();
    byte[] frame;
    int frameWidth;
    int frameHeight;
    bool frameReady;

    Thread captureThread;
    volatile bool capturing;
    IntPtr window;
    float nextWindowLookup;

    // The frame as it is in the scene gives the mirror's width, place and plane
    void Start()
    {
        if (stretchableFrame != null)
            stretchableFrame.ReadSizesFromBounds();
    }

    void OnDisable()
    {
        StopCapture();
        DestroyOverlay();
        ShowPreview(false);
        ShowFrame(false);
    }

    void OnDestroy()
    {
        if (texture != null)
            Destroy(texture);
        if (preview != null)
            Destroy(preview);
        if (previewMaterial != null)
            Destroy(previewMaterial);
    }

    void Update()
    {
        ShowFrame(UpdateMirror());
    }

    // True while the mirror shows
    bool UpdateMirror()
    {
        AssistantTerminal.Settings settings = AssistantTerminal.Current;
        // Without SteamVR (desktop mode, or VR without a headset) there is no overlay, only the preview
        CVROverlay overlay = SteamVR.active ? OpenVR.Overlay : null;
        if (!settings.installed || !settings.mirrorInVR || (overlay == null && !desktopPreview) || stretchableFrame == null || !stretchableFrame.HasSizes)
        {
            StopCapture();
            DestroyOverlay();
            ShowPreview(false);
            return false;
        }

        // Looking the window up by title is cheap, but not needed every frame
        if (Time.unscaledTime >= nextWindowLookup)
        {
            nextWindowLookup = Time.unscaledTime + 0.5f;
            lock (frameLock)
                window = AssistantTerminal.Window;
        }

        if (window == IntPtr.Zero || AssistantTerminal.IsIconic(window))
        {
            if (overlay != null && handle != OpenVR.k_ulOverlayHandleInvalid)
                overlay.HideOverlay(handle);
            ShowPreview(false);
            return false;
        }

        StartCapture();
        if (!UploadFrame() && texture == null)
            return false;

        float aspect = (float)texture.height / texture.width;
        if (overlay == null)
        {
            DestroyOverlay();
            PlacePreview(aspect);
            return true;
        }

        ShowPreview(false);
        if (handle == OpenVR.k_ulOverlayHandleInvalid && !CreateOverlay(overlay))
            return false;

        if (!PlaceOverlay(overlay, aspect))
            return false;

        overlay.ShowOverlay(handle);
        return true;
    }

    // The terminal on a quad where the overlay would be, laid out the same way; a Unity quad is seen from -Z, as the
    // mirror is
    void PlacePreview(float aspect)
    {
        if (preview == null)
        {
            preview = GameObject.CreatePrimitive(PrimitiveType.Quad);
            preview.name = "Assistant terminal preview";
            preview.hideFlags = HideFlags.DontSave;
            Destroy(preview.GetComponent<Collider>());

            Shader shader = Shader.Find("Unlit/Texture") ?? Shader.Find("Sprites/Default");
            previewMaterial = new Material(shader);
            preview.GetComponent<MeshRenderer>().sharedMaterial = previewMaterial;
        }

        SteamVR_Behaviour_Pose framePose = FramePose();
        float scale = framePose != null && Mathf.Abs(framePose.transform.lossyScale.x) > 1e-6f ? Mathf.Abs(framePose.transform.lossyScale.x) : 1;
        float width = MirrorWidthWorld(scale);
        float height = width * aspect;

        Quaternion rotation = MirrorRotation;
        preview.transform.SetPositionAndRotation(MirrorBottom(rotation, scale) + rotation * Vector3.up * (height / 2), rotation);
        preview.transform.localScale = new Vector3(width, height, 1);
        previewMaterial.mainTexture = texture;
        ShowPreview(true);

        LayOutFrame(height, scale);
    }

    void ShowPreview(bool show)
    {
        if (preview != null && preview.activeSelf != show)
            preview.SetActive(show);
    }

    // The frame keeps its width; its content is the mirror's height plus the padding below and above
    void LayOutFrame(float height, float scale)
    {
        stretchableFrame.SetContentSize(stretchableFrame.ModelledGapWorld.x, height + 2 * framePadding.y * scale);
    }

    void ShowFrame(bool show)
    {
        if (stretchableFrame != null && stretchableFrame.gameObject.activeSelf != show)
            stretchableFrame.gameObject.SetActive(show);
    }

    bool CreateOverlay(CVROverlay overlay)
    {
        string key = "vroxel.assistant." + GetInstanceID();
        EVROverlayError error = overlay.CreateOverlay(key, "VRoxel Assistant", ref handle);
        if (error != EVROverlayError.None)
        {
            Debug.LogWarning("Assistant terminal mirror: " + overlay.GetOverlayErrorNameFromEnum(error));
            handle = OpenVR.k_ulOverlayHandleInvalid;
            enabled = false;
            return false;
        }

        // Unity textures start at the bottom row
        VRTextureBounds_t bounds = new VRTextureBounds_t { uMin = 0, uMax = 1, vMin = 1, vMax = 0 };
        overlay.SetOverlayTextureBounds(handle, ref bounds);
        SetTexture(overlay);
        return true;
    }

    void DestroyOverlay()
    {
        if (handle == OpenVR.k_ulOverlayHandleInvalid)
            return;

        CVROverlay overlay = OpenVR.Overlay;
        if (overlay != null)
            overlay.DestroyOverlay(handle);
        handle = OpenVR.k_ulOverlayHandleInvalid;
    }

    void SetTexture(CVROverlay overlay)
    {
        if (handle == OpenVR.k_ulOverlayHandleInvalid || texture == null)
            return;

        Texture_t overlayTexture = new Texture_t
        {
            handle = texture.GetNativeTexturePtr(),
            eType = SteamVR.instance.textureType,
            eColorSpace = EColorSpace.Auto
        };
        overlay.SetOverlayTexture(handle, ref overlayTexture);
    }

    // The newest captured frame into the texture; false if there was none
    bool UploadFrame()
    {
        lock (frameLock)
        {
            if (!frameReady)
                return false;

            if (texture == null || texture.width != frameWidth || texture.height != frameHeight)
            {
                if (texture != null)
                    Destroy(texture);
                texture = new Texture2D(frameWidth, frameHeight, TextureFormat.BGRA32, false);
            }

            texture.LoadRawTextureData(frame);
            frameReady = false;
        }

        texture.Apply(false);
        if (handle != OpenVR.k_ulOverlayHandleInvalid)
            SetTexture(OpenVR.Overlay);
        return true;
    }

    // The mirror's orientation: the frame's, turned from its modelled plane into right along X, up along Y and seen
    // from -Z; flipped, it is seen from the frame's other side
    Quaternion MirrorRotation
    {
        get
        {
            Quaternion rotation = stretchableFrame.transform.rotation * Quaternion.Inverse(stretchableFrame.ContentRotation);
            return flipSide ? rotation * Quaternion.Euler(0, 180, 0) : rotation;
        }
    }

    // The mirror's bottom centre: above the frame's bottom row by the padding, depthOffset in front of the centre
    // piece's surface on the side it is seen from
    Vector3 MirrorBottom(Quaternion rotation, float scale)
    {
        Vector3 up = rotation * Vector3.up;
        Vector3 towardsViewer = rotation * Vector3.back;
        float surface = flipSide ? stretchableFrame.CentreBackWorld : stretchableFrame.CentreFrontWorld;
        return stretchableFrame.transform.position + up * (stretchableFrame.BottomBorderWorld + framePadding.y * scale) +
            towardsViewer * (surface + depthOffset * scale);
    }

    // In meters (world units): the space between the frame's corners less the padding; scale is the player's (the rig's)
    float MirrorWidthWorld(float scale)
    {
        return Mathf.Max(0.01f * scale, stretchableFrame.ModelledGapWorld.x - 2 * framePadding.x * scale);
    }

    // The controller the frame is under; looked up again when the frame changes parents (e.g. with the menu's hand).
    // Searched by hand, as the frame may be inactive
    SteamVR_Behaviour_Pose FramePose()
    {
        Transform parent = stretchableFrame.transform.parent;
        if (poseOf != parent)
        {
            poseOf = parent;
            pose = null;
            for (Transform t = parent; t != null && pose == null; t = t.parent)
                pose = t.GetComponent<SteamVR_Behaviour_Pose>();
        }
        return pose;
    }

    // Relative to the controller the frame is under if there is one, so SteamVR moves it with the controller, in real
    // meters, so it keeps its size when the player is scaled; otherwise placed in the tracking space every frame. The
    // frame is then laid out around it.
    bool PlaceOverlay(CVROverlay overlay, float aspect)
    {
        SteamVR_Behaviour_Pose framePose = FramePose();
        int device = framePose != null ? framePose.GetDeviceIndex() : -1;
        Transform reference = device >= 0 ? framePose.transform : SteamVR_Render.Top() != null ? SteamVR_Render.Top().origin : null;
        if (reference == null)
            return false;

        float scale = Mathf.Abs(reference.lossyScale.x) > 1e-6f ? Mathf.Abs(reference.lossyScale.x) : 1;
        float width = MirrorWidthWorld(scale);
        float height = width * aspect;
        overlay.SetOverlayWidthInMeters(handle, width / scale);

        Quaternion rotation = MirrorRotation;
        Vector3 center = MirrorBottom(rotation, scale) + rotation * Vector3.up * (height / 2);

        Quaternion inverse = Quaternion.Inverse(reference.rotation);
        SteamVR_Utils.RigidTransform offset = new SteamVR_Utils.RigidTransform(inverse * (center - reference.position) / scale, inverse * rotation);
        HmdMatrix34_t matrix = offset.ToHmdMatrix34();

        if (device >= 0)
            overlay.SetOverlayTransformTrackedDeviceRelative(handle, (uint)device, ref matrix);
        else
            overlay.SetOverlayTransformAbsolute(handle, SteamVR.settings.trackingSpace, ref matrix);

        LayOutFrame(height, scale);
        return true;
    }

    void StartCapture()
    {
        if (captureThread != null)
            return;

        capturing = true;
        AssistantTerminal.Settings settings = AssistantTerminal.Current;
        float fps = Mathf.Clamp(settings.mirrorFramesPerSecond, 1, 60);
        int maxWidth = Mathf.Max(64, settings.mirrorMaxWidth);
        int minChangedTiles = Mathf.Max(1, settings.mirrorMinChangedTiles);
        float refreshSeconds = Mathf.Max(0.1f, settings.mirrorRefreshSeconds);
        captureThread = new Thread(() => CaptureLoop(fps, maxWidth, minChangedTiles, refreshSeconds)) { IsBackground = true, Name = "Assistant terminal capture" };
        captureThread.Start();
    }

    void StopCapture()
    {
        if (captureThread == null)
            return;

        capturing = false;
        captureThread.Join(1000);
        captureThread = null;
    }

    void CaptureLoop(float fps, int maxWidth, int minChangedTiles, float refreshSeconds)
    {
        WindowCapture capture = new WindowCapture(maxWidth);
        System.Diagnostics.Stopwatch sincePublished = System.Diagnostics.Stopwatch.StartNew();
        int[] publishedTiles = null;
        int publishedWidth = 0, publishedHeight = 0;
        try
        {
            int interval = Mathf.RoundToInt(1000 / fps);
            while (capturing)
            {
                IntPtr target;
                lock (frameLock)
                    target = window;

                byte[] pixels;
                int width, height;
                if (target != IntPtr.Zero && capture.Capture(target, out pixels, out width, out height))
                {
                    int[] tiles = TileHashes(pixels, width, height);
                    bool resized = width != publishedWidth || height != publishedHeight;
                    if (!resized && publishedTiles != null && sincePublished.Elapsed.TotalSeconds < refreshSeconds &&
                        ChangedTiles(tiles, publishedTiles) < minChangedTiles)
                    {
                        // Too little changed to be worth an upload
                        capture.Recycle(pixels);
                        Thread.Sleep(interval);
                        continue;
                    }

                    publishedTiles = tiles;
                    publishedWidth = width;
                    publishedHeight = height;
                    sincePublished.Reset();
                    sincePublished.Start();

                    lock (frameLock)
                    {
                        // Swap buffers, the capture writes into the one the main thread has finished with
                        capture.Recycle(frame);
                        frame = pixels;
                        frameWidth = width;
                        frameHeight = height;
                        frameReady = true;
                    }
                }

                Thread.Sleep(interval);
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning("Assistant terminal mirror: capture stopped, " + e.Message);
        }
        finally
        {
            capture.Dispose();
        }
    }

    const int Tile = 16;

    // A hash of each tile of 16 x 16 pixels, row by row, of BGRA pixels
    static int[] TileHashes(byte[] pixels, int width, int height)
    {
        int columns = (width + Tile - 1) / Tile;
        int rows = (height + Tile - 1) / Tile;
        int[] hashes = new int[columns * rows];
        for (int y = 0; y < height; ++y)
        {
            int rowTiles = (y / Tile) * columns;
            int index = y * width * 4;
            for (int x = 0; x < width; ++x, index += 4)
            {
                int tile = rowTiles + x / Tile;
                hashes[tile] = hashes[tile] * 31 + (pixels[index] | pixels[index + 1] << 8 | pixels[index + 2] << 16);
            }
        }
        return hashes;
    }

    static int ChangedTiles(int[] a, int[] b)
    {
        int changed = 0;
        for (int i = 0; i < a.Length; ++i)
            if (a[i] != b[i])
                ++changed;
        return changed;
    }

    // Draws where the mirror goes in the frame, for a 16:10 window
    void OnDrawGizmosSelected()
    {
        if (stretchableFrame == null || !stretchableFrame.HasSizes)
            return;

        SteamVR_Behaviour_Pose parentPose = stretchableFrame.GetComponentInParent<SteamVR_Behaviour_Pose>();
        float scale = parentPose != null ? Mathf.Abs(parentPose.transform.lossyScale.x) : 1;
        float width = MirrorWidthWorld(scale);

        Quaternion rotation = MirrorRotation;
        Vector3 right = rotation * Vector3.right * (width / 2);
        Vector3 up = rotation * Vector3.up * (width * 10 / 16);
        Vector3 bottom = MirrorBottom(rotation, scale);

        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(bottom - right, bottom + right);
        Gizmos.DrawLine(bottom + right, bottom + right + up);
        Gizmos.DrawLine(bottom + right + up, bottom - right + up);
        Gizmos.DrawLine(bottom - right + up, bottom - right);
        // Points away from the viewer
        Gizmos.DrawLine(bottom + up / 2, bottom + up / 2 + rotation * Vector3.forward * (0.05f * scale));
    }

    // Copies a window's client area into BGRA pixels, bottom row first, as Texture2D.LoadRawTextureData takes them,
    // scaled down to at most maxWidth. PrintWindow renders the window even when it is covered; for windows it returns
    // black for, the screen is copied instead, which works while the window is on top (AssistantTerminal keeps it there).
    class WindowCapture : IDisposable
    {
        readonly int maxWidth;
        IntPtr screenDC;
        IntPtr memoryDC;
        IntPtr bitmap;
        IntPtr previousBitmap;
        IntPtr bits;
        int width;
        int height;
        // The scaled copy, if the window is wider than maxWidth
        IntPtr scaledDC;
        IntPtr scaledBitmap;
        IntPtr previousScaledBitmap;
        IntPtr scaledBits;
        int scaledWidth;
        int scaledHeight;
        bool copyScreen;
        byte[] spare;

        public WindowCapture(int maxWidth)
        {
            this.maxWidth = maxWidth;
        }

        public bool Capture(IntPtr window, out byte[] pixels, out int captureWidth, out int captureHeight)
        {
            pixels = null;
            captureWidth = captureHeight = 0;

            RECT client;
            if (IsIconic(window) || !GetClientRect(window, out client) || client.right <= 0 || client.bottom <= 0)
                return false;

            if (client.right != width || client.bottom != height || memoryDC == IntPtr.Zero)
                Allocate(client.right, client.bottom);

            bool captured = false;
            if (!copyScreen)
            {
                captured = PrintWindow(window, memoryDC, PW_CLIENTONLY | PW_RENDERFULLCONTENT);
                GdiFlush();
                if (captured && IsBlack())
                {
                    copyScreen = true;
                    captured = false;
                }
            }
            if (!captured)
            {
                POINT origin = new POINT();
                ClientToScreen(window, ref origin);
                captured = BitBlt(memoryDC, 0, 0, width, height, screenDC, origin.x, origin.y, SRCCOPY);
                GdiFlush();
            }
            if (!captured)
                return false;

            IntPtr source = bits;
            int outWidth = width, outHeight = height;
            if (scaledDC != IntPtr.Zero)
            {
                // Halftone averages the pixels, so text stays readable
                SetStretchBltMode(scaledDC, HALFTONE);
                SetBrushOrgEx(scaledDC, 0, 0, IntPtr.Zero);
                StretchBlt(scaledDC, 0, 0, scaledWidth, scaledHeight, memoryDC, 0, 0, width, height, SRCCOPY);
                GdiFlush();
                source = scaledBits;
                outWidth = scaledWidth;
                outHeight = scaledHeight;
            }

            int length = outWidth * outHeight * 4;
            pixels = spare != null && spare.Length == length ? spare : new byte[length];
            spare = null;
            Marshal.Copy(source, pixels, 0, length);

            // GDI leaves alpha at 0, the overlay would be transparent
            for (int i = 3; i < length; i += 4)
                pixels[i] = 255;

            captureWidth = outWidth;
            captureHeight = outHeight;
            return true;
        }

        // A buffer the main thread is done with, reused for the next capture
        public void Recycle(byte[] buffer)
        {
            spare = buffer;
        }

        // A sample of pixels, all black: what PrintWindow returns for windows it can't render
        bool IsBlack()
        {
            int pixels = width * height;
            for (int i = 0; i < 256; ++i)
            {
                int offset = (int)((long)pixels * i / 256) * 4;
                if ((Marshal.ReadInt32(bits, offset) & 0xFFFFFF) != 0)
                    return false;
            }
            return true;
        }

        void Allocate(int newWidth, int newHeight)
        {
            Release();

            width = newWidth;
            height = newHeight;
            screenDC = GetDC(IntPtr.Zero);
            memoryDC = CreateCompatibleDC(screenDC);

            // A positive height makes a bottom-up bitmap
            BITMAPINFOHEADER header = new BITMAPINFOHEADER
            {
                biSize = Marshal.SizeOf(typeof(BITMAPINFOHEADER)),
                biWidth = width,
                biHeight = height,
                biPlanes = 1,
                biBitCount = 32
            };
            bitmap = CreateDIBSection(screenDC, ref header, 0, out bits, IntPtr.Zero, 0);
            previousBitmap = SelectObject(memoryDC, bitmap);

            if (width > maxWidth)
            {
                scaledWidth = maxWidth;
                scaledHeight = Mathf.Max(1, Mathf.RoundToInt((float)height * maxWidth / width));
                scaledDC = CreateCompatibleDC(screenDC);
                header.biWidth = scaledWidth;
                header.biHeight = scaledHeight;
                scaledBitmap = CreateDIBSection(screenDC, ref header, 0, out scaledBits, IntPtr.Zero, 0);
                previousScaledBitmap = SelectObject(scaledDC, scaledBitmap);
            }
        }

        void Release()
        {
            if (scaledDC != IntPtr.Zero)
            {
                SelectObject(scaledDC, previousScaledBitmap);
                DeleteDC(scaledDC);
                scaledDC = IntPtr.Zero;
            }
            if (scaledBitmap != IntPtr.Zero)
            {
                DeleteObject(scaledBitmap);
                scaledBitmap = IntPtr.Zero;
            }
            if (memoryDC != IntPtr.Zero)
            {
                SelectObject(memoryDC, previousBitmap);
                DeleteDC(memoryDC);
                memoryDC = IntPtr.Zero;
            }
            if (bitmap != IntPtr.Zero)
            {
                DeleteObject(bitmap);
                bitmap = IntPtr.Zero;
            }
            if (screenDC != IntPtr.Zero)
            {
                ReleaseDC(IntPtr.Zero, screenDC);
                screenDC = IntPtr.Zero;
            }
        }

        public void Dispose()
        {
            Release();
        }

        [StructLayout(LayoutKind.Sequential)]
        struct RECT { public int left, top, right, bottom; }

        [StructLayout(LayoutKind.Sequential)]
        struct POINT { public int x, y; }

        [StructLayout(LayoutKind.Sequential)]
        struct BITMAPINFOHEADER
        {
            public int biSize;
            public int biWidth;
            public int biHeight;
            public short biPlanes;
            public short biBitCount;
            public int biCompression;
            public int biSizeImage;
            public int biXPelsPerMeter;
            public int biYPelsPerMeter;
            public int biClrUsed;
            public int biClrImportant;
        }

        const uint PW_CLIENTONLY = 0x1;
        const uint PW_RENDERFULLCONTENT = 0x2;
        const int SRCCOPY = 0x00CC0020;

        [DllImport("user32.dll")] static extern bool IsIconic(IntPtr window);
        [DllImport("user32.dll")] static extern bool GetClientRect(IntPtr window, out RECT rect);
        [DllImport("user32.dll")] static extern bool ClientToScreen(IntPtr window, ref POINT point);
        [DllImport("user32.dll")] static extern bool PrintWindow(IntPtr window, IntPtr dc, uint flags);
        [DllImport("user32.dll")] static extern IntPtr GetDC(IntPtr window);
        [DllImport("user32.dll")] static extern int ReleaseDC(IntPtr window, IntPtr dc);
        [DllImport("gdi32.dll")] static extern IntPtr CreateCompatibleDC(IntPtr dc);
        [DllImport("gdi32.dll")] static extern bool DeleteDC(IntPtr dc);
        [DllImport("gdi32.dll")] static extern IntPtr CreateDIBSection(IntPtr dc, ref BITMAPINFOHEADER header, uint usage, out IntPtr bits, IntPtr section, uint offset);
        [DllImport("gdi32.dll")] static extern IntPtr SelectObject(IntPtr dc, IntPtr gdiObject);
        [DllImport("gdi32.dll")] static extern bool DeleteObject(IntPtr gdiObject);
        [DllImport("gdi32.dll")] static extern bool BitBlt(IntPtr destination, int x, int y, int width, int height, IntPtr source, int sourceX, int sourceY, int operation);
        [DllImport("gdi32.dll")] static extern bool GdiFlush();
        [DllImport("gdi32.dll")] static extern bool StretchBlt(IntPtr destination, int x, int y, int width, int height, IntPtr source, int sourceX, int sourceY, int sourceWidth, int sourceHeight, int operation);
        [DllImport("gdi32.dll")] static extern int SetStretchBltMode(IntPtr dc, int mode);
        [DllImport("gdi32.dll")] static extern bool SetBrushOrgEx(IntPtr dc, int x, int y, IntPtr previous);
        const int HALFTONE = 4;
    }
}
