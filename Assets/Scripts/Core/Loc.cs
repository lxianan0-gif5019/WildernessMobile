// 职责：中英文字符串表，所有 UI 文本的唯一来源
// 依赖：无
using System.Collections.Generic;

// Tiny string table. English + Chinese, switchable at runtime.
// Every user-facing string goes through T("key"); item names go through ItemName(id).
public static class Loc
{
    public enum Lang { English, Chinese }

    public static Lang Current = Lang.English;

    static readonly Dictionary<string, string[]> table = new Dictionary<string, string[]>
    {
        // ---- menus
        { "menu.continue",      new[] { "Continue",        "继续游戏" } },
        { "menu.newgame",       new[] { "New Game",        "新游戏" } },
        { "menu.load",          new[] { "Load Game",       "读取存档" } },
        { "menu.save",          new[] { "Save Game",       "保存游戏" } },
        { "menu.settings",      new[] { "Settings",        "设置" } },
        { "menu.mainmenu",      new[] { "Main Menu",       "返回主菜单" } },
        { "menu.quit",          new[] { "Quit",            "退出" } },
        { "menu.resume",        new[] { "Resume",          "继续" } },
        { "menu.paused",        new[] { "PAUSED",          "已暂停" } },
        { "menu.title",         new[] { "WILDERNESS",      "荒野求生" } },
        { "menu.hint",          new[] { "WASD move   E use   Tab bag   C craft   B build   Esc menu",
                                        "WASD 移动   E 使用   Tab 背包   C 合成   B 建造   Esc 菜单" } },
        { "menu.empty",         new[] { "Empty",           "空存档" } },
        { "menu.confirm_overwrite", new[] { "Tap again to overwrite", "再点一次覆盖" } },
        { "menu.hint_mobile",   new[] { "Left stick to move   drag the right side to look   buttons on the right",
                                        "左手摇杆移动   右半屏滑动转视角   右侧按钮操作" } },
        { "menu.slot",          new[] { "Slot",            "存档位" } },
        { "menu.back",          new[] { "Back",            "返回" } },
        { "menu.saved",         new[] { "Saved",           "已保存" } },
        { "menu.language",      new[] { "Language",        "语言" } },

        // ---- in-game panels
        { "ui.inventory",       new[] { "INVENTORY",       "背包" } },
        { "ui.craft",           new[] { "CRAFTING",        "制作" } },
        { "ui.crafting_start",  new[] { "Crafting {0}",    "开始制作 {0}" } },
        { "ui.close",           new[] { "Close",           "关闭" } },

        // ---- settings
        { "set.master",         new[] { "Master Volume",   "总音量" } },
        { "set.sfx",            new[] { "SFX Volume",      "音效音量" } },
        { "set.look",           new[] { "Look Speed",      "视角速度" } },
        { "set.stick",          new[] { "Joystick Size",   "摇杆大小" } },
        { "set.quality",        new[] { "Quality",         "画质" } },
        { "set.invert",         new[] { "Invert Look",     "反转视角" } },
        { "set.on",             new[] { "On",              "开" } },
        { "set.off",            new[] { "Off",             "关" } },
        { "set.low",            new[] { "Low",             "低" } },
        { "set.medium",         new[] { "Medium",          "中" } },
        { "set.high",           new[] { "High",            "高" } },

        // ---- stats
        { "stat.hp",            new[] { "HP",              "生命" } },
        { "stat.food",          new[] { "FOOD",            "食物" } },
        { "stat.water",         new[] { "WATER",           "水" } },
        { "stat.stamina",       new[] { "STAMINA",         "体力" } },

        // ---- hud
        { "hud.empty",          new[] { "Empty pack  -  WASD move, E use, Tab bag, C craft, B build",
                                        "背包是空的  -  WASD 移动，E 使用，Tab 背包，C 合成，B 建造" } },
        { "hud.build_on",       new[] { "BUILD MODE   -   E to place {0} ({1})   -   B to exit",
                                        "建造模式   -   E 放置 {0}（{1}）   -   B 退出" } },
        { "hud.build_none",     new[] { "BUILD MODE   -   no {0} left - craft one first   -   B to exit",
                                        "建造模式   -   没有 {0} 了，先合成一个   -   B 退出" } },
        { "hud.crafting",       new[] { "CRAFTING {0} {1}%",   "合成中 {0} {1}%" } },
        { "hud.wolves",         new[] { "WOLVES   x{0}",       "狼   x{0}" } },
        { "hud.take_out",       new[] { "Tap an item to use it", "点击物品使用" } },
        { "hud.nothing",        new[] { "(nothing yet - harvest a tree with E)",
                                        "（还没有东西——对着树按 E 采集）" } },

        // ---- prompts
        { "prompt.harvest",     new[] { "Harvest {0}",     "采集 {0}" } },
        { "prompt.pickup",      new[] { "Pick up {0} x{1}", "拾取 {0} x{1}" } },
        { "prompt.drink",       new[] { "Drink {0}",       "喝水" } },
        { "prompt.cook",        new[] { "Cook meat",       "烤肉" } },
        { "prompt.refuel",      new[] { "Add wood to fire", "添柴" } },
        { "prompt.fire_out",    new[] { "Fire is out - add wood", "火灭了，添柴" } },
        { "prompt.sleep",       new[] { "Sleep",           "睡觉" } },
        { "prompt.sleep_block", new[] { "Sleep ({0})",     "睡觉（{0}）" } },
        { "prompt.hit",         new[] { "Hit {0}",         "攻击 {0}" } },
        { "prompt.fight",       new[] { "Fight Wolf",      "对抗狼" } },
        { "prompt.pickup_str",  new[] { "Pick up {0}",     "捡起 {0}" } },
        { "prompt.need_tool",   new[] { "need {0}",        "需要 {0}" } },
        { "prompt.cannot_sleep", new[] { "cannot sleep: {0}", "无法睡觉：{0}" } },

        // ---- sleep reasons
        { "sleep.night",        new[] { "you can only sleep at night", "只能在夜里睡觉" } },
        { "sleep.wolves",       new[] { "wolves are too close to sleep", "附近有狼，睡不着" } },

        // ---- tutorial
        { "tut.title",          new[] { "OBJECTIVE",       "目标" } },
        { "tut.wood",           new[] { "Chop trees for wood  ({0}/5)",   "砍树收集木头（{0}/5）" } },
        { "tut.bench",          new[] { "Craft and place a Workbench",    "合成并放下工作台" } },
        { "tut.fire",           new[] { "Craft and light a Campfire",     "合成并点燃篝火" } },
        { "tut.night",          new[] { "Survive your first night",       "活过第一个夜晚" } },
        { "tut.done",           new[] { "You survived. Now thrive.",      "你活下来了。接下来，活得更好。" } },

        // ---- items
        { "item.wood",          new[] { "Wood",            "木头" } },
        { "item.stone",         new[] { "Stone",           "石头" } },
        { "item.fiber",         new[] { "Fiber",           "纤维" } },
        { "item.berry",         new[] { "Berry",           "浆果" } },
        { "item.meat",          new[] { "Meat",            "生肉" } },
        { "item.cooked_meat",   new[] { "Cooked Meat",     "熟肉" } },
        { "item.campfire",      new[] { "Campfire",        "篝火" } },
        { "item.stick",         new[] { "Stick",           "木棍" } },
        { "item.plank",         new[] { "Plank",           "木板" } },
        { "item.wall",          new[] { "Wall",            "墙" } },
        { "item.workbench",     new[] { "Workbench",       "工作台" } },
        { "item.furnace",       new[] { "Furnace",         "熔炉" } },
        { "item.stone_axe",     new[] { "Stone Axe",       "石斧" } },
        { "item.stone_pick",    new[] { "Stone Pick",      "石镐" } },
        { "item.iron_ore",      new[] { "Iron Ore",        "铁矿" } },
        { "item.iron_ingot",    new[] { "Iron Ingot",      "铁锭" } },
        { "item.iron_axe",      new[] { "Iron Axe",        "铁斧" } },
        { "item.iron_pick",     new[] { "Iron Pick",       "铁镐" } },
        { "item.wolf_pelt",     new[] { "Wolf Pelt",       "狼皮" } },
        { "item.hay_bed",       new[] { "Hay Nest",        "干草窝" } },
        { "item.bed",           new[] { "Wooden Bed",      "木床" } },
        { "item.rabbit",        new[] { "Rabbit",          "兔子" } },
        { "item.deer",          new[] { "Deer",            "鹿" } },
        { "item.boar",          new[] { "Boar",            "野猪" } },

        // ---- world objects
        { "world.tree",         new[] { "Tree",            "树" } },
        { "world.rock",         new[] { "Rock",            "石头" } },
        { "world.bush",         new[] { "Bush",            "灌木" } },
        { "world.ironvein",     new[] { "Iron Vein",       "铁矿脉" } },
        { "world.water",        new[] { "Water",           "水" } },
        { "world.wall",         new[] { "Wall",            "墙" } },
        { "world.campfire",     new[] { "Campfire",        "篝火" } },
        { "world.workbench",    new[] { "Workbench",       "工作台" } },
        { "world.furnace",      new[] { "Furnace",         "熔炉" } },
        { "world.bed",          new[] { "Bed",             "木床" } },
        { "world.haybed",       new[] { "Hay Nest",        "干草窝" } },
    };

    public static string T(string key)
    {
        string[] pair;
        if (!table.TryGetValue(key, out pair)) return key;
        return pair[(int)Current];
    }

    public static string F(string key, params object[] args)
    {
        return string.Format(T(key), args);
    }

    /// <summary>Localized item name; falls back to the key itself.</summary>
    public static string ItemName(string id)
    {
        string[] pair;
        if (table.TryGetValue("item." + id, out pair)) return pair[(int)Current];
        return id;
    }

    public static string QualityName(int quality)
    {
        if (quality <= 0) return T("set.low");
        if (quality == 1) return T("set.medium");
        return T("set.high");
    }

    public static void ToggleLanguage()
    {
        Current = Current == Lang.English ? Lang.Chinese : Lang.English;
    }
}
