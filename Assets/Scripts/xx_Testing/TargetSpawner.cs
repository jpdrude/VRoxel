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
        if (LegoContainer.Container == null) return;

        CheckMovement();
        CheckScale();
        CheckExport();

        if (Input.GetKey(KeyCode.Return))
            SpawnBricksAtPosition(transform.position, radius);
        if (Input.GetKey(KeyCode.Backspace))
            RemoveBricksAtPosition(transform.position, radius);

        MessageSystem.DisplayPermanentMessage(LegoContainer.Container.GetCount() + " Bricks placed");
    }

    void SpawnBrickAtPosition(Vector3 pos)
    {
        LegoContainer.Container.AddBrick(GameManager.Bond.GetClosestBrick(pos));
    }

    void SpawnBricksAtPosition(Vector3 pos, float radius)
    {
        LegoContainer.Container.AddBricks(GameManager.Bond.GetBricksInSphere(pos, radius));
    }

    void RemoveBricksAtPosition(Vector3 pos, float radius)
    {
        LegoContainer.Container.RemoveBricks(GameManager.Bond.GetBricksInSphere(pos, radius));
    }

    void CheckExport()
    {
        if (Input.GetKeyDown(KeyCode.F2))
            LegoIO.SaveLegoLayout("E:/02_Forschung/Unity/2022/VoxelLego_Data/save/testSave.json");

        if (Input.GetKeyDown(KeyCode.F3))
            LegoIO.ExportLegoLayoutToAssembly("E:/02_Forschung/Unity/2022/VoxelLego_Data/save/testAssembly.json");

        if (Input.GetKeyDown(KeyCode.F4))
            LegoIO.LoadLegoLayout("E:/02_Forschung/Unity/2022/VoxelLego_Data/save/testSave.json");

        if (Input.GetKeyDown(KeyCode.F5))
            LegoIO.NewGame();
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
