using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TargetSpawner : MonoBehaviour
{
    [SerializeField]
    float speed;

    [SerializeField]
    GameObject sphere;

    [SerializeField]
    GameObject cameraTarget;

    float radius = 0.05f;

    private void Start()
    {
        sphere.transform.localScale = Vector3.one * 2 * radius;
        cameraTarget.transform.position = transform.position;
    }

    void Update()
    {
        if (ModuleContainer.Container == null) return;

        CheckMovement();
        CheckScale();

        if (Input.GetKey(KeyCode.Return))
            SpawnBricksAtPosition(transform.position, radius);
        if (Input.GetKey(KeyCode.Backspace))
            RemoveBricksAtPosition(transform.position, radius);

        MessageSystem.DisplayPermanentMessage(ModuleContainer.Container.GetCount() + " Bricks placed");
    }

    void SpawnBrickAtPosition(Vector3 pos)
    {
        ModuleContainer.Container.AddBrick(GameManager.Bond.GetClosestBrick(pos));
    }

    void SpawnBricksAtPosition(Vector3 pos, float radius)
    {
        Vector3Int[] dictPositions;
        Matrix4x4[] bricks = GameManager.Bond.GetBricksInSphere(pos, radius, out dictPositions);
        ModuleContainer.Container.AddBricks(bricks, dictPositions);
    }

    void RemoveBricksAtPosition(Vector3 pos, float radius)
    {
        Vector3Int[] dictPositions;
        Matrix4x4[] bricks = GameManager.Bond.GetBricksInSphere(pos, radius, out dictPositions);
        ModuleContainer.Container.RemoveBricks(bricks, dictPositions);
    }

    void CheckScale()
    {
        if (Input.GetKey(KeyCode.UpArrow))
        {
            radius += Time.deltaTime * 0.05f;
            if (radius > 0.3f) radius = 0.3f;
            sphere.transform.localScale = Vector3.one * 2 * radius;
        }
        if (Input.GetKey(KeyCode.DownArrow))
        {
            radius -= Time.deltaTime * 0.05f;
            if (radius < 0.005f) radius = 0.005f;
            sphere.transform.localScale = Vector3.one * 2 * radius;
        }
    }

    void CheckMovement()
    {
        Vector3 movement = Vector3.zero;

        if (Input.GetKey(KeyCode.W))
            movement.x += Time.deltaTime * speed;
        if (Input.GetKey(KeyCode.S))
            movement.x -= Time.deltaTime * speed;
        if (Input.GetKey(KeyCode.A))
            movement.z -= Time.deltaTime * speed;
        if (Input.GetKey(KeyCode.D))
            movement.z += Time.deltaTime * speed;
        if (Input.GetKey(KeyCode.Space))
            movement.y += Time.deltaTime * speed;
        if (Input.GetKey(KeyCode.LeftControl))
            movement.y -= Time.deltaTime * speed;

        transform.position += movement;

    }
}
