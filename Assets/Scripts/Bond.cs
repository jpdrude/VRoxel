using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using GPUInstancer;

using Unity.Jobs;
using Unity.Burst;
using Unity.Mathematics;
using Unity.Collections;

using System;

public class Bond : MonoBehaviour
{
    [Header("BondInfo")]
    [SerializeField]
    string bondName;

    [Header("Parts")]
    [SerializeField]
    GPUInstancerPrefab[] partPrefabs;
    
    [SerializeField]
    PartTypeArray[] bondBlocks;
    public PartTypeArray[] BondBlocks { get { return bondBlocks; } }

    Transform[][] blocks;

    [Space]
    [Header("Scale")]
    [SerializeField]
    float scale = 1;

    [SerializeField]
    int containerResolutionDepth = 4;

    int containerRes = 1000;
    public int ContainerRes { get { return containerRes; } }

    public float Scale 
    { 
        get { return scale; }
        set
        {
            scale = value;

            scaledXSpacing = xSpacing * scale;
            scaledYSpacing = ySpacing * scale;
            scaledZSpacing = zSpacing * scale;

            xIntSpacing = Mathf.RoundToInt(scaledXSpacing * containerRes);
            yIntSpacing = Mathf.RoundToInt(scaledYSpacing * containerRes);
            zIntSpacing = Mathf.RoundToInt(scaledZSpacing * containerRes);
        }
    }

    [Space]
    [Header("Offset")]
    [SerializeField]
    int xSize;
    [SerializeField]
    int ySize;
    [SerializeField]
    int zSize;

    [Space]
    [SerializeField]
    float xSpacing;
    [SerializeField]
    float ySpacing;
    [SerializeField]
    float zSpacing;

    float scaledXSpacing;
    float scaledYSpacing;
    float scaledZSpacing;

    [Space]
    [Header("Global Offset")]
    [SerializeField]
    Vector3 defaultOffset;

    [Space]
    [Header("Extend")]
    [SerializeField]
    Vector3Int minExtend = new Vector3Int();
    [SerializeField]
    Vector3Int maxExtend = new Vector3Int();

    [Space]
    [Header("Align Orientations")]
    [SerializeField]
    List<Vector3> orientations;

    int orientationIndex = 0;

    int layerMask = 1 << 8;

    public float XSpacing { get { return scaledXSpacing; } }
    public float YSpacing { get { return scaledYSpacing; } }
    public float ZSpacing { get { return scaledZSpacing; } }

    int xIntSpacing;
    int yIntSpacing;
    int zIntSpacing;

    public int XIntSpacing { get { return Mathf.RoundToInt(scaledXSpacing * containerRes); } }
    public int YIntSpacing { get { return Mathf.RoundToInt(scaledYSpacing * containerRes); } }
    public int ZIntSpacing { get { return Mathf.RoundToInt(scaledZSpacing * containerRes); } }
    
    public GPUInstancerPrefab[] PartPrefabs { get { return partPrefabs; } }

    public string BondName { get { return bondName; } }

    Vector3 offset;
    public Vector3 Offset 
    { 
        get { return offset; } 
        set
        {
            BlockContainer.Container.RecalculateOffset(value, offset);
            offset = value;
        }
    }

    public static MeshBrushLevels meshBrushLevel = MeshBrushLevels.fine;

    public void Initialize()
    {
        containerRes = (int)Math.Pow(10, containerResolutionDepth);

        blocks = new Transform[bondBlocks.Length][];

        for (int i = 0; i < bondBlocks.Length; ++i)
            blocks[i] = bondBlocks[i].blocks;

        offset = defaultOffset;

        scaledXSpacing = xSpacing * scale;
        scaledYSpacing = ySpacing * scale;
        scaledZSpacing = zSpacing * scale;

        xIntSpacing = Mathf.RoundToInt(scaledXSpacing * containerRes);
        yIntSpacing = Mathf.RoundToInt(scaledYSpacing * containerRes);
        zIntSpacing = Mathf.RoundToInt(scaledZSpacing * containerRes);
    }

    public void SetBondBlocks(Transform[] transforms)
    {
        PartTypeArray[] bb = new PartTypeArray[partPrefabs.Length];

        for (int i = 0; i < partPrefabs.Length; ++i)
        {
            List<Transform> prefabTransforms = new List<Transform>();
            foreach (Transform t in transforms)
            {
                if (t.gameObject.GetComponent<GPUInstancerPrefab>().prefabPrototype.name == PartPrefabs[i].prefabPrototype.name)
                    prefabTransforms.Add(t);
            }

            bb[i] = new PartTypeArray { blocks = prefabTransforms.ToArray() };
        }

        bondBlocks = bb;
    }

    public void SetOrientations(List<Vector3> _orientations)
    {
        orientations = _orientations;
    }

    public Vector3 GetBondBase(Vector3 position)
    {
        int x = Mathf.FloorToInt((position.x - (offset.x * scale)) / scaledXSpacing);
        int y = Mathf.FloorToInt((position.y - (offset.y * scale)) / scaledYSpacing);
        int z = Mathf.FloorToInt((position.z - (offset.z * scale)) / scaledZSpacing);

        x /= xSize;
        y /= ySize;
        z /= zSize;
        x *= xSize;
        y *= ySize;
        z *= zSize;

        x += xSize * minExtend.x;
        y += ySize * minExtend.y;
        z += zSize * minExtend.z;

        Vector3 bondBase = new Vector3(x * scaledXSpacing, y * scaledYSpacing, z * scaledZSpacing);
        return bondBase;
    }

    public Vector3Int GetBondBaseInt(Vector3 position)
    {
        int x = Mathf.FloorToInt((position.x - offset.x) * containerRes) / xIntSpacing;
        int y = Mathf.FloorToInt((position.y - offset.y) * containerRes) / yIntSpacing;
        int z = Mathf.FloorToInt((position.z - offset.z) * containerRes) / zIntSpacing;

        x /= xSize;
        y /= ySize;
        z /= zSize;
        x *= xSize;
        y *= ySize;
        z *= zSize;

        x += xSize * minExtend.x;
        y += ySize * minExtend.y;
        z += zSize * minExtend.z;

        Vector3Int bondBase = new Vector3Int(x * xIntSpacing, y * yIntSpacing, z * zIntSpacing);
        return bondBase;
    }

    public Matrix4x4 GetBlockFromPosition(Vector3 position, int type)
    {
        position -= offset;

        int xInt = Mathf.RoundToInt(position.x * containerRes);
        int yInt = Mathf.RoundToInt(position.y * containerRes);
        int zInt = Mathf.RoundToInt(position.z * containerRes);

        int xBondSize = xIntSpacing * xSize;
        int yBondSize = yIntSpacing * ySize;
        int zBondSize = zIntSpacing * zSize;

        Transform fit = null;

        foreach(Transform t in blocks[type])
        {
            int xBondInt = Mathf.RoundToInt(t.position.x * scale * containerRes);
            int yBondInt = Mathf.RoundToInt(t.position.y * scale * containerRes);
            int zBondInt = Mathf.RoundToInt(t.position.z * scale * containerRes);

            if ((xInt - xBondInt) % xBondSize == 0 && (yInt - yBondInt) % yBondSize == 0 && (zInt - zBondInt) % zBondSize == 0)
                fit = t;
        }

        if (fit == null)
            return Matrix4x4.TRS(position + offset, Quaternion.identity, Vector3.one * scale);

        return Matrix4x4.TRS(position + offset, fit.localRotation, Vector3.one * scale);
    }

    //Doesn't work yet!
    public Matrix4x4 GetClosestBlockFromPosition(Vector3 position, int type)
    {
        int xInt = Mathf.RoundToInt(position.x * containerRes);
        int yInt = Mathf.RoundToInt(position.y * containerRes);
        int zInt = Mathf.RoundToInt(position.z * containerRes);

        int xBondSize = xIntSpacing * xSize;
        int yBondSize = yIntSpacing * ySize;
        int zBondSize = zIntSpacing * zSize;

        Transform fit = null;
        int bestError = int.MaxValue;

        foreach (Transform t in blocks[type])
        {
            int xBondInt = Mathf.RoundToInt(t.position.x * scale * containerRes);
            int yBondInt = Mathf.RoundToInt(t.position.y * scale * containerRes);
            int zBondInt = Mathf.RoundToInt(t.position.z * scale * containerRes);

            int error = (xInt - xBondInt) % xBondSize + (yInt - yBondInt) % yBondSize + (zInt - zBondInt) % zBondSize;

            if (error < bestError)
            {
                fit = t;
                bestError = error;
            }  
        }

        xInt = xInt / xBondSize;
        yInt = yInt / yBondSize;
        zInt = zInt / zBondSize;

        xInt *= xBondSize;
        yInt *= yBondSize;
        zInt *= zBondSize;

        position = new Vector3(xInt / (float)containerRes, yInt / (float)containerRes, zInt / (float)containerRes);

        if (fit == null)
            return Matrix4x4.TRS(position, Quaternion.identity, Vector3.one * scale);

        return Matrix4x4.TRS(position, fit.localRotation, Vector3.one * scale);
    }

    public Matrix4x4[][] GetBlocksInBox(Vector3 min, Vector3 max, out PositionKey[][] _blockDictsKeys)
    {
        /*  
         *  offset is used for representational 
         *  geometry only.
         *  Dictionary keys are not offset!
         */

        Vector3Int minInt = GetBondBaseInt(min);
        Vector3 maxScaled = (max - offset) * containerRes;

        if (maxScaled.x - minInt.x < xSize * xIntSpacing)
            maxScaled.x = minInt.x + xSize * xIntSpacing * 1.1f;
        if (maxScaled.y - minInt.y < ySize * yIntSpacing)
            maxScaled.y = minInt.y + ySize * yIntSpacing * 1.1f;
        if (maxScaled.z - minInt.z < zSize * zIntSpacing)
            maxScaled.z = minInt.z + zSize * zIntSpacing * 1.1f;

        maxScaled += new Vector3(xSize * xIntSpacing * maxExtend.x, ySize * yIntSpacing * maxExtend.y, zSize * zIntSpacing * maxExtend.z);

        List<Matrix4x4>[] blockTransforms = new List<Matrix4x4>[blocks.Length];
        List<PositionKey>[] blockDictKeys = new List<PositionKey>[blocks.Length];

        for (int i = 0; i < blockTransforms.Length; ++i)
        {
            blockTransforms[i] = new List<Matrix4x4>();
            blockDictKeys[i] = new List<PositionKey>();
        }

        for (int x = minInt.x; x < maxScaled.x; x += xSize * xIntSpacing)
        {
            for (int y = minInt.y; y < maxScaled.y; y += ySize * yIntSpacing)
            {
                for (int z = minInt.z; z < maxScaled.z; z += zSize * zIntSpacing)
                {
                    for (int i = 0; i < blocks.Length; ++i)
                    {
                        for (int j = 0; j < blocks[i].Length; ++j)
                        {
                            Transform block = blocks[i][j].transform;
                            Vector3 blockPosition = block.localPosition * scale + new Vector3Int(x, y, z).Divide(containerRes) + offset;
                            if (blockPosition.x >= min.x && blockPosition.y >= min.y && blockPosition.z >= min.z && blockPosition.x <= max.x && blockPosition.y <= max.y && blockPosition.z <= max.z)
                            {
                                blockTransforms[i].Add(Matrix4x4.TRS(blockPosition, block.localRotation, Vector3.one * scale));
                                Vector3Int localIntPosition = new Vector3Int(Mathf.RoundToInt(block.localPosition.x * scale * containerRes), Mathf.RoundToInt(block.localPosition.y * scale * containerRes), Mathf.RoundToInt(block.localPosition.z * scale * containerRes));
                                blockDictKeys[i].Add(new PositionKey(localIntPosition + new Vector3Int(x, y, z)));
                            }
                        }
                    }
                }
            }
        }

        Matrix4x4[][] _blockTransforms = new Matrix4x4[blocks.Length][];
        _blockDictsKeys = new PositionKey[blocks.Length][];

        for (int i = 0; i < blockTransforms.Length; ++i)
        {
            _blockTransforms[i] = blockTransforms[i].ToArray();
            _blockDictsKeys[i] = blockDictKeys[i].ToArray();
        }

        return _blockTransforms;
    }

    public Matrix4x4[][] GetBlocksInSphere(Vector3 position, float radius, out PositionKey[][] _blockDictKeys)
    {
        List<Matrix4x4>[] blockTransforms = new List<Matrix4x4>[blocks.Length];
        List<PositionKey>[] blockDictKeys = new List<PositionKey>[blocks.Length];

        int blockCounter = 0;

        Vector3Int min = GetBondBaseInt(position - Vector3.one * radius);
        Vector3 max = (position - offset + Vector3.one * radius) * containerRes;

        if (max.x - min.x < xSize * xIntSpacing)
            max.x = min.x + xSize * xIntSpacing * 1.1f;
        if (max.y - min.y < ySize * yIntSpacing)
            max.y = min.y + ySize * yIntSpacing * 1.1f;
        if (max.z - min.z < zSize * zIntSpacing)
            max.z = min.z + zSize * zIntSpacing * 1.1f;

        max += new Vector3(xSize * xIntSpacing * maxExtend.x, ySize * yIntSpacing * maxExtend.y, zSize * zIntSpacing * maxExtend.z);

        for (int i = 0; i < blockTransforms.Length; ++i)
        {
            blockTransforms[i] = new List<Matrix4x4>();
            blockDictKeys[i] = new List<PositionKey>();
        }

        for (int x = min.x; x < max.x; x += xSize * xIntSpacing)
        {
            for (int y = min.y; y < max.y; y += ySize * yIntSpacing)
            {
                for (int z = min.z; z < max.z; z += zSize * zIntSpacing)
                {
                    for (int i = 0; i < blocks.Length; ++i)
                    {
                        for (int j = 0; j < blocks[i].Length; ++j)
                        {
                            if (Vector3.Distance(blocks[i][j].localPosition * scale + new Vector3Int(x, y, z).Divide(containerRes), position - offset) < radius)
                            {
                                
                                Transform block = blocks[i][j].transform;   
                                blockTransforms[i].Add(Matrix4x4.TRS(block.localPosition * scale + new Vector3Int(x, y, z).Divide(containerRes) + offset, block.localRotation, Vector3.one * scale));
                                Vector3Int localIntPosition = new Vector3Int(Mathf.RoundToInt(block.localPosition.x * scale * containerRes), Mathf.RoundToInt(block.localPosition.y * scale * containerRes), Mathf.RoundToInt(block.localPosition.z * scale * containerRes));
                                blockDictKeys[i].Add(new PositionKey(localIntPosition + new Vector3Int(x, y, z)));
                                ++blockCounter;
                                
                            }
                        }
                    }
                }
            }
        }

        /*
         * Not yet working!
        if (blockCounter == 0)
        {
            KeyValuePair<int, Matrix4x4> bestBlock = new KeyValuePair<int, Matrix4x4>(0, new Matrix4x4());
            float bestDistance = float.MaxValue;

            for (int i = 0; i < blocks.Length; ++i)
            {
                Matrix4x4 b = GetClosestBlockFromPosition(position, i);
                float dist = Vector3.Distance(b.GetPosition(), position);
                if (dist < bestDistance)// && dist < 0.02f)
                {
                    bestDistance = dist;
                    bestBlock = new KeyValuePair<int, Matrix4x4>(i, b);
                }
            }
             
            if (bestDistance != float.MaxValue)
            {
                blockTransforms[bestBlock.Key].Add(bestBlock.Value);
                blockDictKeys[bestBlock.Key].Add(new PositionKey(bestBlock.Value));
            }
        }
        */

        Matrix4x4[][] _blockTransforms = new Matrix4x4[blocks.Length][];
        _blockDictKeys = new PositionKey[blocks.Length][];

        for (int i = 0; i < blockTransforms.Length; ++i)
        {
            _blockTransforms[i] = blockTransforms[i].ToArray();
            _blockDictKeys[i] = blockDictKeys[i].ToArray();
        }

        return _blockTransforms;
    }

    public Matrix4x4[][] GetBlocksInCylinder(Vector3 position, float radius, float height, out PositionKey[][] _blockDictKeys)
    {
        Vector3 min = position - new Vector3(radius, height / 2, radius);
        Vector3 max = position + new Vector3(radius, height / 2, radius);

        Vector3Int minInt = GetBondBaseInt(min);
        Vector3 maxScaled = (max - offset) * containerRes;

        if (maxScaled.x - minInt.x < xSize * xIntSpacing)
            maxScaled.x = minInt.x + xSize * xIntSpacing * 1.1f;
        if (maxScaled.y - minInt.y < ySize * yIntSpacing)
            maxScaled.y = minInt.y + ySize * yIntSpacing * 1.1f;
        if (maxScaled.z - minInt.z < zSize * zIntSpacing)
            maxScaled.z = minInt.z + zSize * zIntSpacing * 1.1f;

        maxScaled += new Vector3(xSize * xIntSpacing * maxExtend.x, ySize * yIntSpacing * maxExtend.y, zSize * zIntSpacing * maxExtend.z);

        List<Matrix4x4>[] blockTransforms = new List<Matrix4x4>[blocks.Length];
        List<PositionKey>[] blockDictKeys = new List<PositionKey>[blocks.Length];

        for (int i = 0; i < blockTransforms.Length; ++i)
        {
            blockTransforms[i] = new List<Matrix4x4>();
            blockDictKeys[i] = new List<PositionKey>();
        }

        for (int x = minInt.x; x < maxScaled.x; x += xSize * xIntSpacing)
        {
            for (int y = minInt.y; y < maxScaled.y; y += ySize * yIntSpacing)
            {
                for (int z = minInt.z; z < maxScaled.z; z += zSize * zIntSpacing)
                {
                    for (int i = 0; i < blocks.Length; ++i)
                    {
                        for (int j = 0; j < blocks[i].Length; ++j)
                        {
                            Transform block = blocks[i][j];
                            Vector3 blockPosition = block.localPosition * scale + offset + new Vector3Int(x, y, z).Divide(containerRes);

                            bool inRadius = Vector3.Distance(new Vector3(position.x, 0, position.z), new Vector3(blockPosition.x, 0, blockPosition.z)) < radius;

                            if (inRadius && blockPosition.y <= position.y + height / 2 && blockPosition.y >= position.y - height / 2)
                            {
                                blockTransforms[i].Add(Matrix4x4.TRS(blockPosition, block.localRotation, Vector3.one * scale));
                                Vector3Int localIntPosition = new Vector3Int(Mathf.RoundToInt(block.localPosition.x * scale * containerRes), Mathf.RoundToInt(block.localPosition.y * scale * containerRes), Mathf.RoundToInt(block.localPosition.z * scale * containerRes));
                                blockDictKeys[i].Add(new PositionKey(localIntPosition + new Vector3Int(x, y, z)));
                            }
                        }
                    }
                }
            }
        }        

        Matrix4x4[][] _blockTransforms = new Matrix4x4[blocks.Length][];
        _blockDictKeys = new PositionKey[blocks.Length][];

        for (int i = 0; i < blockTransforms.Length; ++i)
        {
            _blockTransforms[i] = blockTransforms[i].ToArray();
            _blockDictKeys[i] = blockDictKeys[i].ToArray();
        }

        return _blockTransforms;
    }

    public Matrix4x4[][] GetBlocksInCapsule(Vector3 positionA, Vector3 positionB, float radius, out PositionKey[][] _blockDictKeys)
    {
        Vector3 min = positionA;
        if (positionB.x < min.x) min.x = positionB.x;
        if (positionB.y < min.y) min.y = positionB.y;
        if (positionB.z < min.z) min.z = positionB.z;
        min -= Vector3.one * radius;

        Vector3 max = positionB;
        if (positionA.x > max.x) max.x = positionA.x;
        if (positionA.y > max.y) max.y = positionA.y;
        if (positionA.z > max.z) max.z = positionA.z;
        max += Vector3.one * radius;

        Vector3Int minInt = GetBondBaseInt(min);
        Vector3 maxScaled = (max - offset) * containerRes;

        if (maxScaled.x - minInt.x < xSize * xIntSpacing)
            maxScaled.x = minInt.x + xSize * xIntSpacing * 1.1f;
        if (maxScaled.y - minInt.y < ySize * yIntSpacing)
            maxScaled.y = minInt.y + ySize * yIntSpacing * 1.1f;
        if (maxScaled.z - minInt.z < zSize * zIntSpacing)
            maxScaled.z = minInt.z + zSize * zIntSpacing * 1.1f;

        maxScaled += new Vector3(xSize * xIntSpacing * maxExtend.x, ySize * yIntSpacing * maxExtend.y, zSize * zIntSpacing * maxExtend.z);

        List<Matrix4x4>[] blockTransforms = new List<Matrix4x4>[blocks.Length];
        List<PositionKey>[] blockDictKeys = new List<PositionKey>[blocks.Length];

        for (int i = 0; i < blockTransforms.Length; ++i)
        {
            blockTransforms[i] = new List<Matrix4x4>();
            blockDictKeys[i] = new List<PositionKey>();
        }

        for (int x = minInt.x; x < maxScaled.x; x += xSize * xIntSpacing)
        {
            for (int y = minInt.y; y < maxScaled.y; y += ySize * yIntSpacing)
            {
                for (int z = minInt.z; z < maxScaled.z; z += zSize * zIntSpacing)
                {
                    for (int i = 0; i < blocks.Length; ++i)
                    {
                        for (int j = 0; j < blocks[i].Length; ++j)
                        {
                            Transform block = blocks[i][j];
                            Vector3 blockPosition = block.localPosition * scale + offset + new Vector3Int(x, y, z).Divide(containerRes);

                            bool inCylinder = Vector3.Distance(blockPosition, NearestPointOnLine(positionA, positionB, blockPosition)) < radius;

                            if (inCylinder)
                            {
                                blockTransforms[i].Add(Matrix4x4.TRS(blockPosition, block.localRotation, Vector3.one * scale));
                                Vector3Int localIntPosition = new Vector3Int(Mathf.RoundToInt(block.localPosition.x * scale * containerRes), Mathf.RoundToInt(block.localPosition.y * scale * containerRes), Mathf.RoundToInt(block.localPosition.z * scale * containerRes));
                                blockDictKeys[i].Add(new PositionKey(localIntPosition + new Vector3Int(x, y, z)));
                            }
                        }
                    }
                }
            }
        }

        Matrix4x4[][] _blockTransforms = new Matrix4x4[blocks.Length][];
        _blockDictKeys = new PositionKey[blocks.Length][];

        for (int i = 0; i < blockTransforms.Length; ++i)
        {
            _blockTransforms[i] = blockTransforms[i].ToArray();
            _blockDictKeys[i] = blockDictKeys[i].ToArray();
        }

        return _blockTransforms;
    }

    public static Vector3 NearestPointOnLine(Vector3 lineStart, Vector3 lineEnd, Vector3 pnt)
    {
        Vector3 lineDir = lineEnd - lineStart;
        lineDir.Normalize();
        var v = pnt - lineStart;
        var d = Vector3.Dot(v, lineDir);

        Vector3 cp = lineStart + lineDir * d;

        if (Vector3.Distance(lineStart, lineEnd) < Vector3.Distance(lineStart, cp))
            cp = lineEnd;

        if (Vector3.Distance(lineEnd, lineStart) < Vector3.Distance(lineEnd, cp))
            cp = lineStart;

        return cp;
    }

    public Matrix4x4[][] GetBlocksInMesh(Collider mesh, out PositionKey[][] _blockDictKeys)
    {
        Vector3Int minInt = GetBondBaseInt(mesh.bounds.min);
        Vector3 maxScaled = (mesh.bounds.max - offset) * containerRes;
        float rayLength = mesh.bounds.max.x - mesh.bounds.min.x + 0.1f;

        if (maxScaled.x - minInt.x < xSize * xIntSpacing)
            maxScaled.x = minInt.x + xSize * xIntSpacing * 1.1f;
        if (maxScaled.y - minInt.y < ySize * yIntSpacing)
            maxScaled.y = minInt.y + ySize * yIntSpacing * 1.1f;
        if (maxScaled.z - minInt.z < zSize * zIntSpacing)
            maxScaled.z = minInt.z + zSize * zIntSpacing * 1.1f;

        maxScaled += new Vector3(xSize * xIntSpacing * maxExtend.x, ySize * yIntSpacing * maxExtend.y, zSize * zIntSpacing * maxExtend.z);

        List<Matrix4x4>[] blockTransforms = new List<Matrix4x4>[blocks.Length];
        List<PositionKey>[] blockDictKeys = new List<PositionKey>[blocks.Length];

        Mesh m = ((MeshCollider)mesh).sharedMesh;
        Vector3[] vertices = m.vertices;
        int[] triangles = m.triangles;

        Matrix4x4 localToWorld = mesh.gameObject.transform.localToWorldMatrix;
        NativeArray<Vector3> worldVertices = new NativeArray<Vector3>(vertices.Length, Allocator.TempJob);
        for (int i = 0; i < vertices.Length; ++i)
            worldVertices[i] = localToWorld.MultiplyPoint3x4(vertices[i]);


        NativeArray<int> triangleArray = new NativeArray<int>(triangles, Allocator.TempJob);

        List<Vector3> checkPoints = new List<Vector3>();
        List<Transform> checkBlocks = new List<Transform>();
        List<Vector3Int> checkIntPos = new List<Vector3Int>();
        List<int> checkPresetIndex = new List<int>();

        for (int i = 0; i < blockTransforms.Length; ++i)
        {
            blockTransforms[i] = new List<Matrix4x4>();
            blockDictKeys[i] = new List<PositionKey>();
        }

        for (int x = minInt.x; x < maxScaled.x; x += xSize * xIntSpacing)
        {
            for (int y = minInt.y; y < maxScaled.y; y += ySize * yIntSpacing)
            {
                for (int z = minInt.z; z < maxScaled.z; z += zSize * zIntSpacing)
                {
                    for (int i = 0; i < blocks.Length; ++i)
                    {
                        for (int j = 0; j < blocks[i].Length; ++j)
                        {
                            Transform block = blocks[i][j];
                            Vector3 blockPosition = block.localPosition * scale + offset + new Vector3Int(x, y, z).Divide(containerRes);

                            //Run Jobs based custom check
                            if (meshBrushLevel == MeshBrushLevels.fine)
                            {
                                checkPoints.Add(blockPosition);
                                checkBlocks.Add(block);
                                checkIntPos.Add(new Vector3Int(x, y, z));
                                checkPresetIndex.Add(i);
                            }
                            else // Run old check
                            {
                                bool pointInMesh = PointInMesh(blockPosition, rayLength, Vector3.right);

                                if (pointInMesh)
                                {
                                    blockTransforms[i].Add(Matrix4x4.TRS(blockPosition, block.localRotation, Vector3.one * scale));
                                    Vector3Int localIntPosition = new Vector3Int(Mathf.RoundToInt(block.localPosition.x * scale * containerRes), Mathf.RoundToInt(block.localPosition.y * scale * containerRes), Mathf.RoundToInt(block.localPosition.z * scale * containerRes));
                                    blockDictKeys[i].Add(new PositionKey(localIntPosition + new Vector3Int(x, y, z)));
                                }
                            }
                        }
                    }
                }
            }
        }

        if (meshBrushLevel == MeshBrushLevels.fine)
        {
            //Create Arrays for Checks and Results
            NativeArray<Vector3> pointArray = new NativeArray<Vector3>(checkPoints.ToArray(), Allocator.TempJob);
            NativeArray<bool> resultArray = new NativeArray<bool>(checkPoints.Count, Allocator.TempJob);

            //Create and run Job
            PointInMeshJob job = new PointInMeshJob
            {
                testPoints = pointArray,
                worldVertices = worldVertices,
                triangles = triangleArray,
                isInside = resultArray
            };

            JobHandle handle = job.Schedule(checkPoints.Count, 64);
            handle.Complete();

            //Set Results
            for (int i = 0; i < checkPoints.Count; i++)
            {
                if (resultArray[i])
                {
                    blockTransforms[checkPresetIndex[i]].Add(Matrix4x4.TRS(checkPoints[i], checkBlocks[i].localRotation, Vector3.one * scale));
                    Vector3Int localIntPosition = new Vector3Int(Mathf.RoundToInt(checkBlocks[i].localPosition.x * scale * containerRes), Mathf.RoundToInt(checkBlocks[i].localPosition.y * scale * containerRes), Mathf.RoundToInt(checkBlocks[i].localPosition.z * scale * containerRes));
                    blockDictKeys[checkPresetIndex[i]].Add(new PositionKey(localIntPosition + checkIntPos[i]));
                }
            }

            // Cleanup
            pointArray.Dispose();
            resultArray.Dispose();
            worldVertices.Dispose();
            triangleArray.Dispose();
        }

        Matrix4x4[][] _blockTransforms = new Matrix4x4[blocks.Length][];
        _blockDictKeys = new PositionKey[blocks.Length][];

        for (int i = 0; i < blockTransforms.Length; ++i)
        {
            _blockTransforms[i] = blockTransforms[i].ToArray();
            _blockDictKeys[i] = blockDictKeys[i].ToArray();
        }

        return _blockTransforms;
    }

    bool PointInMesh(Vector3 pos, float rayLength, Vector3 rayDirection)
    {
        Physics.queriesHitBackfaces = true;
        Ray ray = new Ray(pos, rayDirection);
        RaycastHit hit;
        int hits = 0;

        while (true)
        {
            Physics.Raycast(ray, out hit, rayLength, layerMask);
            if (hit.collider == null)
                break;

            ++hits;
            ray = new Ray(hit.point + rayDirection * 0.0001f, rayDirection);
        }


        if (hits % 2 == 1)
        {
            return true;
        }
        else
            return false;
    }

    [BurstCompile]
    public struct PointInMeshJob : IJobParallelFor
    {
        [ReadOnly] public NativeArray<Vector3> testPoints;
        [ReadOnly] public NativeArray<Vector3> worldVertices;
        [ReadOnly] public NativeArray<int> triangles;

        [WriteOnly] public NativeArray<bool> isInside;

        public void Execute(int index)
        {
            Vector3 point = testPoints[index];
            Vector3 rayDir = Vector3.right; // Constant direction to avoid branching
            int hitCount = 0;

            for (int i = 0; i < triangles.Length; i += 3)
            {
                Vector3 v0 = worldVertices[triangles[i]];
                Vector3 v1 = worldVertices[triangles[i + 1]];
                Vector3 v2 = worldVertices[triangles[i + 2]];

                if (RayIntersectsTriangle(point, rayDir, v0, v1, v2))
                {
                    hitCount++;
                }
            }

            isInside[index] = (hitCount % 2 == 1); // Odd number of hits = inside
        }

        private static bool RayIntersectsTriangle(Vector3 orig, Vector3 dir, Vector3 v0, Vector3 v1, Vector3 v2)
        {
            const float EPSILON = 1e-8f;
            Vector3 edge1 = v1 - v0;
            Vector3 edge2 = v2 - v0;
            Vector3 h = Vector3.Cross(dir, edge2);
            float a = Vector3.Dot(edge1, h);

            if (math.abs(a) < EPSILON) return false;

            float f = 1.0f / a;
            Vector3 s = orig - v0;
            float u = f * Vector3.Dot(s, h);
            if (u < 0.0f || u > 1.0f) return false;

            Vector3 q = Vector3.Cross(s, edge1);
            float v = f * Vector3.Dot(dir, q);
            if (v < 0.0f || u + v > 1.0f) return false;

            float t = f * Vector3.Dot(edge2, q);
            return t > EPSILON;
        }
    }
    
    public Vector3 GetCurrentOrientation()
    {
        if (orientations == null || orientations.Count == 0) return Vector3.zero;

        return orientations[orientationIndex];
    }

    public Vector3 GetNextOrientation()
    {
        if (orientations == null || orientations.Count == 0) return Vector3.zero;

        ++orientationIndex;
        if (orientationIndex >= orientations.Count)
            orientationIndex = 0;

        return orientations[orientationIndex];
    }

    public Vector3 GetPreviousOrientation()
    {
        if (orientations == null || orientations.Count == 0) return Vector3.zero;

        --orientationIndex;
        if (orientationIndex < 0)
            orientationIndex = orientations.Count - 1;

        return orientations[orientationIndex];
    }
}

public enum MeshBrushLevels
{
    rough,
    fine
}

[Serializable]
public struct PartTypeArray
{ 
    [SerializeField]
    public Transform[] blocks;
}
