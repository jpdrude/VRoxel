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
    Material mat_cloudToBlocks;
    [SerializeField]
    Material mat_blocksToCloud;

    float radius = 0.05f;
    float scaleSpeed = 0.1f;
    Vector3 initialOffset = new Vector3(0, 0, 0.05f);

    SteamVR_AssemblySpawner spawner;

    HashSet<PositionKey> toConvert = new HashSet<PositionKey>();

    VRPlacementMode mode = VRPlacementMode.BlocksToPointCloud;
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

            if (mode == VRPlacementMode.BlocksToPointCloud)
                sphere.GetComponent<MeshRenderer>().material = mat_blocksToCloud;
            else if (mode == VRPlacementMode.PointCloudToBlocks)
                sphere.GetComponent<MeshRenderer>().material = mat_cloudToBlocks;
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
        PositionKey[][] blockDictKeys;
        GameManager.Bond.GetBlocksInSphere(transform.position, radius, out blockDictKeys);

        if (mode == VRPlacementMode.BlocksToPointCloud)
        {
            for (int i = 0; i < blockDictKeys.Length; ++i)
                foreach (PositionKey entry in blockDictKeys[i])
                {
                    if (!toConvert.Contains(entry) && BlockContainer.Container.BlockDictionary.ContainsKey(entry))
                    {
                        toConvert.Add(entry);
                    }
                }
        }
        else
        {
            BlockContainer.Container.ReinstateBlocks(blockDictKeys);
        }
    }

    public override void OnTriggerUp()
    {
        if (Mode == VRPlacementMode.BlocksToPointCloud)
        {
            BlockContainer.Container.BlocksToPointCloud(GetConversionArray());
        }
    }

    PositionKey[] GetConversionArray()
    {
        PositionKey[] toConvertArray = new PositionKey[toConvert.Count];

        int counter = 0;
        foreach (PositionKey key in toConvert)
        {
            toConvertArray[counter] = key;
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
