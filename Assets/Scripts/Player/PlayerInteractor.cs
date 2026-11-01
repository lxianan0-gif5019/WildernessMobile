using UnityEngine;

// Looks at whatever is in front of the camera and harvests it on interact.
public class PlayerInteractor : MonoBehaviour
{
    public static PlayerInteractor Instance;

    public float reach = 4.5f;
    public Camera cam;
    public ResourceNode Current { get; private set; }

    void Awake()
    {
        Instance = this;
        if (cam == null) cam = Camera.main;
    }

    void Update()
    {
        Current = null;
        if (cam == null) return;

        Ray ray = new Ray(cam.transform.position, cam.transform.forward);
        RaycastHit hit;
        if (Physics.Raycast(ray, out hit, reach))
            Current = hit.collider.GetComponentInParent<ResourceNode>();

        if (InputHub.InteractDown && Current != null)
            Current.Harvest();
    }

    public string Prompt()
    {
        if (Current == null) return "";
        return "Harvest " + Current.displayName;
    }
}
