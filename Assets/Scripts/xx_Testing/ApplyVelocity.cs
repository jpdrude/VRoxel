using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ApplyVelocity : MonoBehaviour
{
    public Vector3 vector;

    Rigidbody rb;

    // Start is called before the first frame update
    void Start()
    {
        rb = GetComponent<Rigidbody>();
    }

    // Update is called once per frame
    void Update()
    {
        if (vector.magnitude > 0.001f)
            rb.velocity = vector;
    }
}
