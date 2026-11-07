// 职责：输入的唯一中转站，键鼠与触屏都写这里
// 依赖：无
using UnityEngine;

// Single source of input for both keyboard/mouse and touch.
// Devices write here; gameplay reads from here. Nothing else calls Input.*.
public static class InputHub
{
    public static Vector2 Move;          // x = strafe, y = forward, magnitude <= 1
    public static Vector2 LookDelta;     // accumulated look motion this frame
    public static bool JumpHeld;
    public static bool JumpDown;
    public static bool SprintHeld;
    public static bool InteractDown;
    public static bool InventoryDown;
    public static bool CraftDown;
    public static bool BuildDown;
    public static bool TouchMoveActive;

    /// <summary>True while a menu is open: the world ignores all gameplay input.</summary>
    public static bool MenuOpen;

    /// <summary>Test hook: when set, overrides the stick so the editor can reproduce input.</summary>
    public static bool DebugMoveActive;
    public static Vector2 DebugMove;

    public static void ClearOneShots()
    {
        JumpDown = false;
        InteractDown = false;
        InventoryDown = false;
        CraftDown = false;
        BuildDown = false;
        LookDelta = Vector2.zero;
    }
}
