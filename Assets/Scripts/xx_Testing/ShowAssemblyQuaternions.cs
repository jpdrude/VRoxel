using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class ShowAssemblyQuaternions : MonoBehaviour
{
    [SerializeField]
    bool update = false;

    [SerializeField]
    List<SerializableQuaternion> quaternions = new List<SerializableQuaternion>();

    [SerializeField]
    List<string> stringQuaternions = new List<string>();


    // Update is called once per frame
    void Update()
    {
        if (!update) return;


        quaternions.Clear();
        stringQuaternions.Clear();

        foreach (Matrix4x4[] type in BlockContainer.Container.TransformArray)
        {
            foreach (Matrix4x4 m in type)
            {
                if (m != Matrix4x4.zero)
                {
                    var q = m.GetRotation();
                    quaternions.Add(new SerializableQuaternion() { x = q.x, y = q.y, z = q.z, w = q.w });
                    stringQuaternions.Add(q.x.ToString("F3") + ", " + q.y.ToString("F3") + ", " + q.z.ToString("F3") + ", " + q.w.ToString("F3"));
                }
                    
            }
        }


        update = false;
    }
}

[Serializable]
public struct SerializableQuaternion
{
    public float x;
    public float y;
    public float z;
    public float w;
}