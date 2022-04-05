using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using GPUInstancer;
using UnityEditor;

public class MesoInstanceContainer : MonoBehaviour
{
    public List<GPUInstancerPrefab> instances = new List<GPUInstancerPrefab>();

    [ContextMenu("Collect GPUInstances")]
    public void UpdateInstances()
    {
        instances.Clear();

        instances.AddRange(GetComponentsInChildren<GPUInstancerPrefab>());
    }
}
