using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PointCloudRuntimeViewer;
using unitycodercom_PointCloudBinaryViewer;
using System;

public class PointCloudIO : MonoBehaviour
{
    [Header("Viewers")]
    [SerializeField]
    RuntimeViewerDX11 _runtimeViewer;
    static RuntimeViewerDX11 runtimeViewer;
    public static RuntimeViewerDX11 RuntimeViewer { get { return runtimeViewer; } }

    [SerializeField]
    PointCloudViewerDX11 _cloudViewer;
    static PointCloudViewerDX11 cloudViewer;
    public static PointCloudViewerDX11 CloudViewer { get { return cloudViewer; } }

    [Space]
    [Header("Visuals")]
    [SerializeField]
    Material defaultMaterial;

    [SerializeField]
    Color _blockColor = new Color(0.9f, 0.9f, 0.9f);

    static bool viewerInitialized = false;
    static bool useRuntime = false;

    static string lastCloudPath = null;

    static Material cloudMaterial = null;
    public static Material CloudMaterial { get { return cloudMaterial; } set{ cloudMaterial = value; } }

    public static float PointRadius
    {
        get
        {
            return cloudMaterial.GetFloat("_Size");
        }
    }
    static Color blockColor;
    public static Color BlockColor { get { return blockColor; } }

    static PointCloudFormat cloudFormat = PointCloudFormat.XYZRGB;
    public static PointCloudFormat CloudFormat { get { return cloudFormat; } set { cloudFormat = value; } }

    static bool readRGB = true;
    public static bool ReadRGB { get { return readRGB; } set { readRGB = value; } }

    static bool overrideCache = false;
    public static bool OverrideCache { get { return overrideCache; } set { overrideCache = value; } }

    static Vector3[] loadedPoints = new Vector3[0];
    static Vector3[] loadedColors = new Vector3[0];

    private void Awake()
    {
        runtimeViewer = _runtimeViewer;
        cloudViewer = _cloudViewer;
        cloudMaterial = defaultMaterial;
        blockColor = _blockColor;
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

        runtimeViewer.OnLoadingComplete += new RuntimeViewerDX11.OnLoadComplete(OnRuntimeViewerLoad);
    }

    static void ResetRuntimeViewer()
    {
        var viewerGo = runtimeViewer.gameObject;
        Destroy(runtimeViewer);

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
        try
        {
            ResetPointCloudViewer();
            ResetRuntimeViewer();

            cloudViewer.CallReadPointCloudThreaded(path);
            useRuntime = false;
            viewerInitialized = true;
            lastCloudPath = path;

            cloudViewer.OnLoadingComplete += new PointCloudViewerDX11.OnLoadComplete(OnCloudViewerLoad);
        }
        catch
        {
            MessageSystem.DisplayWarning("Point cloud couldn't load. Delete *.bin file and retry!");
        }
    }


    static void OnCloudViewerLoad(string fileName)
    {
        loadedPoints = new Vector3[cloudViewer.points.Length];
        loadedColors = new Vector3[cloudViewer.pointColors.Length];

        Array.Copy(cloudViewer.points, loadedPoints, loadedPoints.Length);
        Array.Copy(cloudViewer.pointColors, loadedColors, loadedColors.Length);

        Debug.Log("Point Cload loaded. BlockContainer has " + BlockContainer.Container.GetCloudCount() + " blocks to add to cloud");
        
        if (BlockContainer.Container.GetCloudCount() > 0)
            RecalculatePointCloud();
        
    }

    static void OnRuntimeViewerLoad(string fileName)
    {
        loadedPoints = new Vector3[runtimeViewer.Points.Length];
        loadedColors = new Vector3[runtimeViewer.PointColors.Length];

        Array.Copy(runtimeViewer.Points, loadedPoints, loadedPoints.Length);
        Array.Copy(runtimeViewer.PointColors, loadedColors, loadedColors.Length);

        Debug.Log("Point Cload loaded. BlockContainer has " + BlockContainer.Container.GetCloudCount() + " blocks to add to cloud");
        
        if (BlockContainer.Container.GetCloudCount() > 0)
            RecalculatePointCloud();
        
    }

    static void ResetPointCloudViewer()
    {
        var viewerGo = cloudViewer.gameObject;
        Destroy(cloudViewer);

        cloudViewer = viewerGo.AddComponent<PointCloudViewerDX11>();
        cloudViewer.fileName = "";
        cloudViewer.loadAtStart = false;
        cloudViewer.cloudMaterial = CloudMaterial;
        cloudViewer.displayPoints = true;
    }

    public static void ModulesToCloud(List<Matrix4x4> transforms)
    {
        Vector3[] points = new Vector3[transforms.Count];
        Vector3[] colors = new Vector3[transforms.Count];

        int counter = 0;
        for (int i = 0; i < transforms.Count; ++i)
        {
            points[counter] = transforms[i].GetPosition();
            colors[counter] = new Vector3(blockColor.r, blockColor.g, blockColor.b);
            ++counter;
        }

        if (viewerInitialized)
        {
            cloudViewer.AddPoints(points, colors);
        }
        else if (useRuntime)
        {
            cloudViewer.CreateWithPoints(runtimeViewer.Points, runtimeViewer.PointColors);
            cloudViewer.AddPoints(points, colors);
            useRuntime = false;
            viewerInitialized = true;
            ResetRuntimeViewer();
        }
        else
        {
            cloudViewer.CreateWithPoints(points, colors);
            viewerInitialized = true;
        }
    }

    public static void RecalculatePointCloud()
    {
        List<Vector3> cloudPositions = new List<Vector3>();
        Matrix4x4[][] cloudTransformArray = BlockContainer.Container.CloudTransformArray;
        int cloudLength = 0;

        for (int i = 0; i < cloudTransformArray.Length; ++i)
        {
            cloudLength += cloudTransformArray[i].Length;
            for (int j = 0; j < cloudTransformArray[i].Length; ++j)
            {
                if (cloudTransformArray[i][j] != Matrix4x4.zero)
                    cloudPositions.Add(cloudTransformArray[i][j].GetPosition());
            }
        }

        Vector3[] points = new Vector3[loadedPoints.Length + cloudPositions.Count];
        Vector3[] colors = new Vector3[loadedColors.Length + cloudPositions.Count];

        if (loadedPoints.Length > 0)
        {
            Array.Copy(loadedPoints, points, loadedPoints.Length);
            Array.Copy(loadedColors, colors, loadedColors.Length);
        }

        int loadedLength = loadedPoints.Length;
        Vector3 _blockColor = new Vector3(blockColor.r, blockColor.g, blockColor.b);

        for (int i = 0; i < cloudPositions.Count; ++i)
        {
            points[loadedLength + i] = cloudPositions[i];
            colors[loadedLength + i] = _blockColor;
        }

        if (viewerInitialized)
        {
            ResetPointCloudViewer();
            cloudViewer.CreateWithPoints(points, colors);
        }
        else if (useRuntime)
        {
            ResetRuntimeViewer();
            cloudViewer.CreateWithPoints(points, colors);
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
