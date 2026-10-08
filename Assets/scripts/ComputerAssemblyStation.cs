using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>Builds an interactive computer assembly area on the Cube (9) platform.</summary>
public class ComputerAssemblyStation : MonoBehaviour
{
    [HideInInspector] public GameObject motherboardPrefab;

    private static readonly ComputerPartKind[] BuildOrder =
    {
        ComputerPartKind.Case, ComputerPartKind.Motherboard, ComputerPartKind.Processor,
        ComputerPartKind.Memory, ComputerPartKind.GraphicsCard, ComputerPartKind.Cooler
    };

    private static readonly string[] PartNames =
    {
        "Корпус", "Материнская плата", "Процессор", "Оперативная память", "Видеокарта", "Кулер"
    };

    private readonly HashSet<ComputerPartKind> collected = new HashSet<ComputerPartKind>();
    private Transform bench;
    private Transform computer;
    private Camera playerCamera;
    private ComputerPartPickup heldPart;
    private int installedCount;
    private bool explosionStarted;
    private bool assemblyComplete;
    private string notice = "Осмотрите мастерскую и соберите детали компьютера.";
    private float noticeUntil;

    public Camera InteractionCamera => playerCamera;

    private Vector3 Center => new Vector3(transform.position.x, transform.position.y + 0.05f, transform.position.z);

    public bool IsPlayerOnWorkshopPlatform()
    {
        if (playerCamera == null) return false;
        Collider platformCollider = GetComponent<Collider>();
        if (platformCollider == null) return false;

        Bounds bounds = platformCollider.bounds;
        Vector3 playerPosition = playerCamera.transform.root.position;
        bool insideFootprint = playerPosition.x >= bounds.min.x && playerPosition.x <= bounds.max.x
            && playerPosition.z >= bounds.min.z && playerPosition.z <= bounds.max.z;
        bool nearPlatformHeight = playerPosition.y >= bounds.max.y - 1.5f
            && playerPosition.y <= bounds.max.y + 3f;
        return insideFootprint && nearPlatformHeight;
    }

    private void Start()
    {
        playerCamera = Camera.main;
        if (playerCamera == null)
        {
            WeaponController playerWeapon = FindObjectOfType<WeaponController>();
            if (playerWeapon != null) playerCamera = playerWeapon.GetComponentInChildren<Camera>();
        }
        CreateWorkshop();
    }

    private void Update()
    {
        if (playerCamera == null)
        {
            playerCamera = Camera.main;
            if (playerCamera == null)
            {
                WeaponController playerWeapon = FindObjectOfType<WeaponController>();
                if (playerWeapon != null) playerCamera = playerWeapon.GetComponentInChildren<Camera>();
            }
        }
        if (playerCamera == null || !Input.GetKeyDown(KeyCode.E)) return;

        Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);
        if (Physics.Raycast(ray, out RaycastHit hit, 3.5f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
        {
            ComputerPartPickup pickup = hit.collider.GetComponentInParent<ComputerPartPickup>();
            if (pickup != null)
            {
                PickUp(pickup);
                return;
            }
        }

        if (Vector3.Distance(playerCamera.transform.position, bench.position) <= 4f)
            InstallHeldPart();
    }

    private void CreateWorkshop()
    {
        Vector3 center = Center;
        bench = CreateBlock("Верстак", center + new Vector3(0, 0.85f, 3.5f), new Vector3(4.2f, 0.22f, 2.2f), new Color(0.16f, 0.19f, 0.23f)).transform;
        CreateBlock("Опора верстака", center + new Vector3(0, 0.4f, 3.5f), new Vector3(3.8f, 0.7f, 1.8f), new Color(0.09f, 0.11f, 0.14f));
        CreateBlock("Сборочная площадка", center + new Vector3(0, 1.01f, 3.55f), new Vector3(1.8f, 0.1f, 1.55f), new Color(0.08f, 0.34f, 0.42f));

        computer = new GameObject("Собираемый компьютер").transform;
        computer.position = center + new Vector3(0, 1.08f, 3.55f);

        Vector3[] locations =
        {
            new Vector3(-3.4f, 0.2f, 0.2f), new Vector3(3.2f, 0.2f, -0.8f),
            new Vector3(-4.2f, 0.2f, 4.4f), new Vector3(4.1f, 0.2f, 4.8f),
            new Vector3(-2.4f, 0.2f, 7.1f), new Vector3(2.4f, 0.2f, 7.3f)
        };

        for (int i = 0; i < BuildOrder.Length; i++)
        {
            ComputerPartKind kind = BuildOrder[i];
            Vector3 partPosition = Center + locations[i];
            partPosition.y = Center.y - LowestPoint(kind) + 0.03f;
            GameObject part = CreatePartModel(kind, partPosition, Quaternion.identity);
            part.name = "Деталь: " + PartNames[(int)kind];
            ComputerPartPickup pickup = part.AddComponent<ComputerPartPickup>();
            pickup.kind = kind;
            pickup.displayName = PartNames[(int)kind];
            pickup.station = this;
            SetColliders(part, true);
        }
    }

    private static float LowestPoint(ComputerPartKind kind)
    {
        switch (kind)
        {
            case ComputerPartKind.Case: return 0.045f;
            case ComputerPartKind.Motherboard: return 0.89f;
            case ComputerPartKind.Processor: return 0.925f;
            case ComputerPartKind.Memory: return 0.57f;
            case ComputerPartKind.GraphicsCard: return 0.44f;
            default: return 1.21f;
        }
    }

    private void PickUp(ComputerPartPickup pickup)
    {
        if (heldPart != null)
        {
            ShowNotice("Сначала установите деталь, которую держите.");
            return;
        }
        if (!collected.Add(pickup.kind)) return;

        heldPart = pickup;
        pickup.transform.SetParent(playerCamera.transform, false);
        pickup.transform.localPosition = new Vector3(-0.45f, -0.45f, 0.85f);
        pickup.transform.localRotation = Quaternion.Euler(0, 180, 0);
        SetColliders(pickup.gameObject, false);
        ShowNotice("Подобрано: " + pickup.displayName + ". Вернитесь к верстаку и нажмите E.");
    }

    private void InstallHeldPart()
    {
        if (heldPart == null)
        {
            ShowNotice("Подберите деталь и подойдите к верстаку.");
            return;
        }
        if (installedCount >= BuildOrder.Length) return;
        ComputerPartKind required = BuildOrder[installedCount];
        if (heldPart.kind != required)
        {
            ShowNotice("Сейчас нужна деталь: " + PartNames[(int)required] + ".");
            return;
        }

        ComputerPartKind kind = heldPart.kind;
        Destroy(heldPart.gameObject);
        heldPart = null;
        AddInstalledPart(kind);
        installedCount++;

        if (installedCount == BuildOrder.Length)
        {
            assemblyComplete = true;
            ShowNotice("Компьютер собран! Через две секунды он взорвётся.");
            StartCoroutine(ExplodeComputerAfterDelay());
        }
        else
            ShowNotice("Установлено: " + PartNames[(int)kind] + ". Следующая деталь: " + PartNames[(int)BuildOrder[installedCount]] + ".");
    }

    private IEnumerator ExplodeComputerAfterDelay()
    {
        if (explosionStarted) yield break;
        explosionStarted = true;
        yield return new WaitForSeconds(2f);
        if (computer == null) yield break;

        Vector3 center = computer.position + Vector3.up * 0.95f;
        CreateExplosionEffect(center);
        CreateComputerDebris(center);

        Quaternion motherboardRotation = Quaternion.Euler(90f, Random.Range(0f, 360f), 0f);
        GameObject motherboardSource = motherboardPrefab;
        if (motherboardSource == null)
            motherboardSource = GameObject.Find("bio_motherboard");

        if (motherboardSource == null)
        {
            Debug.LogError("Не найдена модель bio_motherboard для выпадения из компьютера.", this);
        }
        else
        {
            GameObject motherboard = Instantiate(
                motherboardSource,
                computer.position + Vector3.up * 1.4f,
                motherboardRotation);
            motherboard.transform.localScale = Vector3.one * 0.18f;
            EnsureMotherboardCollider(motherboard);
            motherboard.name = "Выпавшая материнская плата";
            SetColliders(motherboard, true);
            Rigidbody motherboardBody = motherboard.AddComponent<Rigidbody>();
            motherboardBody.mass = 1.2f;
            motherboardBody.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            motherboardBody.interpolation = RigidbodyInterpolation.Interpolate;
            motherboardBody.AddExplosionForce(6f, center, 3.5f, 1.5f, ForceMode.Impulse);
            motherboardBody.AddTorque(Random.insideUnitSphere * 2f, ForceMode.Impulse);
            motherboard.AddComponent<ComputerMotherboardTouch>();
        }

        Destroy(computer.gameObject);
        computer = null;
        ShowNotice("Компьютер взорвался! Материнская плата выпала на платформу.");

        yield return new WaitForSeconds(5f);
        TeleportPlayerToCubeTen();
    }

    private static void EnsureMotherboardCollider(GameObject root)
    {
        Collider[] existing = root.GetComponentsInChildren<Collider>();
        if (existing.Length > 0)
        {
            foreach (Collider collider in existing) collider.enabled = true;
            return;
        }

        Renderer[] renderers = root.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0)
        {
            root.AddComponent<BoxCollider>();
            return;
        }

        Bounds localBounds = new Bounds();
        bool hasBounds = false;
        foreach (Renderer renderer in renderers)
        {
            Bounds bounds = renderer.bounds;
            for (int x = -1; x <= 1; x += 2)
            for (int y = -1; y <= 1; y += 2)
            for (int z = -1; z <= 1; z += 2)
            {
                Vector3 corner = bounds.center + Vector3.Scale(bounds.extents, new Vector3(x, y, z));
                Vector3 localCorner = root.transform.InverseTransformPoint(corner);
                if (!hasBounds)
                {
                    localBounds = new Bounds(localCorner, Vector3.zero);
                    hasBounds = true;
                }
                else
                {
                    localBounds.Encapsulate(localCorner);
                }
            }
        }

        BoxCollider box = root.AddComponent<BoxCollider>();
        box.center = localBounds.center;
        box.size = localBounds.size;
    }

    private void TeleportPlayerToCubeTen()
    {
        GameObject destinationPlatform = GameObject.Find("Cube (10)");
        WeaponController player = FindObjectOfType<WeaponController>();
        if (destinationPlatform == null || player == null) return;

        Vector3 destination = destinationPlatform.transform.position;
        Collider platformCollider = destinationPlatform.GetComponent<Collider>();
        if (platformCollider != null)
            destination.y = platformCollider.bounds.max.y + 1f;
        else
            destination.y += 1f;

        Rigidbody playerBody = player.GetComponent<Rigidbody>();
        if (playerBody != null)
        {
            playerBody.position = destination;
            playerBody.velocity = Vector3.zero;
            playerBody.angularVelocity = Vector3.zero;
        }
        else
        {
            player.transform.position = destination;
        }
    }

    private void CreateComputerDebris(Vector3 center)
    {
        Color[] colors = { new Color(0.12f, 0.14f, 0.18f), new Color(0.1f, 0.57f, 0.72f), new Color(0.18f, 0.2f, 0.23f) };
        for (int i = 0; i < 7; i++)
        {
            GameObject fragment = GameObject.CreatePrimitive(PrimitiveType.Cube);
            fragment.name = "Обломок компьютера";
            fragment.transform.position = center + Random.insideUnitSphere * 0.35f;
            fragment.transform.localScale = new Vector3(
                Random.Range(0.12f, 0.34f), Random.Range(0.12f, 0.3f), Random.Range(0.12f, 0.3f));
            fragment.GetComponent<Renderer>().material.color = colors[Random.Range(0, colors.Length)];
            Rigidbody body = fragment.AddComponent<Rigidbody>();
            body.mass = 0.25f;
            body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            body.AddExplosionForce(Random.Range(2f, 4f), center, 3.5f, 1f, ForceMode.Impulse);
            body.AddTorque(Random.insideUnitSphere * 2f, ForceMode.Impulse);
            Destroy(fragment, 4f);
        }
    }

    private static void CreateExplosionEffect(Vector3 position)
    {
        GameObject effect = new GameObject("Взрыв компьютера");
        effect.transform.position = position;
        ParticleSystem particles = effect.AddComponent<ParticleSystem>();
        ParticleSystem.MainModule main = particles.main;
        main.duration = 0.45f;
        main.loop = false;
        main.playOnAwake = false;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.25f, 0.65f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(2f, 5f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.12f, 0.32f);
        main.startColor = new ParticleSystem.MinMaxGradient(new Color(1f, 0.24f, 0.03f), new Color(1f, 0.88f, 0.18f));
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = 60;

        ParticleSystem.EmissionModule emission = particles.emission;
        emission.rateOverTime = 0f;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 45) });
        ParticleSystem.ShapeModule shape = particles.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.2f;

        ParticleSystemRenderer particleRenderer = particles.GetComponent<ParticleSystemRenderer>();
        Material explosionMaterial = new Material(Shader.Find("Sprites/Default"));
        particleRenderer.material = explosionMaterial;
        Light flash = effect.AddComponent<Light>();
        flash.type = LightType.Point;
        flash.color = new Color(1f, 0.32f, 0.06f);
        flash.intensity = 5f;
        flash.range = 5f;

        particles.Play();
        Destroy(effect, 1f);
        Destroy(explosionMaterial, 1f);
    }

    private void AddInstalledPart(ComputerPartKind kind)
    {
        Vector3 origin = computer.position;
        GameObject part = CreatePartModel(kind, origin, Quaternion.identity);
        part.name = PartNames[(int)kind] + " — установлена";
        part.transform.SetParent(computer, true);
        SetColliders(part, false);
    }

    private GameObject CreatePartModel(ComputerPartKind kind, Vector3 position, Quaternion rotation)
    {
        GameObject root = new GameObject(PartNames[(int)kind]);
        root.transform.SetPositionAndRotation(position, rotation);
        switch (kind)
        {
            case ComputerPartKind.Case:
                Part(root.transform, PrimitiveType.Cube, new Vector3(0, 0.92f, 0), new Vector3(1.25f, 1.75f, 0.72f), new Color(0.12f, 0.14f, 0.18f));
                Part(root.transform, PrimitiveType.Cube, new Vector3(0, 0.92f, -0.37f), new Vector3(0.88f, 1.35f, 0.025f), new Color(0.1f, 0.57f, 0.72f));
                Part(root.transform, PrimitiveType.Cube, new Vector3(0.35f, 0.28f, -0.39f), new Vector3(0.34f, 0.06f, 0.035f), Color.gray);
                break;
            case ComputerPartKind.Motherboard:
                Part(root.transform, PrimitiveType.Cube, new Vector3(0, 0.92f, -0.31f), new Vector3(0.98f, 1.42f, 0.06f), new Color(0.08f, 0.36f, 0.19f));
                for (int i = 0; i < 4; i++) Part(root.transform, PrimitiveType.Cube, new Vector3(-0.28f + i * 0.16f, 0.96f, -0.36f), new Vector3(0.09f, 0.88f, 0.045f), new Color(0.74f, 0.61f, 0.2f));
                break;
            case ComputerPartKind.Processor:
                Part(root.transform, PrimitiveType.Cube, new Vector3(0, 1.05f, -0.38f), new Vector3(0.38f, 0.38f, 0.12f), new Color(0.77f, 0.79f, 0.82f));
                Part(root.transform, PrimitiveType.Cube, new Vector3(0, 1.05f, -0.45f), new Vector3(0.25f, 0.25f, 0.025f), new Color(0.19f, 0.21f, 0.24f));
                break;
            case ComputerPartKind.Memory:
                for (int i = 0; i < 2; i++) Part(root.transform, PrimitiveType.Cube, new Vector3(-0.28f + i * 0.56f, 0.96f, -0.38f), new Vector3(0.12f, 0.78f, 0.1f), new Color(0.18f, 0.75f, 0.43f));
                break;
            case ComputerPartKind.GraphicsCard:
                Part(root.transform, PrimitiveType.Cube, new Vector3(0.04f, 0.58f, -0.43f), new Vector3(0.98f, 0.28f, 0.28f), new Color(0.17f, 0.19f, 0.23f));
                Part(root.transform, PrimitiveType.Cylinder, new Vector3(-0.17f, 0.58f, -0.59f), new Vector3(0.09f, 0.025f, 0.09f), new Color(0.1f, 0.68f, 0.85f));
                Part(root.transform, PrimitiveType.Cylinder, new Vector3(0.25f, 0.58f, -0.59f), new Vector3(0.09f, 0.025f, 0.09f), new Color(0.1f, 0.68f, 0.85f));
                break;
            case ComputerPartKind.Cooler:
                Part(root.transform, PrimitiveType.Cylinder, new Vector3(0, 1.31f, -0.49f), new Vector3(0.28f, 0.1f, 0.28f), new Color(0.12f, 0.63f, 0.8f));
                Part(root.transform, PrimitiveType.Cylinder, new Vector3(0, 1.31f, -0.59f), new Vector3(0.2f, 0.025f, 0.2f), new Color(0.15f, 0.17f, 0.21f));
                break;
        }
        return root;
    }

    private static GameObject Part(Transform parent, PrimitiveType type, Vector3 localPosition, Vector3 scale, Color color)
    {
        GameObject piece = GameObject.CreatePrimitive(type);
        piece.transform.SetParent(parent, false);
        piece.transform.localPosition = localPosition;
        piece.transform.localScale = scale;
        Renderer renderer = piece.GetComponent<Renderer>();
        if (renderer != null) renderer.material.color = color;
        return piece;
    }

    private static GameObject CreateBlock(string name, Vector3 position, Vector3 scale, Color color)
    {
        GameObject block = GameObject.CreatePrimitive(PrimitiveType.Cube);
        block.name = name;
        block.transform.position = position;
        block.transform.localScale = scale;
        block.GetComponent<Renderer>().material.color = color;
        return block;
    }

    private static void SetColliders(GameObject root, bool enabled)
    {
        foreach (Collider collider in root.GetComponentsInChildren<Collider>()) collider.enabled = enabled;
    }

    private void ShowNotice(string message)
    {
        notice = message;
        noticeUntil = Time.time + 5f;
    }

    private void OnGUI()
    {
        if (assemblyComplete || !IsPlayerOnWorkshopPlatform()) return;

        GUIStyle style = new GUIStyle(GUI.skin.box)
        {
            alignment = TextAnchor.UpperLeft,
            fontSize = 16,
            wordWrap = true,
            padding = new RectOffset(14, 14, 10, 10)
        };
        style.normal.textColor = Color.white;
        string held = heldPart != null ? "В руках: " + heldPart.displayName : "В руках: ничего";
        string next = installedCount < BuildOrder.Length ? PartNames[(int)BuildOrder[installedCount]] : "Готово!";
        string status = "СБОРКА КОМПЬЮТЕРА\nE — взять деталь или установить её у верстака\n" + held + "\nУстановлено: " + installedCount + "/" + BuildOrder.Length + "\nСледующая деталь: " + next;
        GUI.Box(new Rect(18, 18, 370, 142), status, style);

        if (Time.time < noticeUntil)
        {
            GUIStyle noticeStyle = new GUIStyle(style) { alignment = TextAnchor.MiddleCenter, fontSize = 18 };
            GUI.Box(new Rect(Screen.width * 0.5f - 310, Screen.height - 105, 620, 56), notice, noticeStyle);
        }
    }
}
