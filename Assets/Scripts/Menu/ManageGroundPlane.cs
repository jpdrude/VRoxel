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

    public void SetTeleport(bool value)
    {
        teleportAreas.SetActive(value);

        if (!groundPlane.activeSelf && !teleportAreas.activeSelf)
            offsetInput.interactable = false;
        else
            offsetInput.interactable = true;
    }

    public void SetPlaneVisibility(bool value)
    {
        groundPlane.SetActive(value);

        if (!groundPlane.activeSelf && !teleportAreas.activeSelf)
            offsetInput.interactable = false;
        else
            offsetInput.interactable = true;
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
