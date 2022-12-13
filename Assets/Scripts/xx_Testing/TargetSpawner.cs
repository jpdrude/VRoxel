using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class TargetSpawner : MonoBehaviour
{
    [SerializeField]
    float speed;

    [SerializeField]
    float scaleSpeed = 0.05f;

    [SerializeField]
    GameObject sphere;

    [SerializeField]
    GameObject box;

    [SerializeField]
    GameObject cylinder;

    [SerializeField]
    Camera camera;

    [SerializeField]
    RectTransform fovImg;

    [SerializeField]
    GameObject cameraTarget;

    [SerializeField]
    GameObject interfacePanel;

    [SerializeField]
    GameObject spawnMeshButton;

    [SerializeField]
    Material spawnerMaterial;

    [SerializeField]
    List<TMP_InputField> inputs;

    float radius = 0.15f;
    Vector3 boxSize = new Vector3(0.1f, 0.1f, 0.1f);
    float cylinderRadius = 0.05f;
    float cylinderHeight = 0.1f;
    Vector3 scaleVector = Vector3.zero;

    List<GameObject> brushes = new List<GameObject>();
    List<Vector3> brushScales = new List<Vector3>();
    int brushIndex;


    float epsilon = 0.000001f;

    PlacementVolume placement;
    PlacementVolume Placement
    {
        get { return placement; }
        set
        {
            placement = value;

            if (placement == PlacementVolume.Sphere)
            {
                sphere.SetActive(true);
                box.SetActive(false);
                cylinder.SetActive(false);
                DeactivateAllMeshes();
                CheckScale(Vector3.zero);
            }
            else if (placement == PlacementVolume.Box)
            {
                sphere.SetActive(false);
                box.SetActive(true);
                cylinder.SetActive(false);
                DeactivateAllMeshes();
                CheckScale(Vector3.zero);
            }
            else if (placement == PlacementVolume.Cylinder)
            {
                sphere.SetActive(false);
                box.SetActive(false);
                cylinder.SetActive(true);
                DeactivateAllMeshes();
                CheckScale(Vector3.zero);
            }
            else if (placement == PlacementVolume.Brush)
            {
                sphere.SetActive(false);
                box.SetActive(false);
                cylinder.SetActive(false);
                DeactivateAllMeshes();
                brushes[brushIndex].SetActive(true); 
            }
        }
    }

    private void Start()
    {
        cameraTarget.transform.position = transform.position;
        SetPlacementVolume(0);
    }

    void Update()
    {
        if (BlockContainer.Container == null) return;

        CheckMovement();
        CheckScale(scaleVector);
        CheckSpawn();

        if (Input.GetKeyDown(KeyCode.H))
            interfacePanel.SetActive(!interfacePanel.activeSelf);

        MessageSystem.DisplayPermanentMessage(BlockContainer.Container.GetCount() + " Blocks placed");
    }

    public void AddBrush(GameObject mesh)
    {
        brushes.Add(mesh);
        brushScales.Add(Vector3.one);
        spawnMeshButton.SetActive(true);
        mesh.GetComponent<MeshRenderer>().material = spawnerMaterial;
    }

    void DeactivateAllMeshes()
    {
        foreach (GameObject mesh in brushes)
            mesh.SetActive(false);
    }

    void CheckSpawn()
    {
        if (Input.GetKey(KeyCode.Return))
        {
            if (Placement == PlacementVolume.Sphere)
                SpawnBlocksAtPosition(transform.position, radius);
            else if (Placement == PlacementVolume.Box)
                SpawnBlocksInBox();
            else if (Placement == PlacementVolume.Cylinder)
                SpawnBlocksInCylinder();
            else if (Placement == PlacementVolume.Brush)
                SpawnBlocksInBrush();
        }
        if (Input.GetKey(KeyCode.Backspace))
        {
            if (Placement == PlacementVolume.Sphere)
                RemoveBlocksAtPosition(transform.position, radius);
            else if (Placement == PlacementVolume.Box)
                RemoveBlocksInBox();
            else if (Placement == PlacementVolume.Cylinder)
                RemoveBlocksInCylinder();
            else if (Placement == PlacementVolume.Brush)
                RemoveBlocksInBrush();
        }
            
    }

    public void SetPlacementVolume(int volume)
    {
        Placement = (PlacementVolume)volume;
    }

    public void SetNextBrush()
    {
        SpawnMeshBehaviour.AlignToWorld = false;

        ++brushIndex;
        if (brushIndex >= brushes.Count)
            brushIndex = 0;

        Placement = PlacementVolume.Brush;
    }

    void SpawnBlockAtPosition(Vector3 pos)
    {
        int blockType;
        Matrix4x4 blockTransform = GameManager.Bond.GetClosestBlock(pos, out blockType);
        BlockContainer.Container.AddBlock(blockTransform, blockType);
    }

    void SpawnBlocksAtPosition(Vector3 pos, float radius)
    {
        PositionKey[][] blockDictKeys;
        Matrix4x4[][] blocks = GameManager.Bond.GetBlocksInSphere(pos, radius, out blockDictKeys);
        BlockContainer.Container.AddBlocks(blocks, blockDictKeys);
    }

    void RemoveBlocksAtPosition(Vector3 pos, float radius)
    {
        PositionKey[][] blockDictKeys;
        Matrix4x4[][] blocks = GameManager.Bond.GetBlocksInSphere(pos, radius, out blockDictKeys);
        BlockContainer.Container.RemoveBlocks(blocks, blockDictKeys);
    }

    void SpawnBlocksInBox()
    {
        PositionKey[][] blockDictKeys;
        Matrix4x4[][] blocks = GameManager.Bond.GetBlocksInBox(transform.position - boxSize / 2, transform.position + boxSize / 2, out blockDictKeys);
        BlockContainer.Container.AddBlocks(blocks, blockDictKeys);
    }

    void RemoveBlocksInBox()
    {
        PositionKey[][] blockDictKeys;
        Matrix4x4[][] blocks = GameManager.Bond.GetBlocksInBox(transform.position - boxSize / 2, transform.position + boxSize / 2, out blockDictKeys);
        BlockContainer.Container.RemoveBlocks(blocks, blockDictKeys);
    }

    void SpawnBlocksInCylinder()
    {
        PositionKey[][] blockDictKeys;
        Matrix4x4[][] blocks = GameManager.Bond.GetBlocksInCylinder(transform.position, cylinderRadius, cylinderHeight, out blockDictKeys);
        BlockContainer.Container.AddBlocks(blocks, blockDictKeys);
    }

    void RemoveBlocksInCylinder()
    {
        PositionKey[][] blockDictKeys;
        Matrix4x4[][] blocks = GameManager.Bond.GetBlocksInCylinder(transform.position, cylinderRadius, cylinderHeight, out blockDictKeys);
        BlockContainer.Container.RemoveBlocks(blocks, blockDictKeys);
    }

    void SpawnBlocksInBrush()
    {
        PositionKey[][] blockDictKeys;
        Matrix4x4[][] blocks = GameManager.Bond.GetBlocksInMesh(brushes[brushIndex].GetComponent<MeshCollider>(), out blockDictKeys);
        BlockContainer.Container.AddBlocks(blocks, blockDictKeys);
    }

    void RemoveBlocksInBrush()
    {
        PositionKey[][] blockDictKeys;
        Matrix4x4[][] blocks = GameManager.Bond.GetBlocksInMesh(brushes[brushIndex].GetComponent<MeshCollider>(), out blockDictKeys);
        BlockContainer.Container.RemoveBlocks(blocks, blockDictKeys);
    }

    public void ClickScaleX(int xScale)
    {
        scaleVector.x = xScale;
    }

    public void ClickScaleY(int yScale)
    {
        scaleVector.y = yScale;
    }

    public void ClickScaleZ(int zScale)
    {
        scaleVector.z = zScale;
    }

    public void UnclickScale()
    {
        scaleVector = Vector3.zero;
    }

    public void SetX(string textInput)
    {
        try
        {
            float x = float.Parse(textInput);
            Vector3 pos = transform.position;
            pos.x = x;
            cameraTarget.transform.position = pos;
            transform.position = pos;
        }
        catch
        {
            MessageSystem.DisplayWarning("couldn't parse number");
        }

        ClearInputs();
    }

    public void SetY(string textInput)
    {
        try
        {
            float y = float.Parse(textInput);
            Vector3 pos = transform.position;
            pos.y = y;
            cameraTarget.transform.position = pos;
            transform.position = pos;
        }
        catch
        {
            MessageSystem.DisplayWarning("couldn't parse number");
        }

        ClearInputs();
    }

    public void SetZ(string textInput)
    {
        try
        {
            float z = float.Parse(textInput);
            Vector3 pos = transform.position;
            pos.z = z;
            cameraTarget.transform.position = pos;
            transform.position = pos;
        }
        catch
        {
            MessageSystem.DisplayWarning("couldn't parse number");
        }

        ClearInputs();
    }

    public void SetFOV(float fov)
    {
        camera.fieldOfView = fov;
        Vector2 minFOV = new Vector2(170, 40);
        Vector2 maxFOV = new Vector2(50, 120);

        fovImg.sizeDelta = Vector2.Lerp(minFOV, maxFOV, (fov - 10) / 100f);
    }

    void ClearInputs()
    {
        foreach (TMP_InputField input in inputs)
        {
            input.Select();
            input.text = "";
        }
    }

    void CheckScale(Vector3 direction)
    {
        if (Placement == PlacementVolume.Sphere)
        {
            if (direction.x > epsilon || direction.y > epsilon || direction.z > epsilon)
            {
                radius += Time.deltaTime * scaleSpeed;
            }
            else if (direction.x < -epsilon || direction.y < -epsilon || direction.z < -epsilon)
            {
                radius -= Time.deltaTime * scaleSpeed;
                if (radius < 0.01f) radius = 0.01f;
            }

            sphere.transform.localScale = Vector3.one * 2 * radius;
        }
        else if (Placement == PlacementVolume.Box)
        {
            boxSize += direction * scaleSpeed * Time.deltaTime;
            if (boxSize.x < 0.025f) boxSize.x = 0.025f;
            if (boxSize.y < 0.025f) boxSize.y = 0.025f;
            if (boxSize.z < 0.025f) boxSize.z = 0.025f;

            box.transform.localScale = boxSize;
        }
        else if (Placement == PlacementVolume.Cylinder)
        {
            if (direction.x > epsilon || direction.z > epsilon)
            {
                cylinderRadius += Time.deltaTime * scaleSpeed;
            }
            else if (direction.x < -epsilon || direction.z < -epsilon)
            {
                cylinderRadius -= Time.deltaTime * scaleSpeed;
                if (cylinderRadius < 0.01f) cylinderRadius = 0.01f;
            }
            if (direction.y > epsilon)
            {
                cylinderHeight += Time.deltaTime * scaleSpeed;
            }
            else if (direction.y < -epsilon)
            {
                cylinderHeight -= Time.deltaTime * scaleSpeed;
                if (cylinderHeight < 0.025f) cylinderHeight = 0.025f;
            }

            cylinder.transform.localScale = new Vector3(cylinderRadius * 2, cylinderHeight, cylinderRadius * 2);
        }
        else if (Placement == PlacementVolume.Brush)
        {
            if (direction.x > epsilon || direction.y > epsilon || direction.z > epsilon)
            {
                brushScales[brushIndex] += Vector3.one * Time.deltaTime * scaleSpeed;
            }
            else if (direction.x < -epsilon || direction.y < -epsilon || direction.z < -epsilon)
            {
                brushScales[brushIndex] -= Vector3.one * Time.deltaTime * scaleSpeed;
                if (brushScales[brushIndex].x < 0.01f) brushScales[brushIndex] = Vector3.one * 0.01f;
            }

            brushes[brushIndex].transform.localScale = brushScales[brushIndex];
        }
    }

    void CheckMovement()
    {
        Vector3 movement = Vector3.zero;

        if (Input.GetKey(KeyCode.W))
            movement.z += Time.deltaTime * speed;
        if (Input.GetKey(KeyCode.S))
            movement.z -= Time.deltaTime * speed;
        if (Input.GetKey(KeyCode.A))
            movement.x -= Time.deltaTime * speed;
        if (Input.GetKey(KeyCode.D))
            movement.x += Time.deltaTime * speed;
        if (Input.GetKey(KeyCode.Space))
            movement.y += Time.deltaTime * speed;
        if (Input.GetKey(KeyCode.LeftControl))
            movement.y -= Time.deltaTime * speed;

        transform.position += movement;

    }

    enum PlacementVolume
    {
        Sphere,
        Box,
        Cylinder,
        Brush
    }
}
