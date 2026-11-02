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
    public string requiresStation = "";   // "" | "workbench" | "furnace"

    public bool Matches(string output) { return outputId == output; }
}

public static class RecipeCatalog
{
    public static readonly List<CraftingRecipe> All = new List<CraftingRecipe>();

    static RecipeCatalog()
    {
        Add("stick", "Stick", 2, new[] { "wood" }, new[] { 1 }, 1.5f, "");
        Add("plank", "Plank", 2, new[] { "wood" }, new[] { 3 }, 2f, "");
        Add("wall", "Wall", 1, new[] { "plank" }, new[] { 2 }, 2f, "");
        Add("campfire", "Campfire", 1, new[] { "stone", "wood" }, new[] { 2, 3 }, 3f, "");
        Add("workbench", "Workbench", 1, new[] { "wood", "stone" }, new[] { 6, 4 }, 5f, "");
        Add("furnace", "Furnace", 1, new[] { "stone", "wood" }, new[] { 8, 4 }, 8f, "workbench");

        Add("stone_axe", "Stone Axe", 1, new[] { "stick", "stone" }, new[] { 2, 3 }, 4f, "workbench");
        Add("stone_pick", "Stone Pick", 1, new[] { "stick", "stone" }, new[] { 2, 3 }, 4f, "workbench");

        Add("iron_ingot", "Iron Ingot", 1, new[] { "iron_ore", "wood" }, new[] { 2, 1 }, 8f, "furnace");

        Add("iron_axe", "Iron Axe", 1, new[] { "stick", "iron_ingot" }, new[] { 2, 2 }, 6f, "workbench");
        Add("iron_pick", "Iron Pick", 1, new[] { "stick", "iron_ingot" }, new[] { 2, 2 }, 6f, "workbench");

        Add("hay_bed", "Hay Nest", 1, new[] { "fiber" }, new[] { 8 }, 5f, "");
        Add("bed", "Wooden Bed", 1, new[] { "wood", "stick" }, new[] { 6, 4 }, 7f, "workbench");
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
        if (station == "workbench") return "Workbench";
        if (station == "furnace") return "Furnace";
        return station;
    }
}
