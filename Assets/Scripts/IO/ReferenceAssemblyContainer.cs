using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using GPUInstancer;

public class ReferenceAssemblyContainer : MonoBehaviour
{
    public static ReferenceAssemblyContainer instance { get; set; }

    [SerializeField]
    GPUInstancerPrefabManager _manager;

    static GPUInstancerPrefabManager manager;

    static Dictionary<string, GPUInstancerPrefab> prefabList = new Dictionary<string, GPUInstancerPrefab>();
    static Dictionary<string, Dictionary<string, Matrix4x4[]>> assemblyReferences = new Dictionary<string, Dictionary<string, Matrix4x4[]>>();
    static Dictionary<string, Matrix4x4[]> assemlyReferenceTransforms = new Dictionary<string, Matrix4x4[]>();

    static string loadBond = null;
    static Matrix4x4[][] transforms = null;
    static string loadFileName = null;

    private void Start()
    {
        instance = this;
        manager = _manager;

        foreach (GameObject prefab in manager.prefabList)
        {
            prefabList.Add(prefab.name, prefab.GetComponent<GPUInstancerPrefab>());
            assemblyReferences.Add(prefab.name, new Dictionary<string, Matrix4x4[]>());
            assemlyReferenceTransforms.Add(prefab.name, new Matrix4x4[0]);
        }
    }

    private void Update()
    {
        if (loadBond != null)
        {
            LoadReference();
        }
    }

    public static void SetAssemblyReferenceData(string _loadBond, Matrix4x4[][] _transforms, string _fileName)
    {
        loadBond = _loadBond;
        transforms = _transforms;
        loadFileName = _fileName;
    }

    static void LoadReference()
    {
        Bond referenceBond = GameManager.AvailableBonds[loadBond];

        for (int i = 0; i < transforms.Length; ++i)
        {
            GPUInstancerPrefab partPrefab = referenceBond.PartPrefabs[i];
            string partName = partPrefab.gameObject.name;

            if (assemblyReferences[partName].ContainsKey(loadFileName))
                continue;

            assemblyReferences[partName].Add(loadFileName, transforms[i]);
        }

        RecalculateTransforms();

        loadBond = null;
        transforms = null;
        loadFileName = null;
    }

    static void RecalculateTransforms()
    {
        foreach (string key in prefabList.Keys)
        {
            int count = 0;
            int fileCount = 0;
            foreach (string fileName in assemblyReferences[key].Keys)
            {
                ++fileCount;
                count += assemblyReferences[key][fileName].Length;
            }

            if (assemlyReferenceTransforms[key].Length != count)
            {
                Matrix4x4[] newTransforms = new Matrix4x4[count];
                int start = 0;

                foreach (string fileName in assemblyReferences[key].Keys)
                {
                    int length = assemblyReferences[key][fileName].Length;

                    System.Array.Copy(assemblyReferences[key][fileName], 0, newTransforms, start, length);

                    assemlyReferenceTransforms[key] = newTransforms;
                    start += length;
                }

                GPUInstancerAPI.InitializeWithMatrix4x4Array(manager, prefabList[key].prefabPrototype, assemlyReferenceTransforms[key]);
            }
        }
    }
}
