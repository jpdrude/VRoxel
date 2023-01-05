using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ScaleSwitch : MonoBehaviour
{
    [Header("Icons")]
    [SerializeField]
    GameObject iconXYZ;

    [SerializeField]
    GameObject iconXY;
    [SerializeField]
    GameObject iconYZ;
    [SerializeField]
    GameObject iconXZ;

    [SerializeField]
    GameObject iconX;
    [SerializeField]
    GameObject iconY;
    [SerializeField]
    GameObject iconZ;

    int iconIndex = 0;

    public void NextConstraint()
    {
        ++iconIndex;
        if (iconIndex >= 7)
            iconIndex = 0;

        switch (iconIndex)
        {
            case 0:
            {
                DisableIcons();
                iconXYZ.SetActive(true);
                BrushOptions.scaleConstraints = BrushOptions.ScaleConstraints.xyz;
                break;
            }
            case 1:
            {
                DisableIcons();
                iconXY.SetActive(true);
                BrushOptions.scaleConstraints = BrushOptions.ScaleConstraints.xy;
                break;
            }
            case 2:
            {
                DisableIcons();
                iconYZ.SetActive(true);
                BrushOptions.scaleConstraints = BrushOptions.ScaleConstraints.yz;
                break;
            }
            case 3:
            {
                DisableIcons();
                iconXZ.SetActive(true);
                BrushOptions.scaleConstraints = BrushOptions.ScaleConstraints.xz;
                break;
            }
            case 4:
            {
                DisableIcons();
                iconX.SetActive(true);
                BrushOptions.scaleConstraints = BrushOptions.ScaleConstraints.x;
                break;
            }
            case 5:
            {
                DisableIcons();
                iconY.SetActive(true);
                BrushOptions.scaleConstraints = BrushOptions.ScaleConstraints.y;
                break;
            }
            case 6:
            {
                DisableIcons();
                iconZ.SetActive(true);
                BrushOptions.scaleConstraints = BrushOptions.ScaleConstraints.z;
                break;
            }
            default:
            {
                DisableIcons();
                iconXYZ.SetActive(true);
                BrushOptions.scaleConstraints = BrushOptions.ScaleConstraints.xyz;
                break;
            }
        }
    }

    void DisableIcons()
    {
        iconXYZ.SetActive(false);

        iconXY.SetActive(false);
        iconYZ.SetActive(false);
        iconXZ.SetActive(false);

        iconX.SetActive(false);
        iconY.SetActive(false);
        iconZ.SetActive(false);
    }
}
