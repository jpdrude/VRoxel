using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GeometryItemRoot : MonoBehaviour
{
    Vector3 rotation;

    private void OnEnable()
    {
        rotation = transform.rotation.eulerAngles;
    }

    public void RotateY(float dir)
    {
        rotation.y += Time.deltaTime * dir;
    }


    void LateUpdate()
    {
        transform.rotation = Quaternion.Euler(rotation);
    }

    private void FixedUpdate()
    {
        transform.rotation = Quaternion.Euler(rotation);
    }
}
