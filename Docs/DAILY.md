# 每日变更日志

> 用法：每天收工前 10 分钟，看这一条。不用读代码，只要能回答下面的问题。

---

## 模板

```markdown
### YYYY-MM-DD  今天做了什么

**新增功能**：
- ...

**数据流变化**（最重要的部分）：
- 什么数据 → 从哪来 → 存到谁那里 → 谁读它

**新增的调参项**（Inspector 里能改什么）：
- 对象 → 组件 → 参数名 → 作用

**已知问题**：
- ...

**明天第一件事**：
- ...
```

---

## 2026-11-01（第 1 天）

**新增功能**

- 森林地形：600×600 米起伏地形、草地/泥土双图层、420 棵装饰树
- 第三人称角色控制：WASD 相对镜头移动、跳跃、重力、镜头环绕跟随、遮挡自动拉近
- 触屏输入：左下摇杆 + 右半屏拖动转视角 + 5 个功能按钮
- 采集：树/石/灌木可采集，砍完消失
- 背包：堆叠式物品栏，可在面板里看到数量
- 合成：木板（3 木 → 2 板）、墙（2 板 → 1 墙）
- 建造：`B` 进入建造模式，蓝色预览墙跟着镜头，`E` 放下，走近按 `E` 可捡回
- 昼夜循环：5 分钟一个完整昼夜，太阳角度/颜色/环境光/雾/天空联动
- 存档：玩家位置、背包、放下的墙、时间，写 JSON 到本地

**数据流（今天新增的三条）**

```
采集：射线命中 ResourceNode → Inventory.Add(id, n) → GameUI 每帧读 Inventory 显示
建造：BuildSystem 花 Inventory 里的 wall → 生成带 PlacedStructure 的方块
存档：SaveSystem 读 Inventory / 玩家 / 所有 PlacedStructure / timeOfDay → 写 JSON
```

**新增的调参项**

| 对象 | 组件 | 参数 | 作用 |
|---|---|---|---|
| Player | ThirdPersonController | moveSpeed / jumpHeight / turnSpeed | 移动手感 |
| Main Camera | ThirdPersonCamera | mouseSensitivity / lookSmoothing / distance / verticalSmooth | 视角手感 |
| GameManager | DayNightCycle | dayLengthSeconds / timeOfDay | 昼夜速度与起始时间 |
| GameManager | TouchRouter | stickRadius / stickDeadZone / lookSensitivity | 手机触屏手感 |
| Player | BuildSystem | range | 建造距离 |
| Player | PlayerInteractor | reach / pickupReach | 采集与拾取距离 |

**已知问题**

- 被砍掉的树不会进入存档，重进游戏会重新长出来
- 手机上还没验证过新触摸方案（本次重写了输入层）
- 美术全部是几何体占位，没有动画和音效

**明天第一件事**

- 手机上验证摇杆与按钮（4 项验收）
- 通过后进入「内容期」：优先做怪物 + 生命值

---

## 2026-11-02（第 2 天 · 上午）

**主题：触屏问题集中修复**

真机反馈的 5 个问题，全部找到根因：

| 现象 | 根因 | 修复 |
|---|---|---|
| 按钮点不开 | `TouchRouter` 与 `GameUI` 执行顺序不确定，点击标志在同一帧被提前清掉 | `TouchRouter` 加 `[DefaultExecutionOrder(-50)]`，先于所有消费者 |
| 点按钮时视角抖动 | 按钮没命中 → 同一根手指落进"视角区" | 按钮改为按屏幕比例定位，不再互相遮挡 |
| 摇杆区域也能转视角 | 判定区只有 240 像素，手指稍偏就出界 | 摇杆外套 400 的透明热区，可见圆仍是 240 |
| Craft / Build 重叠 | 固定像素偏移，2.18:1 宽屏纵向被压缩 | 改用归一化锚点（0.88 / 0.72 / 0.56 / 0.30 / 0.14） |
| 视角太灵敏 | `lookSensitivity = 0.16` | 改为 **0.085** |

**数据流变化**：无（只改了输入命中与 UI 布局）

**新增调参项**：`GameManager → TouchRouter → Look Sensitivity`

**已知问题**：以上修复全部待真机验证

**附**：编辑器异常退出时，用命令行构建更可靠
```
Unity.exe -projectPath <项目> -batchmode -quit -executeMethod BuildTool.BuildApk
```

---

## 维护惯例（给自己定的规矩）

1. **每天收工**：我写一条日志，你读 10 分钟，确认能说出今天的数据流向
2. **每周一次**：更新 `ARCHITECTURE.md` 的模块表（新增了什么系统）
3. **每次加系统**：先回答 `ARCHITECTURE.md` 末尾那 4 个问题，再写代码
4. **提交信息**：写清楚「改了什么 + 为什么」，不要写 "update" 或 "fix"
