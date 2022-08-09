using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpawnCubeBehaviour : MonoBehaviour
{
    [SerializeField]
    GameObject parent;
    public GameObject Parent { get { return parent; } set { parent = value; } }

    public Vector3 Min
    {
        get
        {
            if (flipXZ)
                return transform.position - new Vector3(transform.localScale.z, transform.localScale.y, transform.localScale.x) / 2;
            else if (flipXY)
                return transform.position - new Vector3(transform.localScale.x, transform.localScale.z, transform.localScale.y) / 2;
            else
                return transform.position - transform.localScale / 2;
        }
    }

    public Vector3 Max
    {
        get
        {
            if (flipXZ)
                return transform.position + new Vector3(transform.localScale.z, transform.localScale.y, transform.localScale.x) / 2;
            else if (flipXY)
                return transform.position + new Vector3(transform.localScale.x, transform.localScale.z, transform.localScale.y) / 2;
            else
                return transform.position + transform.localScale / 2;

        }
    }

    bool flipXY = false;
    bool flipXZ = false;

    // Update is called once per frame
    protected virtual void Update()
    {
        Vector3 direction = transform.position - parent.transform.position;

        if (Mathf.Abs(direction.z) > Mathf.Abs(direction.x) && Mathf.Abs(direction.z) > Mathf.Abs(direction.y))
        {
            if (direction.z > 0)
                transform.rotation = Quaternion.Euler(Vector3.zero);
            else
                transform.rotation = Quaternion.Euler(new Vector3(0, 180, 0));

            flipXY = false;
            flipXZ = false;
        }
        else if (Mathf.Abs(direction.y) > Mathf.Abs(direction.x))
        {
            if (direction.y > 0)
                transform.rotation = Quaternion.Euler(new Vector3(-90, 0, 180));
            else
                transform.rotation = Quaternion.Euler(new Vector3(90, 0, 0));
            

            flipXZ = false;
            flipXY = true;
        }
        else
        {
            if (direction.x > 0)
                transform.rotation = Quaternion.Euler(new Vector3(0, 90, 0));
            else
                transform.rotation = Quaternion.Euler(new Vector3(0, -90, 0));

            flipXY = false;
            flipXZ = true;
        }
    }
}
