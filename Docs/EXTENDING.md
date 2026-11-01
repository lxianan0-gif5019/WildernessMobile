# 常见改动地图

> 「我想加个 X，该改哪？」——先查这里。找不到再问我。

---

## 加一种新物品（比如"铁矿石"）

1. `Items/ItemDatabase.cs` → 在构造函数里加一行：
   ```csharp
   Add("iron", "Iron", new Color(0.7f, 0.72f, 0.75f));
   ```
2. 让它能被采集：在 `World/ResourceNode` 所在的对象上设 `itemId = "iron"`。
   或者用编辑器菜单 `Wilderness → 7. Spawn Harvestables` 加一种矿。
3. 想让它出现在背包面板：`UI/GameUI.cs` 里的 `InvItems` 数组加 `"iron"`，并同步扩大
   `invLabels` / `invRows` 数组长度（两处都要改）。

---

## 加一个新配方（比如"木门 = 4 木板 + 2 石头"）

1. `UI/GameUI.cs` → `MakeCraftRows()` 里加一行：
   ```csharp
   MakeCraftRow(panel, "Door", "Door      4 Plank + 2 Stone -> 1", 2);
   ```
2. `UI/GameUI.cs` → `Craft(string recipe)` 里加分支：
   ```csharp
   else if (recipe == "Door")
   {
       if (inv.Spend("plank", 4) && inv.Spend("stone", 2)) inv.Add("door", 1);
   }
   ```

---

## 加一种新资源（石头 / 灌木之外）

`Assets/Editor/ResourceSpawner.cs` 里照着 `SpawnRocks` 写一个 `SpawnOre`，
然后在该文件顶部的 `Spawn()` 里加一行调用。

---

## 加一种新建筑（比如门/篝火/床）

1. `Player/BuildSystem.cs` → 现在只支持墙，逻辑在 `CreateWall`。
   要支持多种：加一个 `public string currentItem = "wall";`，
   把 `CreateWall` 的尺寸和材质按 `currentItem` 分支。
2. `PlacedStructure` 已经通用（有 `itemId`），拾取不用改。

---

## 加怪物（还没做，起步顺序）

1. 新建 `Assets/Scripts/World/Enemy.cs`：血量、被攻击、死亡
2. 用 `CharacterController` 或 `NavMeshAgent` 做移动
3. 把 `PlayerInteractor` 的射线检测扩大到"打怪"分支
4. 在 `SaveSystem` 里决定要不要存怪物状态（建议先不存）

---

## 改手感（不用写代码，改 Inspector）

| 想改什么 | 选中谁 | 参数 |
|---|---|---|
| 走太慢/太快 | `Player` | ThirdPersonController → Move Speed |
| 跳太低/太高 | `Player` | Jump Height |
| 视角太晕 | `Main Camera` | Mouse Sensitivity、Look Smoothing |
| 相机太远 | `Main Camera` | Distance |
| 一天太长/太短 | `GameManager` | DayNightCycle → Day Length Seconds |
| 摇杆不够灵敏 | `GameManager` | TouchRouter → Stick Dead Zone |
| 建造距离 | `Player` | BuildSystem → Range |

---

## 加新存档字段

`Core/SaveSystem.cs` 里两处成对改：

1. `SaveData` 类里加字段
2. `Save()` 里填值
3. `Load()` 里恢复

**漏了第 3 步 = 存档能写但读取时丢失**，这是最常见的存档 bug。
