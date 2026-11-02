# WildernessMobile · 进度看板

> **这份文件由「跟踪会话」维护**（不是开发会话）。
> 开发过程看 [`DAILY.md`](DAILY.md)，代码结构看 [`ARCHITECTURE.md`](ARCHITECTURE.md)，钱看 [`BUDGET.md`](BUDGET.md)，选型理由看 [`DECISIONS.md`](DECISIONS.md)。
> **开工先读这里，收工改这里。**

最后更新：2026-11-02

---

## 一句话现状

第 3 天。**单机生存的核心闭环已经能玩**：采集 → 合成 → 建造 → 饥饿/口渴 → 打猎 → 篝火烹饪 → 存档，APK 已构建（33.6 MB）。
当前卡在三件事：**触屏修复没上真机验证、合成系统 v2 还躺在工作区没提交、角色美术一个像素都还没有**。

---

## 里程碑

| 阶段 | 内容 | 状态 | 验收标准 |
|---|---|---|---|
| M0 | 环境与工程 | ✅ 完成 | Unity 能开、能出 APK |
| M1 | 核心生存闭环 | ✅ 完成 | 采集/合成/建造/属性/存档能跑通 |
| M2 | 触屏真机验收 | 🔄 进行中 | 4 项触摸交互在真机全部正常 |
| M3 | 美术管线 | ⬜ 未开始 | 1 个角色 + 10 个环境资产进游戏 |
| M4 | 战斗与怪物 | ⬜ 未开始 | 有会主动攻击的敌人 |
| M5 | Demo 1.0 | ⬜ 未开始 | 手机上 30 FPS，可完整游玩 30 分钟 |

---

## 已完成（按系统）

**世界**
- 600×600 m 起伏地形，草地/泥土双图层，420 棵装饰树（`Assets/Scenes/Main.unity`）
- 昼夜循环：1 小时一整天，太阳角度/颜色/雾/天空联动，月光 + 30 天月相（`Scripts/World/DayNightCycle.cs`）

**角色与操作**
- 第三人称控制器：WASD 相对镜头移动、跳跃、重力（`Scripts/ThirdPersonController.cs`）
- 环绕相机 + 遮挡自动拉近（`Scripts/ThirdPersonCamera.cs`）
- 输入统一走 `InputHub`，键鼠与触摸共用一条链路（`Scripts/Core/InputHub.cs`）
- 触屏：左下摇杆 + 右半屏拖动转视角 + 5 个功能按钮（`Scripts/UI/TouchRouter.cs`）

**生存**
- 四属性：HP / 食物 / 水 / 体力，顶部状态条，死亡 1.5 秒后重生（`Scripts/Player/PlayerStats.cs`）
- 浆果（+12 食物 +6 水）、水池（+40 水）

**采集与物品**
- 树/石/灌木可采集，砍完消失，掉落进背包（`Scripts/World/ResourceNode.cs`）
- 堆叠式背包（`Scripts/Items/Inventory.cs`）、物品表（`Scripts/Items/ItemDatabase.cs`）

**建造**
- `B` 进建造模式，蓝色预览跟随镜头，`E` 放置，走近 `E` 捡回，可存档（`Scripts/Player/BuildSystem.cs`）

**动物与战斗**
- 兔 20HP 逃跑 / 鹿 45HP 逃跑 / 野猪 70HP 反击，游荡→逃跑→追击→死亡状态机（`Scripts/World/Animal.cs`）
- `E` 攻击，22 伤害

**篝火**
- 照明 12 m 闪烁、生肉→熟肉（+26 → +45 饥饿）、14 m 驱兽、可拾取可存档（`Scripts/World/Campfire.cs`）

**存档**
- JSON：玩家位置、背包、建筑、时间、三项生存数值（`Scripts/Core/SaveSystem.cs`）

---

## 进行中 / 未提交

`git status` 现在有 **14 个改动 + 5 个新文件**，都是合成系统 v2 的一部分：

```
新：Items/CraftingController.cs  Items/CraftingRecipe.cs  World/CraftingStation.cs
     Materials/HarvestIron.mat（铁矿石）
改：SaveSystem  ItemDatabase  BuildSystem  PlayerInteractor  GameUI
     ResourceNode  DebugActions  ResourceSpawner  以及 5 个采集物材质
```

**风险**：这些改动没进 git，一旦 Unity 崩溃或改错，全部白做。
**建议**：先跑通编译 → 提交一次（`feat: crafting station and iron tier`）→ 再继续。

---

## 下一步（按优先级）

1. **提交合成系统** —— 让工作区回到干净状态（我来做，5 分钟）
2. **真机 APK 验收** —— 装 `Builds/WildernessMobile.apk`，验 4 件事：按钮能点开、点按钮视角不抖、摇杆边缘不转视角、视角灵敏度 0.085 是否顺手
3. **合成站进存档** —— 目前只有墙和篝火能存，工作台还没接
4. **角色 POC** —— 拿 `D:\project\Image\大肥鱼*.jpg` 三视图去 Tripo 图生 3D，跑一遍「清理 → 重建头发 → 画脸 → 骨架」管线，渲 4 张图定生死
5. **性能基线** —— 真机 30 FPS 能不能守住，测完才知道后面还有多少美术预算
6. **怪物（M4）** —— 现成的 `Animal` 状态机可以直接扩成敌对 AI

---

## 风险与坑

| 风险 | 影响 | 应对 |
|---|---|---|
| 合成系统未提交 | 丢了就是两天白干 | 立即 commit |
| 触屏修复未真机验证 | 手机上可能还是不能用 | 装 APK 实测 |
| 角色完全空白 | 玩家看到的是胶囊体 | 尽快跑 POC，别拖到最后 |
| 动物是滑行没有动画 | 观感廉价 | 上模型时一起做 Animator |
| 真机帧率未知 | 美术预算可能全部推翻 | 先测基线再加资产 |
| Unity 是 2022.3 不是计划的 6.3 | 部分新特性用不了，但更稳 | 已接受，记在 DECISIONS D1 |
| token 余额 15.55 元 | 约 4–5 天见底 | 见 BUDGET.md |

---

## 进度时间线

- **10-31** 立项。定方案（安卓 3D 开放世界生存）、装 Unity、建工程
- **11-01（第 1 天）** 地形、第三人称控制、触屏输入、采集、背包、合成、建造、昼夜、存档 —— 核心循环一天打通
- **11-02（第 2 天）** 触屏 5 个 bug 定位修复、生存四属性、野生动物、篝火烹饪、合成站 v2（未提交）、APK 构建成功
- **11-02** 建立跟踪会话与这套看板

---

## 跟踪会话的规矩

**开工三步**（你只要说"跟一下进度"，我就自己做）
1. 读本页 → 读 `DAILY.md` 最后一条 → `git log -3` + `git status`
2. 对账预算：token 余额、今日花费
3. 告诉你：上次到哪、现在卡哪、今天建议做哪一件

**收工三件事**
1. 更新本页「一句话现状」+「下一步」+「进度时间线」
2. 更新 `BUDGET.md` 的实际数字
3. 有新决策就记进 `DECISIONS.md`

**边界**：这个会话只负责**记录、对账、提醒、给建议**，不主动改游戏代码。要动代码你说一声，或者去开发会话做。
