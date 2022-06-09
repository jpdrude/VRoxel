using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ClearInputField : MonoBehaviour
{
    public void ClearInput()
    {
        TMP_InputField inputfield = GetComponent<TMP_InputField>();

        inputfield.Select();
        inputfield.text = "";
    }
}
