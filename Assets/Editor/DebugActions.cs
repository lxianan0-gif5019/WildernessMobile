using UnityEngine;
using UnityEditor;

// Temporary verification helpers. These exist so the agent can exercise the
// gameplay code without a keyboard. Remove them once hand testing is done.
public static class DebugActions
{
    [MenuItem("Wilderness/27. Debug Craft Sticks")]
    public static void CraftSticks()
    {
        var r = RecipeCatalog.Find("stick");
        if (r == null || CraftingController.Instance == null) { Debug.Log("[Debug] no recipe/controller"); return; }
        bool ok = CraftingController.Instance.StartCraft(r);
        Debug.Log("[Debug] start stick = " + ok);
    }

    [MenuItem("Wilderness/24. Debug Tool Prep")]
    public static void ToolPrep()
    {
        var player = GameObject.FindWithTag("Player");
        if (player == null || Inventory.Instance == null) { Debug.Log("[Debug] missing player/inventory"); return; }

        Inventory.Instance.Add("wood", 20);
        Inventory.Instance.Add("stone", 20);

        var bench = BuildSystem.Instance.CreateStation("workbench", player.transform.position + player.transform.forward * 3f);
        var furnace = BuildSystem.Instance.CreateStation("furnace", player.transform.position + player.transform.forward * 5f);
        Debug.Log("[Debug] prep done bench=" + (bench != null) + " furnace=" + (furnace != null)
                  + " stationNear=" + CraftingStation.Near("workbench", player.transform.position));
    }

    [MenuItem("Wilderness/25. Debug Craft Stone Axe")]
    public static void CraftStoneAxe()
    {
        var r = RecipeCatalog.Find("stone_axe");
        if (r == null || CraftingController.Instance == null) { Debug.Log("[Debug] no recipe/controller"); return; }
        bool ok = CraftingController.Instance.StartCraft(r);
        Debug.Log("[Debug] start stone_axe = " + ok + " reason=" + CraftingController.Instance.BlockReason(r));
    }

    [MenuItem("Wilderness/28. Debug Enable Run In Background")]
    public static void EnableRunInBackground()
    {
        UnityEditor.PlayerSettings.runInBackground = true;
        Debug.Log("[Debug] runInBackground = " + UnityEditor.PlayerSettings.runInBackground);
    }

    [MenuItem("Wilderness/26. Debug Inventory Dump")]
    public static void DumpInventory()
    {
        if (Inventory.Instance == null) { Debug.Log("[Debug] no inventory"); return; }
        var sb = new System.Text.StringBuilder("[Debug] inv:");
        foreach (var s in Inventory.Instance.items) sb.Append(' ').Append(s.id).Append('=').Append(s.count);
        sb.Append(" | paused=").Append(UnityEditor.EditorApplication.isPaused);
        sb.Append(" time=").Append(UnityEngine.Time.time.ToString("F2"));
        if (CraftingController.Instance != null)
            sb.Append(" busy=").Append(CraftingController.Instance.Busy)
              .Append(" prog=").Append(CraftingController.Instance.Progress.ToString("F2"));
        Debug.Log(sb.ToString());
    }

    [MenuItem("Wilderness/23. Debug Campfire Test")]
    public static void CampfireTest()
    {
        var player = GameObject.FindWithTag("Player");
        if (player == null) { Debug.Log("[Debug] no player"); return; }
        if (BuildSystem.Instance == null) { Debug.Log("[Debug] no BuildSystem"); return; }

        Vector3 pos = player.transform.position + player.transform.forward * 3f;
        var go = BuildSystem.Instance.CreateCampfire(pos);
        var fire = go.GetComponent<Campfire>();

        if (Inventory.Instance != null) Inventory.Instance.Add("meat", 2);
        bool cooked = fire != null && fire.Cook();

        Debug.Log("[Debug] campfire placed, cooked=" + cooked
                  + " meat=" + (Inventory.Instance != null ? Inventory.Instance.Count("meat") : -1)
                  + " cooked_meat=" + (Inventory.Instance != null ? Inventory.Instance.Count("cooked_meat") : -1));
    }

    [MenuItem("Wilderness/22. Debug Hunt Nearest Animal")]
    public static void HuntNearest()
    {
        var player = GameObject.FindWithTag("Player");
        if (player == null) { Debug.Log("[Debug] no player"); return; }

        Animal best = null;
        float bestDist = float.MaxValue;
        foreach (var a in Object.FindObjectsOfType<Animal>())
        {
            float d = Vector3.Distance(a.transform.position, player.transform.position);
            if (d < bestDist) { bestDist = d; best = a; }
        }
        if (best == null) { Debug.Log("[Debug] no animals in scene"); return; }

        Debug.Log("[Debug] hunting " + best.displayName + " at " + bestDist.ToString("F1") + "m");
        best.TakeDamage(22f);
        best.TakeDamage(22f);
        best.TakeDamage(22f);
        best.TakeDamage(22f);

        if (Inventory.Instance != null)
            Debug.Log("[Debug] meat=" + Inventory.Instance.Count("meat"));
    }

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
