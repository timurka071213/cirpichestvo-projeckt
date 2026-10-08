using UnityEngine;

public class WeaponController : MonoBehaviour
{
    [SerializeField] private Camera playerCamera;
    [SerializeField] private float range = 100f;
    [SerializeField] private float fireRate = 0.25f;
    [SerializeField] private float pickupDistance = 1.5f;
    [SerializeField] private GameObject motherboardPrefab;

    private const float PickupGroundOffset = 0.28f;

    private bool hasWeapon;
    private float nextShotTime;
    private Transform equippedModel;

    private void Start()
    {
        if (playerCamera == null) playerCamera = GetComponentInChildren<Camera>();
        if (playerCamera == null)
        {
            Debug.LogError("WeaponController: assign the player's camera.", this);
            enabled = false;
            return;
        }

        CreatePickup();
        if (GetComponent<ShotTargetGoal>() == null)
            gameObject.AddComponent<ShotTargetGoal>();

        // Attach the assembly workshop at runtime as well as in the scene. This
        // lets the platform work even when Unity kept an older scene in memory.
        GameObject assemblyPlatform = GameObject.Find("Cube (9)");
        if (assemblyPlatform != null)
        {
            ComputerAssemblyStation station = assemblyPlatform.GetComponent<ComputerAssemblyStation>();
            if (station == null) station = assemblyPlatform.AddComponent<ComputerAssemblyStation>();
            station.motherboardPrefab = motherboardPrefab;
        }
    }

    private void Update()
    {
        if (!hasWeapon || playerCamera == null) return;
        if (Input.GetMouseButton(0) && Time.time >= nextShotTime)
        {
            nextShotTime = Time.time + fireRate;
            Shoot();
        }
    }

    private void OnGUI()
    {
        if (!hasWeapon || playerCamera == null || !playerCamera.isActiveAndEnabled) return;
        if (Event.current.type != EventType.Repaint) return;

        Rect viewport = playerCamera.pixelRect;
        float x = viewport.center.x;
        float y = Screen.height - viewport.center.y;
        Color previousColor = GUI.color;

        // A dark outline keeps the crosshair visible against bright surfaces.
        GUI.color = Color.black;
        GUI.DrawTexture(new Rect(x - 9f, y - 2f, 18f, 4f), Texture2D.whiteTexture);
        GUI.DrawTexture(new Rect(x - 2f, y - 9f, 4f, 18f), Texture2D.whiteTexture);
        GUI.color = Color.white;
        GUI.DrawTexture(new Rect(x - 8f, y - 1f, 16f, 2f), Texture2D.whiteTexture);
        GUI.DrawTexture(new Rect(x - 1f, y - 8f, 2f, 16f), Texture2D.whiteTexture);
        GUI.color = previousColor;
    }
    private void CreatePickup()
    {
        Vector3 position = transform.position + transform.forward * pickupDistance;
        Vector3 floorRayOrigin = new Vector3(
            position.x,
            transform.position.y + 0.25f,
            position.z);
        if (Physics.Raycast(
                floorRayOrigin,
                Vector3.down,
                out RaycastHit groundHit,
                10f,
                Physics.DefaultRaycastLayers,
                QueryTriggerInteraction.Ignore))
        {
            position.y = groundHit.point.y + PickupGroundOffset;
        }
        else
        {
            position.y = PickupGroundOffset;
        }

        GameObject pickup = new GameObject("Подбираемая пушка");
        pickup.transform.SetPositionAndRotation(
            position,
            Quaternion.Euler(0f, transform.eulerAngles.y, 90f));
        SphereCollider trigger = pickup.AddComponent<SphereCollider>();
        trigger.isTrigger = true;
        trigger.radius = 0.8f;
        pickup.AddComponent<WeaponPickup>();

        CreatePart(pickup.transform, PrimitiveType.Cube, new Vector3(0, 0, 0), new Vector3(0.48f, 0.16f, 0.22f));
        CreatePart(pickup.transform, PrimitiveType.Cube, new Vector3(0, -0.14f, -0.04f), new Vector3(0.16f, 0.28f, 0.18f));
        CreatePart(pickup.transform, PrimitiveType.Cylinder, new Vector3(0, 0.015f, 0.25f), new Vector3(0.035f, 0.28f, 0.035f), new Vector3(90, 0, 0));
    }

    private static GameObject CreatePart(Transform parent, PrimitiveType type, Vector3 position, Vector3 scale, Vector3 rotation = default)
    {
        GameObject part = GameObject.CreatePrimitive(type);
        part.name = "Пушка — деталь";
        part.transform.SetParent(parent, false);
        part.transform.localPosition = position;
        part.transform.localRotation = Quaternion.Euler(rotation);
        part.transform.localScale = scale;
        MeshRenderer renderer = part.GetComponent<MeshRenderer>();
        if (renderer != null)
            renderer.material.color = Color.black;
        Collider collider = part.GetComponent<Collider>();
        if (collider != null) Destroy(collider);
        return part;
    }

    public void PickUp(WeaponPickup pickup)
    {
        if (hasWeapon) return;
        hasWeapon = true;
        Destroy(pickup.gameObject);

        GameObject model = new GameObject("Пушка в руках");
        equippedModel = model.transform;
        equippedModel.SetParent(playerCamera.transform, false);
        equippedModel.localPosition = new Vector3(0.28f, -0.24f, 0.55f);
        equippedModel.localRotation = Quaternion.identity;
        CreatePart(equippedModel, PrimitiveType.Cube, Vector3.zero, new Vector3(0.16f, 0.13f, 0.42f));
        CreatePart(equippedModel, PrimitiveType.Cube, new Vector3(0, -0.12f, -0.08f), new Vector3(0.11f, 0.24f, 0.13f));
        CreatePart(equippedModel, PrimitiveType.Cylinder, new Vector3(0, 0.015f, 0.3f), new Vector3(0.025f, 0.21f, 0.025f), new Vector3(90, 0, 0));
    }

    private void Shoot()
    {
        Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);
        Vector3 end = ray.origin + ray.direction * range;
        Vector3 muzzlePosition = equippedModel != null
            ? equippedModel.TransformPoint(new Vector3(0f, 0.015f, 0.51f))
            : ray.origin;
        Quaternion muzzleRotation = equippedModel != null
            ? equippedModel.rotation
            : playerCamera.transform.rotation;
        CreateMuzzleFlash(muzzlePosition, muzzleRotation);

        if (Physics.Raycast(ray, out RaycastHit hit, range))
        {
            end = hit.point;
            CreateImpactEffect(hit.point, hit.normal);
            Rigidbody hitBody = hit.rigidbody;
            if (hitBody != null && !hitBody.isKinematic)
                hitBody.AddForceAtPosition(ray.direction * 12f, hit.point, ForceMode.Impulse);
            Debug.Log("Попадание: " + hit.collider.name);
            DisappearOnShot target = hit.collider.GetComponentInParent<DisappearOnShot>();
            if (target != null && target.isActiveAndEnabled)
                target.OnShot();
        }

        GameObject tracer = new GameObject("След выстрела");
        LineRenderer line = tracer.AddComponent<LineRenderer>();
        line.positionCount = 2;
        line.SetPosition(0, muzzlePosition);
        line.SetPosition(1, end);
        line.startWidth = 0.07f;
        line.endWidth = 0.035f;
        Material tracerMaterial = new Material(Shader.Find("Sprites/Default"));
        line.sharedMaterial = tracerMaterial;
        line.startColor = Color.yellow;
        line.endColor = Color.yellow;
        Destroy(tracer, 0.25f);
        Destroy(tracerMaterial, 0.25f);
    }

    private static void CreateMuzzleFlash(Vector3 position, Quaternion rotation)
    {
        GameObject effect = new GameObject("Вспышка выстрела");
        effect.transform.SetPositionAndRotation(position, rotation);

        ParticleSystem particles = effect.AddComponent<ParticleSystem>();
        ParticleSystem.MainModule main = particles.main;
        main.duration = 0.12f;
        main.loop = false;
        main.playOnAwake = false;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.05f, 0.12f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(2.5f, 5f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.04f, 0.1f);
        main.startColor = new ParticleSystem.MinMaxGradient(new Color(1f, 0.2f, 0.02f), new Color(1f, 0.95f, 0.2f));
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = 24;

        ParticleSystem.EmissionModule emission = particles.emission;
        emission.rateOverTime = 0f;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 12) });

        ParticleSystem.ShapeModule shape = particles.shape;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = 22f;
        shape.radius = 0.015f;
        shape.length = 0.05f;

        ParticleSystemRenderer particleRenderer = particles.GetComponent<ParticleSystemRenderer>();
        Material particleMaterial = new Material(Shader.Find("Sprites/Default"));
        particleMaterial.color = new Color(1f, 0.35f, 0.05f, 0.95f);
        particleRenderer.material = particleMaterial;

        Light flashLight = effect.AddComponent<Light>();
        flashLight.type = LightType.Point;
        flashLight.color = new Color(1f, 0.3f, 0.05f);
        flashLight.intensity = 3f;
        flashLight.range = 2f;

        particles.Play();
        Destroy(effect, 0.25f);
        Destroy(particleMaterial, 0.25f);
    }

    private static void CreateImpactEffect(Vector3 position, Vector3 normal)
    {
        GameObject effect = new GameObject("Взрыв попадания");
        effect.transform.position = position + normal * 0.02f;

        ParticleSystem particles = effect.AddComponent<ParticleSystem>();
        ParticleSystem.MainModule main = particles.main;
        main.duration = 0.35f;
        main.loop = false;
        main.playOnAwake = false;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.25f, 0.65f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(3f, 7f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.08f, 0.2f);
        main.startColor = new ParticleSystem.MinMaxGradient(new Color(1f, 0.25f, 0.02f), new Color(1f, 0.9f, 0.15f));
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = 48;

        ParticleSystem.EmissionModule emission = particles.emission;
        emission.rateOverTime = 0f;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 24) });

        ParticleSystem.ShapeModule shape = particles.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.08f;

        ParticleSystemRenderer particleRenderer = particles.GetComponent<ParticleSystemRenderer>();
        Material particleMaterial = new Material(Shader.Find("Sprites/Default"));
        particleMaterial.color = new Color(1f, 0.4f, 0.05f, 0.9f);
        particleRenderer.material = particleMaterial;

        Light impactLight = effect.AddComponent<Light>();
        impactLight.type = LightType.Point;
        impactLight.color = new Color(1f, 0.35f, 0.05f);
        impactLight.intensity = 5f;
        impactLight.range = 3f;

        particles.Play();
        Destroy(effect, 0.7f);
        Destroy(particleMaterial, 0.7f);
    }
}

public class WeaponPickup : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        WeaponController controller = other.GetComponentInParent<WeaponController>();
        if (controller != null) controller.PickUp(this);
    }
}
