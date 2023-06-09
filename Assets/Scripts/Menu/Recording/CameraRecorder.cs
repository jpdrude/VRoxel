using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using System;
using System.IO;

public class CameraRecorder : MonoBehaviour
{
    [SerializeField]
    int width = 3840;
    [SerializeField]
    int height = 2160;

    [SerializeField]
    bool record = false;
    public bool Record { get { return record; } }

    [SerializeField]
    bool play = false;
    public bool Play { get { return play; } }

    bool export = false;

    [SerializeField]
    int samplingWidth = 10;

    [SerializeField]
    UnityEvent ExportFinished = new UnityEvent();

    List<string> frameImgs = new List<string>();

    string exportPath = null;

    CADController controller;

    List<Vector3> camPositions = new List<Vector3>();
    List<Quaternion> camRotations = new List<Quaternion>();

    List<Vector3> smoothCamPositions = null;
    List<Quaternion> smoothCamRotations = null;

    int playbackPosition;

    float recordTimer = 1;

    int frameCounter;

    // Start is called before the first frame update
    void Start()
    {
        controller = GetComponent<CADController>();
    }

    // Update is called once per frame
    void Update()
    {
        if (record)
        {
            if (recordTimer > 0.03f)
            {
                camPositions.Add(transform.position);
                camRotations.Add(transform.rotation);
                recordTimer = 0;
            }

            recordTimer += Time.deltaTime;
        }
        else if (play)
        {
            if (smoothCamPositions == null)
                PlayRecording();

            transform.position = smoothCamPositions[playbackPosition];
            transform.rotation = smoothCamRotations[playbackPosition];

            ++playbackPosition;
            if (playbackPosition >= smoothCamPositions.Count)
            {
                playbackPosition = 0;

                if (export)
                    FinishExport();
            }

            if (export)
                SaveFrame();
        }

        ++frameCounter;
    }

    void FinishExport()
    {
        AnimationIO.constantAnimationTime = false;
        export = false;
        exportPath = null;
        StopPlayback();
        MessageSystem.DisplayMessage("Captured " + smoothCamPositions.Count + " frames!");
        ExportFinished.Invoke();
    }

    void StartExport(string _path)
    {
        AnimationIO.constantAnimationTime = true;
        exportPath = _path;
        export = true;
        ClearTempFiles();

        ClearFilesInDirectory(Path.GetDirectoryName(_path), Path.GetFileNameWithoutExtension(_path) + "*");
    }

    void SaveFrame()
    {
        string path;
        if (exportPath == null || !Directory.Exists(Path.GetDirectoryName(exportPath)))
            path = Application.persistentDataPath + "\\frame_" + playbackPosition.ToString("D5") + ".png";
        else
            path = Path.GetDirectoryName(exportPath) + "\\" + Path.GetFileNameWithoutExtension(exportPath) + 
                "_" + playbackPosition.ToString("D5") + ".png";


        frameImgs.Add(path);
        ScreenCapture.CaptureScreenshot(path);
    }

    public void StartScreenshotCoroutine(string path)
    {
        frameCounter = 0;
        StartCoroutine(CaptureScreenshot(path));
    }

    public IEnumerator CaptureScreenshot(string path)
    {
        if (!Directory.Exists(Path.GetDirectoryName(path)))
        {
            ExportFinished.Invoke();
            yield break;
        }

        RenderTexture renderTexture = new RenderTexture(width, height, 0, RenderTextureFormat.ARGB32);
        renderTexture.antiAliasing = 8;
        GetComponent<Camera>().targetTexture = renderTexture;

        yield return new WaitWhile(() => frameCounter < 3);

        if (File.Exists(path))
            File.Delete(path);

        RenderTexture.active = renderTexture;

        Texture2D tex = new Texture2D(renderTexture.width, renderTexture.height, TextureFormat.ARGB32, false);
        tex.ReadPixels(new Rect(0, 0, renderTexture.width, renderTexture.height), 0, 0);
        tex.Apply();

        //ApplyGammaCorrection(tex);

        byte[] bytes = tex.EncodeToJPG();
        File.WriteAllBytes(path, bytes);

        RenderTexture.active = null;
        GetComponent<Camera>().targetTexture = null;
        Destroy(renderTexture);

        yield return new WaitWhile(() => frameCounter < 5);

        ExportFinished.Invoke();
    }

    void ApplyGammaCorrection(Texture2D texture)
    {
        //float gamma = 2.2f;
        float gamma = 1 / 2.2f;

        // Get the pixel data from the source texture
        Color[] sourcePixels = texture.GetPixels();

        // Create a new array to store the gamma-corrected pixels
        Color[] targetPixels = new Color[sourcePixels.Length];

        // Apply gamma correction to each pixel
        for (int i = 0; i < sourcePixels.Length; i++)
        {
            Color sourcePixel = sourcePixels[i];
            Color targetPixel = new Color(
                Mathf.Pow(sourcePixel.r, gamma),
                Mathf.Pow(sourcePixel.g, gamma),
                Mathf.Pow(sourcePixel.b, gamma),
                sourcePixel.a
            );
            targetPixels[i] = targetPixel;
        }

        // Set the gamma-corrected pixels to the target texture
        texture.SetPixels(targetPixels);
        texture.Apply();
    }

    void ClearTempFiles()
    {
        ClearFilesInDirectory(Application.persistentDataPath, "frame_*");
    }

    void ClearFilesInDirectory(string directory, string filter)
    {
        string[] files = Directory.GetFiles(directory, filter);
        foreach (string fileName in files)
        {
            File.Delete(fileName);
        }
    }

    public void RecordCamera()
    {
        record = true;

        recordTimer = 1;

        if (camPositions.Count > 0)
        {
            if (Vector3.Distance(camPositions[camPositions.Count - 1], transform.position) > 1 || 
                Quaternion.Angle(camRotations[camRotations.Count - 1], transform.rotation) > 30)
            {
                camPositions.Clear();
                camRotations.Clear();

                playbackPosition = 0;

                smoothCamPositions = null;
                smoothCamRotations = null;
            }
        }
    }

    public void StopRecording()
    {
        record = false;
    }

    void SmoothenCameraPath()
    {
        Vector3 startPosition = camPositions[0];
        Vector3 endPosition = camPositions[camPositions.Count - 1];

        Quaternion startRotation = camRotations[0];
        Quaternion endRotation = camRotations[camRotations.Count - 1];

        List<Vector3> tempCamPositions = new List<Vector3>();
        List<Quaternion> tempCamRotations = new List<Quaternion>();

        for (int i = 0; i < camPositions.Count; ++i)
        {
            tempCamPositions.Add(camPositions[i]);
            tempCamRotations.Add(camRotations[i]);
        }

        for (int i = 0; i < samplingWidth; ++i)
        {
            tempCamPositions.Insert(0, startPosition);
            tempCamPositions.Add(endPosition);

            tempCamRotations.Insert(0, startRotation);
            tempCamRotations.Add(endRotation);
        }

        smoothCamPositions = new List<Vector3>();
        smoothCamRotations = new List<Quaternion>();

        for (int i = 0; i < tempCamPositions.Count - samplingWidth; ++i)
        {
            Vector3 averagePos = Vector3.zero;
            Vector4 averageRot = Vector4.zero;

            for (int j = 0; j < samplingWidth; ++j)
            {
                averagePos += tempCamPositions[i + j];
                Vector4 rotVector = new Vector4(tempCamRotations[i + j].x, tempCamRotations[i + j].y, 
                    tempCamRotations[i + j].z, tempCamRotations[i + j].w);
                averageRot += rotVector;
            }

            averagePos /= samplingWidth;
            averageRot /= samplingWidth;

            Quaternion newRot = new Quaternion(averageRot.x, averageRot.y, averageRot.z, averageRot.w);
            newRot.Normalize();

            smoothCamPositions.Add(averagePos);
            smoothCamRotations.Add(newRot);
        }
    }

    public void SetNewSamplingWidth(int newSamplingWidth)
    {
        int offset = newSamplingWidth - samplingWidth;
        samplingWidth = newSamplingWidth;

        if (play)
        {
            playbackPosition += offset;
            if (playbackPosition < 0)
                playbackPosition = 0;

            SmoothenCameraPath();
        }
    }

    public bool PlayRecording(bool fromStart = false, bool _export = false, string _path = null)
    {
        if (camPositions.Count < 100)
            return false;

        if (fromStart)
            playbackPosition = 0;

        if (_export)
        {
            StartExport(_path);
        }

        if (smoothCamPositions == null)
            SmoothenCameraPath();

        play = true;
        record = false;

        if (playbackPosition >= smoothCamPositions.Count)
            playbackPosition = 0;

        controller.enabled = false;

        return true;
    }

    public void StopPlayback()
    {
        play = false;
        controller.enabled = true;
    }
}
