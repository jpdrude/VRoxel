using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SectionCamController : MonoBehaviour
{
    [SerializeField]
    float zoomRate = 10;

    [SerializeField]
    float panRate = 0.2f;

    [SerializeField]
    float clipRate = 0.1f;

    Camera camera;

    float mouseStart = 0;
    float clipStart = 0;

    private void Start()
    {
        camera = GetComponent<Camera>();
    }

    private void LateUpdate()
    {
        float mouseWheel = Input.mouseScrollDelta.y;

        if (Mathf.Abs(mouseWheel) > 0.0001)
            camera.orthographicSize = camera.orthographicSize - mouseWheel * Time.deltaTime * zoomRate;


        float moveX = Input.GetAxis("Mouse X");
        float moveY = Input.GetAxis("Mouse Y");

        Vector3 mouseMove = new Vector3(moveX, moveY, 0);

        mouseMove = transform.rotation * mouseMove;

        if (Input.GetMouseButton(2))
        {
            transform.position = transform.position - mouseMove * panRate;
        }

        if (Input.GetMouseButtonDown(1))
        {
            mouseStart = Input.mousePosition.x;
            clipStart = camera.nearClipPlane;
        }

        if (Input.GetMouseButton(1))
        {
            float mouseDeltaX = Input.mousePosition.x - mouseStart;
            camera.nearClipPlane = clipStart - mouseDeltaX * clipRate;
        }
    }
}
