using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using Newtonsoft.Json;
//using System.Runtime.Serialization.Formatters.Binary;
using LegoAssembly;
using System;
using GameDevWare.Serialization;
using System.Runtime.Serialization;
using UnityEngine.Events;

public class AssemblyIO : MonoBehaviour
{
    static Matrix4x4 conversionMatrix = new Matrix4x4(new Vector4(1, 0, 0, 0), new Vector4(0, 0, 1, 0), new Vector4(0, 1, 0, 0), new Vector4(0, 0, 0, 1));

    static Matrix4x4[][] loadContainer = null;
    static uint[][] loadTimes = null;
    static bool loadAsPointCloud = false;
    static float loadTimeOffset = 0;
    static string loadBond = null;
    public static string LoadBond { get { return loadBond; } }

    static List<string> currentSaveTargets = new List<string>();

    static object lockKey = new object();

    static string message = null;

    static bool remapBond = false;

    static bool loadComplete = false;

    [SerializeField]
    bool loadIncrementally = false;
    [SerializeField]
    IncrementalLoader incrementalLoader;
    [SerializeField]
    UnityEvent OnLoadComplete;

    private void Update()
    {
        if (loadComplete)
        {
            OnLoadComplete.Invoke();
            loadComplete = false;
        }

        if (loadContainer != null)
        {
            if (remapBond)
            {
                Debug.Log("Aborted Loading for Remapping!");
                return;
            }


            if (loadBond != GameManager.Bond.BondName)
            {
                if (BlockContainer.Container.GetCount() < 1000)
                {
                    if (GameManager.AvailableBonds.ContainsKey(loadBond))
                    {
                        GameManager.Manager.SetBond(GameManager.AvailableBonds[loadBond]);
                        Debug.Log("Bond set to " + loadBond);
                    }
                    else
                    {
                        if (!CheckBondReplacements())
                        {
                            Debug.LogError("File could not be loaded, because of unknown Bond");
                            MessageSystem.DisplayWarning("File could not be loaded, because of unknown Bond");
                        }
                        else
                            Debug.Log("Old bond name found and switched to new version");
                    }
                }
                else
                {
                    MessageSystem.DisplayWarning("File could not be loaded, because of incompatible Bond");
                    MessageSystem.DisplayWarning("Session Bond: " + GameManager.Bond.BondName + ", File Bond: " + loadBond);
                    loadBond = null;
                    loadContainer = null;
                    loadTimes = null;
                    loadTimeOffset = 0;
                    loadAsPointCloud = false;

                    return;
                }
            }

            if (!loadAsPointCloud)
            {
                if (!loadIncrementally)
                    BlockContainer.Container.AddBlocks(loadContainer, null, loadTimes);
                else
                    incrementalLoader.SetLoadData(loadContainer, loadTimes);
            }
            else
            {
                BlockContainer.Container.AddBlocks(loadContainer, null, loadTimes, true);
            }

            GameManager.gameTime += loadTimeOffset;

            loadContainer = null;
            loadBond = null;
            loadTimes = null;
            loadTimeOffset = 0;
            loadAsPointCloud = false;
        }

        if (message != null)
        {
            MessageSystem.DisplayMessage(message);
            message = null;
        }
    }

    public static bool CheckBondReplacements()
    {
        return BondReplacements.CheckBondReplacement(ref loadBond);
    }

    void RecoverFromPositions()
    {
        List<Matrix4x4>[] matrices = new List<Matrix4x4>[loadContainer.Length];

        for (int i = 0; i < loadContainer.Length; i++)
        {
            matrices[i] = new List<Matrix4x4>();
            foreach (Matrix4x4 matrix in loadContainer[i])
            {
                matrices[i].Add(GameManager.Bond.GetBlockFromPosition(matrix.GetPosition(), i));
            }
        }

        Matrix4x4[][] outMatrices = new Matrix4x4[loadContainer.Length][];
        for (int i = 0; i < loadContainer.Length; i++)
        {
            outMatrices[i] = matrices[i].ToArray();
        }

        BlockContainer.Container.AddBlocks(outMatrices);

        Debug.Log("Referencing Position finished at " + DateTime.Now.ToString("T"));
    }

    public static void SaveAssemblyLayout(string path, bool saveInactive, bool export = false)
    {
        if (currentSaveTargets.Contains(path))
        {
            MessageSystem.DisplayWarning("There is already a save process in progress to this path!");
            return;
        }

        List<Matrix4x4>[] tempBlockTransforms = new List<Matrix4x4>[BlockContainer.Container.PartTypeCount];
        List<uint>[] tempBlockTimeStamps = new List<uint>[BlockContainer.Container.PartTypeCount];

        for (int i = 0; i < tempBlockTransforms.Length; ++i)
        {
            tempBlockTransforms[i] = new List<Matrix4x4>();
            tempBlockTimeStamps[i] = new List<uint>();
        }
            
        foreach (PositionEntry entry in BlockContainer.Container.BlockDictionary.Values)
        {
            if (entry.item > 0)
            {
                tempBlockTransforms[entry.blockType].Add(BlockContainer.Container.TransformArray[entry.blockType][entry.item]);
                tempBlockTimeStamps[entry.blockType].Add(entry.time);
            }
            else if (entry.item < 0 && saveInactive)
            {
                tempBlockTransforms[entry.blockType].Add(BlockContainer.Container.CloudTransformArray[entry.blockType][-entry.item]);
                tempBlockTimeStamps[entry.blockType].Add(entry.time);
            }
        }

        /*
        if (BlockContainer.Container.CloudTransformArray != null && saveInactive)
        {
            //foreach (Matrix4x4 matrix4x4 in ModuleContainer.Container.PcTransforms)
            for (int i = 0; i < tempBlockTransforms.Length; ++i)
                foreach(Matrix4x4 matrix4x4 in BlockContainer.Container.CloudTransformArray[i])
                {
                    if (matrix4x4 != Matrix4x4.zero)
                    {
                        tempBlockTransforms[i].Add(matrix4x4);
                    }   
                }
        }

        for (int i = 0; i < tempBlockTransforms.Length; ++i)
            foreach (Matrix4x4 matrix4x4 in BlockContainer.Container.TransformArray[i])
            {
                if (matrix4x4 != Matrix4x4.zero)
                {
                    tempBlockTransforms[i].Add(matrix4x4);
                }   
            }
        */

        if (export)
        {
            currentSaveTargets.Add(path);
            JsonMatrixContainer container = new JsonMatrixContainer(GameManager.Bond.BondName, tempBlockTransforms, tempBlockTimeStamps);

            ExportAssemblyDelegate exportDelegate = new ExportAssemblyDelegate(ExportAsync);
            exportDelegate.BeginInvoke(path, container, null, null);
        }
        else
        {
            SaveAssemblyDelegate saveDelegate = new SaveAssemblyDelegate(SaveAsync);
            Debug.Log("Version String: " + Application.version);
            Debug.Log("Parsed Version String: " + float.Parse(Application.version, System.Globalization.CultureInfo.InvariantCulture.NumberFormat).ToString("F3"));
            saveDelegate.BeginInvoke(path, GameManager.Bond.BondName, tempBlockTransforms, tempBlockTimeStamps, float.Parse(Application.version, System.Globalization.CultureInfo.InvariantCulture.NumberFormat), null, null);
        }
    }

    public static void RemapBond(string newBond)
    {
        loadBond = newBond;
        remapBond = false;
    }

    public static void ClearLoadData()
    {
        loadContainer = null;
        loadBond = null;
        loadTimes = null;
        loadTimeOffset = 0;
        loadAsPointCloud = false;
        remapBond = false;
    }

    static void ExportAsync(string path, JsonMatrixContainer container)
    {
        Debug.Log("Start exporting at " + DateTime.Now.ToString("T"));
        using (StreamWriter file = File.CreateText(path))
        {
            JsonSerializer serializer = new JsonSerializer();
            serializer.Serialize(file, container);
        }

        Debug.Log("Finished exporting at " + DateTime.Now.ToString("T"));

        lock (lockKey)
        {
            currentSaveTargets.Remove(path);
            message = "File Exported!";
        }
    }

    static void SaveAsync(string path, string bondName, List<Matrix4x4>[] transforms, List<uint>[] timeStamps, float version)
    {
        Debug.Log("Start saving at " + DateTime.Now.ToString("T"));

        try
        {
            using (FileStream stream = new FileStream(path, FileMode.Create))
            {
                ByteStreamContainer container = new ByteStreamContainer(bondName, transforms, timeStamps, version);
                stream.Write(container.ToByteArray());
            }
        }
        catch (Exception e)
        {
            Debug.LogError(e.Message);
        }

        Debug.Log("Finished saving at " + DateTime.Now.ToString("T"));

        lock (lockKey)
        {
            currentSaveTargets.Remove(path);
            message = "File Saved!";
        }
    }

    public static void LoadAssemblyLayout(string path, bool _remapBond = false)
    {
        remapBond = _remapBond;
        LoadAssemblyDelegate loadDelegate = new LoadAssemblyDelegate(LoadAsync);
        loadDelegate.BeginInvoke(path, null, null);
    }

    public static void LoadAssemblyAsPointCloud(string path, bool _remapBond = false)
    {
        remapBond = _remapBond;
        loadAsPointCloud = true;
        LoadAssemblyLayout(path);
    }

    static Matrix4x4[][] LoadJsonMatrices(string path, out uint[][] times, out string bond, out float timeOffset)
    {
        Debug.Log("Loading started at " + DateTime.Now.ToString("T"));
        string jsonContent;

        using (StreamReader file = File.OpenText(path))
        {
            jsonContent = file.ReadToEnd();
        }

        Debug.Log("File read at " + DateTime.Now.ToString("T"));

        Matrix4x4[][] transforms = DeserializeFromText(jsonContent, out times, out bond, out timeOffset);

        Debug.Log("Bond Name: " + bond + ", Number of Parts: " + transforms.Length);

        for (int i = 0; i < transforms.Length; ++i)
            Debug.Log("Part " + i + " comes with " + transforms[i].Length + " blocks");

        return transforms;
    }

    static Matrix4x4[][] LoadByteMatrices(string path, out uint[][] times, out string bond, out float timeOffset)
    {
        Debug.Log("Loading started at " + DateTime.Now.ToString("T"));
        byte[] loadBytes = File.ReadAllBytes(path);

        Debug.Log("File read at " + DateTime.Now.ToString("T"));
        ByteStreamContainer container;
        
        try
        {
            container = new ByteStreamContainer(loadBytes);
        }
        catch (Exception e)
        {
            Debug.LogError(e.Message);
            bond = "";
            timeOffset = 0;
            times = null;
            return null;
        }

        times = container.times;

        bond = container.bondName;

        timeOffset = container.timeOffset;

        int loadAmount = 0;
        for (int i = 0; i < container.matrices.Length; ++i)
            loadAmount += container.matrices[i].Length;

        Debug.Log("Bond Name: " + bond + ", Matrices: " + loadAmount);

        return container.matrices;
    }

    static void LoadAsync(string path)
    {
        string bond;
        Matrix4x4[][] transforms;
        uint[][] times;
        float timeOffset;

        if (Path.GetExtension(path).ToLower() == ".json")
            transforms = LoadJsonMatrices(path, out times, out bond, out timeOffset);
        else
            transforms = LoadByteMatrices(path, out times, out bond, out timeOffset);

        Debug.Log("Transforms received! Bond: " + bond);

        if (transforms == null || transforms.Length == 0 || bond == null || bond == "")
        {
            MessageSystem.DisplayWarning("Loading Failed! Couldn't read file!");
            loadAsPointCloud = false;
            return;
        }
        
        lock (lockKey)
        {
            loadBond = bond;
            loadContainer = transforms;
            loadTimes = times;
            loadTimeOffset = timeOffset;

            loadComplete = true;
        }
        
        Debug.Log("Loading finished at " + DateTime.Now.ToString("T"));
    }

    static Matrix4x4[][] DeserializeFromText(string jsonContent, out uint[][] _times, out string bond, out float timeOffset)
    {
        Matrix4x4 currentMatrix = Matrix4x4.zero;

        uint startTime = (uint)(GameManager.gameTime * 10);
        uint currentTime = 0;
        timeOffset = 0;

        List<List<Matrix4x4>> matrices = new List<List<Matrix4x4>>();
        List<List<uint>> times = new List<List<uint>>();

        int partTypeCounter = 0;
        matrices.Add(new List<Matrix4x4>());
        times.Add(new List<uint>());

        bond = "";

        Newtonsoft.Json.JsonTextReader reader = new Newtonsoft.Json.JsonTextReader(new StringReader(jsonContent));
        while (reader.Read())
        {
            if (reader.Value != null)
            {
                if (reader.TokenType == Newtonsoft.Json.JsonToken.PropertyName)
                {
                    switch ((string)reader.Value)
                    {
                        case "m00":
                            {
                                currentMatrix.m00 = (float)reader.ReadAsDouble();
                                break;
                            }
                        case "m01":
                            {
                                currentMatrix.m01 = (float)reader.ReadAsDouble();
                                break;
                            }
                        case "m02":
                            {
                                currentMatrix.m02 = (float)reader.ReadAsDouble();
                                break;
                            }
                        case "m03":
                            {
                                currentMatrix.m03 = (float)reader.ReadAsDouble();
                                break;
                            }

                        case "m10":
                            {
                                currentMatrix.m10 = (float)reader.ReadAsDouble();
                                break;
                            }
                        case "m11":
                            {
                                currentMatrix.m11 = (float)reader.ReadAsDouble();
                                break;
                            }
                        case "m12":
                            {
                                currentMatrix.m12 = (float)reader.ReadAsDouble();
                                break;
                            }
                        case "m13":
                            {
                                currentMatrix.m13 = (float)reader.ReadAsDouble();
                                break;
                            }

                        case "m20":
                            {
                                currentMatrix.m20 = (float)reader.ReadAsDouble();
                                break;
                            }
                        case "m21":
                            {
                                currentMatrix.m21 = (float)reader.ReadAsDouble();
                                break;
                            }
                        case "m22":
                            {
                                currentMatrix.m22 = (float)reader.ReadAsDouble();
                                break;
                            }
                        case "m23":
                            {
                                currentMatrix.m23 = (float)reader.ReadAsDouble();
                                break;
                            }

                        case "m30":
                            {
                                currentMatrix.m30 = (float)reader.ReadAsDouble();
                                break;
                            }
                        case "m31":
                            {
                                currentMatrix.m31 = (float)reader.ReadAsDouble();
                                break;
                            }
                        case "m32":
                            {
                                currentMatrix.m32 = (float)reader.ReadAsDouble();
                                break;
                            }
                        case "m33":
                            {
                                currentMatrix.m33 = (float)reader.ReadAsDouble();
                                break;
                            }
                        case "time":
                            {
                                currentTime = (uint)reader.ReadAsInt32();
                                break;
                            }
                        case "bondName":
                            {
                                bond = reader.ReadAsString();
                                Debug.Log("Bond read as: " + bond);
                                break;
                            }
                    }
                }
            }
            else
            {
                if (reader.TokenType == Newtonsoft.Json.JsonToken.EndObject)
                {
                    if (currentMatrix != Matrix4x4.zero)
                    {
                        matrices[partTypeCounter].Add(conversionMatrix * currentMatrix);
                        times[partTypeCounter].Add(currentTime + startTime);

                        if (currentTime / 10f > timeOffset)
                            timeOffset = currentTime / 10f;

                        currentMatrix = Matrix4x4.zero;
                    }
                }
                if (reader.TokenType == Newtonsoft.Json.JsonToken.EndArray)
                {
                    matrices.Add(new List<Matrix4x4>());
                    times.Add(new List<uint>());
                    ++partTypeCounter;
                }
            }
        }

        Debug.Log("Everything read");

        int bondPartCount = GameManager.AvailableBonds[bond].PartPrefabs.Length;

        for (int i = matrices.Count - 1; i >= bondPartCount; --i)
        {
            matrices.RemoveAt(i);
            times.RemoveAt(i);
        }

        Matrix4x4[][] _matrices = new Matrix4x4[matrices.Count][];
        _times = new uint[times.Count][];

        for (int i = 0; i < matrices.Count; ++i)
        {
            _matrices[i] = matrices[i].ToArray();
            _times[i] = times[i].ToArray();
        }

        return _matrices;
    }
    
    public static void NewGame()
    {
        if (BlockContainer.Container == null) return;

        BlockContainer.Container.ResetContainer();
        BlockContainer.Container.ResetCloudContainer();
        GameManager.Bond = null;
        GameManager.gameTime = 0;
        //PointCloudIO.RecalculatePointCloud();
    }   
}

public delegate void ExportAssemblyDelegate(string path, JsonMatrixContainer container);
public delegate void SaveAssemblyDelegate(string path, string bondName, List<Matrix4x4>[] matrices, List<uint>[] timeStamps, float version);
public delegate void LoadAssemblyDelegate(string path);

public class JsonMatrixContainer
{
    public JsonMatrix[][] matrices;
    public string bondName;

    public JsonMatrixContainer()
    {

    }

    public JsonMatrixContainer(string _bondName)
    {
        bondName = _bondName;
    }

    public JsonMatrixContainer(string _bondName, List<Matrix4x4>[] _matrices, List<uint>[] _times)
    {
        bondName = _bondName;
        matrices = new JsonMatrix[_matrices.Length][];

        for (int i = 0; i < matrices.Length; ++i)
        {
            matrices[i] = new JsonMatrix[_matrices[i].Count];
            for (int j = 0; j < matrices[i].Length; ++j)
                matrices[i][j] = new JsonMatrix(_matrices[i][j], _times[i][j]);
        }
    }
}


public class JsonMatrix
{
    public float m00;
    public float m01;
    public float m02;
    public float m03;

    public float m10;
    public float m11;
    public float m12;
    public float m13;

    public float m20;
    public float m21;
    public float m22;
    public float m23;

    public float m30;
    public float m31;
    public float m32;
    public float m33;

    public uint time;

    public Matrix4x4 ToMatrix4x4()
    {
        return new Matrix4x4(new Vector4(m00, m10, m20, m30), new Vector4(m01, m11, m21, m31), new Vector4(m02, m12, m22, m32), new Vector4(m03, m13, m23, m33));
    }

    public JsonMatrix(Matrix4x4 matrix, uint _time)
    {
        m00 = matrix.m00;
        m01 = matrix.m01;
        m02 = matrix.m02;
        m03 = matrix.m03;

        m10 = matrix.m10;
        m11 = matrix.m11;
        m12 = matrix.m12;
        m13 = matrix.m13;

        m20 = matrix.m20;
        m21 = matrix.m21;
        m22 = matrix.m22;
        m23 = matrix.m23;

        m30 = matrix.m30;
        m31 = matrix.m31;
        m32 = matrix.m32;
        m33 = matrix.m33;

        time = _time;
    }
}
