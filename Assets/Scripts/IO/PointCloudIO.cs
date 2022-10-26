using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PointCloudRuntimeViewer;
using unitycodercom_PointCloudBinaryViewer;

public class PointCloudIO : MonoBehaviour
{
    [SerializeField]
    RuntimeViewerDX11 _runtimeViewer;
    static RuntimeViewerDX11 runtimeViewer;
    public static RuntimeViewerDX11 RuntimeViewer { get { return runtimeViewer; } }

    [SerializeField]
    PointCloudViewerDX11 _pcViewer;
    static PointCloudViewerDX11 pcViewer;
    public static PointCloudViewerDX11 PcViewer { get { return pcViewer; } }

    [SerializeField]
    Material defaultMaterial;

    static bool viewerInitialized = false;
    static bool useRuntime = false;

    static string lastCloudPath = null;


    static Material cloudMaterial = null;
    public static Material CloudMaterial { get { return cloudMaterial; } set{ cloudMaterial = value; } }

    static PointCloudFormat cloudFormat = PointCloudFormat.XYZRGB;
    public static PointCloudFormat CloudFormat { get { return cloudFormat; } set { cloudFormat = value; } }

    static bool readRGB = true;
    public static bool ReadRGB { get { return readRGB; } set { readRGB = value; } }

    static bool overrideCache = false;
    public static bool OverrideCache { get { return overrideCache; } set { overrideCache = value; } }


    private void Awake()
    {
        runtimeViewer = _runtimeViewer;
        pcViewer = _pcViewer;
        cloudMaterial = defaultMaterial;
    }

    public static void ImportPointCloud(string path)
    {
        ResetRuntimeViewer();
        ResetPointCloudViewer();

        runtimeViewer.CallImporterThreaded(path);
        runtimeViewer.displayPoints = true;
        viewerInitialized = false;
        useRuntime = true;
        lastCloudPath = path + ".bin";
    }

    static void ResetRuntimeViewer()
    {
        var viewerGo = runtimeViewer.gameObject;
        DestroyImmediate(runtimeViewer);

        runtimeViewer = viewerGo.AddComponent<RuntimeViewerDX11>();
        runtimeViewer.cloudMaterial = cloudMaterial;
        runtimeViewer.pointCloudFormat = cloudFormat;
        runtimeViewer.readRGB = readRGB;
        runtimeViewer.overrideExistingCacheFile = overrideCache;
        runtimeViewer.unitScale = 1;
        runtimeViewer.autoOffsetNearZero = false;
        runtimeViewer.cacheBinFile = true;
        runtimeViewer.fullPath = "";
    }

    public static void DisplayPointCloud(string path)
    {
        ResetPointCloudViewer();
        ResetRuntimeViewer();

        pcViewer.CallReadPointCloudThreaded(path);
        useRuntime = false;
        viewerInitialized = true;
        lastCloudPath = path;
    }

    static void ResetPointCloudViewer()
    {
        var viewerGo = pcViewer.gameObject;
        DestroyImmediate(pcViewer);

        pcViewer = viewerGo.AddComponent<PointCloudViewerDX11>();
        pcViewer.fileName = "";
        pcViewer.loadAtStart = false;
        pcViewer.cloudMaterial = CloudMaterial;
        pcViewer.displayPoints = true;
    }

    public static void ModulesToCloud(Matrix4x4[][] transforms)
    {
        int transformLength = 0;
        for (int i = 0; i < transforms.Length; ++i)
            transformLength += transforms[i].Length;

        Vector3[] points = new Vector3[transformLength];
        Vector3[] colors = new Vector3[transformLength];

        int counter = 0;
        for (int i = 0; i < transforms.Length; ++i)
        {
            for (int j = 0; j < transforms[i].Length; ++j)
            {
                points[counter] = transforms[i][j].GetPosition();
                colors[counter] = new Vector3(0.98f, 0.98f, 0.98f);
                ++counter;
            }
        }

        if (viewerInitialized)
        {
            pcViewer.AddPoints(points, colors);
        }
        else if (useRuntime)
        {
            pcViewer.CreateWithPoints(runtimeViewer.Points, runtimeViewer.PointColors);
            pcViewer.AddPoints(points, colors);
            useRuntime = false;
            viewerInitialized = true;
        }
        else
        {
            pcViewer.CreateWithPoints(points, colors);
        }
    }

    public static void RestorePointCloud()
    {
        if (lastCloudPath == null) return;

        if (viewerInitialized)
        {
            DisplayPointCloud(lastCloudPath);
        }
        else if (useRuntime)
        {
            DisplayPointCloud(lastCloudPath);
        }
    }
}
