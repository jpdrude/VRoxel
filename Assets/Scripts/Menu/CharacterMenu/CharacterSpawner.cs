using System.Collections;
using System.Collections.Generic;
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

    // Thickness of the brush relative to the body colliders: above 1 the avatar adds or removes a wider swath
    [SerializeField]
    float brushSize = 1;
    public float BrushSize { get { return brushSize; } set { brushSize = Mathf.Max(0.1f, value); } }
    int strokeCounter = 0;

    DoAddStroke currentAddStroke = new DoAddStroke(DoAction.DoType.AddStrocke);
    DoRemoveStroke currentRemoveStroke = new DoRemoveStroke(DoAction.DoType.RemoveStroke);

    // Brushing caps the animation's frame time (AnimationIO.cappedAnimationTime), so strokes have no gaps when frames drop.
    // Several avatars can brush at once; the cap stays on until the last of them stops.
    static int activeBrushes = 0;
    bool counted = false;

    // A Mixamo avatar brushes only once its body is animated, not with its T-pose right after spawning
    MixamoAnimationPlayer mixamoPlayer;

    public bool Brushing { get { return addBlocks || removeBlocks; } }

    // Start is called before the first frame update
    void Start()
    {
        sphereColliders = GetComponentsInChildren<SphereCollider>();
        capsuleColliders = GetComponentsInChildren<CapsuleCollider>();
        mixamoPlayer = GetComponent<MixamoAnimationPlayer>();
    }

    public void AddCharacterBrush()
    {
        if (removeBlocks)
        {
            UndoStack.Push(currentRemoveStroke);
            currentRemoveStroke = new DoRemoveStroke(DoAction.DoType.RemoveStroke);
        }

        addBlocks = true;
        removeBlocks = false;

        CountBrush(true);
        enabled = true;
    }

    public void RemoveCharacterBrush()
    {
        if (addBlocks)
        {
            UndoStack.Push(currentAddStroke);
            currentAddStroke = new DoAddStroke(DoAction.DoType.AddStrocke);
        }


        removeBlocks = true;
        addBlocks = false;

        CountBrush(true);
        enabled = true;
    }

    public void DisableCharacterBrush()
    {
        if (!removeBlocks && !addBlocks) return;

        if (removeBlocks)
        {
            UndoStack.Push(currentRemoveStroke);
            currentRemoveStroke = new DoRemoveStroke(DoAction.DoType.RemoveStroke);
        }
        else if (addBlocks)
        {
            UndoStack.Push(currentAddStroke);
            currentAddStroke = new DoAddStroke(DoAction.DoType.AddStrocke);
        }

        addBlocks = false;
        removeBlocks = false;
        CountBrush(false);
        enabled = false;
    }

    void CountBrush(bool active)
    {
        if (active != counted)
        {
            activeBrushes += active ? 1 : -1;
            counted = active;
        }

        AnimationIO.cappedAnimationTime = activeBrushes > 0;
    }

    // Update is called once per frame
    void Update()
    {
        if (mixamoPlayer != null && !mixamoPlayer.Posed)
            return;

        if (addBlocks)
        {

            foreach (SphereCollider sc in sphereColliders)
            {
                AddRemoveFromSphere(sc, true);
            }
            foreach (CapsuleCollider cc in capsuleColliders)
            {
                AddRemoveFromCapsule(cc, true);
            }

            ++strokeCounter;

            if (strokeCounter >= historyBatchSize)
            {
                UndoStack.Push(currentAddStroke);
                currentAddStroke = new DoAddStroke(DoAction.DoType.AddStrocke);
                strokeCounter = 0;
            }
        }
        else if (removeBlocks)
        {

            foreach (SphereCollider sc in sphereColliders)
            {
                AddRemoveFromSphere(sc, false);
            }
            foreach (CapsuleCollider cc in capsuleColliders)
            {
                AddRemoveFromCapsule(cc, false);
            }

            ++strokeCounter;

            if (strokeCounter >= historyBatchSize)
            {
                UndoStack.Push(currentRemoveStroke);
                currentRemoveStroke = new DoRemoveStroke(DoAction.DoType.RemoveStroke);
                strokeCounter = 0;
            }
        }
    }

    // Collider sizes follow the scale of the bones they sit on (lossyScale), so the brush fits CharacterModel's body:
    // height, proportions and limb widths
    void AddRemoveFromSphere(SphereCollider sc, bool add)
    {
        Vector3 scale = sc.transform.lossyScale;
        float maxScale = Mathf.Max(Mathf.Abs(scale.x), Mathf.Abs(scale.y), Mathf.Abs(scale.z));

        PositionKey[][] blockDictKeys;
        Matrix4x4[][] blocks = GameManager.Bond.GetBlocksInSphere(sc.transform.TransformPoint(sc.center), sc.radius * maxScale * brushSize, out blockDictKeys);
        
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

    void AddRemoveFromCapsule(CapsuleCollider cc, bool add)
    {
        Vector3 capsuleDirection = Vector3.right;
        if (cc.direction == 1) capsuleDirection = Vector3.up;
        else if (cc.direction == 2) capsuleDirection = Vector3.forward;

        Vector3 positionA = cc.transform.TransformPoint(cc.center - capsuleDirection * (cc.height / 2) + capsuleDirection * cc.radius);
        Vector3 positionB = cc.transform.TransformPoint(cc.center + capsuleDirection * (cc.height / 2) - capsuleDirection * cc.radius);

        // The radius scales with the widest axis across the capsule
        Vector3 scale = cc.transform.lossyScale;
        float across = cc.direction == 0 ? Mathf.Max(Mathf.Abs(scale.y), Mathf.Abs(scale.z))
            : cc.direction == 1 ? Mathf.Max(Mathf.Abs(scale.x), Mathf.Abs(scale.z))
            : Mathf.Max(Mathf.Abs(scale.x), Mathf.Abs(scale.y));

        PositionKey[][] blockDictKeys;
        Matrix4x4[][] blocks = GameManager.Bond.GetBlocksInCapsule(positionA, positionB, cc.radius * across * brushSize, out blockDictKeys);

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

    private void OnDestroy()
    {
        DisableCharacterBrush();
    }

    private void OnDisable()
    {
        DisableCharacterBrush();
    }
}
