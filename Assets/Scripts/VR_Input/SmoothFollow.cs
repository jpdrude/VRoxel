using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SmoothFollow : MonoBehaviour
{
    [SerializeField]
    Transform target;
    [SerializeField]
    float posDamping;
    public float PosDamping { get { return posDamping; } set { posDamping = value; } }
    [SerializeField]
    float rotDamping;
    public float RotDamping { get { return rotDamping; } set { rotDamping = value; } }



    void OnEnable()
    {
        if (target == null) return;
        transform.position = target.position;
        transform.rotation = target.rotation;
    }

    void Update()
    {
        if (target == null) return;
        transform.position = Vector3.Lerp(transform.position, target.position, posDamping);
        transform.rotation = Quaternion.Lerp(transform.rotation, target.rotation, rotDamping);
    }
}
