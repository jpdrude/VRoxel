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

    [Space]
    [Header("Colors")]
    [SerializeField]
    Color neutralColor;
    [SerializeField]
    Color spawnColor;
    [SerializeField]
    Color deleteColor;

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
            mesh2Bricks.ScanMesh = false;
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
}
