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
    private void Start()
    {
        runtimeViewer = _runtimeViewer;
        pcViewer = _pcViewer;
    }

    public static void ImportPointCloud(string path)
    {
        runtimeViewer.CallImporterThreaded(path);
    }

    public static void DisplayPointCloud(string path)
    {
        pcViewer.CallReadPointCloudThreaded(path);
    }
}
