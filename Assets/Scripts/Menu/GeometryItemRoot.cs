using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GeometryItemRoot : MonoBehaviour
{
    Vector3 rotation;
    static bool allowFreeRotation = true;
    public static bool AllowFreeRotation 
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

    static bool gridSnap = false;
    public static bool GridSnap
    {
        get
        {
            return gridSnap;
        }
        set
        {
            gridSnap = value;
        }
    }

    static float xSpacing;
    static float ySpacing;
    static float zSpacing;

    public void InitializeRotation()
    {
        rotation = transform.rotation.eulerAngles;
    }

    public void RotateY(float dir)
    {
        rotation.x = 0;
        rotation.y += Time.deltaTime * dir;
        rotation.z = 0;
    }

    public void SetRotation(Vector3 euler)
    {
        rotation = euler;
    }

    void LateUpdate()
    {
        CheckConstraints();
    }

    private void FixedUpdate()
    {
        CheckConstraints();
    }

    void CheckConstraints()
    {
        if (!allowFreeRotation)
            transform.rotation = Quaternion.Euler(rotation);
        if (gridSnap && !allowFreeRotation)
        {
            xSpacing = GameManager.Bond.XSpacing;
            ySpacing = GameManager.Bond.YSpacing;
            zSpacing = GameManager.Bond.ZSpacing;

            Vector3 newPos = new Vector3();
            newPos.x = Mathf.Round(transform.parent.position.x / xSpacing) * xSpacing;
            newPos.y = Mathf.Round(transform.parent.position.y / ySpacing) * ySpacing;
            newPos.z = Mathf.Round(transform.parent.position.z / zSpacing) * zSpacing;

            transform.position = newPos;
        }
    }
}
