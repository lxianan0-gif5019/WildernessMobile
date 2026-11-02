// 职责：背包数据的唯一真源，只暴露 Add/Spend/Clear/Count
// 依赖：无
using System;
using System.Collections.Generic;
using UnityEngine;

// Flat stack list. Enough for a prototype; swap for a slot grid later.
public class Inventory : MonoBehaviour
{
    public static Inventory Instance;

    [Serializable]
    public class Stack
    {
        public string id;
        public int count;
    }

    public List<Stack> items = new List<Stack>();
    public event Action Changed;

    void Awake() { Instance = this; }

    public int Count(string id)
    {
        foreach (var s in items) if (s.id == id) return s.count;
        return 0;
    }

    public void Add(string id, int amount)
    {
        var s = items.Find(x => x.id == id);
        if (s == null)
        {
            s = new Stack { id = id, count = 0 };
            items.Add(s);
        }
        s.count += amount;
        Raise();
    }

    public bool Spend(string id, int amount)
    {
        var s = items.Find(x => x.id == id);
        if (s == null || s.count < amount) return false;
        s.count -= amount;
        if (s.count <= 0) items.Remove(s);
        Raise();
        return true;
    }

    public void Clear()
    {
        items.Clear();
        Raise();
    }

    void Raise() { if (Changed != null) Changed(); }
}
