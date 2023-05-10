using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CheckboxDiabledState : MonoBehaviour
{
    [SerializeField]
    GameObject disabledStateIcon;
    public void ShowDidabledState(bool state)
    {
        disabledStateIcon.SetActive(!state);
    }
}
