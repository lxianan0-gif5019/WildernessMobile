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
        ghost.GetComponent<Renderer>().sharedMaterial = ghostMat;
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

        Vector3 point = cam.transform.position + cam.transform.forward * range;
        RaycastHit hit;
        if (Physics.Raycast(cam.transform.position, cam.transform.forward, out hit, range * 1.6f))
            point = hit.point;

        float yaw = cam.transform.eulerAngles.y;
        if (ghost != null)
        {
            ghost.transform.position = point + Vector3.up * 1.25f;
            ghost.transform.rotation = Quaternion.Euler(0f, yaw, 0f);
        }

        bool aimingAtResource = PlayerInteractor.Instance != null && PlayerInteractor.Instance.Current != null;
        if (InputHub.InteractDown && !aimingAtResource) Place(point, yaw);
    }

    void Place(Vector3 point, float yaw)
    {
        if (Inventory.Instance == null || !Inventory.Instance.Spend("wall", 1))
        {
            Debug.Log("[Build] need 1 Wall (craft walls first)");
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
