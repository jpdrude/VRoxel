using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GeometryItemRoot : MonoBehaviour
{
    Vector3 rotation;
    bool allowFreeRotation = false;
    public bool AllowFreeRotation 
    { 
        get 
        { 
            return allowFreeRotation; 
        } 
        set 
        { 
            allowFreeRotation = value; 
        } 
    }

    public void InitializeRotation()
    {
        rotation = transform.rotation.eulerAngles;
    }

    public void RotateY(float dir)
    {
        rotation.y += Time.deltaTime * dir;
    }


    void LateUpdate()
    {
        if (allowFreeRotation) return;

        transform.rotation = Quaternion.Euler(rotation);
    }

    private void FixedUpdate()
    {
        if (allowFreeRotation) return;

        transform.rotation = Quaternion.Euler(rotation);
    }
}
