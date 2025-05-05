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
        {"Cuboct_Volumetric_Bond_10cm", new List<string>() {"Cuboct_Bond_10cm"} },

        {"Akro_BondBasic_Lenticular", new List<string>(){ "Akro_BondBasic", "Akro_BondBasic_White", "Akro_BondBasic_Grey" } },
        {"Akro_BondBrutalist_Lenticular", new List<string>(){ "Akro_BondBrutalist", "Akro_BondBrutalist_White", "Akro_BondBrutalist_Grey" } },
        {"Akro_BondChaotic_Lenticular", new List<string>(){ "Akro_BondChaotic", "Akro_BondChaotic_White", "Akro_BondChaotic_Grey" } },
        {"Akro_BondElephant_Lenticular", new List<string>(){ "Akro_BondElephant", "Akro_BondElephant_White", "Akro_BondElephant_Grey" } },
        {"Akro_BondReef_Lenticular", new List<string>(){ "Akro_BondReef", "Akro_BondReef_White", "Akro_BondReef_Grey" } },
        {"Akro_BondTree_Lenticular", new List<string>(){ "Akro_BondTree", "Akro_BondTree_White", "Akro_BondTree_Grey" } },

        {"Akro_BondBasic", new List<string>(){ "Akro_BondBasic_Lenticular", "Akro_BondBasic_White", "Akro_BondBasic_Grey" } },
        {"Akro_BondBrutalist", new List<string>(){ "Akro_BondBrutalist_Lenticular", "Akro_BondBrutalist_White", "Akro_BondBrutalist_Grey" } },
        {"Akro_BondChaotic", new List<string>(){ "Akro_BondChaotic_Lenticular", "Akro_BondChaotic_White", "Akro_BondChaotic_Grey" } },
        {"Akro_BondElephant", new List<string>(){ "Akro_BondElephant_Lenticular", "Akro_BondElephant_White", "Akro_BondElephant_Grey" } },
        {"Akro_BondReef", new List<string>(){ "Akro_BondReef_Lenticular", "Akro_BondReef_White", "Akro_BondReef_Grey" } },
        {"Akro_BondTree", new List<string>(){ "Akro_BondTree_Lenticular", "Akro_BondTree_White", "Akro_BondTree_Grey" } },

        {"Akro_BondBasic_White", new List<string>(){ "Akro_BondBasic_Lenticular", "Akro_BondBasic", "Akro_BondBasic_Grey" } },
        {"Akro_BondBrutalist_White", new List<string>(){ "Akro_BondBrutalist_Lenticular", "Akro_BondBrutalist", "Akro_BondBrutalist_Grey" } },
        {"Akro_BondChaotic_White", new List<string>(){ "Akro_BondChaotic_Lenticular", "Akro_BondChaotic", "Akro_BondChaotic_Grey" } },
        {"Akro_BondElephant_White", new List<string>(){ "Akro_BondElephant_Lenticular", "Akro_BondElephant", "Akro_BondElephant_Grey" } },
        {"Akro_BondReef_White", new List<string>(){ "Akro_BondReef_Lenticular", "Akro_BondReef", "Akro_BondReef_Grey" } },
        {"Akro_BondTree_White", new List<string>(){ "Akro_BondTree_Lenticular", "Akro_BondTree", "Akro_BondTree_Grey" } },

        {"Akro_BondBasic_Grey", new List<string>(){ "Akro_BondBasic_Lenticular", "Akro_BondBasic_White", "Akro_BondBasic" } },
        {"Akro_BondBrutalist_Grey", new List<string>(){ "Akro_BondBrutalist_Lenticular", "Akro_BondBrutalist_White", "Akro_BondBrutalist" } },
        {"Akro_BondChaotic_Grey", new List<string>(){ "Akro_BondChaotic_Lenticular", "Akro_BondChaotic_White", "Akro_BondChaotic" } },
        {"Akro_BondElephant_Grey", new List<string>(){ "Akro_BondElephant_Lenticular", "Akro_BondElephant_White", "Akro_BondElephant" } },
        {"Akro_BondReef_Grey", new List<string>(){ "Akro_BondReef_Lenticular", "Akro_BondReef_White", "Akro_BondReef" } },
        {"Akro_BondTree_Grey", new List<string>(){ "Akro_BondTree_Lenticular", "Akro_BondTree_White", "Akro_BondTree" } }
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