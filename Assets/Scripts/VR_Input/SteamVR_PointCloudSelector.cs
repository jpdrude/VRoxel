using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using GPUInstancer;

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

    [Header("Placeholder")]
    [SerializeField]
    GPUInstancerPrefabManager manager;
    [SerializeField]
    GPUInstancerPrefab placeholder;
    Matrix4x4[] placeholderArray = new Matrix4x4[100000];
    Matrix4x4[] emptyArray = new Matrix4x4[1];
    int placeholderArrayPos = 0;

    float radius = 0.05f;
    float scaleSpeed = 0.1f;

    SteamVR_AssemblySpawner assemblySpawner;
    SteamVR_CharacterPointer characterPointer;
    SteamVR_GeometryPointer geometryPointer;

    Dictionary<PositionKey, Matrix4x4> toConvertTransforms = new Dictionary<PositionKey, Matrix4x4>();

    DoFromPointCloud currentFromDoAction = new DoFromPointCloud(DoAction.DoType.ToBlocks);

    VRPlacementMode mode = VRPlacementMode.BlocksToPointCloud;
    public VRPlacementMode Mode
    {
        get { return mode; }
        set
        {
            enabled = true;
            mode = value;
            assemblySpawner.Disable();
            characterPointer.Disable();
            geometryPointer.Disable();

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
        assemblySpawner = GetComponent<SteamVR_AssemblySpawner>();
        characterPointer = GetComponent<SteamVR_CharacterPointer>();
        geometryPointer = GetComponent<SteamVR_GeometryPointer>();
        GPUInstancerAPI.InitializeWithMatrix4x4Array(manager, placeholder.prefabPrototype, emptyArray);
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

    private void OnDisable()
    {
        OnTriggerUp();
    }

    public override void OnTrigger()
    {
        PositionKey[][] blockDictKeys;
        GameManager.Bond.GetBlocksInSphere(sphere.transform.position, radius, out blockDictKeys);

        if (mode == VRPlacementMode.BlocksToPointCloud)
        {
            /*
            for (int i = 0; i < blockDictKeys.Length; ++i)
                foreach (PositionKey key in blockDictKeys[i])
                {
                    if (!toConvertTransforms.ContainsKey(key) && BlockContainer.Container.BlockDictionary.ContainsKey(key))
                    {
                        PositionEntry entry = BlockContainer.Container.BlockDictionary[key];
                        if (entry.item >= 0)
                            toConvertTransforms.Add(key, BlockContainer.Container.TransformArray[entry.blockType][entry.item]);
                    }
                }
            */

            Dictionary<PositionKey, Matrix4x4> blockTransforms = BlockContainer.Container.HideBlocks(blockDictKeys);

            foreach (KeyValuePair<PositionKey, Matrix4x4> pair in blockTransforms)
            {
                if (!toConvertTransforms.ContainsKey(pair.Key))
                    toConvertTransforms.Add(pair.Key, pair.Value);
            }
             
            if (placeholderArrayPos == 0)
            {
                GPUInstancerAPI.InitializeWithMatrix4x4Array(manager, placeholder.prefabPrototype, placeholderArray);
            }

            Vector3 camPos = Camera.main.transform.position;
            foreach (Matrix4x4 t in blockTransforms.Values)
            {
                Vector3 pos = t.GetPosition();
                Matrix4x4 trans = Matrix4x4.TRS(pos, Quaternion.LookRotation(pos - camPos), Vector3.one * 2 * PointCloudIO.PointRadius);
                placeholderArray[placeholderArrayPos] = trans;
                ++placeholderArrayPos;
            }

            GPUInstancerAPI.UpdateVisibilityBufferWithMatrix4x4Array(manager, placeholder.prefabPrototype, placeholderArray);
        }
        else
        {
            Dictionary<PositionKey, Matrix4x4> transformDict = BlockContainer.Container.ReinstateBlocks(blockDictKeys);
            
            if (transformDict.Count > 0)
                currentFromDoAction.AppendKeys(blockDictKeys, transformDict);
        }
    }

    public override void OnTriggerUp()
    {
        if (Mode == VRPlacementMode.BlocksToPointCloud && toConvertTransforms.Count > 0)
        {
            PositionKey[] conversionArray = GetConversionArray();
            BlockContainer.Container.BlocksToPointCloud(conversionArray, toConvertTransforms);

            if (conversionArray.Length > 0)
                UndoStack.Push(new DoToPointCloud(DoAction.DoType.ToPointCloud, conversionArray, toConvertTransforms));

            toConvertTransforms.Clear();

            Array.Clear(placeholderArray, 0, placeholderArrayPos);
            placeholderArrayPos = 0;

            GPUInstancerAPI.InitializeWithMatrix4x4Array(manager, placeholder.prefabPrototype, emptyArray);
        }
        else if (Mode == VRPlacementMode.PointCloudToBlocks)
        {
            PointCloudIO.RecalculatePointCloud();

            if (currentFromDoAction.Count > 0)
                UndoStack.Push(currentFromDoAction);

            currentFromDoAction = new DoFromPointCloud(DoAction.DoType.ToBlocks);
        }
    }

    PositionKey[] GetConversionArray()
    {

        PositionKey[] toConvertArray = new PositionKey[toConvertTransforms.Count];

        int counter = 0;
        foreach (PositionKey key in toConvertTransforms.Keys)
        {
            toConvertArray[counter] = key;
            ++counter;
        }

        return toConvertArray;
    }

    public override void DPadInputPressed(bool _up, bool _down, bool _left, bool _right)
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
        sphere.transform.localPosition = Vector3.forward * radius;
    }
}
