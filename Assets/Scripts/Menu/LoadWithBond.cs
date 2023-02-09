using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using SimpleFileBrowser;
using TMPro;
using UnityEngine.UI;
using System;

public class LoadWithBond : MonoBehaviour
{
    [SerializeField]
    EscapeMenu escapeMenu;

    [SerializeField]
    TMP_Dropdown targetBondDropdown;

    [SerializeField]
    Button loadButton;

    [SerializeField]
    TMP_Text bondNameText;

    string loadBond;
    bool waitingForCallback = false;

    Dictionary<string, List<string>> bondMapping = new Dictionary<string, List<string>>()
    {
        {"inter(b)lock_6cm_black", new List<string>() {"inter(b)lock_6cm_white"} },
        {"inter(b)lock_6cm_white", new List<string>() {"inter(b)lock_6cm_black"} },
        {"inter(b)lock_18cm_black", new List<string>() {"inter(b)lock_18cm_white"} },
        {"inter(b)lock_18cm_white", new List<string>() {"inter(b)lock_18cm_black"} },
        {"Cuboct_Bond_2cm", new List<string>() {"Cuboct_Volumetric_Bond_2cm"} },
        {"Cuboct_Volumetric_Bond_2cm", new List<string>() {"Cuboct_Bond_2cm"} },
        {"Cuboct_Bond_2.5cm", new List<string>() {"Cuboct_Volumetric_Bond_2.5cm"} },
        {"Cuboct_Volumetric_Bond_2.5cm", new List<string>() {"Cuboct_Bond_2.5cm"} },
        {"Cuboct_Bond_3cm", new List<string>() {"Cuboct_Volumetric_Bond_3cm"} },
        {"Cuboct_Volumetric_Bond_3cm", new List<string>() {"Cuboct_Bond_3cm"} },
        {"Cuboct_Bond_5cm", new List<string>() {"Cuboct_Volumetric_Bond_5cm"} },
        {"Cuboct_Volumetric_Bond_5cm", new List<string> {"Cuboct_Bond_5cm"} },
        {"Cuboct_Bond_10cm", new List<string>() {"Cuboct_Volumetric_Bond_10cm"} },
        {"Cuboct_Volumetric_Bond_10cm", new List<string>() {"Cuboct_Bond_10cm"} }
    };

    public void SelectLoadFile()
    {
        var filter = new FileBrowser.Filter("VRoxel", ".vrxl", ".json");
        FileBrowser.SetFilters(false, filter);

        FileBrowser.ShowLoadDialog(OnLoadSuccess, OnCancel, FileBrowser.PickMode.Files, true);
        FileBrowser.Instance.SetPointCloudToggle();
    }

    void OnLoadSuccess(string[] paths)
    {

        if (!FileBrowser.LoadAsPointCloud)
            AssemblyIO.LoadAssemblyLayout(paths[0], true);
        else
            AssemblyIO.LoadAssemblyAsPointCloud(paths[0], true);


        waitingForCallback = true;
    }

    public void LoadCallback()
    {
        if (!waitingForCallback) return;

        if (!GameManager.AvailableBonds.ContainsKey(AssemblyIO.LoadBond))
            AssemblyIO.CheckBondReplacements();

        loadButton.interactable = true;
        targetBondDropdown.options[0].text = AssemblyIO.LoadBond;
        loadBond = AssemblyIO.LoadBond;
        bondNameText.text = loadBond;

        targetBondDropdown.interactable = true;
        waitingForCallback = false;

        try
        {
            foreach (string bondName in bondMapping[loadBond])
                targetBondDropdown.options.Add(new TMP_Dropdown.OptionData(bondName));

            targetBondDropdown.value = 0;
            targetBondDropdown.RefreshShownValue();
        }
        catch
        {
            Debug.LogError("No remapping available.");
            MessageSystem.DisplayWarning("No remapping available.");
            Resume();
        }
    }

    public void OnBondChange(int bondIndex)
    {
        loadBond = targetBondDropdown.options[bondIndex].text;
    }

    public void Load()
    {
        AssemblyIO.RemapBond(loadBond);
        ClearLoadWithBond();
        escapeMenu.Resume();
    }

    void OnCancel()
    {
        Resume();
    }

    public void Resume()
    {
        ClearValues();

        escapeMenu.Resume();
    }

    public void ClearValues()
    {
        ClearLoadWithBond();

        AssemblyIO.ClearLoadData();
    }

    void ClearLoadWithBond()
    {
        targetBondDropdown.options.Clear();
        targetBondDropdown.options.Add(new TMP_Dropdown.OptionData("Bond"));
        targetBondDropdown.RefreshShownValue();
        targetBondDropdown.interactable = false;
        loadButton.interactable = false;
        bondNameText.text = "";
    }
}