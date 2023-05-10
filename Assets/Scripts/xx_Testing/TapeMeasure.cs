using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class TapeMeasure : MonoBehaviour
{
    [SerializeField]
    Transform a;

    [SerializeField]
    Transform b;

    [SerializeField]
    float distance;

    [SerializeField]
    TextMeshPro text;

    LineRenderer lr;

    // Start is called before the first frame update
    void Start()
    {
        lr = GetComponent<LineRenderer>();
    }

    // Update is called once per frame
    void Update()
    {
        lr.SetPosition(0, a.position);
        lr.SetPosition(1, b.position);

        text.gameObject.transform.position = Vector3.Lerp(a.position, b.position, 0.5f);
        text.gameObject.transform.LookAt(a);
        text.gameObject.transform.Rotate(Vector3.up, 90);
        text.text = distance.ToString("F2") + "cm";

        distance = Vector3.Distance(a.position, b.position) * 100;
    }
}
