using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CharacterPointerBehaviour : MonoBehaviour
{
    [SerializeField]
    GameObject target;

    LineRenderer lr;
    CustomAnimationPlayer focusCharacter = null;
    Vector3? focusPoint = null;

    GameObject pickupCharacter = null;
    Vector3 pickupPosition;
    Vector3 pickupHeight = Vector3.zero;

    Vector3 characterPosition;
    bool setDirection = false;

    int layerMaskCharacter = 1 << 10;
    int layerMaskFloor = 1 << 3 | 1 << 6;

    int currentLayerMask;

    private void Start()
    {
        lr = GetComponent<LineRenderer>();
        currentLayerMask = layerMaskCharacter;
    }

    private void Update()
    {
        RaycastHit hit;

        if (Physics.Raycast(transform.position, transform.TransformDirection(Vector3.forward), out hit, Mathf.Infinity, currentLayerMask))
        {
            if (pickupCharacter == null)
            {
                focusCharacter = FindCharacterInParent(hit.collider.gameObject);
            }
            else if (pickupCharacter != null && !setDirection)
            {

                pickupCharacter.transform.position = hit.point + pickupHeight;
            }
            else if (pickupCharacter != null && setDirection)
            {
                pickupCharacter.transform.LookAt(hit.point + pickupHeight, Vector3.up);
            }

            focusPoint = hit.point;
        }
        else
        {
            focusCharacter = null;
            focusPoint = null;

            if (pickupCharacter != null && !setDirection)
                pickupCharacter.transform.position = pickupPosition;
        }

        DrawPointer();
    }

    public void OnTriggerDown()
    {
        if (focusCharacter != null && pickupCharacter == null)
        {
            focusCharacter.PauseAnimation(true);
            pickupCharacter = focusCharacter.gameObject;
            currentLayerMask = layerMaskFloor;
            pickupPosition = pickupCharacter.transform.position;
            pickupHeight.y = pickupPosition.y;
        }
        else if (pickupCharacter != null && focusPoint != null && !setDirection)
        {
            setDirection = true;
            characterPosition = (Vector3)focusPoint + pickupHeight;
        }
        else if (pickupCharacter != null && focusPoint != null && setDirection)
        {
            float angle = pickupCharacter.transform.rotation.eulerAngles.y;

            pickupCharacter.GetComponent<CustomAnimationPlayer>().SetOffset(characterPosition, angle);
            pickupCharacter = null;
            currentLayerMask = layerMaskCharacter;
            setDirection = false;
        }
        else if (focusPoint == null)
        {
            if (pickupCharacter != null)
                pickupCharacter.GetComponent<CustomAnimationPlayer>().PauseAnimation(false);

            pickupCharacter = null;
            currentLayerMask = layerMaskCharacter;
            setDirection = false;
        }
    }

    CustomAnimationPlayer FindCharacterInParent(GameObject go)
    {
        while (true)
        {
            CustomAnimationPlayer player = go.GetComponent<CustomAnimationPlayer>();
            if (player != null) return player;

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

}
