// 职责：工作台/熔炉标记，按距离解锁配方
// 依赖：无
using System.Collections.Generic;
using UnityEngine;

// Marks a placed structure (workbench / furnace) that unlocks certain recipes.
public class CraftingStation : MonoBehaviour
{
    public static readonly List<CraftingStation> All = new List<CraftingStation>();

    public string kind = ItemIds.Workbench;
    public float radius = 5f;

    void OnEnable() { All.Add(this); }
    void OnDisable() { All.Remove(this); }

    public static bool Near(string kind, Vector3 position, float extraRange = 0f)
    {
        for (int i = 0; i < All.Count; i++)
        {
            var s = All[i];
            if (s == null || s.kind != kind) continue;
            if (Vector3.Distance(s.transform.position, position) <= s.radius + extraRange) return true;
        }
        return false;
    }
}
