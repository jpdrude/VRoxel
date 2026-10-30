using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class CharacterSpawner : MonoBehaviour
{
    [Header("Add/Subtract")]
    [SerializeField]
    bool removeBlocks = false;
    public bool RemoveBlocks { get { return removeBlocks; } }
    [SerializeField]
    bool addBlocks = false;
    public bool AddBlocks { get { return addBlocks; } }

    SphereCollider[] sphereColliders = null;
    CapsuleCollider[] capsuleColliders = null;

    [SerializeField]
    int historyBatchSize = 25;
    int strokeCounter = 0;

    DoAddStroke currentAddStroke = new DoAddStroke(DoAction.DoType.AddStrocke);
    DoRemoveStroke currentRemoveStroke = new DoRemoveStroke(DoAction.DoType.RemoveStroke);

    readonly List<Bond.BrushShape> shapes = new List<Bond.BrushShape>();
    // The colliders of the last brushed frame: what they covered is done, so each frame only processes the newly swept volume
    readonly List<Bond.BrushShape> previousShapes = new List<Bond.BrushShape>();

    // Start is called before the first frame update
    void Start()
    {
        sphereColliders = GetComponentsInChildren<SphereCollider>();
        capsuleColliders = GetComponentsInChildren<CapsuleCollider>();
    }

    public void AddCharacterBrush()
    {
        if (removeBlocks)
        {
            if (currentRemoveStroke.StrokeLength > 0)
                UndoStack.Push(currentRemoveStroke);
            currentRemoveStroke = new DoRemoveStroke(DoAction.DoType.RemoveStroke);
        }

        addBlocks = true;
        removeBlocks = false;
        previousShapes.Clear();

        AnimationIO.constantAnimationTime = true;
        enabled = true;
    }

    public void RemoveCharacterBrush()
    {
        if (addBlocks)
        {
            if (currentAddStroke.StrokeLength > 0)
                UndoStack.Push(currentAddStroke);
            currentAddStroke = new DoAddStroke(DoAction.DoType.AddStrocke);
        }


        removeBlocks = true;
        addBlocks = false;
        previousShapes.Clear();

        AnimationIO.constantAnimationTime = true;
        enabled = true;
    }

    public void DisableCharacterBrush()
    {
        if (!removeBlocks && !addBlocks) return;

        if (removeBlocks)
        {
            if (currentRemoveStroke.StrokeLength > 0)
                UndoStack.Push(currentRemoveStroke);
            currentRemoveStroke = new DoRemoveStroke(DoAction.DoType.RemoveStroke);
        }
        else if (addBlocks)
        {
            if (currentAddStroke.StrokeLength > 0)
                UndoStack.Push(currentAddStroke);
            currentAddStroke = new DoAddStroke(DoAction.DoType.AddStrocke);
        }

        addBlocks = false;
        removeBlocks = false;
        previousShapes.Clear();
        AnimationIO.constantAnimationTime = false;
        enabled = false;
    }

    // Update is called once per frame
    void Update()
    {
        if (!addBlocks && !removeBlocks)
            return;

        AnimationIO.constantAnimationTime = true;

        BrushBlocks(addBlocks);

        ++strokeCounter;

        if (strokeCounter >= historyBatchSize)
        {
            if (addBlocks)
            {
                if (currentAddStroke.StrokeLength > 0)
                    UndoStack.Push(currentAddStroke);
                currentAddStroke = new DoAddStroke(DoAction.DoType.AddStrocke);
            }
            else
            {
                if (currentRemoveStroke.StrokeLength > 0)
                    UndoStack.Push(currentRemoveStroke);
                currentRemoveStroke = new DoRemoveStroke(DoAction.DoType.RemoveStroke);
            }
            strokeCounter = 0;
        }
    }

    // All colliders in one query (Bond.GetBlocksInShapes) and one add or remove: cells shared by overlapping colliders are
    // tested once, only blocks that change are built, and the blocks are uploaded to the GPU at most once per frame
    void BrushBlocks(bool add)
    {
        shapes.Clear();
        foreach (SphereCollider sc in sphereColliders)
            shapes.Add(SphereShape(sc));
        foreach (CapsuleCollider cc in capsuleColliders)
            shapes.Add(CapsuleShape(cc));

        Dictionary<PositionKey, PositionEntry> placed = BlockContainer.Container.BlockDictionary;
        PositionKey[][] blockDictKeys;
        Matrix4x4[][] blocks = GameManager.Bond.GetBlocksInShapes(shapes, add ? (key => !placed.ContainsKey(key)) : (System.Func<PositionKey, bool>)(key => placed.ContainsKey(key)), out blockDictKeys, previousShapes);

        previousShapes.Clear();
        previousShapes.AddRange(shapes);

        if (blocks.All(part => part.Length == 0))
            return;

        if (add)
        {
            bool[][] blockMask = BlockContainer.Container.AddBlocks(blocks, blockDictKeys, null, false, true);
            currentAddStroke.AppendPartialStroke(blocks, blockDictKeys, blockMask, null, (uint)(GameManager.gameTime * 10));
        }
        else
        {
            var blockMaskAndTimes = BlockContainer.Container.RemoveBlocks(blocks, blockDictKeys, true);
            currentRemoveStroke.AppendPartialStroke(blocks, blockDictKeys, blockMaskAndTimes.blockMask, blockMaskAndTimes.blockTimes);
        }
    }

    Bond.BrushShape SphereShape(SphereCollider sc)
    {
        float maxScale = sc.transform.localScale.x;
        if (sc.transform.localScale.y > maxScale) maxScale = sc.transform.localScale.y;
        if (sc.transform.localScale.z > maxScale) maxScale = sc.transform.localScale.z;

        return Bond.BrushShape.Sphere(sc.transform.position + sc.transform.rotation * sc.center, sc.radius * maxScale);
    }

    Bond.BrushShape CapsuleShape(CapsuleCollider cc)
    {
        Vector3 capsuleDirection = Vector3.right;
        if (cc.direction == 1) capsuleDirection = Vector3.up;
        else if (cc.direction == 2) capsuleDirection = Vector3.forward;

        Vector3 positionA = cc.transform.position + cc.transform.rotation * (cc.center - capsuleDirection * (cc.height / 2) + capsuleDirection * cc.radius);
        Vector3 positionB = cc.transform.position + cc.transform.rotation * (cc.center + capsuleDirection * (cc.height / 2) - capsuleDirection * cc.radius);

        return Bond.BrushShape.Capsule(positionA, positionB, cc.radius);
    }

    private void OnDestroy()
    {
        DisableCharacterBrush();
    }

    private void OnDisable()
    {
        DisableCharacterBrush();
    }
}
