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
    GameObject capsule;

    [SerializeField]
    SpawnCapsuleBehavior capsuleBehavior;

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
    GameObject messagePanel;

    [SerializeField]
    List<TMP_InputField> inputs;

    float radius = 0.15f;
    Vector3 boxSize = new Vector3(0.3f, 0.3f, 0.3f);
    float cylinderRadius = 0.15f;
    float cylinderHeight = 0.3f;
    Vector3 scaleVector = Vector3.zero;
    float capsuleRadius = 0.15f;
    float capsuleHeight = 0.3f;

    List<GameObject> brushes = new List<GameObject>();
    List<Vector3> brushScales = new List<Vector3>();
    int brushIndex;

    const float epsilon = 0.000001f;

    bool interfaceVisible = true;
    int _lastDisplayedCount = -1;

    PlacementVolume placement;
    PlacementVolume Placement
    {
        get { return placement; }
        set
        {
            placement = value;

            if (placement == PlacementVolume.Sphere)
            {
                HideAllBrushes();
                sphere.SetActive(true);
                CheckScale(Vector3.zero);
            }
            else if (placement == PlacementVolume.Box)
            {
                HideAllBrushes();
                box.SetActive(true);
                CheckScale(Vector3.zero);
            }
            else if (placement == PlacementVolume.Cylinder)
            {
                HideAllBrushes();
                cylinder.SetActive(true);
                CheckScale(Vector3.zero);
            }
            else if (placement == PlacementVolume.Capsule)
            {
                HideAllBrushes();
                capsule.SetActive(true);
                CheckScale(Vector3.zero);
            }
            else if (placement == PlacementVolume.Brush)
            {
                HideAllBrushes();
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

        if (Input.GetKeyDown(KeyCode.H))
            HideMenuToggle();

        if (!interfaceVisible) return;

        CheckMovement();
        CheckScale(scaleVector);
        CheckSpawn();
        CheckUndo();

        int count = BlockContainer.Container.GetCount();
        if (count != _lastDisplayedCount)
        {
            _lastDisplayedCount = count;
            MessageSystem.DisplayPermanentMessage(count + " Blocks placed");
        }
    }

    void HideMenuToggle()
    {
        interfaceVisible = !interfaceVisible;
        
        interfacePanel.SetActive(interfaceVisible);
        messagePanel.SetActive(interfaceVisible);

        if (!interfaceVisible)
            HideAllBrushes();
        else
            ShowCurrentBrush();
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
            else if (Placement == PlacementVolume.Capsule)
                SpawnBlocksInCapsule();
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
            else if (Placement == PlacementVolume.Capsule)
                RemoveBlocksInCapsule();
            else if (Placement == PlacementVolume.Brush)
                RemoveBlocksInBrush();
        }

        if (Input.GetKeyUp(KeyCode.Return))
            FinishSpawnAction(PlacementModeType.Spawn);
        else if (Input.GetKeyUp(KeyCode.Backspace))
            FinishSpawnAction(PlacementModeType.Destroy);
    }

    void CheckUndo()
    {
        if (Input.GetKey(KeyCode.LeftControl))
        {
            if (Input.GetKeyDown(KeyCode.Z))
                UndoStack.Undo();
            else if (Input.GetKeyDown(KeyCode.Y))
                UndoStack.Redo();
        }
    }

    void FinishSpawnAction(PlacementModeType type)
    {
        BlockContainer.Container.FinishStroke(type);
    }

    void HideAllBrushes()
    {
        sphere.SetActive(false);
        box.SetActive(false);
        cylinder.SetActive(false);
        capsule.SetActive(false);
        DeactivateAllMeshes();
    }

    void ShowCurrentBrush()
    {
        Placement = placement;
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

    void SpawnBlocksInCapsule()
    {
        PositionKey[][] blockDictKeys;
        Matrix4x4[][] blocks = GameManager.Bond.GetBlocksInCapsule(capsuleBehavior.PositionA, capsuleBehavior.PositionB, capsuleBehavior.Radius, out blockDictKeys);
        BlockContainer.Container.AddBlocks(blocks, blockDictKeys);
    }

    void RemoveBlocksInCapsule()
    {
        PositionKey[][] blockDictKeys;
        Matrix4x4[][] blocks = GameManager.Bond.GetBlocksInCapsule(capsuleBehavior.PositionA, capsuleBehavior.PositionB, capsuleBehavior.Radius, out blockDictKeys);
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
        else if (Placement == PlacementVolume.Capsule)
        {
            if (direction.x > epsilon || direction.z > epsilon)
            {
                capsuleRadius += Time.deltaTime * scaleSpeed;
            }
            else if (direction.x < -epsilon || direction.z < -epsilon)
            {
                capsuleRadius -= Time.deltaTime * scaleSpeed;
                if (capsuleRadius < 0.01f) capsuleRadius = 0.01f;
            }
            if (direction.y > epsilon)
            {
                capsuleHeight += Time.deltaTime * scaleSpeed;
            }
            else if (direction.y < -epsilon)
            {
                capsuleHeight -= Time.deltaTime * scaleSpeed;
                if (capsuleHeight < 0.005f) capsuleHeight = 0.005f;
            }

            capsuleBehavior.SetScale(capsuleRadius, capsuleHeight);
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
        Capsule,
        Brush
    }
}
