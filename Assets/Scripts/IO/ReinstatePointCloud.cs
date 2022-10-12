using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PointCloudRuntimeViewer;
using unitycodercom_PointCloudBinaryViewer;

public class ReinstatePointCloud : MonoBehaviour
{
    [SerializeField]
    RuntimeViewerDX11 runtimeViewer;

    [SerializeField]
    PointCloudViewerDX11 viewer;

    private void Start()
    {
        runtimeViewer.OnLoadingComplete += new RuntimeViewerDX11.OnLoadComplete(LoadComplete);
        viewer.OnLoadingComplete += new PointCloudViewerDX11.OnLoadComplete(LoadComplete);
    }

    void LoadComplete(string args)
    {
        if (BlockContainer.Container.GetCount() > 0)
            PointCloudIO.ModulesToCloud(BlockContainer.Container.PcTransformArray);
    }
}
