using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.IO;
using Newtonsoft.Json;

public class KitBashIO : MonoBehaviour
{
    static KitBashIO instance;

    [SerializeField]
    Button btn_Distribute;

    [SerializeField]
    Material brushMaterial;

    public static bool FixTransform { get; set; }

    static Dictionary<string, GameObject> palette;
    public static Dictionary<string, GameObject> Palette 
    {
        get { return palette; }
        set
        {
            palette = value;

            if (transforms == null) return;

            foreach(string key in transforms.Keys)
                if (!palette.ContainsKey(key))
                    return;

            instance.btn_Distribute.interactable = true;
        }
    }
    static Dictionary<string, List<Matrix4x4>> transforms;
    public static Dictionary<string, List<Matrix4x4>> Transforms
    {
        get { return transforms; }
        set
        {
            transforms = value;

            if (palette == null) return;

            foreach (string key in transforms.Keys)
                if (!palette.ContainsKey(key))
                    return;

            instance.btn_Distribute.interactable = true;
        }
    }

    static GameObject paletteAsset;

    public static bool Distribute { get; set; }

    public static Vector3 Offset { get; set; }

    DoAddStroke addStroke = new DoAddStroke(DoAction.DoType.AddStrocke);

    private void Start()
    {
        instance = this;
    }

    private void Update()
    {
        if (!Distribute)
            return;

        if (palette.Keys.Count <= 0)
        {
            UndoStack.Push(addStroke);
            addStroke = new DoAddStroke(DoAction.DoType.AddStrocke);

            Distribute = false;
            DestroyKitBashContext();
            return;
        }


        string[] keys = new string[palette.Keys.Count];
        palette.Keys.CopyTo(keys, 0);
        string key = keys[0];

        Debug.Log("Should Distribute Key: " + key);

        GameObject brush = palette[key];

        if (transforms[key].Count <= 0)
        {
            palette.Remove(key);
            transforms.Remove(key);

            Destroy(brush);

            return;
        }

        brush.SetActive(true);
        brush.transform.position = transforms[key][0].GetPosition() + Offset;

        brush.transform.rotation = transforms[key][0].GetRotation();

        brush.transform.localScale = transforms[key][0].GetScale();

        if (FixTransform)
            brush.transform.RotateAround(Vector3.zero, Vector3.up, 180);

        Collider brushCollider = brush.GetComponent<MeshCollider>();

        PositionKey[][] blockDictKeys;
        Matrix4x4[][] blocks = GameManager.Bond.GetBlocksInMesh(brushCollider, out blockDictKeys);
        BlockContainer.Container.AddBlocks(blocks, blockDictKeys, null, false, true);


        addStroke.AppendPartialStroke(blocks, blockDictKeys, null, null, (uint)(GameManager.gameTime * 10));

        transforms[key].RemoveAt(0);
    }

    public static void DestroyKitBashContext()
    {
        if (transforms != null)
            transforms.Clear();

        if (palette != null)
        {
            foreach (GameObject go in palette.Values)
                Destroy(go);

            palette.Clear();
        }

        /*
        if (paletteAsset != null)
            Destroy(paletteAsset);
        */
    }

    public static void SetPalette(GameObject paletteRoot)
    {
        paletteAsset = paletteRoot;

        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        Dictionary<string, GameObject> tempPalette = new Dictionary<string, GameObject>();

        foreach (MeshFilter mf in paletteRoot.GetComponentsInChildren<MeshFilter>())
        {
            string name = mf.gameObject.name;

            if (tempPalette.ContainsKey(name))
                continue;

            GameObject go = new GameObject();
            go.name = name;

            MeshFilter _mf = go.AddComponent<MeshFilter>();
            _mf.sharedMesh = mf.sharedMesh;

            MeshRenderer _mr = go.AddComponent<MeshRenderer>();
            _mr.material = instance.brushMaterial;


            MeshCollider _mc = go.AddComponent<MeshCollider>();
            _mc.sharedMesh = mf.sharedMesh;

            tempPalette.Add(name, go);

            go.tag = "MeshSpawner";

            go.layer = 8;

            go.SetActive(false);

            sb.Append(name + ", ");
        }

        Palette = tempPalette;

        paletteRoot.SetActive(false);

        Debug.Log("KitBash Palette Imported!");
        Debug.Log("Palette Names: " + sb.ToString());
        MessageSystem.DisplayMessage("KitBash Palette Imported!");
    }
}

public class SaveContainer
{
    public Dictionary<string, List<SaveObject>> assembly;

    public SaveContainer()
    {
        assembly = new Dictionary<string, List<SaveObject>>();
    }

    public static Dictionary<string, List<Matrix4x4>> LoadAssembly(string path)
    {
        SaveContainer container;

        using (StreamReader file = File.OpenText(path))
        {
            JsonSerializer serializer = new JsonSerializer();
            container = (SaveContainer)serializer.Deserialize(file, typeof(SaveContainer));
        }

        Dictionary<string, List<Matrix4x4>> tempTransforms = new Dictionary<string, List<Matrix4x4>>();

        foreach (KeyValuePair<string, List<SaveObject>> pair in container.assembly)
        {
            if (!tempTransforms.ContainsKey(pair.Key))
                tempTransforms.Add(pair.Key, new List<Matrix4x4>());

            foreach(SaveObject so in pair.Value)
            {
                tempTransforms[pair.Key].Add(so.ToMatrix4x4());
            }
        }

        return tempTransforms;
    }
}

public class SaveObject
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

    public SaveObject(Matrix4x4 t)
    {
        m00 = t.m00;
        m01 = t.m01;
        m02 = t.m02;
        m03 = t.m03;

        m10 = t.m10;
        m11 = t.m11;
        m12 = t.m12;
        m13 = t.m13;

        m20 = t.m20;
        m21 = t.m21;
        m22 = t.m22;
        m23 = t.m23;

        m30 = t.m30;
        m31 = t.m31;
        m32 = t.m32;
        m33 = t.m33;
    }

    public Matrix4x4 ToMatrix4x4()
    {
        Matrix4x4 m = new Matrix4x4();

        m.m00 = m00;
        m.m01 = m01;
        m.m02 = m02;
        m.m03 = m03;

        m.m10 = m10;
        m.m11 = m11;
        m.m12 = m12;
        m.m13 = m13;

        m.m20 = m20;
        m.m21 = m21;
        m.m22 = m22;
        m.m23 = m23;

        m.m30 = m30;
        m.m31 = m31;
        m.m32 = m32;
        m.m33 = m33;

        return m;
    }
}
