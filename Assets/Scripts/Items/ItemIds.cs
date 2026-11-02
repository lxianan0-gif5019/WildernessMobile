// 职责：所有物品 id 的唯一来源，替代裸字符串
// 依赖：无
public static class ItemIds
{
    public const string Wood = "wood";
    public const string Stone = "stone";
    public const string Fiber = "fiber";
    public const string Berry = "berry";
    public const string Meat = "meat";
    public const string CookedMeat = "cooked_meat";
    public const string Campfire = "campfire";
    public const string Stick = "stick";
    public const string Plank = "plank";
    public const string Wall = "wall";
    public const string Workbench = "workbench";
    public const string Furnace = "furnace";
    public const string StoneAxe = "stone_axe";
    public const string StonePick = "stone_pick";
    public const string IronOre = "iron_ore";
    public const string IronIngot = "iron_ingot";
    public const string IronAxe = "iron_axe";
    public const string IronPick = "iron_pick";
    public const string WolfPelt = "wolf_pelt";
    public const string HayBed = "hay_bed";
    public const string Bed = "bed";

    public static readonly string[] All =
    {
        Wood, Stone, Fiber, Berry, Meat, CookedMeat, Campfire, Stick, Plank, Wall,
        Workbench, Furnace, StoneAxe, StonePick, IronOre, IronIngot, IronAxe, IronPick,
        WolfPelt, HayBed, Bed
    };
}
