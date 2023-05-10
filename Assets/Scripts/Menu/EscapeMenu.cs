using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using SimpleFileBrowser;
using System;
using System.IO;
using PointCloudRuntimeViewer;

public class EscapeMenu : MonoBehaviour
{
    [Header("SteamVR")]
    [SerializeField]
    GameObject player;
    [SerializeField]
    Valve.VR.SteamVR_ActionSet actionSet;

    [Space]
    [Header("Platform")]
    [SerializeField]
    GameObject fpsController;
    [SerializeField]
    GameObject vrController;
    [SerializeField]
    GameObject startCamera;

    [Space]
    [Header("Panels")]
    [SerializeField]
    GameObject escapePanel;
    [SerializeField]
    GameObject selectPlatform;
    [SerializeField]
    GameObject cadPanel;
    [SerializeField]
    GameObject selectBondPanel;
    [SerializeField]
    GameObject selectPlatformPanel;
    [SerializeField]
    List<GameObject> panels;
    
    [Space]
    [Header("Buttons")]
    [SerializeField]
    GameObject quit;
    [SerializeField]
    GameObject reallyQuit;

    [Space]
    [Header("Point Cloud Options")]
    [SerializeField]
    List<Material> pcMat;
    [SerializeField]
    UnityEngine.UI.Text pcResolution;

    [Space]
    [Header("Motion Capture Options")]
    [SerializeField]
    UnityEngine.UI.Text fadeTime;
    [SerializeField]
    UnityEngine.UI.Text fadeSteps;

    bool menuActive = false;

    string pcPath = null;
    bool overrideCache;

    private void Start()
    {
        escapePanel.SetActive(false);
        selectPlatform.SetActive(true);
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            menuActive = !menuActive;

            if (menuActive)
            {
                escapePanel.SetActive(true);
                actionSet.Deactivate();
                cadPanel.SetActive(false);

                quit.SetActive(true);
                reallyQuit.SetActive(false);
            }
            else
            {
                Resume();
            }
        }
    }

    public void NewGame()
    {
        try
        {
            UnityEngine.XR.Management.XRGeneralSettings.Instance.Manager.activeLoader.Start();
        }
        catch
        {

        }

        AssemblyIO.NewGame();
        Destroy(player);
        SceneManager.LoadScene(SceneManager.GetActiveScene().name, LoadSceneMode.Single);
    }

    public void ClearAssembly()
    {
        BlockContainer.Container.ResetContainer();
        PointCloudIO.RecalculatePointCloud();

        Resume();
        selectBondPanel.SetActive(true);
    }

    public void ChangeController()
    {
        vrController.SetActive(false);
        fpsController.SetActive(false);
        startCamera.SetActive(true);

        UnityEngine.XR.Management.XRGeneralSettings.Instance.Manager.InitializeLoaderSync();
        UnityEngine.XR.Management.XRGeneralSettings.Instance.Manager.StartSubsystems();

        Resume();
        selectPlatformPanel.SetActive(true);
    }

    public void Save(bool saveInactive)
    {
        FileBrowser.SetFilters(true, new FileBrowser.Filter("VRoxel", ".vrxl"));
        FileBrowser.SetDefaultFilter(".vrxl");

        if (saveInactive)
            FileBrowser.ShowSaveDialog(OnSaveAllSuccess, OnCancel, FileBrowser.PickMode.Files, false);
        else
            FileBrowser.ShowSaveDialog(OnSaveSuccess, OnCancel, FileBrowser.PickMode.Files, false);
    }

    public void Export()
    {
        FileBrowser.SetFilters(true, new FileBrowser.Filter("Json", ".json"));
        FileBrowser.SetDefaultFilter(".json");

        FileBrowser.ShowSaveDialog(OnExportSuccess, OnCancel, FileBrowser.PickMode.Files, false);
    }

    public void Load()
    {
        var filter = new FileBrowser.Filter("VRoxel", ".vrxl", ".json");
        FileBrowser.SetFilters(false, filter);

        FileBrowser.ShowLoadDialog(OnLoadSuccess, OnCancel, FileBrowser.PickMode.Files, true);
        FileBrowser.Instance.SetPointCloudToggle();
    }
    
    //Not in use any more
    public void LoadAsPointCloud()
    {
        FileBrowser.SetFilters(true, new FileBrowser.Filter("VRoxel", ".vrxl", ".json"));
        FileBrowser.SetDefaultFilter(".vrxl");
        FileBrowser.ShowLoadDialog(OnLoadAsPointCloudSuccess, OnCancel, FileBrowser.PickMode.Files, true);
    }

    public void Quit()
    {
        quit.SetActive(false);
        reallyQuit.SetActive(true);
    }

    public void Resume()
    {
        quit.SetActive(true);
        reallyQuit.SetActive(false);

        escapePanel.SetActive(false);

        foreach (GameObject panel in panels)
            panel.SetActive(false);

        actionSet.Activate();

        menuActive = false;

        pcPath = null;

        if (fpsController.activeSelf)
            cadPanel.SetActive(true);
    }

    public void ReallyQuit()
    {
        Application.Quit();
    }

    public void ImportGeometry()
    {
        FileBrowser.SetFilters(true, new FileBrowser.Filter("Geometry", ".fbx", ".obj", ".stl"));
        FileBrowser.SetDefaultFilter(".fbx");
        FileBrowser.ShowLoadDialog(OnGeometryImportSuccess, OnCancel, FileBrowser.PickMode.Files);
        FileBrowser.Instance.SetFixTransformToggle(true);
    }

    public void ImportBrushes()
    {
        FileBrowser.SetFilters(true, new FileBrowser.Filter("Geometry", ".fbx", ".obj", ".stl"));
        FileBrowser.SetDefaultFilter(".fbx");
        FileBrowser.ShowLoadDialog(OnBrushImportSuccess, OnCancel, FileBrowser.PickMode.Files);
        //FileBrowser.Instance.SetFixTransformToggle(false);
    }

    public void ImportTeleportAreas()
    {
        if (player.activeSelf == false)
        {
            MessageSystem.DisplayWarning("No VR activated!");
            Resume();
            return;
        }

        FileBrowser.SetFilters(true, new FileBrowser.Filter("Geometry", ".fbx", ".obj", ".stl"));
        FileBrowser.SetDefaultFilter(".fbx");
        FileBrowser.ShowLoadDialog(OnTeleportAreaImportSuccess, OnCancel, FileBrowser.PickMode.Files);
        FileBrowser.Instance.SetFixTransformToggle(true);
    }

    public void ImportPointCloud()
    {
        if (!File.Exists(pcPath)) return;

        if (!overrideCache && File.Exists(pcPath + ".bin"))
            PointCloudIO.DisplayPointCloud(pcPath + ".bin");
        else
            PointCloudIO.ImportPointCloud(pcPath);

        Resume();
    }

    public void ChangePCImportFormat(int index)
    {
        switch (index)
        {
            case 0:
                PointCloudIO.CloudFormat = PointCloudFormat.XYZ;
                PointCloudIO.ReadRGB = false;
                break;
            case 1:
                PointCloudIO.CloudFormat = PointCloudFormat.XYZRGB;
                PointCloudIO.ReadRGB = true;
                break;
            case 2:
                PointCloudIO.CloudFormat = PointCloudFormat.PLY_ASCII;
                PointCloudIO.ReadRGB = true;
                break;
            case 3:
                PointCloudIO.CloudFormat = PointCloudFormat.PTS;
                PointCloudIO.ReadRGB = true;
                break;
            case 4:
                PointCloudIO.CloudFormat = PointCloudFormat.ASC;
                PointCloudIO.ReadRGB = true;
                break;
            case 5:
                PointCloudIO.CloudFormat = PointCloudFormat.PCD_ASCII;
                PointCloudIO.ReadRGB = true;
                break;
        }
    }

    public void ChangeOverrideChache(bool ovr)
    {
        overrideCache = ovr;
        PointCloudIO.OverrideCache = ovr;
    }

    public void ChangeResolution(float idx)
    {
        int index = Mathf.RoundToInt(idx);

        switch (index)
        {
            case 0:
                PointCloudIO.CloudMaterial = pcMat[0];
                pcResolution.text = "Resolution: 1cm";
                break;
            case 1:
                PointCloudIO.CloudMaterial = pcMat[1];
                pcResolution.text = "Resolution: 2cm";
                break;
            case 2:
                PointCloudIO.CloudMaterial = pcMat[2];
                pcResolution.text = "Resolution: 3cm";
                break;
            case 3:
                PointCloudIO.CloudMaterial = pcMat[3];
                pcResolution.text = "Resolution: 4cm";
                break;
            case 4:
                PointCloudIO.CloudMaterial = pcMat[4];
                pcResolution.text = "Resolution: 5cm";
                break;
        }
    }

    public void SetPointCloudPath()
    {
        FileBrowser.SetFilters(true, new FileBrowser.Filter("Point Cloud Files (Ascii)", ".xyz", ".xyzrgb", ".ply", ".pts", ".pcd", ".asc"));
        FileBrowser.SetDefaultFilter(".xyz");
        FileBrowser.ShowLoadDialog(OnPointCloudImportSuccess, OnCancel, FileBrowser.PickMode.Files);
    }

    void OnGeometryImportSuccess(string[] paths)
    {
        GeometryIO.ImportGeometry(paths[0], FileBrowser.FixTransform);
        Resume();
    }

    void OnBrushImportSuccess(string[] paths)
    {
        GeometryIO.ImportBrushes(paths[0], FileBrowser.FixTransform);
        Resume();
    }

    void OnTeleportAreaImportSuccess(string[] paths)
    {
        GeometryIO.ImportTeleportArea(paths[0], FileBrowser.FixTransform);
        Resume();
    }

    void OnPointCloudImportSuccess(string[] paths)
    {
        pcPath = paths[0];
        ImportPointCloud();
    }

    void OnExportSuccess(string[] paths)
    {
        string path = paths[0];
        string[] split = path.Split('.');
        if (split[split.Length - 1].ToLower() != "json")
            path = path + ".json";
        AssemblyIO.SaveAssemblyLayout(path, true, true);
    }

    void OnSaveAllSuccess(string[] paths)
    {
        string path = paths[0];
        string[] split = path.Split('.');
        if (split[split.Length - 1].ToLower() != "vrxl")
            path = path + ".vrxl";
        AssemblyIO.SaveAssemblyLayout(path, true);
    }

    void OnSaveSuccess(string[] paths)
    {
        string path = paths[0];
        string[] split = path.Split('.');
        if (split[split.Length - 1].ToLower() != "vrxl")
            path = path + ".vrxl";
        AssemblyIO.SaveAssemblyLayout(path, false);
    }

    void OnLoadSuccess(string[] paths)
    {
        foreach (string path in paths)
        {
            if (!FileBrowser.LoadAsPointCloud)
                AssemblyIO.LoadAssemblyLayout(path);
            else
                AssemblyIO.LoadAssemblyAsPointCloud(path);
        }

        Resume();
    }

    void OnLoadAsPointCloudSuccess(string[] paths)
    {
        foreach (string path in paths)
            AssemblyIO.LoadAssemblyAsPointCloud(path);

        Resume();
    }

    public void SetVRPlayer()
    {
        //UnityEngine.XR.Management.XRGeneralSettings.Instance.Manager.InitializeLoaderSync();
        //UnityEngine.XR.Management.XRGeneralSettings.Instance.Manager.StartSubsystems();

        //Valve.VR.SteamVR_Settings.instance.autoEnableVR = true;
        //ManualXRControl.StartXRCoroutine();

        startCamera.SetActive(false);
        vrController.SetActive(true);


        if (GameManager.Bond == null)
            selectBondPanel.SetActive(true);
    }

    public void SetFPSPlayer()
    {
        startCamera.SetActive(false);
        fpsController.SetActive(true);

        if (GameManager.Bond == null)
            selectBondPanel.SetActive(true);

        try
        {
            UnityEngine.XR.Management.XRGeneralSettings.Instance.Manager.activeLoader.Stop();
        }
        catch
        {
            Debug.Log("No XR to stop");
        }
        
    }

    void OnCancel()
    {
        Resume();
    }

    public void ConvertToPC()
    {
        BlockContainer.Container.BlocksToPointCloud();
        Resume();
    }
}

