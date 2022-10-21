using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Text;

public class ByteStreamContainer
{
    public string bondName;
    public Matrix4x4[][] matrices;
    public float version;

    static Matrix4x4 conversionMatrix = new Matrix4x4(new Vector4(1, 0, 0, 0), new Vector4(0, 0, 1, 0), new Vector4(0, 1, 0, 0), new Vector4(0, 0, 0, 1));

    public byte[] ToByteArray()
    {
        int matricesLength = 0;
        for (int i = 0; i < matrices.Length; ++i)
            matricesLength += matrices[i].Length;

        byte[] array = new byte[4 + 1 + bondName.Length + 1 + matrices.Length * 4 + matricesLength * 7 * 4];

        byte[] vroxelVersion = BitConverter.GetBytes(version);
        array[0] = vroxelVersion[0];
        array[1] = vroxelVersion[1];
        array[2] = vroxelVersion[2];
        array[3] = vroxelVersion[3];
        Debug.Log("VRoxel version: " + version.ToString("F3"));
        
        array[4] = (byte)bondName.Length;

        for (int i = 0; i < bondName.Length; ++i)
        {
            array[i + 5] = (byte)bondName[i];
        }

        array[5 + bondName.Length] = (byte)matrices.Length;

        int headerOffset = 5 + bondName.Length + 1;

        for (int i = 0; i < matrices.Length; ++ i)
        {
            byte[] typeLength = BitConverter.GetBytes(matrices[i].Length);
            array[headerOffset + i * 4 + 0] = typeLength[0];
            array[headerOffset + i * 4 + 1] = typeLength[1];
            array[headerOffset + i * 4 + 2] = typeLength[2];
            array[headerOffset + i * 4 + 3] = typeLength[3];
        }

        headerOffset += matrices.Length * 4;

        for (int i = 0; i < matrices.Length; ++i)
        {
            for (int j = 0; j < matrices[i].Length; ++j)
            {
                Vector3 position = matrices[i][j].GetPosition();
                Quaternion rotation = matrices[i][j].GetRotation();
                
                byte[] posX = BitConverter.GetBytes(position.x);
                array[headerOffset + j * 7 * 4 + 0] = posX[0];
                array[headerOffset + j * 7 * 4 + 1] = posX[1];
                array[headerOffset + j * 7 * 4 + 2] = posX[2];
                array[headerOffset + j * 7 * 4 + 3] = posX[3];

                byte[] posY = BitConverter.GetBytes(position.y);
                array[headerOffset + j * 7 * 4 + 4] = posY[0];
                array[headerOffset + j * 7 * 4 + 5] = posY[1];
                array[headerOffset + j * 7 * 4 + 6] = posY[2];
                array[headerOffset + j * 7 * 4 + 7] = posY[3];

                byte[] posZ = BitConverter.GetBytes(position.z);
                array[headerOffset + j * 7 * 4 + 8] = posZ[0];
                array[headerOffset + j * 7 * 4 + 9] = posZ[1];
                array[headerOffset + j * 7 * 4 + 10] = posZ[2];
                array[headerOffset + j * 7 * 4 + 11] = posZ[3];

                byte[] rotX = BitConverter.GetBytes(rotation.x);
                array[headerOffset + j * 7 * 4 + 12] = rotX[0];
                array[headerOffset + j * 7 * 4 + 13] = rotX[1];
                array[headerOffset + j * 7 * 4 + 14] = rotX[2];
                array[headerOffset + j * 7 * 4 + 15] = rotX[3];

                byte[] rotY = BitConverter.GetBytes(rotation.y);
                array[headerOffset + j * 7 * 4 + 16] = rotY[0];
                array[headerOffset + j * 7 * 4 + 17] = rotY[1];
                array[headerOffset + j * 7 * 4 + 18] = rotY[2];
                array[headerOffset + j * 7 * 4 + 19] = rotY[3];

                byte[] rotZ = BitConverter.GetBytes(rotation.z);
                array[headerOffset + j * 7 * 4 + 20] = rotZ[0];
                array[headerOffset + j * 7 * 4 + 21] = rotZ[1];
                array[headerOffset + j * 7 * 4 + 22] = rotZ[2];
                array[headerOffset + j * 7 * 4 + 23] = rotZ[3];

                byte[] rotW = BitConverter.GetBytes(rotation.w);
                array[headerOffset + j * 7 * 4 + 24] = rotW[0];
                array[headerOffset + j * 7 * 4 + 25] = rotW[1];
                array[headerOffset + j * 7 * 4 + 26] = rotW[2];
                array[headerOffset + j * 7 * 4 + 27] = rotW[3]; 
            }

            headerOffset += matrices[i].Length * 4 * 7;
        }

        return array;
    }

    public ByteStreamContainer(string _bondName, Matrix4x4[][] _matrices, float _version)
    {
        bondName = _bondName;
        matrices = _matrices;
        version = _version;
    }

    public ByteStreamContainer(string _bondName, List<Matrix4x4>[] _matrices, float _version)
    {
        bondName = _bondName;
        matrices = new Matrix4x4[_matrices.Length][];
        for (int i = 0; i < matrices.Length; ++i)
            matrices[i] = _matrices[i].ToArray();

        version = _version;
    }

    public ByteStreamContainer(byte[] array)
    {
        version = BitConverter.ToSingle(array, 0);
        Debug.Log("VersionNumber: " + version.ToString("F3"));

        int headerOffset = 4;

        StringBuilder _bondName = new StringBuilder();
        for (int i = headerOffset; i < headerOffset + array[headerOffset]; ++i)
        {
            _bondName.Append((char)array[i + 1]);
        }

        bondName = _bondName.ToString();
        Debug.Log("BondName: " + bondName);


        headerOffset += array[headerOffset] + 1;

        int typeLength = array[headerOffset];

        Debug.Log("Number of types: " + typeLength);

        headerOffset += 1;

        int[] typeArrayLengths = new int[typeLength];

        for (int i = 0; i < typeLength; ++ i)
        {
            typeArrayLengths[i] = BitConverter.ToInt32(array, headerOffset + i * 4);
        }

        for (int i = 0; i < typeArrayLengths.Length; ++i)
            Debug.Log("Type " + i + " has " + typeArrayLengths[i] + " elements");

        headerOffset += typeLength * 4;

        matrices = new Matrix4x4[typeLength][];
        int matricesEnd = headerOffset;

        for (int i = 0; i < typeLength; ++i)
        {
            matricesEnd += typeArrayLengths[i] * 4 * 7;

            matrices[i] = new Matrix4x4[typeArrayLengths[i]];

            int matrixCounter = 0;
            for (int j = headerOffset; j < matricesEnd; j += 7 * 4)
            {
                float posX = BitConverter.ToSingle(array, j + 4 * 0);
                float posY = BitConverter.ToSingle(array, j + 4 * 1);
                float posZ = BitConverter.ToSingle(array, j + 4 * 2);
                Vector3 position = new Vector3(posX, posY, posZ);

                float rotX = BitConverter.ToSingle(array, j + 4 * 3);
                float rotY = BitConverter.ToSingle(array, j + 4 * 4);
                float rotZ = BitConverter.ToSingle(array, j + 4 * 5);
                float rotW = BitConverter.ToSingle(array, j + 4 * 6);
                Quaternion rotation = new Quaternion(rotX, rotY, rotZ, rotW);

                Matrix4x4 matrix = Matrix4x4.TRS(position, rotation, Vector3.one);

                matrices[i][matrixCounter] = conversionMatrix * matrix;
                ++matrixCounter;
            }

            headerOffset += typeArrayLengths[i] * 4 * 7;
        }
    }
}
