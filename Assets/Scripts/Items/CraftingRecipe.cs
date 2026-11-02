// 职责：配方表（产物/材料/耗时/需要的设施）
// 依赖：无
using System.Collections.Generic;
using UnityEngine;

// Recipe definition + the catalogue of everything craftable.
[System.Serializable]
public class CraftingRecipe
{
    public string outputId;
    public string displayName;
    public int outputCount = 1;
    public string[] inputIds;
    public int[] inputCounts;
    public float seconds = 2f;
    public string requiresStation = "";   // "" | ItemIds.Workbench | ItemIds.Furnace

    public bool Matches(string output) { return outputId == output; }
}

public static class RecipeCatalog
{
    public static readonly List<CraftingRecipe> All = new List<CraftingRecipe>();

    static RecipeCatalog()
    {
        Add(ItemIds.Stick, "Stick", 2, new[] { ItemIds.Wood }, new[] { 1 }, 1.5f, "");
        Add(ItemIds.Plank, "Plank", 2, new[] { ItemIds.Wood }, new[] { 3 }, 2f, "");
        Add(ItemIds.Wall, "Wall", 1, new[] { ItemIds.Plank }, new[] { 2 }, 2f, "");
        Add(ItemIds.Campfire, "Campfire", 1, new[] { ItemIds.Stone, ItemIds.Wood }, new[] { 2, 3 }, 3f, "");
        Add(ItemIds.Workbench, "Workbench", 1, new[] { ItemIds.Wood, ItemIds.Stone }, new[] { 6, 4 }, 5f, "");
        Add(ItemIds.Furnace, "Furnace", 1, new[] { ItemIds.Stone, ItemIds.Wood }, new[] { 8, 4 }, 8f, ItemIds.Workbench);

        Add(ItemIds.StoneAxe, "Stone Axe", 1, new[] { ItemIds.Stick, ItemIds.Stone }, new[] { 2, 3 }, 4f, ItemIds.Workbench);
        Add(ItemIds.StonePick, "Stone Pick", 1, new[] { ItemIds.Stick, ItemIds.Stone }, new[] { 2, 3 }, 4f, ItemIds.Workbench);

        Add(ItemIds.IronIngot, "Iron Ingot", 1, new[] { ItemIds.IronOre, ItemIds.Wood }, new[] { 2, 1 }, 8f, ItemIds.Furnace);

        Add(ItemIds.IronAxe, "Iron Axe", 1, new[] { ItemIds.Stick, ItemIds.IronIngot }, new[] { 2, 2 }, 6f, ItemIds.Workbench);
        Add(ItemIds.IronPick, "Iron Pick", 1, new[] { ItemIds.Stick, ItemIds.IronIngot }, new[] { 2, 2 }, 6f, ItemIds.Workbench);

        Add(ItemIds.HayBed, "Hay Nest", 1, new[] { ItemIds.Fiber }, new[] { 8 }, 5f, "");
        Add(ItemIds.Bed, "Wooden Bed", 1, new[] { ItemIds.Wood, ItemIds.Stick }, new[] { 6, 4 }, 7f, ItemIds.Workbench);
    }

    static void Add(string id, string name, int count, string[] ins, int[] amounts, float seconds, string station)
    {
        All.Add(new CraftingRecipe
        {
            outputId = id,
            displayName = name,
            outputCount = count,
            inputIds = ins,
            inputCounts = amounts,
            seconds = seconds,
            requiresStation = station
        });
    }

    public static CraftingRecipe Find(string outputId)
    {
        for (int i = 0; i < All.Count; i++)
            if (All[i].outputId == outputId) return All[i];
        return null;
    }

    public static string StationLabel(string station)
    {
        if (string.IsNullOrEmpty(station)) return "";
        if (station == ItemIds.Workbench) return "Workbench";
        if (station == ItemIds.Furnace) return "Furnace";
        return station;
    }
}
