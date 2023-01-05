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

    [SerializeField]
    EscapeMenu escapeMenu;


    public void ImportKitBashPalette()
    {
        FileBrowser.SetFilters(true, new FileBrowser.Filter("Geometry", ".fbx", ".obj", ".stl"));
        FileBrowser.SetDefaultFilter(".fbx");
        FileBrowser.ShowLoadDialog(OnGeometryImportSuccess, OnCancel, FileBrowser.PickMode.Files);
    }

    public void LoadKitBashAssembly()
    {
        FileBrowser.SetFilters(true, new FileBrowser.Filter("JSON", ".json"));
        FileBrowser.SetDefaultFilter(".json");
        FileBrowser.ShowLoadDialog(LoadAssemblySuccess, OnCancel, FileBrowser.PickMode.Files);
    }

    public void DistributeKitBash()
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
        KitBashIO.Distribute = true;
        gameObject.SetActive(false);
    }

    void OnCancel()
    {
        Resume();
    }

    public void Resume()
    {
        KitBashIO.DestroyKitBashContext();
        gameObject.SetActive(false);
        escapeMenu.Resume();
    }

    void OnGeometryImportSuccess(string[] paths)
    {
        GeometryIO.ImportKitBashPalette(paths[0]);
    }

    void LoadAssemblySuccess(string[] paths)
    {
        string path = paths[0];
        KitBashIO.Transforms = SaveContainer.LoadAssembly(path);
    }
}
