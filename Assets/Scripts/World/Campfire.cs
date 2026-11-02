using System.Collections.Generic;
using UnityEngine;

// A campfire: lights the night, cooks raw meat into cooked meat, and keeps
// boars away. Also a valid build target and save target.
public class Campfire : MonoBehaviour
{
    public static readonly List<Campfire> All = new List<Campfire>();

    [Header("Light")]
    public Color lightColor = new Color(1f, 0.62f, 0.28f);
    public float lightRange = 12f;
    public float lightIntensity = 2.4f;
    public float flickerAmount = 0.18f;

    [Header("Cooking")]
    public string rawItem = "meat";
    public string cookedItem = "cooked_meat";
    public float repelRadius = 14f;

    Light fireLight;
    float seed;

    void Awake()
    {
        seed = Random.value * 10f;

        var lightGo = new GameObject("FireLight");
        lightGo.transform.SetParent(transform, false);
        lightGo.transform.localPosition = new Vector3(0f, 1.1f, 0f);
        fireLight = lightGo.AddComponent<Light>();
        fireLight.type = LightType.Point;
        fireLight.color = lightColor;
        fireLight.range = lightRange;
        fireLight.intensity = lightIntensity;
        fireLight.shadows = LightShadows.None;
    }

    void OnEnable() { All.Add(this); }
    void OnDisable() { All.Remove(this); }

    void Update()
    {
        if (fireLight == null) return;
        // cheap flicker: two out-of-phase sine waves
        float f = Mathf.Sin(Time.time * 9.1f + seed) * 0.6f + Mathf.Sin(Time.time * 23.3f + seed) * 0.4f;
        fireLight.intensity = lightIntensity * (1f + f * flickerAmount);
    }

    /// <summary>Turn one raw meat into one cooked meat. Returns false if there is no meat.</summary>
    public bool Cook()
    {
        if (Inventory.Instance == null) return false;
        if (Inventory.Instance.Count(rawItem) <= 0) return false;

        Inventory.Instance.Spend(rawItem, 1);
        Inventory.Instance.Add(cookedItem, 1);
        Debug.Log("[Campfire] cooked 1 meat");
        return true;
    }

    public bool HasMeat()
    {
        return Inventory.Instance != null && Inventory.Instance.Count(rawItem) > 0;
    }

    /// <summary>Nearest campfire to a position, or null.</summary>
    public static Campfire Nearest(Vector3 position)
    {
        Campfire best = null;
        float bestDist = float.MaxValue;
        for (int i = 0; i < All.Count; i++)
        {
            var c = All[i];
            if (c == null) continue;
            float d = Vector3.Distance(c.transform.position, position);
            if (d < bestDist) { bestDist = d; best = c; }
        }
        return best;
    }

    /// <summary>True when the given world position is close enough that animals stay away.</summary>
    public static bool Repelled(Vector3 position, out float distance)
    {
        float best = float.MaxValue;
        for (int i = 0; i < All.Count; i++)
        {
            var c = All[i];
            if (c == null) continue;
            float d = Vector3.Distance(c.transform.position, position);
            if (d < best) best = d;
        }
        distance = best;
        return best < 14f;
    }
}
