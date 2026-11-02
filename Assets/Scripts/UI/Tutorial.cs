// 职责：四步新手目标链
// 依赖：Inventory、CraftingStation、Campfire、DayNightCycle
using UnityEngine;

// A four-step first-run objective chain: wood -> workbench -> campfire -> first night.
public class Tutorial : MonoBehaviour
{
    public static Tutorial Instance;

    public int step;              // 0 wood, 1 bench, 2 fire, 3 night, 4 done
    int woodPeak;

    void Awake() { Instance = this; }

    void Update()
    {
        if (Inventory.Instance != null)
            woodPeak = Mathf.Max(woodPeak, Inventory.Instance.Count(ItemIds.Wood));

        switch (step)
        {
            case 0:
                if (woodPeak >= 5) step = 1;
                break;
            case 1:
                if (HasStation(ItemIds.Workbench)) step = 2;
                break;
            case 2:
                if (Campfire.All.Count > 0) step = 3;
                break;
            case 3:
                if (DayNightCycle.Instance != null && DayNightCycle.Instance.dayNumber >= 2) step = 4;
                break;
        }
    }

    static bool HasStation(string kind)
    {
        var all = CraftingStation.All;
        for (int i = 0; i < all.Count; i++)
            if (all[i] != null && all[i].kind == kind) return true;
        return false;
    }

    public string Objective()
    {
        switch (step)
        {
            case 0: return Loc.F("tut.wood", Mathf.Min(5, woodPeak));
            case 1: return Loc.T("tut.bench");
            case 2: return Loc.T("tut.fire");
            case 3: return Loc.T("tut.night");
            default: return Loc.T("tut.done");
        }
    }

    public bool Done { get { return step >= 4; } }
}
