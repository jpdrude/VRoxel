using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SteamVR_PointCloudSelector : SpawnerActions
{
    [Header("PointCloud Sphere")]
    [SerializeField]
    GameObject sphere;

    [Header("Materials")]
    [SerializeField]
    Material mat_cloudToBricks;
    [SerializeField]
    Material mat_bricksToCloud;

    float radius = 0.05f;
    float scaleSpeed = 0.1f;
    Vector3 initialOffset = new Vector3(0, 0, 0.05f);

    SteamVR_AssemblySpawner spawner;

    HashSet<DictPosition> toConvert = new HashSet<DictPosition>();

    VRPlacementMode mode = VRPlacementMode.BricksToPointCloud;
    public VRPlacementMode Mode
    {
        get { return mode; }
        set
        {
            enabled = true;
            mode = value;
            spawner.Disable();

            sphere.SetActive(true);
            UpdateSphereScale();

            if (mode == VRPlacementMode.BricksToPointCloud)
                sphere.GetComponent<MeshRenderer>().material = mat_bricksToCloud;
            else if (mode == VRPlacementMode.PointCloudToBricks)
                sphere.GetComponent<MeshRenderer>().material = mat_cloudToBricks;
        }
    }

    private void Awake()
    {
        spawner = GetComponent<SteamVR_AssemblySpawner>();
        Disable();
    }

    public void ChangeMode(int _mode)
    {
        Mode = (VRPlacementMode)_mode;
    }

    public override void Disable()
    {
        sphere.SetActive(false);
        enabled = false;
    }

    public override void OnTrigger()
    {
        Vector3Int[] dictPositions;
        GameManager.Bond.GetBricksInSphere(transform.position, radius, out dictPositions);

        if (mode == VRPlacementMode.BricksToPointCloud)
        {
            foreach (Vector3Int dictPos in dictPositions)
            {
                DictPosition entry = new DictPosition(dictPos);
                if (!toConvert.Contains(entry) && ModuleContainer.Container.ModuleDictionary.ContainsKey(entry))
                {
                    toConvert.Add(entry);
                }
            }
        }
        else
        {
            ModuleContainer.Container.ReinstateBricks(dictPositions);
        }
    }

    public override void OnTriggerUp()
    {
        if (Mode == VRPlacementMode.BricksToPointCloud)
        {
            ModuleContainer.Container.ModulesToPointCloud(GetConversionArray());
        }
    }

    DictPosition[] GetConversionArray()
    {
        DictPosition[] toConvertArray = new DictPosition[toConvert.Count];

        int counter = 0;
        foreach (DictPosition entry in toConvert)
        {
            toConvertArray[counter] = entry;
            ++counter;
        }

        toConvert.Clear();
        return toConvertArray;
    }

    public override void DPadInput(bool _up, bool _down, bool _left, bool _right)
    {
        CheckSphereScale(_up, _down);
    }

    void CheckSphereScale(bool up, bool down)
    {
        if (up)
        {
            radius += Time.deltaTime * scaleSpeed;
            UpdateSphereScale();
        }
        else if (down)
        {
            radius -= Time.deltaTime * scaleSpeed;
            if (radius < 0.001f)
                radius = 0.001f;
            UpdateSphereScale();
        }
    }

    void UpdateSphereScale()
    {
        sphere.transform.localScale = Vector3.one * 2 * radius;
        transform.localPosition = initialOffset + Vector3.forward * radius;
    }
}
