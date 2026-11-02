using System.Collections.Generic;
using UnityEngine;

// Static item definitions. Small enough to live in code for now.
public static class ItemDatabase
{
    public class Def
    {
        public string id;
        public string display;
        public Color color;
    }

    static readonly Dictionary<string, Def> map = new Dictionary<string, Def>();

    static ItemDatabase()
    {
        Add("wood", "Wood", new Color(0.55f, 0.36f, 0.18f));
        Add("stone", "Stone", new Color(0.58f, 0.58f, 0.60f));
        Add("fiber", "Fiber", new Color(0.45f, 0.66f, 0.30f));
        Add("berry", "Berry", new Color(0.78f, 0.20f, 0.35f));
        Add("meat", "Meat", new Color(0.72f, 0.30f, 0.28f));
        Add("cooked_meat", "CookedMeat", new Color(0.60f, 0.34f, 0.18f));
        Add("campfire", "Campfire", new Color(0.95f, 0.55f, 0.20f));
        Add("plank", "Plank", new Color(0.74f, 0.56f, 0.30f));
        Add("wall", "Wall", new Color(0.63f, 0.46f, 0.26f));
    }

    static void Add(string id, string display, Color color)
    {
        map[id] = new Def { id = id, display = display, color = color };
    }

    public static Def Get(string id)
    {
        Def d;
        return map.TryGetValue(id, out d) ? d : new Def { id = id, display = id, color = Color.white };
    }

    public static string Display(string id) { return Get(id).display; }
}
