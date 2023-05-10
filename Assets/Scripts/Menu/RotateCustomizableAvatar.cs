using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class RotateCustomizableAvatar : MonoBehaviour
{
    [SerializeField]
    EventSystem eventSystem;
    [SerializeField]
    GraphicRaycaster graphicRaycaster;
    [SerializeField]
    GameObject cameraBase;
    [SerializeField]
    float speed = 1;

    PointerEventData eventData;

    bool rotating = false;
    float lastMousePosition;

    private void Update()
    {
        if (rotating)
        {
            cameraBase.transform.Rotate(0, 0, (Input.mousePosition.x - lastMousePosition) * Time.deltaTime * speed);
            lastMousePosition = Input.mousePosition.x;
        }

        if (Input.GetMouseButtonDown(0))
        {
            eventData = new UnityEngine.EventSystems.PointerEventData(eventSystem);
            eventData.position = Input.mousePosition;

            List<RaycastResult> results = new List<RaycastResult>();
            graphicRaycaster.Raycast(eventData, results);

            foreach (RaycastResult hit in results)
                if (hit.gameObject.GetComponent<RotateCustomizableAvatar>() != null)
                {
                    lastMousePosition = Input.mousePosition.x;
                    rotating = true;
                }
        }

        if (Input.GetMouseButtonUp(0))
        {
            rotating = false;
        }
    }
}
