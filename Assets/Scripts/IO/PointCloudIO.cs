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

    static bool viewerInitialized = false;
    static bool useRuntime = false;

    private void Start()
    {
        runtimeViewer = _runtimeViewer;
        pcViewer = _pcViewer;
    }

    public static void ImportPointCloud(string path)
    {
        runtimeViewer.CallImporterThreaded(path);
        useRuntime = true;
    }

    public static void DisplayPointCloud(string path)
    {
        pcViewer.CallReadPointCloudThreaded(path);
        viewerInitialized = true;
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
}
