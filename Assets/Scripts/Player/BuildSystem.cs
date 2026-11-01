using UnityEngine;

// Place-mode: shows a ghost of the wall in front of the camera and drops a real
// one on interact, spending one Wall item.
public class BuildSystem : MonoBehaviour
{
    public static BuildSystem Instance;

    public Camera cam;
    public float range = 8f;

    public bool Active { get; private set; }

    GameObject ghost;
    Material ghostMat;
    Material wallMat;
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
        var ghostRenderer = ghost.GetComponent<Renderer>();
        ghostRenderer.sharedMaterial = ghostMat;
        ghostRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        ghostRenderer.receiveShadows = false;
        ghost.transform.localScale = new Vector3(3f, 2.5f, 0.3f);
        ghost.SetActive(false);

        wallMat = new Material(shader);
        wallMat.color = new Color(0.63f, 0.46f, 0.26f, 1f);
    }

    void Update()
    {
        if (InputHub.BuildDown)
        {
            Active = !Active;
            if (ghost != null) ghost.SetActive(Active);
            Debug.Log("[Build] mode " + (Active ? "on" : "off"));
        }

        if (!Active || cam == null) return;

        Vector3 point = AimPoint();
        float yaw = cam.transform.eulerAngles.y;

        // Only show the preview when the wall can actually be placed.
        bool hasWall = Inventory.Instance != null && Inventory.Instance.Count("wall") > 0;
        if (ghost != null)
        {
            ghost.SetActive(hasWall);
            if (hasWall)
            {
                ghost.transform.position = point + Vector3.up * 1.25f;
                ghost.transform.rotation = Quaternion.Euler(0f, yaw, 0f);
            }
        }

        bool aimingAtResource = PlayerInteractor.Instance != null && PlayerInteractor.Instance.Current != null;
        if (InputHub.InteractDown && !aimingAtResource) Place(point, yaw);
    }

    // Nearest hit in front of the camera that is not the player and not the ghost.
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

        // Never let the preview sit on top of the camera.
        Vector3 flat = point - origin;
        if (flat.magnitude < 2f) point = origin + cam.transform.forward * 2f;
        return point;
    }

    void Place(Vector3 point, float yaw)
    {
        if (Inventory.Instance == null || !Inventory.Instance.Spend("wall", 1))
        {
            if (Time.unscaledTime >= nextHint)
            {
                nextHint = Time.unscaledTime + 1.5f;
                Debug.Log("[Build] need 1 Wall - open Craft (C) and make one from 2 Planks");
            }
            return;
        }

        var wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
        wall.name = "Wall";
        wall.transform.localScale = new Vector3(3f, 2.5f, 0.3f);
        wall.transform.position = point + Vector3.up * 1.25f;
        wall.transform.rotation = Quaternion.Euler(0f, yaw, 0f);
        wall.GetComponent<Renderer>().sharedMaterial = wallMat;
        Debug.Log("[Build] placed wall");
    }
}
