using System.Collections.Generic;
using UnityEngine;

// Spawns wolves after nightfall and removes them at dawn.
public class WolfNight : MonoBehaviour
{
    public static WolfNight Instance;

    [Header("Spawning")]
    public int maxWolves = 3;
    public float spawnRadiusMin = 26f;
    public float spawnRadiusMax = 42f;
    public float firstSpawnDelay = 15f;    // seconds after nightfall
    public float respawnInterval = 50f;

    public int AliveCount { get; private set; }

    readonly List<Wolf> wolves = new List<Wolf>();
    float nextSpawnAt;
    bool wasNight;

    Material bodyMat;

    void Awake()
    {
        Instance = this;
        var shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null) shader = Shader.Find("Standard");
        bodyMat = new Material(shader);
        bodyMat.color = new Color(0.26f, 0.25f, 0.26f, 1f);
    }

    void Update()
    {
        bool night = DayNightCycle.Instance != null && DayNightCycle.Instance.IsNight;

        if (night != wasNight)
        {
            wasNight = night;
            if (night)
            {
                nextSpawnAt = Time.time + firstSpawnDelay;
                Debug.Log("[WolfNight] nightfall - wolves will come");
            }
            else
            {
                DespawnAll();
                Debug.Log("[WolfNight] dawn - wolves retreat");
            }
        }

        if (!night)
        {
            AliveCount = 0;
            return;
        }

        for (int i = wolves.Count - 1; i >= 0; i--)
            if (wolves[i] == null || wolves[i].Dead) wolves.RemoveAt(i);

        AliveCount = wolves.Count;

        if (Time.time >= nextSpawnAt && wolves.Count < maxWolves)
        {
            nextSpawnAt = Time.time + respawnInterval;
            SpawnOne();
        }
    }

    void SpawnOne()
    {
        var player = GameObject.FindWithTag("Player");
        if (player == null) return;

        float angle = Random.value * Mathf.PI * 2f;
        float radius = Random.Range(spawnRadiusMin, spawnRadiusMax);
        Vector3 pos = player.transform.position + new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius);

        var terrain = Terrain.activeTerrain;
        if (terrain != null) pos.y = terrain.SampleHeight(pos) + terrain.transform.position.y;

        var root = new GameObject("Wolf");
        root.transform.position = pos;

        var body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        body.name = "Body";
        Object.Destroy(body.GetComponent<Collider>());
        body.transform.SetParent(root.transform, false);
        body.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        body.transform.localScale = new Vector3(0.55f, 1.05f, 0.55f);
        body.transform.localPosition = new Vector3(0f, 0.6f, 0f);
        body.GetComponent<Renderer>().sharedMaterial = bodyMat;

        var head = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        head.name = "Head";
        Object.Destroy(head.GetComponent<Collider>());
        head.transform.SetParent(root.transform, false);
        head.transform.localScale = Vector3.one * 0.5f;
        head.transform.localPosition = new Vector3(0f, 0.78f, 0.6f);
        head.GetComponent<Renderer>().sharedMaterial = bodyMat;

        var cc = root.AddComponent<CharacterController>();
        cc.center = new Vector3(0f, 0.6f, 0f);
        cc.height = 1.2f;
        cc.radius = 0.4f;

        var wolf = root.AddComponent<Wolf>();
        wolves.Add(wolf);

        if (AudioManager.Instance != null) AudioManager.Instance.Play(AudioManager.Sfx.WolfHowl, 1f, 0.03f);

        Debug.Log("[WolfNight] spawned a wolf at " + radius.ToString("F0") + "m");
    }

    void DespawnAll()
    {
        for (int i = 0; i < wolves.Count; i++)
            if (wolves[i] != null) Destroy(wolves[i].gameObject);
        wolves.Clear();
        AliveCount = 0;
    }
}
