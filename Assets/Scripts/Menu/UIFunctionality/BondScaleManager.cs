using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BondScaleManager : MonoBehaviour
{
    [SerializeField]
    Bond bond;
    public Bond Bond { get { return bond; } }

    [SerializeField]
    float scale = 1;
    public float Scale { get { return scale; } set { scale = value; } }

}
