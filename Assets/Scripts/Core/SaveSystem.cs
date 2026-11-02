using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

[Serializable]
public class SaveData
{
    public float px, py, pz, ry;
    public float timeOfDay;
    public float health, hunger, thirst;
    public List<ItemEntry> items = new List<ItemEntry>();
    public List<WallEntry> walls = new List<WallEntry>();
}

[Serializable]
public class ItemEntry { public string id; public int count; }

[Serializable]
public class WallEntry { public float x, y, z, ry; }

// Saves and restores the world: player position, backpack contents, placed walls
// and the time of day. JSON on disk, no dependencies.
public class SaveSystem : MonoBehaviour
{
    public static SaveSystem Instance;

    public string fileName = "wilderness_save.json";
    public bool loadOnStart = true;

    public string FilePath { get { return Path.Combine(Application.persistentDataPath, fileName); } }

    void Awake() { Instance = this; }

    void Start()
    {
        if (loadOnStart) Load();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.F5))
        {
            Save();
            Debug.Log("[Save] saved -> " + FilePath);
        }
    }

    void OnApplicationQuit() { Save(); }

    void OnApplicationPause(bool paused) { if (paused) Save(); }

    public void Save()
    {
        var data = new SaveData();

        var player = GameObject.FindWithTag("Player");
        if (player != null)
        {
            var p = player.transform.position;
            data.px = p.x; data.py = p.y; data.pz = p.z;
            data.ry = player.transform.eulerAngles.y;
        }

        if (DayNightCycle.Instance != null) data.timeOfDay = DayNightCycle.Instance.timeOfDay;

        if (PlayerStats.Instance != null)
        {
            data.health = PlayerStats.Instance.health;
            data.hunger = PlayerStats.Instance.hunger;
            data.thirst = PlayerStats.Instance.thirst;
        }

        if (Inventory.Instance != null)
            foreach (var s in Inventory.Instance.items)
                data.items.Add(new ItemEntry { id = s.id, count = s.count });

        foreach (var w in FindObjectsOfType<PlacedStructure>())
        {
            var t = w.transform;
            data.walls.Add(new WallEntry { x = t.position.x, y = t.position.y, z = t.position.z, ry = t.eulerAngles.y });
        }

        try
        {
            File.WriteAllText(FilePath, JsonUtility.ToJson(data, true));
            Debug.Log("[Save] wrote " + data.items.Count + " item stacks, " + data.walls.Count + " walls");
        }
        catch (Exception e)
        {
            Debug.Log("[Save] write failed: " + e.Message);
        }
    }

    public void Load()
    {
        if (!File.Exists(FilePath)) { Debug.Log("[Save] no save file yet"); return; }

        SaveData data;
        try
        {
            data = JsonUtility.FromJson<SaveData>(File.ReadAllText(FilePath));
        }
        catch (Exception e)
        {
            Debug.Log("[Save] load failed: " + e.Message);
            return;
        }
        if (data == null) return;

        var player = GameObject.FindWithTag("Player");
        if (player != null)
        {
            var cc = player.GetComponent<CharacterController>();
            if (cc != null) cc.enabled = false;                 // bypass collision while teleporting
            player.transform.position = new Vector3(data.px, data.py, data.pz);
            player.transform.rotation = Quaternion.Euler(0f, data.ry, 0f);
            if (cc != null) cc.enabled = true;
        }

        if (DayNightCycle.Instance != null && data.timeOfDay > 0f)
        {
            DayNightCycle.Instance.timeOfDay = data.timeOfDay;
            DayNightCycle.Instance.Apply(data.timeOfDay);
        }

        if (PlayerStats.Instance != null && data.health > 0f)
        {
            PlayerStats.Instance.health = data.health;
            PlayerStats.Instance.hunger = data.hunger > 0f ? data.hunger : PlayerStats.Instance.maxHunger;
            PlayerStats.Instance.thirst = data.thirst > 0f ? data.thirst : PlayerStats.Instance.maxThirst;
        }

        if (Inventory.Instance != null)
        {
            Inventory.Instance.Clear();
            if (data.items != null)
                foreach (var it in data.items)
                    if (it != null && it.count > 0) Inventory.Instance.Add(it.id, it.count);
        }

        // rebuild placed walls
        foreach (var old in FindObjectsOfType<PlacedStructure>()) Destroy(old.gameObject);

        if (data.walls != null && BuildSystem.Instance != null)
            foreach (var w in data.walls)
                BuildSystem.Instance.CreateWall(new Vector3(w.x, w.y, w.z), w.ry);

        Debug.Log("[Save] loaded " + (data.items != null ? data.items.Count : 0) + " stacks, "
                  + (data.walls != null ? data.walls.Count : 0) + " walls");
    }
}
