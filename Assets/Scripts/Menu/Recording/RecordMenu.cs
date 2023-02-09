using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using SimpleFileBrowser;

public class RecordMenu : MonoBehaviour
{
    [Header("Sprites")]
    [SerializeField]
    Sprite recordSprite;
    [SerializeField]
    Sprite stopRecordSprite;
    [SerializeField]
    Sprite playSprite;
    [SerializeField]
    Sprite pauseSprite;

    [Header("Buttons")]
    [Space]
    [SerializeField]
    Button recordButton;
    [SerializeField]
    Button playbackButton;

    [Space]
    [Header("Recorder")]
    [SerializeField]
    CameraRecorder recorder;

    [Space]
    [Header("Exporter")]
    [SerializeField]
    GameObject interfacePanel;
    [SerializeField]
    GameObject targetSpawner;
    [SerializeField]
    EscapeMenu escapeMenu;
    [SerializeField]
    GameObject messagePanel;
    [SerializeField]
    GraphicsSettingsMenu graphicsMenu;


    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.R))
        {
            RecordButton();
        }
        if (Input.GetKeyDown(KeyCode.P))
        {
            PlayButton();
        }
    }

    public void PlayButton()
    {
        if (!recorder.Play)
        {
            if (recorder.PlayRecording())
            {
                playbackButton.image.sprite = pauseSprite;
                recordButton.image.sprite = recordSprite;
            }   
        }
        else
        {
            recorder.StopPlayback();
            playbackButton.image.sprite = playSprite;
        }
    }

    public void RecordButton()
    {
        if (recorder.Play)
            return;

        if (!recorder.Record)
        {
            recorder.RecordCamera();
            recordButton.image.sprite = stopRecordSprite;
        }
        else
        {
            recorder.StopRecording();
            recordButton.image.sprite = recordSprite;
        }
    }

    public void ChangeSamplingWidth(float newSamplingWidth)
    {
        recorder.SetNewSamplingWidth((int)newSamplingWidth);
    }

    void ExportVideo(string[] paths)
    {
        ActivateInput();

        interfacePanel.SetActive(false);
        targetSpawner.SetActive(false);
        messagePanel.SetActive(false);
        graphicsMenu.DeactivateCulling();

        escapeMenu.enabled = false;
        recorder.PlayRecording(true, true, paths[0]);
    }

    public void OnExportFinished()
    {
        interfacePanel.SetActive(true);
        targetSpawner.SetActive(true);
        messagePanel.SetActive(true);
        escapeMenu.enabled = true;

        graphicsMenu.ActivateCulling();
    }

    public void ExportVideoBrowser()
    {
        FileBrowser.SetFilters(true, new FileBrowser.Filter("PNG", ".png"));
        FileBrowser.SetDefaultFilter(".png");

        FileBrowser.ShowSaveDialog(ExportVideo, OnCancel, FileBrowser.PickMode.Files, false);

        DeactivateInput();
    }

    void OnCancel()
    {
        ActivateInput();
    }

    void DeactivateInput()
    {
        targetSpawner.GetComponent<TargetSpawner>().enabled = false;
        this.enabled = false;

    }

    void ActivateInput()
    {
        targetSpawner.GetComponent<TargetSpawner>().enabled = true;
        this.enabled = true;
    }
}
