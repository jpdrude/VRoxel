using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Text;

public class ByteStreamContainer
{
    public string bondName;
    public Matrix4x4[] matrices;

    static Matrix4x4 conversionMatrix = new Matrix4x4(new Vector4(1, 0, 0, 0), new Vector4(0, 0, 1, 0), new Vector4(0, 1, 0, 0), new Vector4(0, 0, 0, 1));

    public byte[] ToByteArray()
    {
        byte[] array = new byte[1 + bondName.Length + matrices.Length * 16 * 4];
        array[0] = (byte)bondName.Length;

        for (int i = 0; i < bondName.Length; ++i)
        {
            array[i + 1] = (byte)bondName[i];
        }

        int headerOffset = bondName.Length + 1;

        for (int i = 0; i < matrices.Length; ++i)
        {
            byte[] m00 = BitConverter.GetBytes(matrices[i].m00);
            array[headerOffset + i * 16 * 4 + 0] = m00[0];
            array[headerOffset + i * 16 * 4 + 1] = m00[1];
            array[headerOffset + i * 16 * 4 + 2] = m00[2];
            array[headerOffset + i * 16 * 4 + 3] = m00[3];

            byte[] m01 = BitConverter.GetBytes(matrices[i].m01);
            array[headerOffset + i * 16 * 4 + 4] = m01[0];
            array[headerOffset + i * 16 * 4 + 5] = m01[1];
            array[headerOffset + i * 16 * 4 + 6] = m01[2];
            array[headerOffset + i * 16 * 4 + 7] = m01[3];

            byte[] m02 = BitConverter.GetBytes(matrices[i].m02);
            array[headerOffset + i * 16 * 4 + 8] = m02[0];
            array[headerOffset + i * 16 * 4 + 9] = m02[1];
            array[headerOffset + i * 16 * 4 + 10] = m02[2];
            array[headerOffset + i * 16 * 4 + 11] = m02[3];

            byte[] m03 = BitConverter.GetBytes(matrices[i].m03);
            array[headerOffset + i * 16 * 4 + 12] = m03[0];
            array[headerOffset + i * 16 * 4 + 13] = m03[1];
            array[headerOffset + i * 16 * 4 + 14] = m03[2];
            array[headerOffset + i * 16 * 4 + 15] = m03[3];

            byte[] m10 = BitConverter.GetBytes(matrices[i].m10);
            array[headerOffset + i * 16 * 4 + 0 + 16] = m10[0];
            array[headerOffset + i * 16 * 4 + 1 + 16] = m10[1];
            array[headerOffset + i * 16 * 4 + 2 + 16] = m10[2];
            array[headerOffset + i * 16 * 4 + 3 + 16] = m10[3];

            byte[] m11 = BitConverter.GetBytes(matrices[i].m11);
            array[headerOffset + i * 16 * 4 + 4 + 16] = m11[0];
            array[headerOffset + i * 16 * 4 + 5 + 16] = m11[1];
            array[headerOffset + i * 16 * 4 + 6 + 16] = m11[2];
            array[headerOffset + i * 16 * 4 + 7 + 16] = m11[3];

            byte[] m12 = BitConverter.GetBytes(matrices[i].m12);
            array[headerOffset + i * 16 * 4 + 8 + 16] = m12[0];
            array[headerOffset + i * 16 * 4 + 9 + 16] = m12[1];
            array[headerOffset + i * 16 * 4 + 10 + 16] = m12[2];
            array[headerOffset + i * 16 * 4 + 11 + 16] = m12[3];

            byte[] m13 = BitConverter.GetBytes(matrices[i].m13);
            array[headerOffset + i * 16 * 4 + 12 + 16] = m13[0];
            array[headerOffset + i * 16 * 4 + 13 + 16] = m13[1];
            array[headerOffset + i * 16 * 4 + 14 + 16] = m13[2];
            array[headerOffset + i * 16 * 4 + 15 + 16] = m13[3];

            byte[] m20 = BitConverter.GetBytes(matrices[i].m20);
            array[headerOffset + i * 16 * 4 + 0 + 32] = m20[0];
            array[headerOffset + i * 16 * 4 + 1 + 32] = m20[1];
            array[headerOffset + i * 16 * 4 + 2 + 32] = m20[2];
            array[headerOffset + i * 16 * 4 + 3 + 32] = m20[3];

            byte[] m21 = BitConverter.GetBytes(matrices[i].m21);
            array[headerOffset + i * 16 * 4 + 4 + 32] = m21[0];
            array[headerOffset + i * 16 * 4 + 5 + 32] = m21[1];
            array[headerOffset + i * 16 * 4 + 6 + 32] = m21[2];
            array[headerOffset + i * 16 * 4 + 7 + 32] = m21[3];

            byte[] m22 = BitConverter.GetBytes(matrices[i].m22);
            array[headerOffset + i * 16 * 4 + 8 + 32] = m22[0];
            array[headerOffset + i * 16 * 4 + 9 + 32] = m22[1];
            array[headerOffset + i * 16 * 4 + 10 + 32] = m22[2];
            array[headerOffset + i * 16 * 4 + 11 + 32] = m22[3];

            byte[] m23 = BitConverter.GetBytes(matrices[i].m23);
            array[headerOffset + i * 16 * 4 + 12 + 32] = m23[0];
            array[headerOffset + i * 16 * 4 + 13 + 32] = m23[1];
            array[headerOffset + i * 16 * 4 + 14 + 32] = m23[2];
            array[headerOffset + i * 16 * 4 + 15 + 32] = m23[3];

            byte[] m30 = BitConverter.GetBytes(matrices[i].m30);
            array[headerOffset + i * 16 * 4 + 0 + 48] = m30[0];
            array[headerOffset + i * 16 * 4 + 1 + 48] = m30[1];
            array[headerOffset + i * 16 * 4 + 2 + 48] = m30[2];
            array[headerOffset + i * 16 * 4 + 3 + 48] = m30[3];

            byte[] m31 = BitConverter.GetBytes(matrices[i].m31);
            array[headerOffset + i * 16 * 4 + 4 + 48] = m31[0];
            array[headerOffset + i * 16 * 4 + 5 + 48] = m31[1];
            array[headerOffset + i * 16 * 4 + 6 + 48] = m31[2];
            array[headerOffset + i * 16 * 4 + 7 + 48] = m31[3];

            byte[] m32 = BitConverter.GetBytes(matrices[i].m32);
            array[headerOffset + i * 16 * 4 + 8 + 48] = m32[0];
            array[headerOffset + i * 16 * 4 + 9 + 48] = m32[1];
            array[headerOffset + i * 16 * 4 + 10 + 48] = m32[2];
            array[headerOffset + i * 16 * 4 + 11 + 48] = m32[3];

            byte[] m33 = BitConverter.GetBytes(matrices[i].m33);
            array[headerOffset + i * 16 * 4 + 12 + 48] = m33[0];
            array[headerOffset + i * 16 * 4 + 13 + 48] = m33[1];
            array[headerOffset + i * 16 * 4 + 14 + 48] = m33[2];
            array[headerOffset + i * 16 * 4 + 15 + 48] = m33[3];
        }

        return array;
    }

    public ByteStreamContainer(string _bondName, Matrix4x4[] _matrices)
    {
        bondName = _bondName;
        matrices = _matrices;
    }

    public ByteStreamContainer(string _bondName, List<Matrix4x4> _matrices)
    {
        bondName = _bondName;
        matrices = _matrices.ToArray();
    }

    public ByteStreamContainer(byte[] array)
    {
        StringBuilder _bondName = new StringBuilder();
        for (int i = 0; i < array[0]; ++ i)
        {
            _bondName.Append((char)array[i + 1]);
        }

        bondName = _bondName.ToString();

        matrices = new Matrix4x4[(array.Length - array[0] - 1) / (16 * 4)];

        int headerOffset = array[0] + 1;

        int matrixCounter = 0;
        int lastIndex = 0;
        for (int i = headerOffset; i < array.Length - 4; i += 16 * 4)
        {
            Matrix4x4 matrix = new Matrix4x4();

            matrix.m00 = BitConverter.ToSingle(array, i + 4 * 0);
            matrix.m01 = BitConverter.ToSingle(array, i + 4 * 1);
            matrix.m02 = BitConverter.ToSingle(array, i + 4 * 2);
            matrix.m03 = BitConverter.ToSingle(array, i + 4 * 3);

            matrix.m10 = BitConverter.ToSingle(array, i + 4 * 0 + 16 * 1);
            matrix.m11 = BitConverter.ToSingle(array, i + 4 * 1 + 16 * 1);
            matrix.m12 = BitConverter.ToSingle(array, i + 4 * 2 + 16 * 1);
            matrix.m13 = BitConverter.ToSingle(array, i + 4 * 3 + 16 * 1);

            matrix.m20 = BitConverter.ToSingle(array, i + 4 * 0 + 16 * 2);
            matrix.m21 = BitConverter.ToSingle(array, i + 4 * 1 + 16 * 2);
            matrix.m22 = BitConverter.ToSingle(array, i + 4 * 2 + 16 * 2);
            matrix.m23 = BitConverter.ToSingle(array, i + 4 * 3 + 16 * 2);

            matrix.m30 = BitConverter.ToSingle(array, i + 4 * 0 + 16 * 3);
            matrix.m31 = BitConverter.ToSingle(array, i + 4 * 1 + 16 * 3);
            matrix.m32 = BitConverter.ToSingle(array, i + 4 * 2 + 16 * 3);
            matrix.m33 = BitConverter.ToSingle(array, i + 4 * 3 + 16 * 3);

            lastIndex = i + 4 * 3 + 16 * 3 + 3;
            

            matrices[matrixCounter] = conversionMatrix * matrix;
            ++matrixCounter;
        }
    }
}
