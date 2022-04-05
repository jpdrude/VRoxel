using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MenuSwitch : MonoBehaviour
{
    [Header("Menu Selection")]
    [SerializeField]
    GameObject activeAggregationTool;

    [SerializeField]
    GameObject menuSelector;

    [Header("Menu Options")]
    [SerializeField]
    GameObject icons;
    [SerializeField]
    GameObject reallyNew;
    [SerializeField]
    GameObject reallyLoad;

    private void Start()
    {
        activeAggregationTool.SetActive(true);
        menuSelector.SetActive(false);

        reallyLoad.SetActive(false);
        reallyNew.SetActive(false);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.tag != "MenuTrigger") return;
        Debug.Log("Should switch to selection mode");
        activeAggregationTool.SetActive(false);
        menuSelector.SetActive(true);
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.gameObject.tag != "MenuTrigger") return;
        Debug.Log("Should switch to placement mode");
        activeAggregationTool.SetActive(true);
        menuSelector.SetActive(false);
    }

    public void ReallyLoad()
    {
        icons.SetActive(false);
        reallyLoad.SetActive(true);
    }

    public void ReallyNew()
    {
        icons.SetActive(false);
        reallyNew.SetActive(true);
    }

    public void RestoreMenu()
    {
        icons.SetActive(true);
        reallyNew.SetActive(false);
        reallyLoad.SetActive(false);
    }
}
