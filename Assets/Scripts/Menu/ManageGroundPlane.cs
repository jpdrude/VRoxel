using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class ManageGroundPlane : MonoBehaviour
{
    [SerializeField]
    GameObject groundPlane;

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
    }

    public void SetPlaneOffset(string input)
    {
        float? offset = input.ParseStringToFloat();

        if (offset != null)
        {
            Vector3 groundPlanePos = groundPlane.transform.position;
            groundPlanePos.y = (float)offset;

            groundPlane.transform.position = groundPlanePos;
        }
    }
}
