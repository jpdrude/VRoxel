using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CADMenu : MonoBehaviour
{
    [SerializeField]
    CADMeshToBricks mesh2Bricks;
    [SerializeField]
    Button btnMesh2Bricks;
    [SerializeField]
    Toggle meshBrushFine;

    [Space]
    [Header("Colors")]
    [SerializeField]
    Color neutralColor;
    [SerializeField]
    Color spawnColor;
    [SerializeField]
    Color deleteColor;

    [Space]
    [Header("Undo/Redo")]
    [SerializeField]
    Button undoButton;
    [SerializeField]
    Button redoButton;

    private void Start()
    {
        if (Bond.meshBrushLevel == MeshBrushLevels.fine)
            meshBrushFine.isOn = true;
        else
            meshBrushFine.isOn = false;
    }

    private void OnEnable()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
            mesh2Bricks.ScanMesh = false;

        if (UndoStack.GetUndoStack().Count > 0)
            SetUndoAvailable();
        else
            SetUndoUnavailable();

        if (UndoStack.GetRedoStack().Count > 0)
            SetRedoAvailable();
        else
            SetRedoUnavailable();

        UndoStack.UndoStackAvailable.AddListener(SetUndoAvailable);
        UndoStack.UndoStackUnavailable.AddListener(SetUndoUnavailable);

        UndoStack.RedoStackAvailable.AddListener(SetRedoAvailable);
        UndoStack.RedoStackUnavailable.AddListener(SetRedoUnavailable);
    }

    private void OnDisable()
    {
        UndoStack.UndoStackAvailable.RemoveListener(SetUndoAvailable);
        UndoStack.UndoStackUnavailable.RemoveListener(SetUndoUnavailable);

        UndoStack.RedoStackAvailable.RemoveListener(SetRedoAvailable);
        UndoStack.RedoStackUnavailable.RemoveListener(SetRedoUnavailable);
    }

    public void Undo()
    {
        UndoStack.Undo();
    }

    public void Redo()
    {
        UndoStack.Redo();
    }

    public void ChangeCADMode()
    {
        if (!mesh2Bricks.ScanMesh)
        {
            mesh2Bricks.ScanMesh = true;
            mesh2Bricks.Remove = false;

            ColorBlock colors = btnMesh2Bricks.colors;
            colors.normalColor = spawnColor;
            colors.highlightedColor = spawnColor;
            btnMesh2Bricks.colors = colors;
        }
        else if (mesh2Bricks.ScanMesh && !mesh2Bricks.Remove)
        {
            mesh2Bricks.Remove = true;

            ColorBlock colors = btnMesh2Bricks.colors;
            colors.normalColor = deleteColor;
            colors.highlightedColor = deleteColor;
            btnMesh2Bricks.colors = colors;
        }
        else
        {
            mesh2Bricks.Remove = false;
            mesh2Bricks.ScanMesh = false;

            ColorBlock colors = btnMesh2Bricks.colors;
            colors.normalColor = neutralColor;
            colors.highlightedColor = neutralColor;
            btnMesh2Bricks.colors = colors;
        }
    }

    public void ToggleMeshBrush(bool value)
    {
        if (value == true)
            Bond.meshBrushLevel = MeshBrushLevels.fine;
        else
            Bond.meshBrushLevel = MeshBrushLevels.rough;
    }

    void SetUndoAvailable()
    {
        undoButton.interactable = true;
    }

    void SetUndoUnavailable()
    {
        undoButton.interactable = false;
    }

    void SetRedoAvailable()
    {
        redoButton.interactable = true;
    }

    void SetRedoUnavailable()
    {
        redoButton.interactable = false;
    }
}
