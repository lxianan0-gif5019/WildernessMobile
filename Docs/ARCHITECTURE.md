# 架构地图

> 这份文件的目的：**让你在不懂代码的情况下，也能知道"什么东西存在哪"。**
> 每次加新系统，这张图都要更新。看不懂的地方，直接问我。

---

## 一句话结构

```
输入(InputHub) → 角色控制器 / 相机 / 交互 → 数据(Inventory / 场景对象) → 存档(SaveSystem)
```

---

## 模块表

| 文件 | 职责 | 它拥有的数据 | 依赖谁 |
|---|---|---|---|
| `Core/InputHub.cs` | 输入的唯一中转站（静态字段） | 当前帧的移动/视角/按键状态 | 无 |
| `Core/PlayerInputReader.cs` | 键鼠 → InputHub | 无 | InputHub |
| `Core/SaveSystem.cs` | 读写 JSON 存档 | **不拥有数据**，只搬运 | Inventory、DayNightCycle、BuildSystem |
| `UI/TouchRouter.cs` | 手机触摸 → InputHub + UI 按钮 | 无 | InputHub、GameUI |
| `Player/ThirdPersonController.cs` | 角色移动/跳跃/重力 | 无（状态在 CharacterController 上） | InputHub |
| `ThirdPersonCamera.cs` | 环绕相机 | 无 | InputHub、GameUI.PanelOpen |
| `Player/PlayerInteractor.cs` | 找目标、采集、捡建筑 | 当前目标（每帧重算） | ResourceNode、PlacedStructure、BuildSystem |
| `Player/BuildSystem.cs` | 建造模式、预览墙、放墙 | 建造模式开关 | Inventory |
| `Items/Inventory.cs` | **背包数据的唯一真源** | `List<Stack> items` | 无 |
| `Items/ItemDatabase.cs` | 物品 id → 名字/颜色 | 静态表 | 无 |
| `World/ResourceNode.cs` | 可采集的树/石/灌木 | 剩余次数 | Inventory |
| `World/PlacedStructure.cs` | 放下的建筑（可捡回） | itemId | Inventory |
| `World/DayNightCycle.cs` | 时间、太阳、雾、天空 | `timeOfDay` (0–1) | 无 |
| `UI/GameUI.cs` | 生成整个 HUD、面板、按钮 | 面板开关状态 | Inventory、BuildSystem |

### 2026-11-02 夜间新增的 7 个系统

| 文件 | 职责 | 拥有什么 | 依赖谁 |
|---|---|---|---|
| `Core/Loc.cs` | 中英文字符串表 | 静态表 | 无 |
| `Core/GameSettings.cs` | 设置存取与下发 | PlayerPrefs 镜像 | AudioManager、TouchRouter |
| `Core/Diagnostics.cs` | 错误日志 + 设备信息 | 日志文件 | 无 |
| `Audio/AudioManager.cs` | 全部音效（程序合成） | 音源池、波形 | 无 |
| `UI/MenuUI.cs` | 主菜单/暂停/设置/存档位 | 菜单开关状态 | SaveSystem、GameSettings、TouchRouter |
| `UI/Tutorial.cs` | 四步新手目标 | `step`、`woodPeak` | Inventory、CraftingStation、Campfire、DayNightCycle |
| `Player/ToolWear.cs` | 工具耐久 | `wear` 字典 | Inventory、AudioManager |
| `Items/CraftingController.cs` | 计时合成 | 当前配方与进度 | Inventory、CraftingStation |
| `Items/ItemIds.cs` | 物品 id 常量 | 无 | 无 |
| `World/ItemPickup.cs` | 地面掉落物（池化） | `All` 列表 | Inventory、ItemDatabase |
| `World/Debris.cs` | 碎屑粒子（池化） | 池 | 无 |
| `World/FlyItem.cs` | 拾取飞入动画 | 无 | 无 |
| `World/Wolf.cs` / `WolfNight.cs` | 夜间狼与生成管理 | 狼列表 | PlayerStats、Campfire |
| `World/Campfire.cs` | 篝火：照明/烹饪/燃料/驱兽 | `fuel`、静态 `All` | Inventory、AudioManager |
| `World/CraftingStation.cs` | 工作台/熔炉标记 | 静态 `All` | 无 |
| `World/SleepSpot.cs` | 睡觉点 | 无 | DayNightCycle、PlayerStats |

---

## 静态状态登记表（⑤）

**静态列表是唯一会跨场景泄漏的状态**，必须逐个说清谁创建、谁清理。

| 静态容器 | 谁添加 | 谁移除 | 会不会漏 |
|---|---|---|---|
| `ItemPickup.All` | `Spawn()` | `Release()`（拾取/过期/超上限） | **不会**：120 上限 + 240 秒过期 |
| `ResourceNode.Harvested` | `OnDestroy`（已采完） | `ApplyHarvested()` 读档时重建 | **会**：跨读档累积，但读档会整体替换 |
| `Campfire.All` | `OnEnable` | `OnDisable` | 不会（随对象销毁自动摘除） |
| `CraftingStation.All` | `OnEnable` | `OnDisable` | 不会 |
| `WolfNight.wolves` | `SpawnOne()` | `Update()` 里剔除 null/Dead、天亮 `DespawnAll()` | 不会 |
| `TouchRouter.buttons` | `RegisterButton` | **无移除接口** | **会**：菜单反复重建会累积失效按钮（目前只在启动时注册一次） |
| `Debris` 池 | `Create` | `Release` | 不会（只回收不销毁） |
| `ItemPickup.mats`（材质缓存） | 首次按 id 创建 | 无 | 不会（最多 21 个） |
| `Loc.table` | 静态初始化 | 无 | 不会 |

### 需要留意的两处

1. **`TouchRouter.buttons` 没有注销接口** —— 如果以后做动态生成的按钮（例如滚动列表复用），必须先加 `UnregisterButton`，否则列表会越滚越大且旧按钮仍会响应。
2. **`ResourceNode.Harvested` 是场景级 ID 集合** —— ID 由坐标取整生成，如果以后移动/复制资源点，可能出现坐标撞车导致误删。

---


## 三条主线（最重要的部分）

### 1. 输入怎么走

```
键盘鼠标 ──> PlayerInputReader ──┐
                                 ├──> InputHub ──> 控制器 / 相机 / 交互
手机触摸 ──> TouchRouter ────────┘
```

**为什么这样设计**：游戏逻辑只认 `InputHub`，不关心是键盘还是手指。
以后要加手柄，只需要再写一个"手柄 → InputHub"的脚本，其他代码一行都不用改。

### 2. 背包数据怎么走

```
砍树 → ResourceNode.Harvest() → Inventory.Add("wood", 2)
                                        ↓
                              Inventory.items（唯一真源）
                                        ↓
                    GameUI 每帧读它 → HUD 显示 / 面板显示 / 合成扣材料
```

**规则：任何地方要改背包，只能调 `Inventory.Add / Spend / Clear`。**
不允许某个脚本自己去改 `items` 列表——那样存档和 UI 都会对不上。

### 3. 存档怎么走

```
SaveSystem.Save()  →  读 Inventory.items / 玩家坐标 / 所有 PlacedStructure / timeOfDay
                   →  写成一个 JSON

SaveSystem.Load()  →  读 JSON
                   →  设置玩家位置、重建背包、重建墙、设置时间
```

**注意**：存档是"读数据 → 写文件"，它自己不存任何东西。
所以以后加新系统（比如怪物），只要在 Save 里多收集一项、Load 里多恢复一项。

---

## 目前有 5 个单例（`Instance`）

`Inventory` / `GameUI` / `TouchRouter` / `BuildSystem` / `DayNightCycle` / `SaveSystem` / `PlayerInteractor`

**好处**：随处可以拿到，写起来快。
**风险**：谁都能改它们，出了问题不容易定位。

**纪律**：单例只用来"取数据"和"调方法"，不要把业务逻辑散落到别的脚本里去改它的内部状态。

---

## 不要做的事

| 禁止 | 原因 |
|---|---|
| 在别的脚本里直接改 `Inventory.items` | 存档和 UI 会不一致 |
| 绕过 `InputHub` 直接用 `Input.GetKey` 写游戏逻辑 | 手机上会失效 |
| 在 `Update` 里调 `FindObjectsOfType` | 每帧扫描全场景，手机上必卡 |
| 用 `GameObject.Find` 做频繁查找 | 同上，改成启动时缓存 |
| 把游戏逻辑写进 `GameUI` | UI 只负责显示和转发点击 |

---

## 每次加新系统，必须回答这 4 个问题

1. **数据存在哪个组件里？**（唯一真源）
2. **谁写它？**（只能有一个入口）
3. **谁读它？**（UI / 存档 / 逻辑）
4. **怎么存盘？**（Save 里加什么、Load 里恢复什么）

答不出来就先别写代码——这四问答完，代码基本就成型了。

---

## 文件体积红线与拆分计划

**规则：单个脚本超过 600 行就要拆。** 新面板一律走新文件，不再往 `GameUI` 里塞。

### 现状（2026-11-02）

| 文件 | 行数 | 状态 |
|---|---|---|
| `UI/GameUI.cs` | 468 | 未到线，**暂不拆** |
| `UI/MenuUI.cs` | 281 | 独立文件，方向正确 |
| `UI/TouchRouter.cs` | 191 | 独立文件，正确 |

`MenuUI` 独立成文件这件事本身就是示范：**新面板 = 新文件**。

### 到 600 行时的拆分方案（只搬代码，不改逻辑）

30 分钟完成，拆成四个文件：

| 新文件 | 搬什么（对应现在的方法） |
|---|---|
| `GameUI.cs` | 只留门面：`Instance`、`PanelOpen`、`Awake/Update` 每帧调度、`MakeImage/MakeText/Anchor` 工具 |
| `InventoryPanel.cs` | `MakeInventoryRows`、`OnInventoryRow`、`RefreshInventory`、`OnItemClicked` |
| `CraftPanel.cs` | `MakeCraftRows`、`RecipeLine`、`RefreshCraft`、`Craft` |
| `BuildHUD.cs` | 建造模式的 prompt 文字、`BuildSystem` 状态提示 |

**拆分原则**

1. **只搬不改**——搬完行为必须完全一致，方便用 git diff 核对
2. 搬完立刻编译 + Play 跑一遍背包/合成/建造三个面板
3. `GameUI` 保留对外的公开方法（`PanelOpen`、`OnItemClicked`），内部转调新类
4. 拆完更新本表

