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

    // Start is called before the first frame update
    void Start()
    {
        sphereColliders = GetComponentsInChildren<SphereCollider>();
        capsuleColliders = GetComponentsInChildren<CapsuleCollider>();
    }

    public void AddCharacterBrush()
    {
        addBlocks = true;
        removeBlocks = false;

        AnimationIO.constantAnimationTime = true;
        enabled = true;
    }

    public void RemoveCharacterBrush()
    {
        removeBlocks = true;
        addBlocks = false;

        AnimationIO.constantAnimationTime = true;
        enabled = true;
    }

    public void DisableCharacterBrush()
    {
        addBlocks = false;
        removeBlocks = false;
        AnimationIO.constantAnimationTime = false;
        enabled = false;
    }

    // Update is called once per frame
    void Update()
    {
        if (addBlocks)
        {
            AnimationIO.constantAnimationTime = true;

            foreach (SphereCollider sc in sphereColliders)
            {
                AddRemoveFromSphere(sc, true);
            }
            foreach (CapsuleCollider cc in capsuleColliders)
            {
                AddRemoveFromCapsule(cc, true);
            }
        }
        else if (removeBlocks)
        {
            AnimationIO.constantAnimationTime = true;

            foreach (SphereCollider sc in sphereColliders)
            {
                AddRemoveFromSphere(sc, false);
            }
            foreach (CapsuleCollider cc in capsuleColliders)
            {
                AddRemoveFromCapsule(cc, false);
            }
        }
    }

    void AddRemoveFromSphere(SphereCollider sc, bool add)
    {
        float maxScale = sc.transform.localScale.x;
        if (sc.transform.localScale.y > maxScale) maxScale = sc.transform.localScale.y;
        if (sc.transform.localScale.z > maxScale) maxScale = sc.transform.localScale.z;

        PositionKey[][] blockDictKeys;
        Matrix4x4[][] blocks = GameManager.Bond.GetBlocksInSphere(sc.transform.position + sc.transform.rotation * sc.center, sc.radius * maxScale, out blockDictKeys);
        
        if (add)
            BlockContainer.Container.AddBlocks(blocks, blockDictKeys);
        else
            BlockContainer.Container.RemoveBlocks(blocks, blockDictKeys);
    }

    void AddRemoveFromCapsule(CapsuleCollider cc, bool add)
    {
        Vector3 capsuleDirection = Vector3.right;
        if (cc.direction == 1) capsuleDirection = Vector3.up;
        else if (cc.direction == 2) capsuleDirection = Vector3.forward;

        Vector3 positionA = cc.transform.position + cc.transform.rotation * (cc.center - capsuleDirection * (cc.height / 2) + capsuleDirection * cc.radius);
        Vector3 positionB = cc.transform.position + cc.transform.rotation * (cc.center + capsuleDirection * (cc.height / 2) - capsuleDirection * cc.radius);

        PositionKey[][] blockDictKeys;
        Matrix4x4[][] blocks = GameManager.Bond.GetBlocksInCapsule(positionA, positionB, cc.radius, out blockDictKeys);
        
        if (add)
            BlockContainer.Container.AddBlocks(blocks, blockDictKeys);
        else
            BlockContainer.Container.RemoveBlocks(blocks, blockDictKeys);
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
