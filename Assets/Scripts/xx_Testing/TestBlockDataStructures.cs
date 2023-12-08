using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TestBlockDataStructures : MonoBehaviour
{
    [SerializeField]
    bool test = false;

    [SerializeField]
    List<Vector3> faultyPos = new List<Vector3>();
    [SerializeField]
    List<int> faultyPosIndex = new List<int>();

    [Space]
    [SerializeField]
    List<Vector3> faultyCloudPos = new List<Vector3>();
    [SerializeField]
    List<int> faultyCloudPosIndex = new List<int>();

    [Space]
    [SerializeField]
    List<Vector3Int> faultyKeys = new List<Vector3Int>();

    // Update is called once per frame
    void Update()
    {
        if (!test) return;

        faultyKeys.Clear();
        faultyPos.Clear();
        faultyPosIndex.Clear();
        faultyCloudPos.Clear();
        faultyCloudPosIndex.Clear();

        foreach (PositionKey key in BlockContainer.Container.BlockDictionary.Keys)
        {
            int type = BlockContainer.Container.BlockDictionary[key].blockType;
            int index = BlockContainer.Container.BlockDictionary[key].item;

            if (index >= 0)
            {
                if (BlockContainer.Container.TransformArray[type][index] == Matrix4x4.zero)
                    faultyKeys.Add(new Vector3Int(key.x, key.y, key.z));
            }
            else
            {
                index *= -1;

                if (BlockContainer.Container.CloudTransformArray[type][index] == Matrix4x4.zero)
                    faultyKeys.Add(new Vector3Int(key.x, key.y, key.z));
            }
        }

        for (int i = 0; i < BlockContainer.Container.TransformArray.Length; ++i)
            for(int j = 0; j < BlockContainer.Container.TransformArray[i].Length; ++j)
            {
                Matrix4x4 matrix = BlockContainer.Container.TransformArray[i][j];
                if (matrix == Matrix4x4.zero) continue;

                PositionKey key = new PositionKey(matrix);

                if (!BlockContainer.Container.BlockDictionary.ContainsKey(key))
                {
                    faultyPos.Add(matrix.GetPosition());
                    faultyPosIndex.Add(j);
                }
            }

        for (int i = 0; i < BlockContainer.Container.CloudTransformArray.Length; ++i)
            for (int j = 0; j < BlockContainer.Container.CloudTransformArray[i].Length; ++j)
            {
                Matrix4x4 matrix = BlockContainer.Container.CloudTransformArray[i][j];
                if (matrix == Matrix4x4.zero) continue;

                PositionKey key = new PositionKey(matrix);

                if (!BlockContainer.Container.BlockDictionary.ContainsKey(key))
                {
                    faultyCloudPos.Add(matrix.GetPosition());
                    faultyCloudPosIndex.Add(j);
                }
            }

        test = false;
        Debug.Log("Test run!");
    }


}
