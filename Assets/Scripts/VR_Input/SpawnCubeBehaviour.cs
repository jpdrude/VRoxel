using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpawnCubeBehaviour : MonoBehaviour
{
    [SerializeField]
    GameObject parent;

    public Vector3 Min
    {
        get
        {
            if (!flip)
                return transform.position - transform.localScale / 2;
            else
                return transform.position - new Vector3(transform.localScale.z, transform.localScale.y, transform.localScale.x) / 2;
        }
    }

    public Vector3 Max
    {
        get
        {
            if (!flip)
                return transform.position + transform.localScale / 2;
            else
                return transform.position + new Vector3(transform.localScale.z, transform.localScale.y, transform.localScale.x) / 2;
        }
    }

    bool flip = false;

    // Update is called once per frame
    void Update()
    {
        Vector3 direction = transform.position - parent.transform.position;

        if (Mathf.Abs(direction.x) < Mathf.Abs(direction.z))
        {
            transform.rotation = Quaternion.Euler(Vector3.zero);
            flip = false;
        }
        else
        {
            transform.rotation = Quaternion.Euler(new Vector3(0, 90, 0));
            flip = true;
        }
    }
}
