using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using GPUInstancer;

public class rot90 : MonoBehaviour
{
    [SerializeField]
    GPUInstancerPrefabManager manager;

    [SerializeField]
    bool rotate = false;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (rotate)
        {
            Matrix4x4[][] transformArray = BlockContainer.Container.TransformArray;
            int count = 0;

            Vector3 rotationPoint = Vector3.zero;
            float rotationAngle = 0;

            for (int i = 0; i < GameManager.Bond.PartPrefabs.Length; ++i)
            {

                for (int j = 0; j < transformArray[i].Length; ++j)
                {
                    if (transformArray[i][j] == Matrix4x4.zero) continue;


                    // Calculate the relative position and rotation
                    Vector3 relativePosition = transformArray[i][j].GetPosition() - rotationPoint;
                    Quaternion relativeRotation = Quaternion.Inverse(transform.rotation) * transformArray[i][j].GetRotation();

                    // Create the rotation matrix
                    Matrix4x4 rotationMatrix = GetRotationMatrix(rotationAngle);

                    // Apply the rotation matrix to the relative position and rotation
                    Vector3 rotatedRelativePosition = rotationMatrix.MultiplyPoint(relativePosition);
                    Quaternion rotatedRelativeRotation = rotationMatrix.rotation * relativeRotation;

                    // Update the child's TRS transform with the rotated position and rotation
                    Vector3 pos = rotationPoint + rotatedRelativePosition;
                    Quaternion rot = transform.rotation * rotatedRelativeRotation;


                    transformArray[i][j] = Matrix4x4.TRS(pos, rot, Vector3.one);
                    ++count;
                }

                GPUInstancerAPI.UpdateVisibilityBufferWithMatrix4x4Array(manager, GameManager.Bond.PartPrefabs[i].prefabPrototype, transformArray[i]);
            }


            rotate = false;

            Debug.Log("Should rotate " + count + " objects 90°");

        }
    }

    Matrix4x4 GetRotationMatrix(float angle)
    {
        float radianAngle = angle * Mathf.Deg2Rad;
        float cos = Mathf.Cos(radianAngle);
        float sin = Mathf.Sin(radianAngle);

        // Create a 2D rotation matrix
        Matrix4x4 rotationMatrix = Matrix4x4.identity;
        rotationMatrix[0, 0] = cos;
        rotationMatrix[0, 1] = -sin;
        rotationMatrix[1, 0] = sin;
        rotationMatrix[1, 1] = cos;

        return rotationMatrix;
    }
}
