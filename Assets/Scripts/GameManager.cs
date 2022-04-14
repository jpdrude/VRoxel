using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using GPUInstancer;

public class GameManager : MonoBehaviour
{
    [Header("GPU Instancer")]
    [SerializeField]
    GPUInstancerPrefabManager GPUIPrefabManager;

    static LegoBond _bond;
    public static LegoBond Bond { get { return _bond; } set { _bond = value; } }

    [SerializeField]
    List<LegoBond> availableBonds;
    static Dictionary<string, LegoBond> _availableBonds = new Dictionary<string, LegoBond>();
    public static Dictionary<string, LegoBond> AvailableBonds { get { return _availableBonds; } }

    [Header("VR Info")]
    [SerializeField]
    Valve.VR.SteamVR_ActionSet actionSet;

    public static Vector3 Spacing
    {
        get { return new Vector3(_bond.XSpacing, _bond.YSpacing, _bond.ZSpacing); }
    }

    LegoContainer container;

    void Awake()
    {
        foreach (LegoBond bond in availableBonds)
            if (!_availableBonds.ContainsKey(bond.name))
                _availableBonds.Add(bond.name, bond);
    }

    public void Save()
    {
        LegoIO.SaveLegoLayout(Application.persistentDataPath + "/quickSave.json");
    }

    public void Load()
    {
        LegoIO.LoadLegoLayout(Application.persistentDataPath + "/quickSave.json");
    }

    public void NewGame()
    {
        LegoIO.NewGame();
    }

    public void SetBond(LegoBond b)
    {
        NewGame();

        Bond = b;

        container = new LegoContainer(GPUIPrefabManager, _bond.BrickPrefab);

        actionSet.Activate();
    }
}
