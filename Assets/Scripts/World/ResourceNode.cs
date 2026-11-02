using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Something the player can harvest with the interact key.
public class ResourceNode : MonoBehaviour
{
    public string itemId = "wood";
    public string displayName = "Tree";
    public int hitsToHarvest = 3;
    public int amountPerHit = 1;

    [Header("Persistence")]
    [Tooltip("Stable id so a harvested node stays harvested after loading a save.")]
    public string persistentId = "";

    /// <summary>Ids of nodes already harvested this session (written to the save file).</summary>
    public static readonly HashSet<string> Harvested = new HashSet<string>();

    [Header("Optional second drop")]
    public string extraItemId = "";
    public int extraAmount = 1;

    [Header("Tool requirement")]
    [Tooltip("Item that must be in the backpack to harvest this, e.g. 'stone_pick'.")]
    public string requiredTool = "";

    int remaining;
    Vector3 baseScale;
    bool shaking;

    void Awake()
    {
        remaining = hitsToHarvest;
        baseScale = transform.localScale;

        if (string.IsNullOrEmpty(persistentId))
            persistentId = Mathf.RoundToInt(transform.position.x * 10f)
                         + "_" + Mathf.RoundToInt(transform.position.z * 10f)
                         + "_" + itemId;
    }

    void Start()
    {
        // A save file may already say this node is gone.
        if (Harvested.Contains(persistentId)) Destroy(gameObject);
    }

    void OnDestroy()
    {
        if (remaining <= 0) Harvested.Add(persistentId);
    }

    /// <summary>Called after loading: remove nodes the save says are gone, and remember them.</summary>
    public static void ApplyHarvested(IEnumerable<string> ids)
    {
        Harvested.Clear();
        if (ids != null) foreach (var id in ids) Harvested.Add(id);

        var nodes = FindObjectsOfType<ResourceNode>();
        for (int i = 0; i < nodes.Length; i++)
            if (nodes[i] != null && Harvested.Contains(nodes[i].persistentId))
                Destroy(nodes[i].gameObject);
    }

    public bool Harvest(float power = 1f)
    {
        remaining -= Mathf.Max(1, Mathf.RoundToInt(power));

        DropLoot();

        if (!shaking) StartCoroutine(Shake());
        transform.localScale = baseScale * (0.94f + 0.06f * Mathf.Clamp01(remaining / (float)hitsToHarvest));

        if (remaining <= 0)
        {
            StartCoroutine(FallAndDie());
            return true;
        }
        return false;
    }

    // Loot falls on the ground; the player has to walk over and collect it.
    void DropLoot()
    {
        Vector3 centre = transform.position + Vector3.up * 0.5f;
        var jitter = new Vector3(Random.Range(-0.7f, 0.7f), 0.2f, Random.Range(-0.7f, 0.7f));
        if (amountPerHit > 0) ItemPickup.Spawn(itemId, amountPerHit, centre + jitter);

        if (!string.IsNullOrEmpty(extraItemId) && extraAmount > 0)
            ItemPickup.Spawn(extraItemId, extraAmount, centre - jitter);
    }

    // Trees topple over, then disappear.
    IEnumerator FallAndDie()
    {
        Quaternion start = transform.rotation;
        Quaternion end = start * Quaternion.Euler(0f, 0f, 82f);
        float t = 0f;
        while (t < 1.1f)
        {
            t += Time.deltaTime;
            transform.rotation = Quaternion.Slerp(start, end, t / 1.1f);
            yield return null;
        }
        Destroy(gameObject);
    }

    IEnumerator Shake()
    {
        shaking = true;
        Vector3 origin = transform.position;
        float t = 0f;
        while (t < 0.18f)
        {
            t += Time.deltaTime;
            transform.position = origin + Random.insideUnitSphere * 0.05f;
            yield return null;
        }
        transform.position = origin;
        shaking = false;
    }
}
