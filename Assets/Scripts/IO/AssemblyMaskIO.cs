using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System;

public class AssemblyMaskIO
{
    public static void CheckAssemblyWithMask(string path)
    {
        if (!File.Exists(path)) return;

        PositionKey[][] mask = LoadMaskFile(path);

        BlockContainer.Container.MaskAssembly(mask);
    }

    static PositionKey[][] LoadMaskFile(string path)
    {
        PositionKey[][] mask;

        byte[] maskArray = File.ReadAllBytes(path);

        int blockType = maskArray[0];

        mask = new PositionKey[blockType][];

        int counter = 1;

        for (int i = 0; i < blockType; ++i)
        {
            int count = BitConverter.ToInt32(maskArray, counter);
            counter += 4;

            mask[i] = new PositionKey[count];

            for (int j = 0; j < count; ++j)
            {
                int x = BitConverter.ToInt32(maskArray, counter);
                counter += 4;
                int y = BitConverter.ToInt32(maskArray, counter);
                counter += 4;
                int z = BitConverter.ToInt32(maskArray, counter);
                counter += 4;

                mask[i][j] = new PositionKey(x, y, z);
            }
        }

        return mask;
    }
}
