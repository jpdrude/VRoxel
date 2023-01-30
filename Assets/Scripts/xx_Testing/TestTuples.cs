using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TestTuples : MonoBehaviour
{
    [SerializeField]
    float x;

    [SerializeField]
    float y;

    [SerializeField]
    float z;

    // Update is called once per frame
    void Update()
    {
        var values = ExtractCoordinates(transform.position);
        x = values.xPos;
        y = values.yPos;
        z = values.zPos;
    }

    (float xPos, float yPos, float zPos) ExtractCoordinates(Vector3 pos)
    {
        return (pos.x, pos.y, pos.z);
    }
}
