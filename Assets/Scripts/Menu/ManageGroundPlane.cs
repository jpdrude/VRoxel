using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class ManageGroundPlane : MonoBehaviour
{
    [SerializeField]
    GameObject groundPlane;
    [SerializeField]
    GameObject teleportAreas;

    [Space]
    [SerializeField]
    TMP_InputField offsetInput;

    private void Start()
    {
        offsetInput.text = groundPlane.transform.position.y.ToString("F1");
    }

    public void SetOffset(bool value)
    {
        if (!value)
            offsetInput.interactable = false;
        else
            offsetInput.interactable = true;

        groundPlane.SetActive(value);

        teleportAreas.SetActive(value);
    }

    public void SetPlaneOffset(string input)
    {
        float? offset = input.ParseStringToFloat();

        if (offset != null)
        {
            Vector3 groundPlanePos = groundPlane.transform.position;
            groundPlanePos.y = (float)offset;

            groundPlane.transform.position = groundPlanePos;

            Vector3 teleportPlanePos = teleportAreas.transform.position;
            teleportPlanePos.y = groundPlanePos.y + 0.001f;

            teleportAreas.transform.position = teleportPlanePos;
        }
    }
}
