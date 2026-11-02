using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

[Serializable]
public class SaveData
{
    public int version = SaveSystem.CurrentVersion;
    public float px, py, pz, ry;
    public float timeOfDay;
    public int dayNumber = 1;
    public float health, hunger, thirst;
    public List<ItemEntry> items = new List<ItemEntry>();
    public List<WallEntry> walls = new List<WallEntry>();
    public List<PickupEntry> pickups = new List<PickupEntry>();
    public List<string> harvested = new List<string>();
}

[Serializable]
public class ItemEntry { public string id; public int count; }

[Serializable]
public class WallEntry { public float x, y, z, ry; public string type = "wall"; }

[Serializable]
public class PickupEntry { public string id; public int count; public float x, y, z; }

// Saves and restores the world: player position, backpack contents, placed walls
// and the time of day. JSON on disk, no dependencies.
public class SaveSystem : MonoBehaviour
{
    public static SaveSystem Instance;

    /// <summary>Bump this whenever SaveData changes, and add a matching branch in Migrate.</summary>
    public const int CurrentVersion = 3;

    public string fileName = "wilderness_save.json";
    public bool loadOnStart = true;

    [Tooltip("0 = the autosave/continue slot, 1-3 = manual slots.")]
    public int slot = 0;

    public string FilePath { get { return PathForSlot(slot); } }

    public static string PathForSlot(int s)
    {
        string name = s <= 0 ? "wilderness_save.json" : "wilderness_save_slot" + s + ".json";
        return Path.Combine(Application.persistentDataPath, name);
    }

    public static bool SlotExists(int s) { return File.Exists(PathForSlot(s)); }

    public void SaveToSlot(int s)
    {
        slot = s;
        Save();
    }

    public bool LoadFromSlot(int s)
    {
        slot = s;
        return Load();
    }

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
        data.version = CurrentVersion;

        var player = GameObject.FindWithTag("Player");
        if (player != null)
        {
            var p = player.transform.position;
            data.px = p.x; data.py = p.y; data.pz = p.z;
            data.ry = player.transform.eulerAngles.y;
        }

        if (DayNightCycle.Instance != null)
        {
            data.timeOfDay = DayNightCycle.Instance.timeOfDay;
            data.dayNumber = DayNightCycle.Instance.dayNumber;
        }

        for (int i = 0; i < ItemPickup.All.Count; i++)
        {
            var p = ItemPickup.All[i];
            if (p == null) continue;
            var t = p.transform.position;
            data.pickups.Add(new PickupEntry { id = p.itemId, count = p.amount, x = t.x, y = t.y, z = t.z });
        }

        foreach (var id in ResourceNode.Harvested) data.harvested.Add(id);

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
            data.walls.Add(new WallEntry { x = t.position.x, y = t.position.y, z = t.position.z, ry = t.eulerAngles.y, type = w.itemId });
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

    public bool Load()
    {
        if (!File.Exists(FilePath)) { Debug.Log("[Save] no save file yet"); return false; }

        SaveData data;
        try
        {
            data = JsonUtility.FromJson<SaveData>(File.ReadAllText(FilePath));
        }
        catch (Exception e)
        {
            Debug.Log("[Save] load failed: " + e.Message);
            return false;
        }
        if (data == null) return false;

        if (data.version < CurrentVersion)
        {
            Debug.Log("[Save] migrating save v" + data.version + " -> v" + CurrentVersion);
            BackupOldSave(data.version);
            Migrate(data);
        }
        else if (data.version > CurrentVersion)
        {
            Debug.LogWarning("[Save] save v" + data.version + " is newer than the game (v" + CurrentVersion + "); loading anyway");
        }

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
            DayNightCycle.Instance.dayNumber = Mathf.Max(1, data.dayNumber);
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

        // harvest state: nodes the save says are gone stay gone
        ResourceNode.ApplyHarvested(data.harvested);

        // restore drops lying on the ground
        ItemPickup.ClearAll();
        if (data.pickups != null)
            foreach (var p in data.pickups)
                if (p != null && !string.IsNullOrEmpty(p.id))
                    ItemPickup.Spawn(p.id, p.count, new Vector3(p.x, p.y, p.z));

        foreach (var old in FindObjectsOfType<PlacedStructure>()) Destroy(old.gameObject);

        if (data.walls != null && BuildSystem.Instance != null)
            foreach (var w in data.walls)
            {
                if (w.type == "campfire") BuildSystem.Instance.CreateCampfire(new Vector3(w.x, w.y, w.z));
                else if (w.type == "workbench" || w.type == "furnace") BuildSystem.Instance.CreateStation(w.type, new Vector3(w.x, w.y, w.z));
                else if (w.type == "bed" || w.type == "hay_bed") BuildSystem.Instance.CreateBed(w.type, new Vector3(w.x, w.y, w.z));
                else BuildSystem.Instance.CreateWall(new Vector3(w.x, w.y, w.z), w.ry);
            }

        Debug.Log("[Save] loaded " + (data.items != null ? data.items.Count : 0) + " stacks, "
                  + (data.walls != null ? data.walls.Count : 0) + " walls, v" + data.version + " (slot " + slot + ")");
        return true;
    }

    void BackupOldSave(int oldVersion)
    {
        try
        {
            var path = FilePath;
            if (!File.Exists(path)) return;
            var backup = path.Replace(".json", "_v" + oldVersion + ".bak.json");
            File.Copy(path, backup, true);
            Debug.Log("[Save] old save backed up -> " + backup);
        }
        catch (Exception e)
        {
            Debug.LogWarning("[Save] backup failed: " + e.Message);
        }
    }

    /// <summary>Fills in anything an older save did not have. Never drops fields.</summary>
    void Migrate(SaveData d)
    {
        // v1 -> v2: survival stats were added
        if (d.version < 2)
        {
            if (d.health <= 0f) d.health = 100f;
            if (d.hunger <= 0f) d.hunger = 100f;
            if (d.thirst <= 0f) d.thirst = 100f;
        }

        // v2 -> v3: day counter, ground drops and harvested resource ids were added
        if (d.version < 3)
        {
            if (d.dayNumber < 1) d.dayNumber = 1;
            if (d.pickups == null) d.pickups = new List<PickupEntry>();
            if (d.harvested == null) d.harvested = new List<string>();
        }

        d.version = CurrentVersion;
    }
}
