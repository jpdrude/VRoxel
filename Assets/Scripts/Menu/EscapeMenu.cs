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
    GameObject importPanel;
    [SerializeField]
    GameObject pointCloudPanel;
    [SerializeField]
    GameObject motionCapturePanel;

    [Space]
    [Header("Buttons")]
    [SerializeField]
    GameObject quit;
    [SerializeField]
    GameObject reallyQuit;
    [SerializeField]
    GameObject newGame;
    [SerializeField]
    GameObject reallyNewGame;

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

                quit.SetActive(true);
                reallyQuit.SetActive(false);
                newGame.SetActive(true);
                reallyNewGame.SetActive(false);
            }
            else
            {
                Resume();
            }
        }
    }

    public void NewGame()
    {
        newGame.SetActive(false);
        reallyNewGame.SetActive(true);
    }

    public void ReallyNewGame()
    {
        AssemblyIO.NewGame();
        Destroy(player);
        SceneManager.LoadScene(SceneManager.GetActiveScene().name, LoadSceneMode.Single);
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
    }

    public void LoadAsPointCloud()
    {
        FileBrowser.SetFilters(true, new FileBrowser.Filter("Json", ".json"));
        FileBrowser.SetDefaultFilter(".json");
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

        newGame.SetActive(true);
        reallyNewGame.SetActive(false);

        escapePanel.SetActive(false);
        importPanel.SetActive(false);
        pointCloudPanel.SetActive(false);
        motionCapturePanel.SetActive(false);
        actionSet.Activate();

        menuActive = false;

        pcPath = null;
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
    }

    public void ImportSpawnShapes()
    {
        FileBrowser.SetFilters(true, new FileBrowser.Filter("Geometry", ".fbx", ".obj", ".stl"));
        FileBrowser.SetDefaultFilter(".fbx");
        FileBrowser.ShowLoadDialog(OnSpawnShapeImportSuccess, OnCancel, FileBrowser.PickMode.Files);
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
    }

    public void ImportMotionCapture()
    {
        FileBrowser.SetFilters(true, new FileBrowser.Filter("Motion Capture", ".fbx"));
        FileBrowser.SetDefaultFilter(".fbx");
        FileBrowser.ShowLoadDialog(OnMotionCaptureSuccess, OnCancel, FileBrowser.PickMode.Files);
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

    public void ChangeMocapSteps(float _steps)
    {
        int steps = Mathf.RoundToInt(_steps);
        fadeSteps.text = "Trail steps: " + steps;
        CustomAnimationPlayer.fadeSteps = steps;
    }

    public void ChangeMocapTime(float time)
    {
        fadeTime.text = "Fade time: " + time.ToString("F1") + " seconds";
        CustomAnimationPlayer.fadeTime = time;
    }

    public void ChangeMocapTrails(bool trails)
    {
        CustomAnimationPlayer.trails = trails;
    }

    public void ChangePCImportFormat(int index)
    {
        switch (index)
        {
            case 0:
                PointCloudIO.RuntimeViewer.pointCloudFormat = PointCloudFormat.XYZ;
                PointCloudIO.RuntimeViewer.readRGB = false;
                break;
            case 1:
                PointCloudIO.RuntimeViewer.pointCloudFormat = PointCloudFormat.XYZRGB;
                PointCloudIO.RuntimeViewer.readRGB = true;
                break;
            case 2:
                PointCloudIO.RuntimeViewer.pointCloudFormat = PointCloudFormat.PLY_ASCII;
                PointCloudIO.RuntimeViewer.readRGB = true;
                break;
            case 3:
                PointCloudIO.RuntimeViewer.pointCloudFormat = PointCloudFormat.PTS;
                PointCloudIO.RuntimeViewer.readRGB = true;
                break;
            case 4:
                PointCloudIO.RuntimeViewer.pointCloudFormat = PointCloudFormat.ASC;
                PointCloudIO.RuntimeViewer.readRGB = true;
                break;
            case 5:
                PointCloudIO.RuntimeViewer.pointCloudFormat = PointCloudFormat.PCD_ASCII;
                PointCloudIO.RuntimeViewer.readRGB = true;
                break;
        }
    }

    public void ChangeOverrideChache(bool ovr)
    {
        overrideCache = ovr;
        PointCloudIO.RuntimeViewer.overrideExistingCacheFile = ovr;
    }

    public void ChangeResolution(float idx)
    {
        int index = Mathf.RoundToInt(idx);

        switch(index)
        {
            case 0:
                PointCloudIO.RuntimeViewer.cloudMaterial = pcMat[0];
                PointCloudIO.PcViewer.cloudMaterial = pcMat[0];
                pcResolution.text = "Resolution: 1cm";
                break;
            case 1:
                PointCloudIO.RuntimeViewer.cloudMaterial = pcMat[1];
                PointCloudIO.PcViewer.cloudMaterial = pcMat[1];
                pcResolution.text = "Resolution: 2cm";
                break;
            case 2:
                PointCloudIO.RuntimeViewer.cloudMaterial = pcMat[2];
                PointCloudIO.PcViewer.cloudMaterial = pcMat[2];
                pcResolution.text = "Resolution: 3cm";
                break;
            case 3:
                PointCloudIO.RuntimeViewer.cloudMaterial = pcMat[3];
                PointCloudIO.PcViewer.cloudMaterial = pcMat[3];
                pcResolution.text = "Resolution: 4cm";
                break;
            case 4:
                PointCloudIO.RuntimeViewer.cloudMaterial = pcMat[4];
                PointCloudIO.PcViewer.cloudMaterial = pcMat[4];
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

    void OnMotionCaptureSuccess(string[] paths)
    {
        AnimationIO.ImportMotionCapture(paths[0]);
        Resume();
    }

    void OnGeometryImportSuccess(string[] paths)
    {
        GeometryIO.ImportGeometry(paths[0]);
        Resume();
    }

    void OnSpawnShapeImportSuccess(string[] paths)
    {
        GeometryIO.ImportSpawnShape(paths[0]);
        Resume();
    }

    void OnTeleportAreaImportSuccess(string[] paths)
    {
        GeometryIO.ImportTeleportArea(paths[0]);
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
        foreach(string path in paths)
            AssemblyIO.LoadAssemblyLayout(path);

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

        startCamera.SetActive(false);
        vrController.SetActive(true);
    }

    public void SetFPSPlayer()
    {
        startCamera.SetActive(false);
        fpsController.SetActive(true);
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
