using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using GPUInstancer;

public class GameManager : MonoBehaviour
{
    [Header("GPU Instancer")]
    [SerializeField]
    GPUInstancerPrefabManager GPUIPrefabManager;

    static Bond _bond = null;
    public static Bond Bond { 
        get 
        { 
            return _bond; 
        } 
        set 
        { 
            _bond = value;

            if (_bond != null)
                _bond.Initialize(); 
        } 
    }

    static GameManager manager;
    public static GameManager Manager { get { return manager; } }

    [SerializeField]
    List<Bond> availableBonds;
    static Dictionary<string, Bond> _availableBonds = new Dictionary<string, Bond>();
    public static Dictionary<string, Bond> AvailableBonds { get { return _availableBonds; } }

    [Header("VR Info")]
    [SerializeField]
    Valve.VR.SteamVR_ActionSet actionSet;

    public static float gameTime = 0;

    public static Vector3 Spacing
    {
        get { return new Vector3(_bond.XSpacing, _bond.YSpacing, _bond.ZSpacing); }
    }

    BlockContainer container;

    void Awake()
    {
        foreach (Bond bond in availableBonds)
            if (!_availableBonds.ContainsKey(bond.BondName))
                _availableBonds.Add(bond.BondName, bond);

        manager = this;
    }

    private void Update()
    {
        gameTime += Time.deltaTime;
    }

    public void Save()
    {
        AssemblyIO.SaveAssemblyLayout(Application.persistentDataPath + "/quickSave.vrxl", true);
    }

    public void Load()
    {
        AssemblyIO.LoadAssemblyLayout(Application.persistentDataPath + "/quickSave.vrxl");
    }

    public void NewGame()
    {
        AssemblyIO.NewGame();
    }

    public void SetBond(Bond b)
    {
        NewGame();

        Bond = b;

        container = new BlockContainer(GPUIPrefabManager, _bond.PartPrefabs);

        actionSet.Activate();
    }

    public void ClearAssembly()
    {
        BlockContainer.Container.ResetContainer();
        PointCloudIO.RecalculatePointCloud();
    }
}
