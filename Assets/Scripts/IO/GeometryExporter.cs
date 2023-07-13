using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using SimpleFileBrowser;
using System.IO;
using UnityFBXExporter;

public class GeometryExporter : MonoBehaviour
{
    [SerializeField]
    GameObject geometryParent;


    public void ExportGeometry()
    {
        FileBrowser.SetFilters(true, new FileBrowser.Filter("FBX", ".fbx"));
        FileBrowser.SetDefaultFilter(".fbx");

        FileBrowser.ShowSaveDialog(OnExportSuccess, OnCancel, FileBrowser.PickMode.Files, false);
    }

    void OnExportSuccess(string[] paths)
    {
        string path = paths[0];
        if (!Directory.Exists(Path.GetDirectoryName(path))) return;
        
        if (Path.GetExtension(path).ToLower() != ".fbx")
            path = Path.GetDirectoryName(path) + "\\" + Path.GetFileNameWithoutExtension(path) + ".fbx";
        
        ExportGeometry(path);
    }

    void OnCancel()
    {

    }

    void ExportGeometry(string path)
    {
        geometryParent.transform.Rotate(Vector3.up, -180);
        geometryParent.transform.Rotate(Vector3.right, -90);


        FBXExporter.ExportGameObjAtRuntime(geometryParent, Path.GetDirectoryName(path) + "\\", Path.GetFileName(path), "maps", true);


        geometryParent.transform.Rotate(Vector3.right, 90);
        geometryParent.transform.Rotate(Vector3.up, 180);
    }
}
