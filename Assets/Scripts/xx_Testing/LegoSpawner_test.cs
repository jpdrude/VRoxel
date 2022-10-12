using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using GPUInstancer;

public class LegoSpawner_test : MonoBehaviour
{
    [Header("Prefab")]
    [SerializeField]
    GPUInstancerPrefab part;
    [SerializeField]
    GPUInstancerPrefabManager manager;
    [SerializeField]
    GameObject camTarget;

    [Space]
    [Header("Spacing")]
    [SerializeField]
    int extendX;
    [SerializeField]
    int extendY;
    [SerializeField]
    int extendZ;

    static readonly float xSpacing = 0.016f;
    static readonly float ySpacing = 0.0096f;
    static readonly float zSpacing = 0.032f;

    List<GPUInstancerPrefab> blocks = new List<GPUInstancerPrefab>();


    // Start is called before the first frame update
    void Start()
    {
        camTarget.transform.position = new Vector3(xSpacing * extendX / 2, 0, zSpacing * extendZ / 2);

        for (int x = 0; x < extendX; ++x)
            for (int y = 0; y < extendY; ++y)
                for (int z = 0; z < extendZ; ++z)
                {
                    var b = Instantiate(part);
                    blocks.Add(b);
                    b.transform.position = new Vector3(xSpacing * x, ySpacing * y, zSpacing * z);
                }

        GPUInstancerAPI.RegisterPrefabInstanceList(manager, blocks);
        GPUInstancerAPI.InitializeGPUInstancer(manager);
    }
}
