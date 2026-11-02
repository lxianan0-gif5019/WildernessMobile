using System.Collections;
using UnityEngine;

// Something the player can harvest with the interact key.
public class ResourceNode : MonoBehaviour
{
    public string itemId = "wood";
    public string displayName = "Tree";
    public int hitsToHarvest = 3;
    public int amountPerHit = 1;

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
