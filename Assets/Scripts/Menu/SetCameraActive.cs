using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using GPUInstancer;

public class SetCameraActive : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        GPUInstancerAPI.SetCamera(GetComponent<Camera>());
    }
}
