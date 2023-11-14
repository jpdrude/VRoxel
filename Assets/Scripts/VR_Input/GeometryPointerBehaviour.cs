using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Valve.VR;

public class GeometryPointerBehaviour : MonoBehaviour
{
    [SerializeField]
    GameObject target;

    [SerializeField]
    float moveSpeed = 1;

    [SerializeField]
    float rotSpeed = 1;

    [SerializeField]
    public SteamVR_Action_Boolean trigger;

    LineRenderer lr;
    Vector3? focusPoint = null;

    GameObject focusObject = null;
    GameObject pickupObject = null;
    GeometryItemRoot pickupRoot = null;
    Transform oldParent = null;

    Vector3 pickupPosition;
    Quaternion pickupRotation;

    int layerMask = 1 << 11;

    bool copy = false;

    static SteamVR_Input_Sources secondaryInputSource = SteamVR_Input_Sources.LeftHand;
    public static SteamVR_Input_Sources SecondaryInputSource { get { return secondaryInputSource; } set { secondaryInputSource = value; } }

    private void Start()
    {
        lr = GetComponent<LineRenderer>();
    }

    private void Update()
    {
        RaycastHit hit;

        if (pickupObject == null)
        {
            lr.enabled = true;
            if (Physics.Raycast(transform.position, transform.TransformDirection(Vector3.forward), out hit, Mathf.Infinity, layerMask))
            {
                focusObject = FindRootObject(hit.collider.gameObject);

                if (focusObject != null)
                {
                    focusPoint = hit.point;
                }
            }
            else
            {
                focusObject = null;
                focusPoint = null;
                target.SetActive(false);
            }
                
            DrawPointer();
        }
        else
        {
            lr.enabled = false;
            target.SetActive(false);
        }

    }

    GameObject FindRootObject(GameObject go)
    {
        while (true)
        {
            var root = go.GetComponent<GeometryItemRoot>();
            if (root != null)
            {
                return go;
            }

            if (go.transform.parent == null)
                return null;

            go = go.transform.parent.gameObject;
        }
    }

    void DrawPointer()
    {
        lr.SetPosition(0, transform.position);

        if (focusPoint != null)
        {
            target.SetActive(true);
            target.transform.position = (Vector3)focusPoint;
            lr.SetPosition(1, (Vector3)focusPoint);
        }
        else
        {
            target.SetActive(false);
            lr.SetPosition(1, transform.forward * 100);
        }
    }

    public void OnTriggerDown()
    {
        if (pickupObject == null && focusObject != null)
        {
            if (trigger.GetState(secondaryInputSource))
            {
                copy = true;
                pickupObject = focusObject.GetComponent<GeometryMenuItemReference>().MenuItem.DuplicateModel();

                oldParent = pickupObject.transform.parent;
                transform.position = focusObject.transform.position;

                pickupObject.transform.parent = transform;
                pickupObject.transform.localPosition = Vector3.zero;
                pickupRoot = pickupObject.GetComponent<GeometryItemRoot>();
                pickupRoot.enabled = true;
            }
            else
            {
                pickupObject = focusObject;
                pickupPosition = pickupObject.transform.position;
                pickupRotation = pickupObject.transform.rotation;

                oldParent = pickupObject.transform.parent;
                transform.position = pickupObject.transform.position;

                pickupObject.transform.parent = transform;
                pickupRoot = pickupObject.GetComponent<GeometryItemRoot>();
                pickupRoot.enabled = true;
            }
        }
        else if (pickupObject != null)
        {
            FinishPickup();
        }
    }

    void FinishPickup()
    {
        copy = false;
        pickupRoot.enabled = false;
        pickupObject.transform.parent = oldParent;
        pickupObject = null;
        focusObject = null;
        pickupRoot = null;
        oldParent = null;

        transform.localPosition = Vector3.zero;
    }

    public void DPadLeft()
    {
        if (pickupRoot != null)
            pickupRoot.RotateY(-rotSpeed);
    }

    public void DPadRight()
    {
        if (pickupRoot != null)
            pickupRoot.RotateY(rotSpeed);
    }

    public void DPadDown()
    {
        if (pickupObject == null) return;

        Vector3 pos = transform.localPosition;
        pos.z = pos.z - Time.deltaTime * moveSpeed;
        if (pos.z < 0) pos.z = 0;

        transform.localPosition = pos;
    }

    public void DPadUp()
    {
        if (pickupObject == null) return;

        Vector3 pos = transform.localPosition;
        pos.z = pos.z + Time.deltaTime * moveSpeed;

        transform.localPosition = pos;
    }

    public void AbortPlacement()
    {
        if (pickupObject == null) return;

        if (copy)
        {
            EditGeometryMenu menuItem = pickupObject.GetComponent<GeometryMenuItemReference>().MenuItem;
            FinishPickup();

            menuItem.DeleteModel();
        }
        else
        {
            pickupObject.transform.parent = null;
            pickupObject.transform.rotation = pickupRotation;
            pickupObject.transform.position = pickupPosition;

            FinishPickup();
        }
    }
}
