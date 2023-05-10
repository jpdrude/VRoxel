using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OrthogonalViewManager : MonoBehaviour
{
    [Header("Cameras")]
    [SerializeField]
    GameObject cadController;
    [SerializeField]
    Camera westCam;

    [Space]
    [Header("General")]
    [SerializeField]
    GraphicsSettingsMenu graphicsSettings;

    public void SetWestCam()
    {
        cadController.SetActive(false);
        westCam.gameObject.SetActive(true);
        graphicsSettings.DeactivateCulling();
        graphicsSettings.SetAllLOD0();
    }

    private void OnDisable()
    {
        graphicsSettings.ActivateCulling();
    }
}
