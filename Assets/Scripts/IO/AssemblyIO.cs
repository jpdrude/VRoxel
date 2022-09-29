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

    static Matrix4x4[] loadContainer = null;
    static string loadBond = null;
    static bool loadAsPointCloud = false;

    static List<string> currentSaveTargets = new List<string>();

    static object lockKey = new object();

    static string message = null;

    private void Update()
    {

        if (loadContainer != null)
        {
            if (loadBond != GameManager.Bond.name)
            {
                if (ModuleContainer.Container.GetCount() < 1000 && GameManager.AvailableBonds.ContainsKey(loadBond))
                {
                    NewGame();
                    GameManager.Bond = GameManager.AvailableBonds[loadBond];
                }
                else
                {
                    MessageSystem.DisplayWarning("File could not be loaded, because of incompatible Bond");
                    MessageSystem.DisplayWarning("Session Bond: " + GameManager.Bond.name + ", File Bond: " + loadBond);
                    loadBond = null;
                    loadContainer = null;
                    return;
                }
            }
            if (!loadAsPointCloud)
                ModuleContainer.Container.AddBricks(loadContainer);
            else
            {
                ModuleContainer.Container.AddBricks(loadContainer, null, true);
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

    public static void SaveAssemblyLayout(string path, bool saveInactive, bool export = false)
    {
        if (currentSaveTargets.Contains(path))
        {
            MessageSystem.DisplayWarning("There is already a save process in progress to this path!");
            return;
        }

        List<Matrix4x4> tempMatrices = new List<Matrix4x4>();

        if (ModuleContainer.Container.PcTransforms != null && saveInactive)
        {
            foreach (Matrix4x4 matrix4x4 in ModuleContainer.Container.PcTransforms)
            {
                if (matrix4x4 != Matrix4x4.zero)
                    tempMatrices.Add(conversionMatrix * matrix4x4);
            }
        }

        foreach (Matrix4x4 matrix4x4 in ModuleContainer.Container.TransformArray)
        {
            if (matrix4x4 != Matrix4x4.zero)
                tempMatrices.Add(conversionMatrix * matrix4x4);
        }

        if (export)
        {
            currentSaveTargets.Add(path);
            JsonMatrixContainer container = new JsonMatrixContainer(GameManager.Bond.name, tempMatrices);

            ExportAssemblyDelegate exportDelegate = new ExportAssemblyDelegate(ExportAsync);
            exportDelegate.BeginInvoke(path, container, null, null);
        }
        else
        {
            SaveAssemblyDelegate saveDelegate = new SaveAssemblyDelegate(SaveAsync);
            saveDelegate.BeginInvoke(path, GameManager.Bond.name, tempMatrices, null, null);
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

    static void SaveAsync(string path, string bondName, List<Matrix4x4> matrices)
    {
        Debug.Log("Start saving at " + DateTime.Now.ToString("T"));
        using (FileStream stream = new FileStream(path, FileMode.Create))
        {
            ByteStreamContainer container = new ByteStreamContainer(bondName, matrices);
            stream.Write(container.ToByteArray());
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

    static Matrix4x4[] LoadJsonMatrices(string path, out string bond)
    {
        Debug.Log("Loading started at " + DateTime.Now.ToString("T"));
        string jsonContent;

        using (StreamReader file = File.OpenText(path))
        {
            jsonContent = file.ReadToEnd();
        }

        Debug.Log("File read at " + DateTime.Now.ToString("T"));

        Matrix4x4[] matrices = DeserializeFromText(jsonContent, out bond).ToArray();

        Debug.Log("Bond Name: " + bond + ", Matrices: " + matrices.Length);

        return matrices;
    }

    static Matrix4x4[] LoadByteMatrices(string path, out string bond)
    {
        Debug.Log("Loading started at " + DateTime.Now.ToString("T"));
        byte[] loadBytes = File.ReadAllBytes(path);

        Debug.Log("File read at " + DateTime.Now.ToString("T"));

        ByteStreamContainer container = new ByteStreamContainer(loadBytes);

        bond = container.bondName;

        Debug.Log("Bond Name: " + bond + ", Matrices: " + container.matrices.Length);

        return container.matrices;
    }

    static void LoadAsync(string path)
    {
        string bond;
        Matrix4x4[] matrices;
        if (Path.GetExtension(path).ToLower() == ".json")
            matrices = LoadJsonMatrices(path, out bond);
        else
            matrices = LoadByteMatrices(path, out bond);

        if (matrices == null || matrices.Length == 0 || bond == null || bond == "")
        {
            MessageSystem.DisplayWarning("Loading Failed! Couldn't read file!");
            loadAsPointCloud = false;
            return;
        }

        lock (lockKey)
        {
            loadBond = bond;
            loadContainer = matrices;
        }

        Debug.Log("Loading finished at " + DateTime.Now.ToString("T"));
    }

    static List<Matrix4x4> DeserializeFromText(string jsonContent, out string bond)
    {
        Matrix4x4 currentMatrix = Matrix4x4.zero;
        List<Matrix4x4> matrices = new List<Matrix4x4>();
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
                        matrices.Add(conversionMatrix * currentMatrix);
                        currentMatrix = Matrix4x4.zero;
                    }
                }
            }
        }

        return matrices;
    }
    
    public static void NewGame()
    {
        if (ModuleContainer.Container == null) return;
        ModuleContainer.Container.ResetContainer();
    }

    public static void ExportAssemblyLayoutToLegoAssembly(string path)
    {
        Assembly assembly = SceneToLegoAssembly();
        ExportAssembly.Export(path, assembly);
    }

    static Assembly SceneToLegoAssembly()
    {
        Vector3 boundsMin;
        Vector3 boundsMax;

        intVector boundsMinLego;
        intVector boundsMaxLego;
        doubleVector offset;

        Matrix4x4[] allBrickMatrices;

        List<Matrix4x4> rawBrickMatrices;
        List<Matrix4x4> rawPotentialBrickMatrices;

        List<Brick> rawBricks;
        List<Brick> rawPotentialBricks;

        Dictionary<int, List<Brick>> bricks;
        Dictionary<int, List<Brick>> potentialBricks;

        Assembly assembly;

        EvaluateTransformArray(out boundsMin, out boundsMax);

        BoundsToLegoSpace(ref boundsMin, ref boundsMax, out boundsMinLego, out offset);

        allBrickMatrices = GameManager.Bond.GetBricksInBounds(boundsMin, boundsMax);

        SplitBricks(allBrickMatrices, out rawBrickMatrices, out rawPotentialBrickMatrices);

        rawBricks = MatrixTo4x2Brick(rawBrickMatrices, offset);
        rawPotentialBricks = MatrixTo4x2Brick(rawPotentialBrickMatrices, offset);

        boundsMaxLego = RecomputeMax(rawBricks, rawPotentialBricks);

        bricks = SortBricks(rawBricks);
        potentialBricks = SortBricks(rawPotentialBricks);

        Debug.Log("min: " + boundsMinLego + ", max: " + boundsMaxLego + " Bricks Count: " + rawBricks.Count);

        assembly = new Assembly(bricks, potentialBricks, boundsMinLego, boundsMaxLego);

        return assembly;
    }

    static Dictionary<int, List<Brick>> SortBricks(List<Brick> bricks)
    {
        Dictionary<int, Dictionary<int, List<Brick>>> brickSorting = new Dictionary<int, Dictionary<int, List<Brick>>>();

        foreach (Brick brick in bricks)
        {
            if (brickSorting.ContainsKey(brick.Pos.Z))
            {
                if (brickSorting[brick.Pos.Z].ContainsKey(brick.Pos.Y))
                {
                    if (brickSorting[brick.Pos.Z][brick.Pos.Y] == null)
                        brickSorting[brick.Pos.Z][brick.Pos.Y] = new List<Brick>();

                    brickSorting[brick.Pos.Z][brick.Pos.Y].Add(brick);
                }
                else
                {
                    List<Brick> tempList = new List<Brick>();
                    tempList.Add(brick);
                    brickSorting[brick.Pos.Z].Add(brick.Pos.Y, tempList);
                }
            }
            else
            {
                List<Brick> tempList = new List<Brick>();
                tempList.Add(brick);
                Dictionary<int, List<Brick>> tempDict = new Dictionary<int, List<Brick>>();
                tempDict.Add(brick.Pos.Y, tempList);
                brickSorting.Add(brick.Pos.Z, tempDict);
            }
        }

        Dictionary<int, List<Brick>> sortedBricks = new Dictionary<int, List<Brick>>();
        foreach (int z in brickSorting.Keys)
        {
            List<Brick> tempList = new List<Brick>();
            List<int> sortY = new List<int>();

            foreach (int y in brickSorting[z].Keys)
                sortY.Add(y);

            sortY.Sort();

            foreach(int y in sortY)
            {
                brickSorting[z][y].Sort();
                tempList.AddRange(brickSorting[z][y]);
            }

            sortedBricks.Add(z, tempList);
        }

        return sortedBricks;
    }

    static intVector RecomputeMax(List<Brick> bricks, List<Brick> potentialBricks)
    {
        intVector max = new intVector(int.MinValue, int.MinValue, int.MinValue);

        foreach(Brick b in bricks)
        {
            if (b.Pos.X + b.Size.X > max.X)
                max.X = b.Pos.X + b.Size.X;

            if (b.Pos.Y + b.Size.Y > max.Y)
                max.Y = b.Pos.Y + b.Size.Y;

            if (b.Pos.Z + b.Size.Z > max.Z)
                max.Z = b.Pos.Z + b.Size.Z;
        }

        foreach (Brick b in potentialBricks)
        {
            if (b.Pos.X + b.Size.X > max.X)
                max.X = b.Pos.X + b.Size.X;

            if (b.Pos.Y + b.Size.Y > max.Y)
                max.Y = b.Pos.Y + b.Size.Y;

            if (b.Pos.Z + b.Size.Z > max.Z)
                max.Z = b.Pos.Z + b.Size.Z;
        }

        return max;
    }

    static List<Brick> MatrixTo4x2Brick(List<Matrix4x4> matrices, doubleVector offset)
    {
        float xSpacing = GameManager.Bond.XSpacing;
        float ySpacing = GameManager.Bond.YSpacing;
        float zSpacing = GameManager.Bond.ZSpacing;

        List<Brick> bricks = new List<Brick>();
        foreach (Matrix4x4 matrix in matrices)
        {
            Matrix4x4 matrix4x4 = conversionMatrix * matrix;
            if (Mathf.Abs(matrix4x4.rotation.eulerAngles.y) < 0.001f)
            {
                int posX = (int)System.Math.Round((matrix4x4.m03 - offset.X) / xSpacing) - 1;
                int posY = (int)System.Math.Round((matrix4x4.m13 - offset.Y) / zSpacing) - 2;
                int posZ = (int)System.Math.Round((matrix4x4.m23 - offset.Z) / ySpacing);

                intVector size = new intVector(2, 4, 1);

                if (posX < 0 || posY < 0 || posZ < 0) continue;

                bricks.Add(new Brick(new intVector(posX, posY, posZ), size, offset));
            }
            else
            {
                int posX = (int)System.Math.Round((matrix4x4.m03 - offset.X) / xSpacing) - 2;
                int posY = (int)System.Math.Round((matrix4x4.m13 - offset.Y) / zSpacing) - 1;
                int posZ = (int)System.Math.Round((matrix4x4.m23 - offset.Z) / ySpacing);

                intVector size = new intVector(4, 2, 1);

                if (posX < 0 || posY < 0 || posZ < 0) continue;

                bricks.Add(new Brick(new intVector(posX, posY, posZ), size, offset));
            }
        }

        return bricks;
    }

    static void SplitBricks(Matrix4x4[] allBricks, out List<Matrix4x4> bricks, out List<Matrix4x4> potentialBricks)
    {
        bricks = new List<Matrix4x4>();
        potentialBricks = new List<Matrix4x4>();

        foreach(Matrix4x4 brick in allBricks)
        {
            if (ModuleContainer.Container.ModuleDictionary.ContainsKey(new DictPosition(brick.GetPosition())))
            {
                bricks.Add(brick);
            }
            else
            {
                potentialBricks.Add(brick);
            }
        }
    }

    static void BoundsToLegoSpace(ref Vector3 min, ref Vector3 max, out intVector minLego, out doubleVector offset)
    {
        min = GameManager.Bond.GetBondBase(min);
        max = GameManager.Bond.GetBondBase(max) + new Vector3(GameManager.Bond.XSize * GameManager.Bond.XSpacing, GameManager.Bond.YSize * GameManager.Bond.YSpacing, GameManager.Bond.ZSize * GameManager.Bond.ZSpacing);

        Vector3 minConvert = conversionMatrix * min;
        Vector3 maxConvert = conversionMatrix * max;

        minLego = new intVector(0, 0, 0);
        offset = new doubleVector(minConvert.x, minConvert.y, minConvert.z);
    }

    static void EvaluateTransformArray(out Vector3 min, out Vector3 max)
    {
        min = Vector3.one * float.MaxValue;
        max = Vector3.one * float.MinValue;

        foreach (Matrix4x4 matrix4x4 in ModuleContainer.Container.TransformArray)
        {
            if (matrix4x4 == Matrix4x4.zero) continue;

            if (matrix4x4.m03 < min.x)
                min.x = matrix4x4.m03;
            else if (matrix4x4.m03 > max.x)
                max.x = matrix4x4.m03;

            if (matrix4x4.m13 < min.y)
                min.y = matrix4x4.m13;
            else if (matrix4x4.m13 > max.y)
                max.y = matrix4x4.m13;

            if (matrix4x4.m23 < min.z)
                min.z = matrix4x4.m23;
            else if (matrix4x4.m23 > max.z)
                max.z = matrix4x4.m23;
        }

        min -= new Vector3(5 * GameManager.Bond.XSize * GameManager.Bond.XSpacing, 5 * GameManager.Bond.YSize * GameManager.Bond.YSpacing, 5 * GameManager.Bond.ZSize * GameManager.Bond.ZSpacing);
    }
}

public delegate void ExportAssemblyDelegate(string path, JsonMatrixContainer container);
public delegate void SaveAssemblyDelegate(string path, string bondName, List<Matrix4x4> matrices);
public delegate void LoadAssemblyDelegate(string path);

public class JsonMatrixContainer
{
    public JsonMatrix[] matrices;
    public string bondName;

    public JsonMatrixContainer()
    {

    }

    public JsonMatrixContainer(string _bondName)
    {
        bondName = _bondName;
    }

    public JsonMatrixContainer(string _bondName, List<Matrix4x4> _matrices)
    {
        bondName = _bondName;
        matrices = new JsonMatrix[_matrices.Count];

        for (int i = 0; i < matrices.Length; ++i)
            matrices[i] = new JsonMatrix(_matrices[i]);
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
