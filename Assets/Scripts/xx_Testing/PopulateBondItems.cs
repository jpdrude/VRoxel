using System.Collections;
using System.Collections.Generic;
using UnityEngine;


[ExecuteInEditMode]
public class PopulateBondItems : MonoBehaviour
{

    [SerializeField]
    bool populate;

    [SerializeField]
    bool setDefaultOrientations;

    [SerializeField]
    bool rotate90;

    [SerializeField]
    bool renamechildren;

    [SerializeField]
    bool roundTo;

    [SerializeField]
    bool replacePrefabs;

    [SerializeField]
    int roundDepth = 5;

    [SerializeField]
    string baseName;

    [SerializeField]
    Bond bond;

    // Start is called before the first frame update
    void Update()
    {
        if (bond == null)
            bond = GetComponent<Bond>();

        if (populate)
        {
            populate = false;

            Transform[] children = new Transform[bond.transform.childCount];

            for (int i = 0; i < bond.transform.childCount; ++i)
            {
                children[i] = bond.transform.GetChild(i);
            }

            bond.SetBondBlocks(children);
        }
        if (setDefaultOrientations)
        {
            setDefaultOrientations = false;
            List<Vector3> defOrient = new List<Vector3>();

            defOrient.Add(Vector3.zero);
            defOrient.Add(new Vector3(0, 90, 0));
            defOrient.Add(new Vector3(0, 180, 0));
            defOrient.Add(new Vector3(0, 270, 0));
            defOrient.Add(new Vector3(90, 0, 0));
            defOrient.Add(new Vector3(270, 0, 0));

            bond.SetOrientations(defOrient);
        }

        if (renamechildren)
        {
            renamechildren = false;

            for (int i = 0; i < bond.transform.childCount; ++i)
            {
                bond.transform.GetChild(i).name = baseName + "_" + i;
            }
        }

        if (rotate90)
        {
            rotate90 = false;

            for (int i = 0; i < bond.transform.childCount; ++i)
            {
                var go = bond.transform.GetChild(i).gameObject;

                var rot = go.transform.rotation.eulerAngles;
                rot.x = rot.x - 90;

                go.transform.rotation = Quaternion.Euler(rot);
            }
        }

        if (roundTo)
        {
            for (int i = 0; i < bond.transform.childCount; ++i)
            {
                var go = bond.transform.GetChild(i).gameObject;

                var pos = go.transform.position;
                pos.x = (float)System.Math.Round(pos.x, roundDepth);
                pos.y = (float)System.Math.Round(pos.y, roundDepth);
                pos.z = (float)System.Math.Round(pos.z, roundDepth);

                go.transform.position = pos;
            }
        }

        if (replacePrefabs)
        {
            replacePrefabs = false;

            for (int i = 0; i < bond.BondBlocks.Length; ++i)
            {
                for (int j = 0; j < bond.BondBlocks[i].blocks.Length; ++j)
                {
                    #if UNITY_EDITOR
                    GameObject go = (GameObject)UnityEditor.PrefabUtility.InstantiatePrefab(bond.PartPrefabs[i].gameObject, transform); ;
                    go.transform.localPosition = bond.BondBlocks[i].blocks[j].localPosition;
                    go.transform.localRotation = bond.BondBlocks[i].blocks[j].localRotation;
                    go.name = go.name + "_" + j;

                    Destroy(bond.BondBlocks[i].blocks[j].gameObject);
                    bond.BondBlocks[i].blocks[j] = go.transform;
                    #endif
                }
            }
        }
    }

}
