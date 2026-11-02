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

    int remaining;
    Vector3 baseScale;
    bool shaking;

    void Awake()
    {
        remaining = hitsToHarvest;
        baseScale = transform.localScale;
    }

    public bool Harvest()
    {
        remaining--;
        if (Inventory.Instance != null)
        {
            Inventory.Instance.Add(itemId, amountPerHit);
            if (!string.IsNullOrEmpty(extraItemId)) Inventory.Instance.Add(extraItemId, extraAmount);
        }

        if (!shaking) StartCoroutine(Shake());
        transform.localScale = baseScale * (0.94f + 0.06f * Mathf.Clamp01(remaining / (float)hitsToHarvest));

        if (remaining <= 0)
        {
            Destroy(gameObject);
            return true;
        }
        return false;
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
