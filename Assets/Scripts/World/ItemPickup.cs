using System.Collections.Generic;
using UnityEngine;

// A dropped item lying on the ground. Walk close and it hops into the backpack.
// Pooled + shared materials: spawning must not allocate (mobile GC + draw calls).
public class ItemPickup : MonoBehaviour
{
    public static readonly List<ItemPickup> All = new List<ItemPickup>();

    static readonly Stack<ItemPickup> pool = new Stack<ItemPickup>();
    static readonly Dictionary<string, Material> mats = new Dictionary<string, Material>();
    static Transform poolRoot;

    const float PickupRadius = 1.9f;
    const int SoftCap = 120;          // older drops start expiring beyond this

    public string itemId = "wood";
    public int amount = 1;

    [Tooltip("Seconds before an uncollected drop disappears.")]
    public float lifetime = 240f;

    float bornAt;
    Vector3 restPos;
    float bobSeed;
    Transform player;
    bool collected;
    Renderer body;

    public static ItemPickup Spawn(string id, int count, Vector3 position)
    {
        var p = pool.Count > 0 ? pool.Pop() : CreateNew();

        p.gameObject.SetActive(true);
        p.transform.position = position;
        p.transform.rotation = Quaternion.identity;
        p.itemId = id;
        p.amount = count;
        p.restPos = position;
        p.bornAt = Time.time;
        p.collected = false;
        p.bobSeed = Random.value * 6f;
        p.ApplyMaterial();

        All.Add(p);

        // keep the world from filling up
        if (All.Count > SoftCap)
        {
            for (int i = 0; i < All.Count && All.Count > SoftCap; i++)
                if (All[i] != p) { All[i].Expire(); break; }
        }
        return p;
    }

    static ItemPickup CreateNew()
    {
        if (poolRoot == null)
        {
            var rootGo = GameObject.Find("_PickupPool");
            if (rootGo == null) rootGo = new GameObject("_PickupPool");
            poolRoot = rootGo.transform;
        }

        var go = new GameObject("Pickup");
        go.transform.SetParent(poolRoot, false);

        var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cube.name = "Body";
        Destroy(cube.GetComponent<Collider>());
        cube.transform.SetParent(go.transform, false);
        cube.transform.localScale = Vector3.one * 0.26f;
        cube.transform.localRotation = Quaternion.Euler(35f, 30f, 15f);
        cube.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

        var p = go.AddComponent<ItemPickup>();
        p.body = cube.GetComponent<Renderer>();
        return p;
    }

    static Material MaterialFor(string id)
    {
        Material m;
        if (mats.TryGetValue(id, out m) && m != null) return m;

        var shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null) shader = Shader.Find("Standard");

        m = new Material(shader);
        m.color = ItemDatabase.ItemColor(id);
        mats[id] = m;
        return m;
    }

    void ApplyMaterial()
    {
        if (body == null) body = GetComponentInChildren<Renderer>();
        if (body != null) body.sharedMaterial = MaterialFor(itemId);
    }

    void Update()
    {
        if (collected) return;

        transform.position = restPos + Vector3.up * (0.18f + Mathf.Sin(Time.time * 2.4f + bobSeed) * 0.06f);
        transform.Rotate(Vector3.up, 55f * Time.deltaTime, Space.World);

        // Drops never collect themselves: the player has to walk over and press the
        // interact key. They only disappear when they age out.
        if (Time.time - bornAt > lifetime) Expire();
    }

    public void Collect()
    {
        if (collected) return;
        if (Inventory.Instance != null) Inventory.Instance.Add(itemId, amount);
        if (AudioManager.Instance != null) AudioManager.Instance.Play(AudioManager.Sfx.Pickup);

        // flourish: the item flies up into the backpack
        Vector3 target = transform.position + Vector3.up;
        var p = GameObject.FindWithTag("Player");
        if (p != null) target = p.transform.position + Vector3.up * 1.2f;
        FlyItem.Spawn(transform.position, target, ItemDatabase.ItemColor(itemId));

        Release();
    }

    public string Label()
    {
        return Loc.F("prompt.pickup", Loc.ItemName(itemId), amount);
    }

    public void Expire()
    {
        if (collected) return;
        Release();
    }

    void Release()
    {
        collected = true;
        All.Remove(this);
        gameObject.SetActive(false);
        pool.Push(this);
    }

    public static void ClearAll()
    {
        for (int i = All.Count - 1; i >= 0; i--) All[i].Release();
        All.Clear();
    }
}
