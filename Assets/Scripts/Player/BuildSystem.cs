using UnityEngine;

// Build mode: shows a ghost of the item in front of the camera and places it on
// interact, spending one of that item. Supports walls and campfires.
public class BuildSystem : MonoBehaviour
{
    public static BuildSystem Instance;

    public Camera cam;
    public float range = 8f;

    [Tooltip("Which item is currently being placed: 'wall' or 'campfire'.")]
    public string buildType = "wall";

    public bool Active { get; private set; }

    GameObject ghost;
    Material ghostMat;
    Material wallMat;
    Material fireMat;
    float nextHint;

    void Awake()
    {
        Instance = this;
        if (cam == null) cam = Camera.main;

        var shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null) shader = Shader.Find("Standard");

        ghost = GameObject.CreatePrimitive(PrimitiveType.Cube);
        ghost.name = "BuildGhost";
        Object.Destroy(ghost.GetComponent<Collider>());
        ghostMat = new Material(shader);
        ghostMat.color = new Color(0.45f, 0.85f, 1f, 1f);
        var gr = ghost.GetComponent<Renderer>();
        gr.sharedMaterial = ghostMat;
        gr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        gr.receiveShadows = false;
        ghost.SetActive(false);

        wallMat = new Material(shader);
        wallMat.color = new Color(0.63f, 0.46f, 0.26f, 1f);

        fireMat = new Material(shader);
        fireMat.color = new Color(0.42f, 0.28f, 0.16f, 1f);
    }

    public void EnterBuildMode(string type)
    {
        buildType = string.IsNullOrEmpty(type) ? "wall" : type;
        Active = true;
        if (ghost != null) ghost.SetActive(false);
        Debug.Log("[Build] build mode on for " + buildType);
    }

    void Update()
    {
        if (InputHub.BuildDown)
        {
            Active = !Active;
            if (ghost != null) ghost.SetActive(false);
            Debug.Log("[Build] mode " + (Active ? "on" : "off"));
        }

        if (!Active || cam == null) return;

        Vector3 point = AimPoint();
        float yaw = cam.transform.eulerAngles.y;

        bool hasItem = Inventory.Instance != null && Inventory.Instance.Count(buildType) > 0;
        if (ghost != null)
        {
            ghost.SetActive(hasItem);
            if (hasItem)
            {
                if (buildType == "campfire")
                {
                    ghost.transform.localScale = new Vector3(1.8f, 0.5f, 1.8f);
                    ghost.transform.position = point + Vector3.up * 0.25f;
                }
                else
                {
                    ghost.transform.localScale = new Vector3(3f, 2.5f, 0.3f);
                    ghost.transform.position = point + Vector3.up * 1.25f;
                }
                ghost.transform.rotation = Quaternion.Euler(0f, yaw, 0f);
            }
        }

        bool aimingAtThing = PlayerInteractor.Instance != null &&
                             (PlayerInteractor.Instance.Current != null ||
                              PlayerInteractor.Instance.CurrentAnimal != null ||
                              PlayerInteractor.Instance.CurrentWater != null);

        if (InputHub.InteractDown && !aimingAtThing) Place(point, yaw);
    }

    Vector3 AimPoint()
    {
        Vector3 origin = cam.transform.position;
        Vector3 fallback = origin + cam.transform.forward * range;

        var hits = Physics.RaycastAll(origin, cam.transform.forward, range * 1.6f, ~0, QueryTriggerInteraction.Ignore);
        float best = float.MaxValue;
        Vector3 point = fallback;

        foreach (var h in hits)
        {
            if (h.collider == null) continue;
            if (ghost != null && h.collider.gameObject == ghost) continue;
            if (transform.root != null && h.collider.transform.IsChildOf(transform.root)) continue;
            if (h.distance < best) { best = h.distance; point = h.point; }
        }

        if ((point - origin).magnitude < 2f) point = origin + cam.transform.forward * 2f;
        return point;
    }

    void Place(Vector3 point, float yaw)
    {
        if (Inventory.Instance == null || !Inventory.Instance.Spend(buildType, 1))
        {
            if (Time.unscaledTime >= nextHint)
            {
                nextHint = Time.unscaledTime + 1.5f;
                Debug.Log("[Build] need 1 " + buildType + " - craft it first");
            }
            return;
        }

        if (buildType == "campfire") CreateCampfire(point);
        else CreateWall(point + Vector3.up * 1.25f, yaw);

        Debug.Log("[Build] placed " + buildType);
    }

    /// <summary>Spawns a wall at an exact world position (used by placing and by load).</summary>
    public GameObject CreateWall(Vector3 worldPosition, float yaw)
    {
        var wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
        wall.name = "Wall";
        wall.transform.localScale = new Vector3(3f, 2.5f, 0.3f);
        wall.transform.position = worldPosition;
        wall.transform.rotation = Quaternion.Euler(0f, yaw, 0f);
        wall.GetComponent<Renderer>().sharedMaterial = wallMat;

        var structure = wall.AddComponent<PlacedStructure>();
        structure.itemId = "wall";
        structure.displayName = "Wall";
        return wall;
    }

    /// <summary>Spawns a campfire (light + cooking) at a ground position.</summary>
    public GameObject CreateCampfire(Vector3 groundPosition)
    {
        var root = new GameObject("Campfire");
        root.transform.position = groundPosition;

        for (int i = 0; i < 3; i++)
        {
            var log = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            log.name = "Log" + i;
            Object.Destroy(log.GetComponent<Collider>());
            log.transform.SetParent(root.transform, false);
            log.transform.localScale = new Vector3(0.22f, 0.9f, 0.22f);
            log.transform.localRotation = Quaternion.Euler(78f, i * 60f, 0f);
            log.transform.localPosition = new Vector3(0f, 0.22f, 0f);
            log.GetComponent<Renderer>().sharedMaterial = fireMat;
        }

        var col = root.AddComponent<BoxCollider>();
        col.center = new Vector3(0f, 0.3f, 0f);
        col.size = new Vector3(1.8f, 0.6f, 1.8f);

        root.AddComponent<Campfire>();

        var structure = root.AddComponent<PlacedStructure>();
        structure.itemId = "campfire";
        structure.displayName = "Campfire";
        return root;
    }
}
