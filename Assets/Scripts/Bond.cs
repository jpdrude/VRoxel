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

    // Blocks a filled cubic meter holds: the GetBlocksIn... queries step through cells of xSize x ySize x zSize spacings,
    // each holding all of the bond's blocks. Used to estimate a volume's block count before generating it.
    public float BlocksPerCubicMeter
    {
        get
        {
            int perCell = 0;
            foreach (Transform[] cellBlocks in blocks)
                perCell += cellBlocks.Length;

            float cell = xSize * xIntSpacing * ySize * yIntSpacing * (float)zSize * zIntSpacing / ((float)containerRes * containerRes * containerRes);
            return cell > 0 ? perCell / cell : 0;
        }
    }

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
        cellBlockPositions = null;

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

    // A volume for GetBlocksInShapes: a sphere around a, a capsule from a to b, a vertical cylinder around a (radius,
    // height), or an axis-aligned box from a (min) to b (max). Blittable, so Burst jobs test it directly.
    public struct BrushShape
    {
        public enum Kind { Sphere, Capsule, Cylinder, Box }

        public Kind kind;
        public Vector3 a;
        public Vector3 b;
        public float radius;
        public float height;
        // Bounding box, so most shapes are skipped for a block with a few comparisons
        public Vector3 min;
        public Vector3 max;

        public static BrushShape Sphere(Vector3 center, float radius)
        {
            return new BrushShape { kind = Kind.Sphere, a = center, b = center, radius = radius, min = center - Vector3.one * radius, max = center + Vector3.one * radius };
        }

        public static BrushShape Capsule(Vector3 a, Vector3 b, float radius)
        {
            return new BrushShape { kind = Kind.Capsule, a = a, b = b, radius = radius, min = Vector3.Min(a, b) - Vector3.one * radius, max = Vector3.Max(a, b) + Vector3.one * radius };
        }

        public static BrushShape Cylinder(Vector3 center, float radius, float height)
        {
            Vector3 extent = new Vector3(radius, height / 2, radius);
            return new BrushShape { kind = Kind.Cylinder, a = center, b = center, radius = radius, height = height, min = center - extent, max = center + extent };
        }

        public static BrushShape Box(Vector3 min, Vector3 max)
        {
            return new BrushShape { kind = Kind.Box, a = min, b = max, min = min, max = max };
        }

        // The same tests as the single-volume queries, with squared distances; a capsule's nearest point is clamped to its axis
        public bool Contains(float3 p)
        {
            if (p.x < min.x || p.y < min.y || p.z < min.z || p.x > max.x || p.y > max.y || p.z > max.z)
                return false;

            float3 start = a;
            switch (kind)
            {
                case Kind.Sphere:
                    return math.lengthsq(p - start) < radius * radius;
                case Kind.Capsule:
                    float3 axis = (float3)b - start;
                    float lengthSquared = math.lengthsq(axis);
                    float t = lengthSquared > 1e-12f ? math.saturate(math.dot(p - start, axis) / lengthSquared) : 0;
                    return math.lengthsq(p - (start + axis * t)) < radius * radius;
                case Kind.Cylinder:
                    float2 across = p.xz - start.xz;
                    return math.lengthsq(across) < radius * radius;
                default:
                    return true;
            }
        }
    }

    // Positions, rotations and grid keys of one cell's blocks relative to the cell's base, read once from the bond's
    // transforms: reading them in the inner loop is what made the single-volume queries slow for brushes
    Vector3[][] cellBlockPositions;
    Quaternion[][] cellBlockRotations;
    Vector3Int[][] cellBlockKeys;

    void CacheCellBlocks()
    {
        if (cellBlockPositions != null && cellBlockPositions.Length == blocks.Length)
            return;

        cellBlockPositions = new Vector3[blocks.Length][];
        cellBlockRotations = new Quaternion[blocks.Length][];
        cellBlockKeys = new Vector3Int[blocks.Length][];

        for (int i = 0; i < blocks.Length; ++i)
        {
            cellBlockPositions[i] = new Vector3[blocks[i].Length];
            cellBlockRotations[i] = new Quaternion[blocks[i].Length];
            cellBlockKeys[i] = new Vector3Int[blocks[i].Length];

            for (int j = 0; j < blocks[i].Length; ++j)
            {
                Transform block = blocks[i][j];
                cellBlockPositions[i][j] = block.localPosition * scale;
                cellBlockRotations[i][j] = block.localRotation;
                cellBlockKeys[i][j] = new Vector3Int(Mathf.RoundToInt(block.localPosition.x * scale * containerRes), Mathf.RoundToInt(block.localPosition.y * scale * containerRes), Mathf.RoundToInt(block.localPosition.z * scale * containerRes));
            }
        }
    }

    // Blocks inside any of several shapes, e.g. all colliders of a character brush or one VR brush volume. Each shape's
    // bounding box gives a range of grid cells (the same as the single-volume queries); a Burst job tests the cells in
    // parallel, each cell once, for the first shape whose range covers it. Blocks inside one of `previous` (the shapes of
    // the stroke's last frame) were handled then and are skipped, so a brush only processes the volume it newly sweeps.
    // keep filters by grid key before a block's matrix is built: removing keeps only placed blocks, adding only free cells.
    public Matrix4x4[][] GetBlocksInShapes(IList<BrushShape> shapes, System.Func<PositionKey, bool> keep, out PositionKey[][] _blockDictKeys, IList<BrushShape> previous = null)
    {
        CacheCellBlocks();

        List<Matrix4x4>[] blockTransforms = new List<Matrix4x4>[blocks.Length];
        List<PositionKey>[] blockDictKeys = new List<PositionKey>[blocks.Length];
        for (int i = 0; i < blocks.Length; ++i)
        {
            blockTransforms[i] = new List<Matrix4x4>();
            blockDictKeys[i] = new List<PositionKey>();
        }

        int3 step = new int3(xSize * xIntSpacing, ySize * yIntSpacing, zSize * zIntSpacing);

        // Each shape's cell range and where its cells start in the job's index space
        NativeArray<BrushShape> shapeArray = new NativeArray<BrushShape>(shapes.Count, Allocator.TempJob);
        NativeArray<int3> rangeMin = new NativeArray<int3>(shapes.Count, Allocator.TempJob);
        NativeArray<int3> rangeCount = new NativeArray<int3>(shapes.Count, Allocator.TempJob);
        NativeArray<int> rangeStart = new NativeArray<int>(shapes.Count, Allocator.TempJob);
        long cells = 0;
        for (int s = 0; s < shapes.Count; ++s)
        {
            shapeArray[s] = shapes[s];

            Vector3Int minInt = GetBondBaseInt(shapes[s].min);
            Vector3 maxScaled = (shapes[s].max - offset) * containerRes;

            if (maxScaled.x - minInt.x < step.x)
                maxScaled.x = minInt.x + step.x * 1.1f;
            if (maxScaled.y - minInt.y < step.y)
                maxScaled.y = minInt.y + step.y * 1.1f;
            if (maxScaled.z - minInt.z < step.z)
                maxScaled.z = minInt.z + step.z * 1.1f;

            maxScaled += new Vector3(step.x * maxExtend.x, step.y * maxExtend.y, step.z * maxExtend.z);

            rangeMin[s] = new int3(minInt.x, minInt.y, minInt.z);
            int3 count = new int3(Mathf.CeilToInt((maxScaled.x - minInt.x) / step.x), Mathf.CeilToInt((maxScaled.y - minInt.y) / step.y), Mathf.CeilToInt((maxScaled.z - minInt.z) / step.z));
            rangeCount[s] = math.max(count, 0);
            rangeStart[s] = (int)cells;
            cells += (long)rangeCount[s].x * rangeCount[s].y * rangeCount[s].z;
        }

        if (cells > int.MaxValue)
        {
            shapeArray.Dispose(); rangeMin.Dispose(); rangeCount.Dispose(); rangeStart.Dispose();
            throw new System.ArgumentException("The shapes cover more than " + int.MaxValue + " grid cells");
        }

        // The bond's blocks, flattened: offset within the cell and part type / index
        int blockCount = 0;
        foreach (Vector3[] positions in cellBlockPositions)
            blockCount += positions.Length;
        NativeArray<float3> blockOffsets = new NativeArray<float3>(blockCount, Allocator.TempJob);
        int[] blockType = new int[blockCount];
        int[] blockIndex = new int[blockCount];
        int flat = 0;
        for (int i = 0; i < cellBlockPositions.Length; ++i)
            for (int j = 0; j < cellBlockPositions[i].Length; ++j, ++flat)
            {
                blockOffsets[flat] = cellBlockPositions[i][j];
                blockType[flat] = i;
                blockIndex[flat] = j;
            }

        NativeArray<BrushShape> previousArray = new NativeArray<BrushShape>(previous != null ? previous.Count : 0, Allocator.TempJob);
        for (int s = 0; s < previousArray.Length; ++s)
            previousArray[s] = previous[s];

        NativeQueue<int4> hits = new NativeQueue<int4>(Allocator.TempJob);
        try
        {
            ShapeBlocksJob job = new ShapeBlocksJob
            {
                shapes = shapeArray,
                previous = previousArray,
                rangeMin = rangeMin,
                rangeCount = rangeCount,
                rangeStart = rangeStart,
                blockOffsets = blockOffsets,
                step = step,
                inverseRes = 1f / containerRes,
                offset = offset,
                hits = hits.AsParallelWriter()
            };
            job.Schedule((int)cells, 64).Complete();

            Vector3 blockScale = Vector3.one * scale;
            while (hits.TryDequeue(out int4 hit))
            {
                int i = blockType[hit.w];
                int j = blockIndex[hit.w];
                Vector3Int cell = new Vector3Int(hit.x, hit.y, hit.z);

                PositionKey key = new PositionKey(cellBlockKeys[i][j] + cell);
                if (keep != null && !keep(key))
                    continue;

                Vector3 blockPosition = (Vector3)cell / containerRes + offset + cellBlockPositions[i][j];
                blockTransforms[i].Add(Matrix4x4.TRS(blockPosition, cellBlockRotations[i][j], blockScale));
                blockDictKeys[i].Add(key);
            }
        }
        finally
        {
            shapeArray.Dispose();
            previousArray.Dispose();
            rangeMin.Dispose();
            rangeCount.Dispose();
            rangeStart.Dispose();
            blockOffsets.Dispose();
            hits.Dispose();
        }

        Matrix4x4[][] _blockTransforms = new Matrix4x4[blocks.Length][];
        _blockDictKeys = new PositionKey[blocks.Length][];
        for (int i = 0; i < blocks.Length; ++i)
        {
            _blockTransforms[i] = blockTransforms[i].ToArray();
            _blockDictKeys[i] = blockDictKeys[i].ToArray();
        }

        return _blockTransforms;
    }

    // One job index per candidate cell: the cell's blocks inside a shape (and not inside a previous one) are hits,
    // given as cell base (grid units) and flattened block index
    [BurstCompile]
    struct ShapeBlocksJob : IJobParallelFor
    {
        [ReadOnly] public NativeArray<BrushShape> shapes;
        [ReadOnly] public NativeArray<BrushShape> previous;
        [ReadOnly] public NativeArray<int3> rangeMin;
        [ReadOnly] public NativeArray<int3> rangeCount;
        [ReadOnly] public NativeArray<int> rangeStart;
        [ReadOnly] public NativeArray<float3> blockOffsets;
        public int3 step;
        public float inverseRes;
        public float3 offset;

        public NativeQueue<int4>.ParallelWriter hits;

        public void Execute(int index)
        {
            // The shape whose range holds this index
            int s = 0;
            int high = rangeStart.Length - 1;
            while (s < high)
            {
                int middle = (s + high + 1) / 2;
                if (rangeStart[middle] <= index) s = middle;
                else high = middle - 1;
            }

            int local = index - rangeStart[s];
            int3 count = rangeCount[s];
            int3 cell = rangeMin[s] + new int3(local % count.x, (local / count.x) % count.y, local / (count.x * count.y)) * step;

            // A cell an earlier shape's range covers is tested there
            for (int t = 0; t < s; ++t)
            {
                int3 end = rangeMin[t] + rangeCount[t] * step;
                if (math.all(cell >= rangeMin[t]) && math.all(cell < end))
                    return;
            }

            float3 cellBase = (float3)cell * inverseRes + offset;
            for (int b = 0; b < blockOffsets.Length; ++b)
            {
                float3 position = cellBase + blockOffsets[b];
                if (Inside(shapes, position) && !Inside(previous, position))
                    hits.Enqueue(new int4(cell, b));
            }
        }

        static bool Inside(NativeArray<BrushShape> list, float3 position)
        {
            for (int s = 0; s < list.Length; ++s)
                if (list[s].Contains(position))
                    return true;
            return false;
        }
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
