using UnityEngine;

// Finds what the player is targeting: a harvestable resource, or a structure
// that was placed earlier and can be picked back up.
public class PlayerInteractor : MonoBehaviour
{
    public static PlayerInteractor Instance;

    [Tooltip("Max distance from the PLAYER to a harvestable node.")]
    public float reach = 4f;

    [Tooltip("Max distance from the PLAYER to a placed structure when picking it up.")]
    public float pickupReach = 5f;

    [Tooltip("How far the camera ray is allowed to travel.")]
    public float rayLength = 14f;

    public Camera cam;
    public ResourceNode Current { get; private set; }
    public PlacedStructure CurrentStructure { get; private set; }
    public WaterSource CurrentWater { get; private set; }
    public Animal CurrentAnimal { get; private set; }
    public Campfire CurrentCampfire { get; private set; }

    [Tooltip("Damage dealt to an animal per interact press.")]
    public float attackDamage = 22f;

    ResourceNode[] cached;
    float nextScan;

    void Awake()
    {
        Instance = this;
        if (cam == null) cam = Camera.main;
    }

    void Update()
    {
        Current = FindResource();
        CurrentStructure = FindStructure();
        CurrentWater = FindWater();
        CurrentAnimal = FindAnimal();
        CurrentCampfire = FindCampfire();

        if (!InputHub.InteractDown) return;

        // Hunting comes first: if you are aiming at an animal, that is the intent.
        if (CurrentAnimal != null) { CurrentAnimal.TakeDamage(attackDamage); return; }

        // Cooking at a fire beats picking it up.
        if (CurrentCampfire != null && CurrentCampfire.HasMeat()) { CurrentCampfire.Cook(); return; }

        if (Current != null) { Current.Harvest(); return; }

        // In build mode the build system owns the interact key.
        if (BuildSystem.Instance != null && BuildSystem.Instance.Active) return;

        if (CurrentWater != null) { CurrentWater.Drink(); return; }

        if (CurrentStructure != null) CurrentStructure.PickUp();
    }

    Campfire FindCampfire()
    {
        if (cam == null) return null;

        var hits = Physics.RaycastAll(cam.transform.position, cam.transform.forward, rayLength, ~0, QueryTriggerInteraction.Ignore);
        float best = float.MaxValue;
        Campfire found = null;

        foreach (var h in hits)
        {
            if (h.collider == null) continue;
            if (transform.root != null && h.collider.transform.IsChildOf(transform.root)) continue;

            var c = h.collider.GetComponentInParent<Campfire>();
            if (c == null) continue;
            if (Vector3.Distance(c.transform.position, transform.position) > pickupReach) continue;

            if (h.distance < best) { best = h.distance; found = c; }
        }
        return found;
    }

    Animal FindAnimal()
    {
        if (cam == null) return null;

        var hits = Physics.RaycastAll(cam.transform.position, cam.transform.forward, rayLength, ~0, QueryTriggerInteraction.Ignore);
        float best = float.MaxValue;
        Animal found = null;

        foreach (var h in hits)
        {
            if (h.collider == null) continue;
            if (transform.root != null && h.collider.transform.IsChildOf(transform.root)) continue;

            var a = h.collider.GetComponentInParent<Animal>();
            if (a == null) continue;
            if (Vector3.Distance(a.transform.position, transform.position) > reach + 1.5f) continue;

            if (h.distance < best) { best = h.distance; found = a; }
        }
        return found;
    }

    WaterSource FindWater()
    {
        if (cam == null) return null;

        var hits = Physics.RaycastAll(cam.transform.position, cam.transform.forward, rayLength, ~0, QueryTriggerInteraction.Ignore);
        float best = float.MaxValue;
        WaterSource found = null;

        foreach (var h in hits)
        {
            if (h.collider == null) continue;
            if (transform.root != null && h.collider.transform.IsChildOf(transform.root)) continue;

            var w = h.collider.GetComponentInParent<WaterSource>();
            if (w == null) continue;
            if (Vector3.Distance(w.transform.position, transform.position) > pickupReach) continue;

            if (h.distance < best) { best = h.distance; found = w; }
        }
        return found;
    }

    ResourceNode FindResource()
    {
        // --- 1) direct ray hit
        if (cam != null)
        {
            var hits = Physics.RaycastAll(cam.transform.position, cam.transform.forward, rayLength, ~0, QueryTriggerInteraction.Ignore);
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

        // --- 2) fallback: nearest node around the player
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

            // Up close, facing does not matter.
            if (cam != null && d > 2f)
            {
                Vector3 to = n.transform.position - cam.transform.position; to.y = 0f;
                Vector3 fwd = cam.transform.forward; fwd.y = 0f;
                if (to.sqrMagnitude > 0.0001f && fwd.sqrMagnitude > 0.0001f && Vector3.Angle(to, fwd) > 100f) continue;
            }

            if (d < nearestDist) { nearestDist = d; nearest = n; }
        }
        return nearest;
    }

    PlacedStructure FindStructure()
    {
        if (cam == null) return null;

        var hits = Physics.RaycastAll(cam.transform.position, cam.transform.forward, rayLength, ~0, QueryTriggerInteraction.Ignore);
        float best = float.MaxValue;
        PlacedStructure found = null;

        foreach (var h in hits)
        {
            if (h.collider == null) continue;
            if (transform.root != null && h.collider.transform.IsChildOf(transform.root)) continue;

            var s = h.collider.GetComponentInParent<PlacedStructure>();
            if (s == null) continue;
            if (Vector3.Distance(s.transform.position, transform.position) > pickupReach) continue;

            if (h.distance < best) { best = h.distance; found = s; }
        }
        return found;
    }

    public string Prompt()
    {
        if (CurrentAnimal != null) return "Hit " + CurrentAnimal.displayName;
        if (CurrentCampfire != null && CurrentCampfire.HasMeat()) return "Cook meat";
        if (Current != null) return "Harvest " + Current.displayName;
        if (CurrentWater != null) return "Drink " + CurrentWater.displayName;
        if (CurrentStructure != null) return "Pick up " + CurrentStructure.displayName;
        return "";
    }
}
