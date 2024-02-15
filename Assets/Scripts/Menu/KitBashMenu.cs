using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using SimpleFileBrowser;
using TriLibCore;
using UnityEngine.UI;
using TMPro;

public class KitBashMenu : MonoBehaviour
{
    [SerializeField]
    TMP_InputField inputX;

    [SerializeField]
    TMP_InputField inputY;

    [SerializeField]
    TMP_InputField inputZ;


    public void ImportKitBashPalette()
    {
        FileBrowser.SetFilters(true, new FileBrowser.Filter("Geometry", ".fbx", ".obj", ".stl"));
        FileBrowser.SetDefaultFilter(".fbx");
        FileBrowser.ShowLoadDialog(OnGeometryImportSuccess, OnCancel, FileBrowser.PickMode.Files);
        FileBrowser.Instance.SetFixTransformToggle(true);
    }

    public void LoadKitBashAssembly()
    {
        FileBrowser.SetFilters(true, new FileBrowser.Filter("JSON", ".json"));
        FileBrowser.SetDefaultFilter(".json");
        FileBrowser.ShowLoadDialog(LoadAssemblySuccess, OnCancel, FileBrowser.PickMode.Files);
    }

    public void ReExportKitBash()
    {
        FileBrowser.SetFilters(true, new FileBrowser.Filter("JSON", ".json"));
        FileBrowser.SetDefaultFilter(".json");
        FileBrowser.ShowSaveDialog(ReExportSuccess, OnCancel, FileBrowser.PickMode.Files);
    }

    public void DistributeKitBash()
    {
        SetOffset();

        KitBashIO.Distribute = true;

    }

    public void ImportKitBash()
    {
        SetOffset();

        KitBashIO.LoadKitBash();
    }

    void SetOffset()
    {
        float offsetX;
        float offsetY;
        float offsetZ;

        if (!float.TryParse(inputX.text, out offsetX))
            offsetX = 0;

        if (!float.TryParse(inputY.text, out offsetY))
            offsetY = 0;

        if (!float.TryParse(inputZ.text, out offsetZ))
            offsetZ = 0;

        KitBashIO.Offset = new Vector3(offsetX, offsetY, offsetZ);
    }

    void OnCancel()
    {

    }

    public void Resume()
    {
        KitBashIO.DestroyKitBashContext();
    }

    void OnGeometryImportSuccess(string[] paths)
    {
        KitBashIO.FixTransform = FileBrowser.FixTransform;
        GeometryIO.ImportKitBashPalette(paths[0], FileBrowser.FixTransform);
    }

    void LoadAssemblySuccess(string[] paths)
    {
        string path = paths[0];
        KitBashIO.Transforms = SaveContainer.LoadAssembly(path);
    }

    void ReExportSuccess(string[] paths)
    {
        if (Directory.Exists(Path.GetDirectoryName(paths[0])))
            KitBashIO.ReExportKitBash(paths[0]);

        EscapeMenu.Instance.Resume();
    }
}
