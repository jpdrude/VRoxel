using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using GPUInstancer;

public class CameraFollowSettings : MonoBehaviour
{
    [SerializeField]
    GameObject followCam;
    Camera cam;
    SmoothFollow follow;

    [Space]
    [Header("UI Elements")]
    [SerializeField]
    Text fovLabel;
    [SerializeField]
    Text posDampingLabel;
    [SerializeField]
    Text rotDampingLabel;

    [Space]
    [Header("GPU Instancing")]
    [SerializeField]
    List<GPUInstancerPrefabManager> manager;

    private void Start()
    {
        cam = followCam.GetComponent<Camera>();
        follow = followCam.GetComponent<SmoothFollow>();
    }

    public void SetEnabled(bool state)
    {
        followCam.SetActive(state);
    }

    public void SetFustrum(bool state)
    {
        foreach (var m in manager)
        {
            m.isFrustumCulling = state;
        }
    }

    public void SetOcclusion(bool state)
    {
        foreach (var m in manager)
        {
            m.isOcclusionCulling = state;
        }
    }

    public void SetFOV(float fov)
    {
        cam.fieldOfView = fov;
        fovLabel.text = "FOV: " + fov.ToString("F0");
    }

    public void SetPosDamping(float value)
    {
        follow.PosDamping = value;
        posDampingLabel.text = "Position Damping: " + value.ToString("F2");
    }

    public void SetRotDamping(float value)
    {
        follow.RotDamping = value;
        rotDampingLabel.text = "Rotation Damping: " + value.ToString("F2");
    }
}
