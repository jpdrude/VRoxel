using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using Newtonsoft.Json;
using LegoAssembly;
using System;

public class LegoIO : MonoBehaviour
{
    static Matrix4x4 conversionMatrix = new Matrix4x4(new Vector4(1, 0, 0, 0), new Vector4(0, 0, 1, 0), new Vector4(0, 1, 0, 0), new Vector4(0, 0, 0, 1));

    static Matrix4x4[] loadContainer = null;
    static string loadBond = null;

    static List<string> currentSaveTargets = new List<string>();

    static object lockKey = new object();

    private void Update()
    {

        if (loadContainer != null)
        {
            if (loadBond != GameManager.Bond.name)
            {
                if (LegoContainer.Container.GetCount() < 1000 && GameManager.AvailableBonds.ContainsKey(loadBond))
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

            LegoContainer.Container.AddBricks(loadContainer);
            loadContainer = null;
            loadBond = null;
        }
    }

    public static void SaveLegoLayout(string path)
    {
        if (currentSaveTargets.Contains(path))
        {
            MessageSystem.DisplayWarning("There is already a save process in progress to this path!");
            return;
        }

        JsonMatrixContainer container = new JsonMatrixContainer(GameManager.Bond.name);

        foreach (Matrix4x4 matrix4x4 in LegoContainer.Container.TransformArray)
        {
            if (matrix4x4 != Matrix4x4.zero)
                container.matrices.Add(new JsonMatrix(conversionMatrix * matrix4x4));
        }

        currentSaveTargets.Add(path);
        SaveLegoDelegate saveDelegate = new SaveLegoDelegate(SaveAsync);
        saveDelegate.BeginInvoke(path, container, null, null);
    }

    static void SaveAsync(string path, JsonMatrixContainer container)
    {
        using (StreamWriter file = File.CreateText(path))
        {
            JsonSerializer serializer = new JsonSerializer();
            serializer.Serialize(file, container);
        }

        lock (lockKey)
            currentSaveTargets.Remove(path);
    }

    public static void LoadLegoLayout(string path)
    {
        LoadLegoDelegate loadDelegate = new LoadLegoDelegate(LoadAsync);
        loadDelegate.BeginInvoke(path, null, null);
    }

    static void LoadAsync(string path)
    {
        JsonMatrixContainer container = new JsonMatrixContainer();

        try
        {
            using (StreamReader file = File.OpenText(path))
            {
                JsonSerializer serializer = new JsonSerializer();
                container = (JsonMatrixContainer)serializer.Deserialize(file, typeof(JsonMatrixContainer));
            }
        }
        catch
        {
            lock (lockKey)
                MessageSystem.DisplayWarning("Loading failed: File format not recognized");
        }

        Matrix4x4[] matrices = new Matrix4x4[container.matrices.Count];

        for (int i = 0; i < container.matrices.Count; ++i)
        {
            matrices[i] = conversionMatrix * container.matrices[i].ToMatrix4x4();
        }

        lock (lockKey)
        {
            loadContainer = matrices;
            loadBond = container.bondName;
        }
    }
    
    public static void NewGame()
    {
        LegoContainer.Container.ResetContainer();
    }

    public static void ExportLegoLayoutToAssembly(string path)
    {
        Assembly assembly = SceneToAssembly();
        ExportAssembly.Export(path, assembly);
    }

    static Assembly SceneToAssembly()
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
        List<Brick> bricks = new List<Brick>();
        foreach (Matrix4x4 matrix in matrices)
        {
            Matrix4x4 matrix4x4 = conversionMatrix * matrix;
            if (Mathf.Abs(matrix4x4.rotation.eulerAngles.y) < 0.001f)
            {
                int posX = (int)System.Math.Round((matrix4x4.m03 - offset.X) / GameManager.xzSpacing) - 1;
                int posY = (int)System.Math.Round((matrix4x4.m13 - offset.Y) / GameManager.xzSpacing) - 2;
                int posZ = (int)System.Math.Round((matrix4x4.m23 - offset.Z) / GameManager.ySpacing);

                intVector size = new intVector(2, 4, 1);

                if (posX < 0 || posY < 0 || posZ < 0) continue;

                bricks.Add(new Brick(new intVector(posX, posY, posZ), size, offset));
            }
            else
            {
                int posX = (int)System.Math.Round((matrix4x4.m03 - offset.X) / GameManager.xzSpacing) - 2;
                int posY = (int)System.Math.Round((matrix4x4.m13 - offset.Y) / GameManager.xzSpacing) - 1;
                int posZ = (int)System.Math.Round((matrix4x4.m23 - offset.Z) / GameManager.ySpacing);

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
            if (LegoContainer.Container.LegoDictionary.ContainsKey(new DictPosition(brick.GetPosition())))
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
        max = GameManager.Bond.GetBondBase(max) + new Vector3(GameManager.Bond.XSize * GameManager.xzSpacing, GameManager.Bond.YSize * GameManager.ySpacing, GameManager.Bond.ZSize * GameManager.xzSpacing);

        Vector3 minConvert = conversionMatrix * min;
        Vector3 maxConvert = conversionMatrix * max;

        minLego = new intVector(0, 0, 0);
        offset = new doubleVector(minConvert.x, minConvert.y, minConvert.z);
    }

    static void EvaluateTransformArray(out Vector3 min, out Vector3 max)
    {
        min = Vector3.one * float.MaxValue;
        max = Vector3.one * float.MinValue;

        

        foreach (Matrix4x4 matrix4x4 in LegoContainer.Container.TransformArray)
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

        min -= new Vector3(5 * GameManager.Bond.XSize * GameManager.xzSpacing, 5 * GameManager.Bond.YSize * GameManager.ySpacing, 5 * GameManager.Bond.ZSize * GameManager.xzSpacing);
    }
}

public delegate void SaveLegoDelegate(string path, JsonMatrixContainer container);
public delegate void LoadLegoDelegate(string path);

public class JsonMatrixContainer
{
    public List<JsonMatrix> matrices = new List<JsonMatrix>();
    public string bondName;

    public JsonMatrixContainer()
    {

    }

    public JsonMatrixContainer(string _bondName)
    {
        bondName = _bondName;
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
