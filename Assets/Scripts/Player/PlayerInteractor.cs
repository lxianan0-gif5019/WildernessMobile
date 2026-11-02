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
    public Wolf CurrentWolf { get; private set; }
    public SleepSpot CurrentSleep { get; private set; }
    public ItemPickup CurrentPickup { get; private set; }

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
        CurrentWolf = FindWolf();
        CurrentSleep = FindSleep();
        CurrentPickup = FindPickup();

        if (InputHub.InteractDown) TryInteract();
    }

    /// <summary>Runs the interact action for whatever is currently targeted.</summary>
    public void TryInteract()
    {
        // A wolf in your face takes priority over everything else.
        if (CurrentWolf != null) { CurrentWolf.TakeDamage(attackDamage); return; }

        // Hunting comes first: if you are aiming at an animal, that is the intent.
        if (CurrentAnimal != null) { CurrentAnimal.TakeDamage(attackDamage); return; }

        // Cooking at a fire beats picking it up.
        if (CurrentCampfire != null && CurrentCampfire.HasMeat()) { CurrentCampfire.Cook(); return; }

        // Sleeping beats picking the bed up.
        if (CurrentSleep != null)
        {
            string why;
            if (CurrentSleep.Sleep(out why)) return;
            Debug.Log("[Sleep] cannot sleep: " + why);
            return;
        }

        // Picking loot up beats chopping: you are clearly reaching for the drop.
        if (CurrentPickup != null) { CurrentPickup.Collect(); return; }

        if (Current != null)
        {
            if (!string.IsNullOrEmpty(Current.requiredTool) &&
                (Inventory.Instance == null || Inventory.Instance.Count(Current.requiredTool) == 0))
            {
                Debug.Log("[Interact] need " + ItemDatabase.Display(Current.requiredTool) + " to harvest " + Current.displayName);
                return;
            }
            Current.Harvest(HarvestPower());
            if (AudioManager.Instance != null) AudioManager.Instance.Play(AudioManager.Sfx.Chop);
            return;
        }

        // In build mode the build system owns the interact key.
        if (BuildSystem.Instance != null && BuildSystem.Instance.Active) return;

        if (CurrentWater != null) { CurrentWater.Drink(); return; }

        if (CurrentStructure != null) CurrentStructure.PickUp();
    }

    ItemPickup FindPickup()
    {
        ItemPickup best = null;
        float bestDist = pickupReach;

        var all = ItemPickup.All;
        for (int i = 0; i < all.Count; i++)
        {
            var p = all[i];
            if (p == null) continue;
            float d = Vector3.Distance(p.transform.position, transform.position);
            if (d < bestDist) { bestDist = d; best = p; }
        }
        return best;
    }

    SleepSpot FindSleep()
    {
        if (cam == null) return null;

        var hits = Physics.RaycastAll(cam.transform.position, cam.transform.forward, rayLength, ~0, QueryTriggerInteraction.Ignore);
        float best = float.MaxValue;
        SleepSpot found = null;

        foreach (var h in hits)
        {
            if (h.collider == null) continue;
            if (transform.root != null && h.collider.transform.IsChildOf(transform.root)) continue;

            var s = h.collider.GetComponentInParent<SleepSpot>();
            if (s == null) continue;
            if (Vector3.Distance(s.transform.position, transform.position) > pickupReach) continue;

            if (h.distance < best) { best = h.distance; found = s; }
        }
        return found;
    }

    Wolf FindWolf()
    {
        if (cam == null) return null;

        var hits = Physics.RaycastAll(cam.transform.position, cam.transform.forward, rayLength, ~0, QueryTriggerInteraction.Ignore);
        float best = float.MaxValue;
        Wolf found = null;

        foreach (var h in hits)
        {
            if (h.collider == null) continue;
            if (transform.root != null && h.collider.transform.IsChildOf(transform.root)) continue;

            var w = h.collider.GetComponentInParent<Wolf>();
            if (w == null || w.Dead) continue;
            if (Vector3.Distance(w.transform.position, transform.position) > reach + 1.5f) continue;

            if (h.distance < best) { best = h.distance; found = w; }
        }
        return found;
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

    /// <summary>How many harvest hits one press counts for, based on the best tool carried.</summary>
    float HarvestPower()
    {
        var inv = Inventory.Instance;
        if (inv == null) return 1f;
        if (inv.Count("iron_axe") > 0 || inv.Count("iron_pick") > 0) return 3f;
        if (inv.Count("stone_axe") > 0 || inv.Count("stone_pick") > 0) return 2f;
        return 1f;
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
        if (CurrentWolf != null) return "Fight Wolf";
        if (CurrentPickup != null) return CurrentPickup.Label();
        if (CurrentSleep != null)
        {
            string why;
            return CurrentSleep.CanSleep(out why) ? "Sleep" : "Sleep (" + why + ")";
        }
        if (CurrentAnimal != null) return "Hit " + CurrentAnimal.displayName;
        if (CurrentCampfire != null && CurrentCampfire.HasMeat()) return "Cook meat";
        if (Current != null) return "Harvest " + Current.displayName;
        if (CurrentWater != null) return "Drink " + CurrentWater.displayName;
        if (CurrentStructure != null) return "Pick up " + CurrentStructure.displayName;
        return "";
    }
}
