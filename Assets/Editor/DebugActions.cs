using UnityEngine;
using UnityEditor;

// Temporary verification helpers. These exist so the agent can exercise the
// gameplay code without a keyboard. Remove them once hand testing is done.
public static class DebugActions
{
    [MenuItem("Wilderness/9. Debug Harvest Nearest")]
    public static void HarvestNearest()
    {
        var player = GameObject.FindWithTag("Player");
        if (player == null) { Debug.Log("[Debug] no player"); return; }

        ResourceNode best = null;
        float bestDist = float.MaxValue;
        foreach (var n in Object.FindObjectsOfType<ResourceNode>())
        {
            float d = Vector3.Distance(n.transform.position, player.transform.position);
            if (d < bestDist) { bestDist = d; best = n; }
        }
        if (best == null) { Debug.Log("[Debug] no resource nodes in scene"); return; }

        Debug.Log("[Debug] harvesting " + best.displayName + " at " + bestDist.ToString("F1") + "m");
        best.Harvest();

        var inv = Inventory.Instance;
        if (inv != null)
            Debug.Log("[Debug] inventory wood=" + inv.Count("wood") + " stone=" + inv.Count("stone") + " fiber=" + inv.Count("fiber"));
    }

    [MenuItem("Wilderness/10. Debug Craft And Build")]
    public static void CraftAndBuild()
    {
        var inv = Inventory.Instance;
        if (inv == null) { Debug.Log("[Debug] no inventory"); return; }

        inv.Add("wood", 30);
        var craft = new GameObject("DebugCraft").AddComponent<CraftButton>();
        craft.recipeName = "Plank";
        craft.craftRepeat = 5;
        var evt = new UnityEngine.EventSystems.PointerEventData(UnityEngine.EventSystems.EventSystem.current);
        craft.OnPointerDown(evt);

        craft.recipeName = "Wall";
        craft.craftRepeat = 2;
        craft.OnPointerDown(evt);

        Object.DestroyImmediate(craft.gameObject);
        Debug.Log("[Debug] after crafting wood=" + inv.Count("wood") + " plank=" + inv.Count("plank") + " wall=" + inv.Count("wall"));
    }
}
