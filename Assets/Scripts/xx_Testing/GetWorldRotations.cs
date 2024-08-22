using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GetWorldRotations : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        Debug.Log(name + " " + transform.rotation.eulerAngles.ToString("F2"));
    }

}
