using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using Valve.Newtonsoft.Json;

public class AutomaticSceneLoader : MonoBehaviour
{
    [Header("Platform")]
    [SerializeField]
    GameObject selectPlatform;
    [SerializeField]
    EscapeMenu escapeMenu;
    [SerializeField]
    bool loaderActive;

    [Space]
    [Header("Create Load Options")]
    [SerializeField]
    string bondName;
    [SerializeField]
    string pointCloudPath;
    [SerializeField]
    string brushPath;
    [SerializeField]
    bool export;

    // Start is called before the first frame update
    void Start()
    {
        if (!loaderActive)
        {
            gameObject.SetActive(false);
            return;
        }

        GameManager manager = GetComponent<GameManager>();
        VRoxelLoadOptions options;

        try
        {
            using (StreamReader file = File.OpenText(Application.persistentDataPath + "/loadOptions.json"))
            {
                JsonSerializer serializer = new JsonSerializer();
                options = (VRoxelLoadOptions)serializer.Deserialize(file, typeof(VRoxelLoadOptions));
            }
        }
        catch
        {
            Debug.LogError("Automatic scene loader couldn't find load options");
            MessageSystem.DisplayWarning("Automatic scene loader couldn't find load options");
            return;
        }

        escapeMenu.SetVRPlayer();
        selectPlatform.SetActive(false);
        manager.SetBond(GameManager.AvailableBonds[options.bondName]);
        GeometryIO.ImportBrushes(options.brushPath, false);
        PointCloudIO.ImportPointCloud(options.pointCloadPath);
    }

    private void Update()
    {
        if (export)
        {
            VRoxelLoadOptions options = new VRoxelLoadOptions(bondName, pointCloudPath, brushPath);

            using (StreamWriter file = File.CreateText(Application.persistentDataPath + "/loadOptions.json"))
            {
                JsonSerializer serializer = new JsonSerializer();
                serializer.Serialize(file, options);
            }

            export = false;
        }
    }
}

public class VRoxelLoadOptions
{
    public string bondName;
    public string pointCloadPath;
    public string brushPath;

    public VRoxelLoadOptions(string _bondName, string _pointCloudPath, string _brushPath)
    {
        bondName = _bondName;
        pointCloadPath = _pointCloudPath;
        brushPath = _brushPath;
    }
}
