using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ManageEnvironment : MonoBehaviour
{
    [SerializeField]
    List<Material> skyboxes = new List<Material>();

    public void SetSkybox(int i)
    {
        RenderSettings.skybox = skyboxes[i];
    }

    public void SetFogColor(float value)
    {
        Color gray = new Color(value, value, value);
        RenderSettings.fogColor = gray;
    }

    public void SetFogDensity(float value)
    {
        RenderSettings.fogDensity = value;
    }
}
