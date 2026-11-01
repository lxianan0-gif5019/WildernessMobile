using UnityEngine;

// Finds the harvestable resource the player is aiming at, and harvests it on
// interact. Works from two signals so it is forgiving on touch screens:
//   1) what the camera ray actually hits
//   2) fallback: the nearest node close to the player and roughly ahead of the camera
public class PlayerInteractor : MonoBehaviour
{
    public static PlayerInteractor Instance;

    [Tooltip("Max distance from the PLAYER to a harvestable node.")]
    public float reach = 4f;

    [Tooltip("How far the camera ray is allowed to travel.")]
    public float rayLength = 14f;

    public Camera cam;
    public ResourceNode Current { get; private set; }

    ResourceNode[] cached;
    float nextScan;

    void Awake()
    {
        Instance = this;
        if (cam == null) cam = Camera.main;
    }

    void Update()
    {
        Current = FindTarget();
        if (InputHub.InteractDown && Current != null)
            Current.Harvest();
    }

    ResourceNode FindTarget()
    {
        // --- 1) direct ray hit
        if (cam != null)
        {
            Vector3 origin = cam.transform.position;
            Vector3 dir = cam.transform.forward;
            var hits = Physics.RaycastAll(origin, dir, rayLength, ~0, QueryTriggerInteraction.Ignore);

            float bestRay = float.MaxValue;
            ResourceNode bestNode = null;
            foreach (var h in hits)
            {
                if (h.collider == null) continue;
                if (transform.root != null && h.collider.transform.IsChildOf(transform.root)) continue;

                var node = h.collider.GetComponentInParent<ResourceNode>();
                if (node == null) continue;
                if (Vector3.Distance(node.transform.position, transform.position) > reach) continue;

                if (h.distance < bestRay) { bestRay = h.distance; bestNode = node; }
            }
            if (bestNode != null) return bestNode;
        }

        // --- 2) fallback: nearest node around the player, in front of the camera
        if (Time.unscaledTime >= nextScan || cached == null)
        {
            cached = Object.FindObjectsOfType<ResourceNode>();
            nextScan = Time.unscaledTime + 0.3f;
        }

        ResourceNode nearest = null;
        float nearestDist = float.MaxValue;

        foreach (var n in cached)
        {
            if (n == null) continue;
            float d = Vector3.Distance(n.transform.position, transform.position);
            if (d > reach) continue;

            // Up close, facing does not matter - you clearly mean the thing you are touching.
            if (cam != null && d > 2f)
            {
                Vector3 to = n.transform.position - cam.transform.position;
                to.y = 0f;
                Vector3 fwd = cam.transform.forward;
                fwd.y = 0f;
                if (to.sqrMagnitude > 0.0001f && fwd.sqrMagnitude > 0.0001f &&
                    Vector3.Angle(to, fwd) > 100f) continue;
            }

            if (d < nearestDist) { nearestDist = d; nearest = n; }
        }
        return nearest;
    }

    public string Prompt()
    {
        if (Current == null) return "";
        return "Harvest " + Current.displayName;
    }
}
