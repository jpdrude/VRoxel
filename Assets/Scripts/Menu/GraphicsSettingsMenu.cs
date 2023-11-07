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
    [SerializeField]
    GPUInstancerManager manager;
    [SerializeField]
    Light sun;

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

    float currentLODLevel = 1;

    private void Start()
    {
        GraphicsMedium();
    }


    public void DeactivateCulling()
    {
        foreach (Bond b in GameManager.AvailableBonds.Values)
        {
            foreach (GPUInstancerPrefab p in b.PartPrefabs)
            {

                p.prefabPrototype.isOcclusionCulling = false;
            }
        }
    }

    public void ActivateCulling()
    {
        foreach (Bond b in GameManager.AvailableBonds.Values)
        {
            foreach (GPUInstancerPrefab p in b.PartPrefabs)
            {
                p.prefabPrototype.isOcclusionCulling = true;
            }
        }
    }

    public void CustomGraphics(int tier, bool shadow, bool occlusionCulling, float lodBias)
    {
        if (shadow)
            sun.shadows = LightShadows.Soft;
        else
            sun.shadows = LightShadows.None;

        if (tier < 2)
        {
            QualitySettings.SetQualityLevel(2, true);
            postVolume.profile = postProfileLow;
            Graphics.activeTier = UnityEngine.Rendering.GraphicsTier.Tier1;

            foreach (PostProcessLayer layer in sceneCameraLayers)
                layer.antialiasingMode = PostProcessLayer.Antialiasing.FastApproximateAntialiasing;
        }
        else if (tier == 2)
        {
            QualitySettings.SetQualityLevel(3, true);
            postVolume.profile = postProfileMedium;
            Graphics.activeTier = UnityEngine.Rendering.GraphicsTier.Tier2;

            foreach (PostProcessLayer layer in sceneCameraLayers)
                layer.antialiasingMode = PostProcessLayer.Antialiasing.FastApproximateAntialiasing;
        }
        else
        {
            QualitySettings.SetQualityLevel(5, true);
            postVolume.profile = postProfileHigh;
            Graphics.activeTier = UnityEngine.Rendering.GraphicsTier.Tier3;

            foreach (PostProcessLayer layer in sceneCameraLayers)
            {
                if (layer.gameObject.name == "VRCamera")
                    layer.antialiasingMode = PostProcessLayer.Antialiasing.FastApproximateAntialiasing;
                else
                    layer.antialiasingMode = PostProcessLayer.Antialiasing.TemporalAntialiasing;
            }
        }


        foreach (Bond b in GameManager.AvailableBonds.Values)
        {
            foreach (GPUInstancerPrefab p in b.PartPrefabs)
            {
                p.prefabPrototype.isShadowCasting = shadow;
                p.prefabPrototype.isOcclusionCulling = occlusionCulling;
            }
        }

        GPUInstancerAPI.SetLODBias(manager, lodBias);
        currentLODLevel = lodBias;
    }

    public void HighGraphicsNoShadow()
    {
        CustomGraphics(3, false, false, 5);
        sun.shadows = LightShadows.None;
    }

    public void GraphicsSuperLow()
    {
        CustomGraphics(2, false, true, 0.1f);
        sun.shadows = LightShadows.None;
    }

    public void GraphicsLow()
    {
        sun.shadows = LightShadows.None;

        QualitySettings.SetQualityLevel(2, true);
        Graphics.activeTier = UnityEngine.Rendering.GraphicsTier.Tier1;
        postVolume.profile = postProfileLow;

        foreach(Bond b in GameManager.AvailableBonds.Values)
        {
            foreach (GPUInstancerPrefab p in b.PartPrefabs)
            {
                p.prefabPrototype.isShadowCasting = false;
                p.prefabPrototype.isOcclusionCulling = true;
            }
        }

        GPUInstancerAPI.SetLODBias(manager, 0.7f);
        currentLODLevel = 0.7f;

        foreach (PostProcessLayer layer in sceneCameraLayers)
            layer.antialiasingMode = PostProcessLayer.Antialiasing.None;
    }

    public void GraphicsMedium()
    {
        sun.shadows = LightShadows.Soft;

        QualitySettings.SetQualityLevel(3, true);
        Graphics.activeTier = UnityEngine.Rendering.GraphicsTier.Tier2;
        postVolume.profile = postProfileMedium;

        foreach (Bond b in GameManager.AvailableBonds.Values)
        {
            foreach (GPUInstancerPrefab p in b.PartPrefabs)
            {
                p.prefabPrototype.isShadowCasting = true;
                p.prefabPrototype.isOcclusionCulling = true;
            }
        }

        GPUInstancerAPI.SetLODBias(manager, 1f);

        foreach (PostProcessLayer layer in sceneCameraLayers)
            layer.antialiasingMode = PostProcessLayer.Antialiasing.FastApproximateAntialiasing;
    }

    public void GraphicsHigh()
    {
        sun.shadows = LightShadows.Soft;

        QualitySettings.SetQualityLevel(5, true);
        Graphics.activeTier = UnityEngine.Rendering.GraphicsTier.Tier3;
        postVolume.profile = postProfileHigh;

        foreach (Bond b in GameManager.AvailableBonds.Values)
        {
            foreach (GPUInstancerPrefab p in b.PartPrefabs)
            {
                if (p.prefabPrototype.name == "PointObj")
                    continue;

                p.prefabPrototype.isShadowCasting = true;
                p.prefabPrototype.isOcclusionCulling = true;
            }
        }

        GPUInstancerAPI.SetLODBias(manager, 2f);
        currentLODLevel = 2;

        foreach (PostProcessLayer layer in sceneCameraLayers)
        {
            if (layer.gameObject.name == "VRCamera")
                layer.antialiasingMode = PostProcessLayer.Antialiasing.FastApproximateAntialiasing;
            else
                layer.antialiasingMode = PostProcessLayer.Antialiasing.TemporalAntialiasing;
        }
    }

    public void SetAllLOD0()
    {
        GPUInstancerAPI.SetLODBias(manager, 100f);
    }

    public void ResetLOD()
    {
        GPUInstancerAPI.SetLODBias(manager, currentLODLevel);
    }
}
