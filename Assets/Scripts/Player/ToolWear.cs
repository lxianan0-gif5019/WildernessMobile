// 职责：工具耐久记账，用坏即扣除物品
// 依赖：Inventory、AudioManager
using System.Collections.Generic;
using UnityEngine;

// Tools wear out as they are used and break when exhausted.
public class ToolWear : MonoBehaviour
{
    public static ToolWear Instance;

    public float stoneMax = 40f;
    public float ironMax = 120f;

    readonly Dictionary<string, float> wear = new Dictionary<string, float>();

    void Awake() { Instance = this; }

    public float MaxFor(string id)
    {
        return id.StartsWith("iron") ? ironMax : stoneMax;
    }

    public float WearOf(string id)
    {
        float w;
        return wear.TryGetValue(id, out w) ? w : 0f;
    }

    public float Left(string id)
    {
        return Mathf.Max(0f, MaxFor(id) - WearOf(id));
    }

    /// <summary>Counts one use. Returns false when the tool just broke.</summary>
    public bool Use(string id)
    {
        if (Inventory.Instance == null || Inventory.Instance.Count(id) <= 0) return false;

        float next = WearOf(id) + 1f;
        if (next >= MaxFor(id))
        {
            Inventory.Instance.Spend(id, 1);
            wear[id] = 0f;
            Debug.Log("[Tool] " + id + " broke");
            if (AudioManager.Instance != null) AudioManager.Instance.Play(AudioManager.Sfx.Chop, 0.9f);
            return false;
        }

        wear[id] = next;
        return true;
    }

    /// <summary>Best tool currently carried, or "" for bare hands.</summary>
    public string BestTool()
    {
        var inv = Inventory.Instance;
        if (inv == null) return "";
        if (inv.Count(ItemIds.IronAxe) > 0) return ItemIds.IronAxe;
        if (inv.Count(ItemIds.IronPick) > 0) return ItemIds.IronPick;
        if (inv.Count(ItemIds.StoneAxe) > 0) return ItemIds.StoneAxe;
        if (inv.Count(ItemIds.StonePick) > 0) return ItemIds.StonePick;
        return "";
    }
}
