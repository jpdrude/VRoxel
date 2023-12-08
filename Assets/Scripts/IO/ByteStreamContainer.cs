using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Text;

public class ByteStreamContainer
{
    public string bondName;
    public Matrix4x4[][] matrices;
    public uint[][] times;
    public float timeOffset = 0;
    public float version;
    public Vector3 offset = new Vector3(0, 0, 0);

    public byte[] ToByteArray()
    {
        int matricesLength = 0;
        for (int i = 0; i < matrices.Length; ++i)
            matricesLength += matrices[i].Length;

        byte[] array = new byte[4 + 12 + 1 + bondName.Length + 1 + matrices.Length * 4 + matricesLength * 8 * 4];
        //version (4) + offset (12) + bondNameStrinLength (1) + bondNameCharacters (variable) + ...

        byte[] vroxelVersion = BitConverter.GetBytes(version);
        array[0] = vroxelVersion[0];
        array[1] = vroxelVersion[1];
        array[2] = vroxelVersion[2];
        array[3] = vroxelVersion[3];
        Debug.Log("VRoxel version: " + version.ToString("F3"));

        byte[] offsetBytes = BitConverter.GetBytes(offset.x);
        array[4] = offsetBytes[0];
        array[5] = offsetBytes[1];
        array[6] = offsetBytes[2];
        array[7] = offsetBytes[3];
        offsetBytes = BitConverter.GetBytes(offset.y);
        array[8] = offsetBytes[0];
        array[9] = offsetBytes[1];
        array[10] = offsetBytes[2];
        array[11] = offsetBytes[3];
        offsetBytes = BitConverter.GetBytes(offset.z);
        array[12] = offsetBytes[0];
        array[13] = offsetBytes[1];
        array[14] = offsetBytes[2];
        array[15] = offsetBytes[3];

        array[16] = (byte)bondName.Length;

        for (int i = 0; i < bondName.Length; ++i)
        {
            array[i + 17] = (byte)bondName[i];
        }

        array[17 + bondName.Length] = (byte)matrices.Length;

        int headerOffset = 17 + bondName.Length + 1;

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
                uint time = times[i][j];
                
                byte[] posX = BitConverter.GetBytes(position.x - offset.x);
                array[headerOffset + j * 8 * 4 + 0] = posX[0];
                array[headerOffset + j * 8 * 4 + 1] = posX[1];
                array[headerOffset + j * 8 * 4 + 2] = posX[2];
                array[headerOffset + j * 8 * 4 + 3] = posX[3];

                byte[] posY = BitConverter.GetBytes(position.y - offset.y);
                array[headerOffset + j * 8 * 4 + 4] = posY[0];
                array[headerOffset + j * 8 * 4 + 5] = posY[1];
                array[headerOffset + j * 8 * 4 + 6] = posY[2];
                array[headerOffset + j * 8 * 4 + 7] = posY[3];

                byte[] posZ = BitConverter.GetBytes(position.z - offset.z);
                array[headerOffset + j * 8 * 4 + 8] = posZ[0];
                array[headerOffset + j * 8 * 4 + 9] = posZ[1];
                array[headerOffset + j * 8 * 4 + 10] = posZ[2];
                array[headerOffset + j * 8 * 4 + 11] = posZ[3];

                byte[] rotX = BitConverter.GetBytes(rotation.x);
                array[headerOffset + j * 8 * 4 + 12] = rotX[0];
                array[headerOffset + j * 8 * 4 + 13] = rotX[1];
                array[headerOffset + j * 8 * 4 + 14] = rotX[2];
                array[headerOffset + j * 8 * 4 + 15] = rotX[3];

                byte[] rotY = BitConverter.GetBytes(rotation.y);
                array[headerOffset + j * 8 * 4 + 16] = rotY[0];
                array[headerOffset + j * 8 * 4 + 17] = rotY[1];
                array[headerOffset + j * 8 * 4 + 18] = rotY[2];
                array[headerOffset + j * 8 * 4 + 19] = rotY[3];

                byte[] rotZ = BitConverter.GetBytes(rotation.z);
                array[headerOffset + j * 8 * 4 + 20] = rotZ[0];
                array[headerOffset + j * 8 * 4 + 21] = rotZ[1];
                array[headerOffset + j * 8 * 4 + 22] = rotZ[2];
                array[headerOffset + j * 8 * 4 + 23] = rotZ[3];

                byte[] rotW = BitConverter.GetBytes(rotation.w);
                array[headerOffset + j * 8 * 4 + 24] = rotW[0];
                array[headerOffset + j * 8 * 4 + 25] = rotW[1];
                array[headerOffset + j * 8 * 4 + 26] = rotW[2];
                array[headerOffset + j * 8 * 4 + 27] = rotW[3];

                byte[] timeStamp = BitConverter.GetBytes(time);
                array[headerOffset + j * 8 * 4 + 28] = timeStamp[0];
                array[headerOffset + j * 8 * 4 + 29] = timeStamp[1];
                array[headerOffset + j * 8 * 4 + 30] = timeStamp[2];
                array[headerOffset + j * 8 * 4 + 31] = timeStamp[3];
            }

            headerOffset += matrices[i].Length * 4 * 8;
        }

        return array;
    }

    public ByteStreamContainer(string _bondName, Matrix4x4[][] _matrices, uint[][] _times, float _version, Vector3 _offset)
    {
        bondName = _bondName;
        matrices = _matrices;
        version = _version;
        times = _times;
        offset = _offset;
    }

    public ByteStreamContainer(string _bondName, List<Matrix4x4>[] _matrices, List<uint>[] _times, float _version, Vector3 _offset)
    {
        bondName = _bondName;
        matrices = new Matrix4x4[_matrices.Length][];
        times = new uint[_times.Length][];
        for (int i = 0; i < matrices.Length; ++i)
        {
            matrices[i] = _matrices[i].ToArray();
            times[i] = _times[i].ToArray();
        }
            
        version = _version;
        offset = _offset;
    }

    public ByteStreamContainer(byte[] array)
    {
        uint startTime = (uint)(GameManager.gameTime * 10);

        version = BitConverter.ToSingle(array, 0);
        Debug.Log("VersionNumber: " + version.ToString("F3"));

        offset = Vector3.zero;
        int headerOffset = 4;
        if (Mathf.RoundToInt(version * 1000) >= 333)
        {
            Debug.Log("Offset considered");

            offset.x = BitConverter.ToSingle(array, 4);
            offset.y = BitConverter.ToSingle(array, 8);
            offset.z = BitConverter.ToSingle(array, 12);

            headerOffset = 16;
        }

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
        times = new uint[typeLength][];
        int matricesEnd = headerOffset;

        //Check Load Offset against Current Offset
        if (Vector3.Distance(offset, GameManager.Bond.Offset) > 0.0000001f)
        {
            if (BlockContainer.Container.GetCount() > 1000 && bondName == GameManager.Bond.BondName)
                offset = GameManager.Bond.Offset;
            else if (bondName == GameManager.Bond.BondName)
            {
                GameManager.Bond.Offset = offset;
            }
        }

        for (int i = 0; i < typeLength; ++i)
        {
            int increment = 4 * 8;

            if (version < 0.28f)
            {
                increment = 4 * 7;
                matricesEnd += typeArrayLengths[i] * 4 * 7;
            }
            else
                matricesEnd += typeArrayLengths[i] * 4 * 8;

            matrices[i] = new Matrix4x4[typeArrayLengths[i]];
            times[i] = new uint[typeArrayLengths[i]];

            int matrixCounter = 0;
            for (int j = headerOffset; j < matricesEnd; j += increment)
            {
                float posX = BitConverter.ToSingle(array, j + 4 * 0);
                float posY = BitConverter.ToSingle(array, j + 4 * 1);
                float posZ = BitConverter.ToSingle(array, j + 4 * 2);
                Vector3 position = new Vector3(posX, posY, posZ) + offset;

                float rotX = BitConverter.ToSingle(array, j + 4 * 3);
                float rotY = BitConverter.ToSingle(array, j + 4 * 4);
                float rotZ = BitConverter.ToSingle(array, j + 4 * 5);
                float rotW = BitConverter.ToSingle(array, j + 4 * 6);
                Quaternion rotation = new Quaternion(rotX, rotY, rotZ, rotW);

                if (version >= 0.28f)
                {
                    uint time = BitConverter.ToUInt32(array, j + 4 * 7);
                    times[i][matrixCounter] = time + startTime;
                    if (time / 10f > timeOffset)
                        timeOffset = time / 10f;
                }
                else
                    times[i][matrixCounter] = startTime;

                //Matrix4x4 matrix = Matrix4x4.TRS(position, rotation, Vector3.one);
                Matrix4x4 matrix = Matrix4x4.TRS(position, rotation, new Vector3(-1, 1, -1));

                matrices[i][matrixCounter] = matrix;
                ++matrixCounter;
            }
            if (version < 0.28f)
                headerOffset += typeArrayLengths[i] * 4 * 7;
            else
                headerOffset += typeArrayLengths[i] * 4 * 8;
        }
    }
}
