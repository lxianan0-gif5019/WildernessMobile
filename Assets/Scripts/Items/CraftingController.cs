using UnityEngine;

// Timed crafting. A recipe needs its ingredients, may need a nearby station,
// and takes real seconds to finish. Only one craft runs at a time.
public class CraftingController : MonoBehaviour
{
    public static CraftingController Instance;

    public CraftingRecipe active;
    float startedAt;

    public bool Busy { get { return active != null; } }
    public float Progress { get { return active == null ? 0f : Mathf.Clamp01((Time.time - startedAt) / Mathf.Max(0.01f, active.seconds)); } }
    public float SecondsLeft { get { return active == null ? 0f : Mathf.Max(0f, active.seconds - (Time.time - startedAt)); } }

    void Awake()
    {
        Instance = this;
        var p = GameObject.FindWithTag("Player");
        if (p != null) player = p.transform;
    }

    public Transform player;

    Vector3 PlayerPosition()
    {
        if (player == null)
        {
            var p = GameObject.FindWithTag("Player");
            if (p != null) player = p.transform;
        }
        return player != null ? player.position : transform.position;
    }

    public bool HasIngredients(CraftingRecipe r)
    {
        if (Inventory.Instance == null) return false;
        for (int i = 0; i < r.inputIds.Length; i++)
            if (Inventory.Instance.Count(r.inputIds[i]) < r.inputCounts[i]) return false;
        return true;
    }

    public bool HasStation(CraftingRecipe r)
    {
        if (string.IsNullOrEmpty(r.requiresStation)) return true;
        return CraftingStation.Near(r.requiresStation, PlayerPosition());
    }

    /// <summary>Why the recipe cannot be started right now, or "" when it can.</summary>
    public string BlockReason(CraftingRecipe r)
    {
        if (Busy) return "Crafting...";
        if (!HasIngredients(r)) return "Missing " + MissingList(r);
        if (!HasStation(r)) return "Need a " + RecipeCatalog.StationLabel(r.requiresStation);
        return "";
    }

    string MissingList(CraftingRecipe r)
    {
        var sb = new System.Text.StringBuilder();
        for (int i = 0; i < r.inputIds.Length; i++)
        {
            int have = Inventory.Instance != null ? Inventory.Instance.Count(r.inputIds[i]) : 0;
            if (have < r.inputCounts[i])
            {
                if (sb.Length > 0) sb.Append(", ");
                sb.Append(r.inputCounts[i] - have).Append(' ').Append(r.inputIds[i]);
            }
        }
        return sb.ToString();
    }

    public bool StartCraft(CraftingRecipe r)
    {
        string reason = BlockReason(r);
        if (reason != "")
        {
            Debug.Log("[Craft] blocked: " + r.displayName + " (" + reason + ")");
            return false;
        }

        active = r;
        startedAt = Time.time;
        Debug.Log("[Craft] started " + r.displayName + " (" + r.seconds + "s)");
        return true;
    }

    void Update()
    {
        if (active == null) return;
        if (Time.time - startedAt < active.seconds) return;

        // consume only on completion
        for (int i = 0; i < active.inputIds.Length; i++)
            Inventory.Instance.Spend(active.inputIds[i], active.inputCounts[i]);

        Inventory.Instance.Add(active.outputId, active.outputCount);
        Debug.Log("[Craft] finished " + active.displayName + " x" + active.outputCount);
        active = null;
    }
}
