using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AssemblerAnimator : MonoBehaviour
{
    [SerializeField]
    List<GameObject> joints;

    [SerializeField]
    List<Vector3> movements;

    List<Vector3> startPositions;

    [SerializeField]
    List<bool> reverse;

    [SerializeField]
    GameObject carrier;

    [SerializeField]
    Vector3 carrierMove;

    [SerializeField]
    float speed = 0.1f;

    int index = 0;

    float progress = 0;

    Vector3 legoUnits = new Vector3(0.008f, 0.0096f, 0.008f);

    bool walk = false;

    private void Start()
    {
        startPositions = new List<Vector3>();
        foreach (GameObject go in joints)
            startPositions.Add(go.transform.localPosition);
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.M))
            walk = true;

        if (!walk) return;

        progress += speed / movements[index].magnitude * Time.deltaTime;

        if (progress > 1)
        {
            if (!reverse[index])
                joints[index].transform.localPosition = startPositions[index] + Multiply(legoUnits, movements[index]);
            else
                joints[index].transform.localPosition = startPositions[index];

            ++index;
            progress = 0;

            if (index >= joints.Count)
            {
                index = 0;
                carrier.transform.localPosition = carrier.transform.localPosition + Multiply(legoUnits, carrierMove);
                ResetAssembler();
            }
            return;
        }

        if (!reverse[index])
            joints[index].transform.localPosition = Vector3.Lerp(startPositions[index], startPositions[index] + Multiply(legoUnits, movements[index]), progress);
        else
            joints[index].transform.localPosition = Vector3.Lerp(startPositions[index] + Multiply(legoUnits, movements[index]), startPositions[index], progress);
    }

    void ResetAssembler()
    {
        for (int i = 0; i < joints.Count; ++i)
            joints[i].transform.localPosition = startPositions[i];
    }

    public static Vector3 Multiply(Vector3 v1, Vector3 v2)
    {
        return new Vector3(v1.x * v2.x, v1.y * v2.y, v1.z * v2.z);
    }
}
