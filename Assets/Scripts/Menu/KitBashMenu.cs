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
        float? offsetX;
        float? offsetY;
        float? offsetZ;

        offsetX = inputX.text.ParseStringToFloat();
        offsetY = inputY.text.ParseStringToFloat();
        offsetZ = inputZ.text.ParseStringToFloat();

        if (offsetX == null) offsetX = 0;
        if (offsetY == null) offsetY = 0;
        if (offsetZ == null) offsetZ = 0;

        KitBashIO.Offset = new Vector3((float)offsetX, (float)offsetY, (float)offsetZ);
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
