using UnityEngine;
using UnityEditor;

// Temporary verification helpers. These exist so the agent can exercise the
// gameplay code without a keyboard. Remove them once hand testing is done.
public static class DebugActions
{
    [MenuItem("Wilderness/17. Debug Save Now")]
    public static void SaveNow()
    {
        if (SaveSystem.Instance == null) { Debug.Log("[Debug] no SaveSystem"); return; }
        SaveSystem.Instance.Save();
        Debug.Log("[Debug] save file = " + SaveSystem.Instance.FilePath + " exists=" + System.IO.File.Exists(SaveSystem.Instance.FilePath));
    }

    [MenuItem("Wilderness/18. Debug Load Now")]
    public static void LoadNow()
    {
        if (SaveSystem.Instance == null) { Debug.Log("[Debug] no SaveSystem"); return; }
        SaveSystem.Instance.Load();
    }

    [MenuItem("Wilderness/14. Debug Toggle Inventory")]
    public static void ToggleInventory()
    {
        if (GameUI.Instance == null) { Debug.Log("[Debug] no GameUI"); return; }
        GameUI.Instance.DebugToggleInventory();
        Debug.Log("[Debug] inventory panel open=" + GameUI.PanelOpen);
    }

    [MenuItem("Wilderness/13. Debug Take Wall From Bag")]
    public static void TakeWall()
    {
        if (GameUI.Instance == null) { Debug.Log("[Debug] no GameUI"); return; }
        var inv = Inventory.Instance;
        if (inv == null) { Debug.Log("[Debug] no Inventory"); return; }
        if (inv.Count("wall") == 0) inv.Add("wall", 1);

        GameUI.Instance.OnItemClicked("wall");
        bool active = BuildSystem.Instance != null && BuildSystem.Instance.Active;
        Debug.Log("[Debug] after OnItemClicked(wall): buildMode=" + active + " walls=" + inv.Count("wall") + " panelOpen=" + GameUI.PanelOpen);
    }

    [MenuItem("Wilderness/12. Debug Toggle Craft Panel")]
    public static void ToggleCraftPanel()
    {
        if (GameUI.Instance == null) { Debug.Log("[Debug] no GameUI instance"); return; }
        GameUI.Instance.DebugToggleCraft();
        Debug.Log("[Debug] PanelOpen=" + GameUI.PanelOpen + " cursor=" + Cursor.lockState + " visible=" + Cursor.visible);
    }

    [MenuItem("Wilderness/11. Debug Report Interact Target")]
    public static void ReportTarget()
    {
        var pi = PlayerInteractor.Instance;
        if (pi == null) { Debug.Log("[Debug] no PlayerInteractor instance"); return; }

        var nodes = Object.FindObjectsOfType<ResourceNode>();
        int within = 0;
        float nearest = float.MaxValue;
        foreach (var n in nodes)
        {
            float dd = Vector3.Distance(n.transform.position, pi.transform.position);
            if (dd <= pi.reach) within++;
            if (dd < nearest) nearest = dd;
        }

        Debug.Log("[Debug] PI tag=" + pi.gameObject.tag + " enabled=" + pi.enabled
                  + " reach=" + pi.reach + " cam=" + (pi.cam != null)
                  + " pos=" + pi.transform.position.ToString("F1")
                  + " nodes=" + nodes.Length + " withinReach=" + within
                  + " nearest=" + nearest.ToString("F2"));

        var cur = pi.Current;
        if (cur == null) { Debug.Log("[Debug] interact target: NONE"); return; }
        float d = Vector3.Distance(cur.transform.position, pi.transform.position);
        Debug.Log("[Debug] interact target=" + cur.displayName + " dist=" + d.ToString("F2") + "m");
    }

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
        if (GameUI.Instance != null)
        {
            for (int i = 0; i < 5; i++) GameUI.Instance.Craft("Plank");
            for (int i = 0; i < 2; i++) GameUI.Instance.Craft("Wall");
        }
        else Debug.Log("[Debug] no GameUI instance - crafting skipped");
        Debug.Log("[Debug] after crafting wood=" + inv.Count("wood") + " plank=" + inv.Count("plank") + " wall=" + inv.Count("wall"));
    }
}
