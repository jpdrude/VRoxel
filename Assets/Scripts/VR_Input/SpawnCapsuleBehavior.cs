using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpawnCapsuleBehavior : MonoBehaviour
{
    [SerializeField]
    Transform positionAGizmo;

    [SerializeField]
    Transform positionBGizmo;

    [SerializeField]
    GameObject capA;
    [SerializeField]
    GameObject capB;
    [SerializeField]
    GameObject sleeve;

    [SerializeField]
    float radius;

    public Vector3 PositionA { get { return positionAGizmo.position; } }
    public Vector3 PositionB { get { return positionBGizmo.position; } }

    public float Radius { get { return radius; } }

    public void SetScale(float _radius, float _height)
    {
        radius = _radius;

        positionBGizmo.transform.localPosition = Vector3.up * _height;
        capA.transform.localScale = Vector3.one * radius * 2;
        capB.transform.localScale = Vector3.one * radius * 2;
        sleeve.transform.localScale = new Vector3(radius * 2, radius * 2, _height);
        transform.localPosition = Vector3.forward * (radius - 0.05f);
    }
}
