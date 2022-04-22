using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using GPUInstancer;

public class GameManager : MonoBehaviour
{
    [Header("GPU Instancer")]
    [SerializeField]
    GPUInstancerPrefabManager GPUIPrefabManager;

    static Bond _bond;
    public static Bond Bond { get { return _bond; } set { _bond = value; } }

    [SerializeField]
    List<Bond> availableBonds;
    static Dictionary<string, Bond> _availableBonds = new Dictionary<string, Bond>();
    public static Dictionary<string, Bond> AvailableBonds { get { return _availableBonds; } }

    [Header("VR Info")]
    [SerializeField]
    Valve.VR.SteamVR_ActionSet actionSet;

    public static Vector3 Spacing
    {
        get { return new Vector3(_bond.XSpacing, _bond.YSpacing, _bond.ZSpacing); }
    }

    ModuleContainer container;

    void Awake()
    {
        foreach (Bond bond in availableBonds)
            if (!_availableBonds.ContainsKey(bond.name))
                _availableBonds.Add(bond.name, bond);
    }

    public void Save()
    {
        AssemblyIO.SaveAssemblyLayout(Application.persistentDataPath + "/quickSave.json");
    }

    public void Load()
    {
        AssemblyIO.LoadAssemblyLayout(Application.persistentDataPath + "/quickSave.json");
    }

    public void NewGame()
    {
        AssemblyIO.NewGame();
    }

    public void SetBond(Bond b)
    {
        NewGame();

        Bond = b;

        container = new ModuleContainer(GPUIPrefabManager, _bond.BrickPrefab);

        actionSet.Activate();
    }
}
