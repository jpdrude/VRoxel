using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using cakeslice;

public class CADMeshToBricks : MonoBehaviour
{
    public bool ScanMesh { get; set; }
    public bool Remove { get; set; }

    int layerMask = 1 << 11 | 1 << 2;

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
        if (EscapeMenu.GamePaused) return;

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

                BlockContainer.Container.FinishStroke(PlacementModeType.Spawn);
            }
            else
            {
                BlockContainer.Container.RemoveBlocks(blocks, blockDictKeys);
                Debug.Log("Removed " + blockCount + " units");

                BlockContainer.Container.FinishStroke(PlacementModeType.Destroy);
            }

            GeometryMenuItemReference menuReference = null;
            GameObject parent = focusMesh.transform.parent.gameObject;
            while (true)
            {
                if (parent.GetComponent<GeometryMenuItemReference>() != null)
                {
                    menuReference = parent.GetComponent<GeometryMenuItemReference>();
                    break;
                }

                if (parent.transform.parent == null)
                    break;

                parent = parent.transform.parent.gameObject;
            }

            MeshCollider[] childColliders = parent.GetComponentsInChildren<MeshCollider>();

            if (childColliders.Length <= 1)
                menuReference.MenuItem.DeleteModel();
            else
            {
                Destroy(focusMesh.gameObject);
            }
        }
    }
}
