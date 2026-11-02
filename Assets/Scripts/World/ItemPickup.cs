using UnityEngine;

// A dropped item lying on the ground. Walk close and it hops into the backpack.
public class ItemPickup : MonoBehaviour
{
    public string itemId = "wood";
    public int amount = 1;

    const float PickupRadius = 1.9f;

    float bornAt;
    Vector3 restPos;
    float bobSeed;
    Transform player;
    bool collected;

    public static ItemPickup Spawn(string id, int count, Vector3 position)
    {
        var go = new GameObject("Pickup_" + id);
        go.transform.position = position;
        var p = go.AddComponent<ItemPickup>();
        p.itemId = id;
        p.amount = count;
        return p;
    }

    void Awake()
    {
        bornAt = Time.time;
        restPos = transform.position;
        bobSeed = Random.value * 6f;

        var shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null) shader = Shader.Find("Standard");
        var mat = new Material(shader);
        var c = ItemDatabase.ItemColor(itemId);
        mat.color = c;

        var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cube.name = "Body";
        Destroy(cube.GetComponent<Collider>());
        cube.transform.SetParent(transform, false);
        cube.transform.localScale = Vector3.one * 0.26f;
        cube.transform.localRotation = Quaternion.Euler(35f, 30f, 15f);
        cube.GetComponent<Renderer>().sharedMaterial = mat;
    }

    void Update()
    {
        if (collected) return;

        // gentle float so pickupable items read as interactive
        transform.position = restPos + Vector3.up * (0.18f + Mathf.Sin(Time.time * 2.4f + bobSeed) * 0.06f);
        transform.Rotate(Vector3.up, 55f * Time.deltaTime, Space.World);

        if (Time.time - bornAt < 0.35f) return;
        if (player == null)
        {
            var p = GameObject.FindWithTag("Player");
            if (p != null) player = p.transform;
            else return;
        }

        if (Vector3.Distance(player.position, restPos) <= PickupRadius) Collect();
    }

    void Collect()
    {
        collected = true;
        if (Inventory.Instance != null) Inventory.Instance.Add(itemId, amount);
        Destroy(gameObject);
    }
}
