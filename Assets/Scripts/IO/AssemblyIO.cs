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

public class AssemblyIO : MonoBehaviour
{
    static Matrix4x4 conversionMatrix = new Matrix4x4(new Vector4(1, 0, 0, 0), new Vector4(0, 0, 1, 0), new Vector4(0, 1, 0, 0), new Vector4(0, 0, 0, 1));

    static Matrix4x4[][] loadContainer = null;
    static string loadBond = null;
    static bool loadAsPointCloud = false;

    static List<string> currentSaveTargets = new List<string>();

    static object lockKey = new object();

    static string message = null;

    private void Update()
    {

        if (loadContainer != null)
        {
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
                        MessageSystem.DisplayWarning("File could not be loaded, because of unknown Bond");
                    }
                }
                else
                {
                    MessageSystem.DisplayWarning("File could not be loaded, because of incompatible Bond");
                    MessageSystem.DisplayWarning("Session Bond: " + GameManager.Bond.BondName + ", File Bond: " + loadBond);
                    loadBond = null;
                    loadContainer = null;
                    return;
                }
            }

            if (!loadAsPointCloud)
                BlockContainer.Container.AddBlocks(loadContainer);
            else
            {
                BlockContainer.Container.AddBlocks(loadContainer, null, true);
                loadAsPointCloud = false;
            }
            loadContainer = null;
            loadBond = null;
        }

        if (message != null)
        {
            MessageSystem.DisplayMessage(message);
            message = null;
        }
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
        for (int i = 0; i < tempBlockTransforms.Length; ++i)
            tempBlockTransforms[i] = new List<Matrix4x4>();

        if (BlockContainer.Container.PcTransformArray != null && saveInactive)
        {
            //foreach (Matrix4x4 matrix4x4 in ModuleContainer.Container.PcTransforms)
            for (int i = 0; i < tempBlockTransforms.Length; ++i)
                foreach(Matrix4x4 matrix4x4 in BlockContainer.Container.PcTransformArray[i])
                {
                    if (matrix4x4 != Matrix4x4.zero)
                        tempBlockTransforms[i].Add(matrix4x4);
                }
        }

        for (int i = 0; i < tempBlockTransforms.Length; ++i)
            foreach (Matrix4x4 matrix4x4 in BlockContainer.Container.TransformArray[i])
            {
                if (matrix4x4 != Matrix4x4.zero)
                    tempBlockTransforms[i].Add(matrix4x4);
            }

        if (export)
        {
            currentSaveTargets.Add(path);
            JsonMatrixContainer container = new JsonMatrixContainer(GameManager.Bond.BondName, tempBlockTransforms);

            ExportAssemblyDelegate exportDelegate = new ExportAssemblyDelegate(ExportAsync);
            exportDelegate.BeginInvoke(path, container, null, null);
        }
        else
        {
            SaveAssemblyDelegate saveDelegate = new SaveAssemblyDelegate(SaveAsync);
            Debug.Log("Version String: " + Application.version);
            Debug.Log("Parsed Version String: " + float.Parse(Application.version, System.Globalization.CultureInfo.InvariantCulture.NumberFormat).ToString("F3"));
            saveDelegate.BeginInvoke(path, GameManager.Bond.BondName, tempBlockTransforms, float.Parse(Application.version, System.Globalization.CultureInfo.InvariantCulture.NumberFormat), null, null);
        }
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

    static void SaveAsync(string path, string bondName, List<Matrix4x4>[] transforms, float version)
    {
        Debug.Log("Start saving at " + DateTime.Now.ToString("T"));

        try
        {
            using (FileStream stream = new FileStream(path, FileMode.Create))
            {
                ByteStreamContainer container = new ByteStreamContainer(bondName, transforms, version);
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

    public static void LoadAssemblyLayout(string path)
    {
        LoadAssemblyDelegate loadDelegate = new LoadAssemblyDelegate(LoadAsync);
        loadDelegate.BeginInvoke(path, null, null);
    }

    public static void LoadAssemblyAsPointCloud(string path)
    {
        loadAsPointCloud = true;
        LoadAssemblyLayout(path);
    }

    static Matrix4x4[][] LoadJsonMatrices(string path, out string bond)
    {
        Debug.Log("Loading started at " + DateTime.Now.ToString("T"));
        string jsonContent;

        using (StreamReader file = File.OpenText(path))
        {
            jsonContent = file.ReadToEnd();
        }

        Debug.Log("File read at " + DateTime.Now.ToString("T"));

        Matrix4x4[][] transforms = DeserializeFromText(jsonContent, out bond);

        Debug.Log("Bond Name: " + bond + ", Number of Parts: " + transforms.Length);

        for (int i = 0; i < transforms.Length; ++i)
            Debug.Log("Part " + i + " comes with " + transforms[i].Length + " blocks");

        return transforms;
    }

    static Matrix4x4[][] LoadByteMatrices(string path, out string bond)
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
            return null;
        }

        bond = container.bondName;
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
        if (Path.GetExtension(path).ToLower() == ".json")
            transforms = LoadJsonMatrices(path, out bond);
        else
            transforms = LoadByteMatrices(path, out bond);

        Debug.Log("Transforms received! Bond: " + bond);

        if (transforms == null || transforms.Length == 0 || bond == null || bond == "" || !GameManager.AvailableBonds.ContainsKey(bond))
        {
            MessageSystem.DisplayWarning("Loading Failed! Couldn't read file!");
            loadAsPointCloud = false;
            return;
        }
        
        lock (lockKey)
        {
            loadBond = bond;
            loadContainer = transforms;
        }
        
        Debug.Log("Loading finished at " + DateTime.Now.ToString("T"));
    }

    static Matrix4x4[][] DeserializeFromText(string jsonContent, out string bond)
    {
        Matrix4x4 currentMatrix = Matrix4x4.zero;
        List<List<Matrix4x4>> matrices = new List<List<Matrix4x4>>();
        int partTypeCounter = 0;
        matrices.Add(new List<Matrix4x4>());

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
                        currentMatrix = Matrix4x4.zero;
                    }
                }
                if (reader.TokenType == Newtonsoft.Json.JsonToken.EndArray)
                {
                    matrices.Add(new List<Matrix4x4>());
                    ++partTypeCounter;
                }
            }
        }

        Debug.Log("Everything read");

        int bondPartCount = GameManager.AvailableBonds[bond].PartPrefabs.Length;

        for (int i = matrices.Count - 1; i >= bondPartCount; --i)
        {
            matrices.RemoveAt(i);
        }

        Matrix4x4[][] _matrices = new Matrix4x4[matrices.Count][];
        for (int i = 0; i < matrices.Count; ++i)
            _matrices[i] = matrices[i].ToArray();

        return _matrices;
    }
    
    public static void NewGame()
    {
        if (BlockContainer.Container == null) return;

        BlockContainer.Container.ResetContainer();
        PointCloudIO.RestorePointCloud();
    }   
}

public delegate void ExportAssemblyDelegate(string path, JsonMatrixContainer container);
public delegate void SaveAssemblyDelegate(string path, string bondName, List<Matrix4x4>[] matrices, float version);
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

    public JsonMatrixContainer(string _bondName, List<Matrix4x4>[] _matrices)
    {
        bondName = _bondName;
        matrices = new JsonMatrix[_matrices.Length][];

        for (int i = 0; i < matrices.Length; ++i)
        {
            matrices[i] = new JsonMatrix[_matrices[i].Count];
            for (int j = 0; j < matrices[i].Length; ++j)
                matrices[i][j] = new JsonMatrix(_matrices[i][j]);
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

    public Matrix4x4 ToMatrix4x4()
    {
        return new Matrix4x4(new Vector4(m00, m10, m20, m30), new Vector4(m01, m11, m21, m31), new Vector4(m02, m12, m22, m32), new Vector4(m03, m13, m23, m33));
    }

    public JsonMatrix(Matrix4x4 matrix)
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
    }
}
