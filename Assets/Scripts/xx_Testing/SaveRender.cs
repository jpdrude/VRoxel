using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;

public class SaveRender : MonoBehaviour
{
    public string path;
    public int width;
    public int height;

    Camera cam;
    RenderTexture texture;

    bool textureSet = false;
    bool imagecaptured = false;

    private void Start()
    {
        cam = GetComponent<Camera>();
    }

    void Update()
    {
        if (imagecaptured)
        {
            cam.targetTexture = null;
            imagecaptured = false;
            textureSet = false;
            return;
        }

        if (!imagecaptured && textureSet)
        {
            //texture = new RenderTexture(width, height, 24);

            //cam.targetTexture = texture;
            //cam.Render();
            RenderTexture.active = texture;
            Texture2D tex = new Texture2D(width, height, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            tex.Apply();

            File.WriteAllBytes(path, tex.EncodeToPNG());

            imagecaptured = true;
            //cam.targetTexture = null;
            //RenderTexture.active = null;
            //DestroyImmediate(texture);
            return;
        }

        if (Input.GetKeyDown(KeyCode.P))
        {
            texture = new RenderTexture(width, height, 24);
            cam.targetTexture = texture;
            textureSet = true;
            return;
        }
    }

    T CopyComponent<T>(T original, GameObject destination) where T : Component
    {
        System.Type type = original.GetType();
        Component copy = destination.AddComponent(type);
        System.Reflection.FieldInfo[] fields = type.GetFields();
        foreach (System.Reflection.FieldInfo field in fields)
        {
            field.SetValue(copy, field.GetValue(original));
        }
        return copy as T;
    }
}
