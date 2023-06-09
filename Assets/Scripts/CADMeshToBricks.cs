using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using cakeslice;

public class CADMeshToBricks : MonoBehaviour
{
    public bool ScanMesh { get; set; }
    public bool Remove { get; set; }

    int layerMask = 1 << 11;

    MeshCollider focusMesh = null;

    Camera camera;

    private void Start()
    {
        camera = GetComponent<Camera>();
    }

    // Update is called once per frame
    void Update()
    {
        if (!ScanMesh) return;

        RaycastHit hit;

        Ray ray = camera.ScreenPointToRay(Input.mousePosition);

        if (Physics.Raycast(ray, out hit, Mathf.Infinity, layerMask))
        {
            if (focusMesh != null && focusMesh != (MeshCollider)hit.collider)
                Destroy(focusMesh.GetComponent<Outline>());

            focusMesh = (MeshCollider)hit.collider;

            Outline outline = focusMesh.GetComponent<Outline>();

            if (outline == null)
            {
                outline = focusMesh.gameObject.AddComponent<Outline>();
            }

            if (!Remove)
                outline.color = 0;
            else
                outline.color = 1;
        }
        else
        {
            if (focusMesh != null)
                Destroy(focusMesh.GetComponent<Outline>());

            focusMesh = null;
        }

        if (Input.GetMouseButtonDown(0) && focusMesh != null)
        {
            focusMesh.gameObject.layer = 8;
            focusMesh.gameObject.tag = "MeshSpawner";

            PositionKey[][] blockDictKeys;
            Matrix4x4[][] blocks = GameManager.Bond.GetBlocksInMesh(focusMesh, out blockDictKeys);

            int blockCount = 0;
            for (int i = 0; i < blocks.Length; ++i)
                blockCount += blocks[i].Length;

            if (!Remove)
            {
                BlockContainer.Container.AddBlocks(blocks, blockDictKeys);
                Debug.Log("Added " + blockCount + " units");
            }
            else
            {
                BlockContainer.Container.RemoveBlocks(blocks, blockDictKeys);
                Debug.Log("Removed " + blockCount + " units");
            }

            Destroy(focusMesh.gameObject);
        }
    }
}
