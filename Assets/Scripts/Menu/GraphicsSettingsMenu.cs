using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.PostProcessing;
using GPUInstancer;

public class GraphicsSettingsMenu : MonoBehaviour
{
    [Header("General")]
    [SerializeField]
    PostProcessVolume postVolume;
    [SerializeField]
    PostProcessLayer[] sceneCameraLayers;

    [Space]
    [Header("Low Graphics")]
    [SerializeField]
    PostProcessProfile postProfileLow;

    [Space]
    [Header("Medium Graphics")]
    [SerializeField]
    PostProcessProfile postProfileMedium;

    [Space]
    [Header("High Graphics")]
    [SerializeField]
    PostProcessProfile postProfileHigh;

    private void Start()
    {
        GraphicsMedium();
    }

    public void GraphicsLow()
    {
        QualitySettings.SetQualityLevel(2, true);
        Graphics.activeTier = UnityEngine.Rendering.GraphicsTier.Tier1;
        postVolume.profile = postProfileLow;

        foreach(Bond b in GameManager.AvailableBonds.Values)
        {
            foreach (GPUInstancerPrefab p in b.PartPrefabs)
            {
                p.prefabPrototype.isShadowCasting = false;
                p.prefabPrototype.lodBiasAdjustment = 0.6f;
            }
        }

        foreach (PostProcessLayer layer in sceneCameraLayers)
            layer.antialiasingMode = PostProcessLayer.Antialiasing.None;
    }

    public void GraphicsMedium()
    {
        QualitySettings.SetQualityLevel(3, true);
        Graphics.activeTier = UnityEngine.Rendering.GraphicsTier.Tier2;
        postVolume.profile = postProfileMedium;

        foreach (Bond b in GameManager.AvailableBonds.Values)
        {
            foreach (GPUInstancerPrefab p in b.PartPrefabs)
            {
                p.prefabPrototype.isShadowCasting = true;
                p.prefabPrototype.lodBiasAdjustment = 1;
            }
        }

        foreach (PostProcessLayer layer in sceneCameraLayers)
            layer.antialiasingMode = PostProcessLayer.Antialiasing.FastApproximateAntialiasing;
    }

    public void GraphicsHigh()
    {

        QualitySettings.SetQualityLevel(5, true);
        Graphics.activeTier = UnityEngine.Rendering.GraphicsTier.Tier3;
        postVolume.profile = postProfileHigh;

        foreach (Bond b in GameManager.AvailableBonds.Values)
        {
            foreach (GPUInstancerPrefab p in b.PartPrefabs)
            {
                p.prefabPrototype.isShadowCasting = true;
                p.prefabPrototype.lodBiasAdjustment = 2;
            }
        }

        foreach (PostProcessLayer layer in sceneCameraLayers)
        {
            if (layer.gameObject.name == "VRCamera")
                layer.antialiasingMode = PostProcessLayer.Antialiasing.FastApproximateAntialiasing;
            else
                layer.antialiasingMode = PostProcessLayer.Antialiasing.TemporalAntialiasing;
        }
    }
}
