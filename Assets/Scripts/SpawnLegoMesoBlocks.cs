using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using GPUInstancer;

public class SpawnLegoMesoBlocks : MonoBehaviour
{
    [Header("Prefab")]
    [SerializeField]
    GameObject mesoBlock;
    [SerializeField]
    GPUInstancerPrefabManager manager;
    [SerializeField]
    GameObject camTarget;

    [Space]
    [Header("Meso Block properties")]
    [SerializeField]
    int xOffset;
    [SerializeField]
    int yOffset;
    [SerializeField]
    int zOffset;

    [Space]
    [Header("Spacing")]
    [SerializeField]
    int extendX;
    [SerializeField]
    int extendY;
    [SerializeField]
    int extendZ;

    [Space]
    [Header("Builtup over time")]
    [SerializeField]
    bool overTime = false;
    [SerializeField]
    bool noGameObject = false;

    int xVal;
    int yVal;
    int zVal;
    int partCount;
    int matrixCounter = 0;

    static readonly float xzSpacing = 0.008f;
    static readonly float ySpacing = 0.0096f;

    List<GPUInstancerPrefab> bricks = new List<GPUInstancerPrefab>();
    GPUInstancerPrefab[] mesoBricks;
    Matrix4x4[] transformArray;
    GPUInstancerPrefab brickPrefab;


    // Start is called before the first frame update
    void Awake()
    {
        camTarget.transform.position = new Vector3(xOffset * extendX * xzSpacing / 2, yOffset * extendY * ySpacing / 2, zOffset * extendZ * xzSpacing / 2);
        mesoBricks = mesoBlock.GetComponentsInChildren<GPUInstancerPrefab>();
        partCount = mesoBricks.Length * extendX * extendY * extendZ;
        matrixCounter = 0;
        brickPrefab = mesoBlock.GetComponentInChildren<GPUInstancerPrefab>();

        if (noGameObject)
            transformArray = new Matrix4x4[partCount];

        if (overTime)
        {
            if (noGameObject)
            {
                for (int i = 0; i < partCount; ++i)
                    transformArray[i] = Matrix4x4.zero;
                GPUInstancerAPI.InitializeWithMatrix4x4Array(manager, brickPrefab.prefabPrototype, transformArray);
            }

            return;
        }

        for (int x = 0; x < extendX; ++x)
            for (int y = 0; y < extendY; ++y)
                for (int z = 0; z < extendZ; ++z)
                {
                    if (!noGameObject)
                    {
                        var b = Instantiate(mesoBlock);
                        bricks.AddRange(b.GetComponent<MesoInstanceContainer>().instances);
                        b.transform.position = new Vector3(xOffset * xzSpacing * x, yOffset * ySpacing * y, zOffset * xzSpacing * z);
                    }
                    else
                    {
                        foreach (GPUInstancerPrefab _mesoBrick in mesoBricks)
                        {
                            Vector3 pos = new Vector3(xOffset * xzSpacing * x, yOffset * ySpacing * y, zOffset * xzSpacing * z) + _mesoBrick.transform.localPosition;
                            Quaternion rot = _mesoBrick.transform.localRotation;
                            transformArray[matrixCounter] = Matrix4x4.TRS(pos, rot, Vector3.one);
                            ++matrixCounter;
                        }
                    }
                }

        if (noGameObject)
            GPUInstancerAPI.InitializeWithMatrix4x4Array(manager, brickPrefab.prefabPrototype, transformArray);
    }

    private void Start()
    {
        if (noGameObject) return;

        GPUInstancerAPI.RegisterPrefabInstanceList(manager, bricks);
        GPUInstancerAPI.InitializeGPUInstancer(manager);
    }

    private void Update()
    {
        if (!overTime) return;
        if (zVal >= extendZ) return;

        if (!noGameObject)
        {
            var b = Instantiate(mesoBlock);
            var mesoBrickContainer = b.GetComponent<MesoInstanceContainer>().instances;
            b.transform.position = new Vector3(xOffset * xzSpacing * xVal, yOffset * ySpacing * yVal, zOffset * xzSpacing * zVal);

            foreach (GPUInstancerPrefab brickPrefab in mesoBrickContainer)
                GPUInstancerAPI.AddPrefabInstance(manager, brickPrefab, true);
        }
        else
        {
            if (matrixCounter >= transformArray.Length) return;

            foreach (GPUInstancerPrefab _mesoBrick in mesoBricks)
            {
                Vector3 pos = new Vector3(xOffset * xzSpacing * xVal, yOffset * ySpacing * yVal, zOffset * xzSpacing * zVal) + _mesoBrick.transform.localPosition;
                Quaternion rot = _mesoBrick.transform.localRotation;
                transformArray[matrixCounter] = Matrix4x4.TRS(pos, rot, Vector3.one);
                ++matrixCounter;
            }
            GPUInstancerAPI.UpdateVisibilityBufferWithMatrix4x4Array(manager, brickPrefab.prefabPrototype, transformArray);
        }

        ++xVal;

        if (xVal >= extendX)
        {
            xVal = 0;
            ++yVal;
        }

        if (yVal >= extendY)
        {
            xVal = 0;
            yVal = 0;
            ++zVal;
        }
    }
}
