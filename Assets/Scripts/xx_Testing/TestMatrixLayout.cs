using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TestMatrixLayout : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.M))
        {
            Matrix4x4 matrix = Matrix4x4.TRS(transform.position, transform.rotation, transform.localScale);
            string messageA = string.Format("M00: {0}, M01: {1}, M02: {2}, M03: {3}", matrix.m00, matrix.m01, matrix.m02, matrix.m03);
            string messageB = string.Format("M10: {0}, M11: {1}, M12: {2}, M13: {3}", matrix.m10, matrix.m11, matrix.m12, matrix.m13);
            string messageC = string.Format("M20: {0}, M21: {1}, M22: {2}, M23: {3}", matrix.m20, matrix.m21, matrix.m22, matrix.m23);
            string messageD = string.Format("M30: {0}, M31: {1}, M32: {2}, M33: {3}", matrix.m30, matrix.m31, matrix.m32, matrix.m33);

            Debug.Log(messageA);
            Debug.Log(messageB);
            Debug.Log(messageC);
            Debug.Log(messageD);
        }
    }
}
