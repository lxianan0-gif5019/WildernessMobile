using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// Builds the whole runtime HUD in code: joystick, touch look area, action
// buttons, inventory panel and crafting panel. No prefabs to wire up.
public class GameUI : MonoBehaviour
{
    public static GameUI Instance;

    /// <summary>True while a menu panel is open: the cursor must be free and the
    /// camera must stop taking look input.</summary>
    public static bool PanelOpen { get; private set; }

    Font font;
    Sprite circle;

    Text hud, prompt, invText, craftText;
    GameObject invPanel, craftPanel;
    bool invOpen, craftOpen;

    void Awake()
    {
        Instance = this;
        font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (font == null) font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        circle = MakeCircle(128);
        EnsureEventSystem();
        Build();
    }

    void Update()
    {
        if (InputHub.InventoryDown) Toggle(ref invOpen, invPanel);
        if (InputHub.CraftDown) Toggle(ref craftOpen, craftPanel);

        if (hud != null && Inventory.Instance != null)
        {
            var sb = new System.Text.StringBuilder();
            foreach (var s in Inventory.Instance.items)
                sb.Append(ItemDatabase.Display(s.id)).Append("  ").Append(s.count).Append("    ");
            hud.text = sb.Length == 0 ? "Empty pack  -  WASD move, E harvest, Tab bag, C craft, B build" : sb.ToString();
        }

        if (prompt != null)
            prompt.text = PlayerInteractor.Instance != null ? PlayerInteractor.Instance.Prompt() : "";

        if (invOpen) RefreshInventory();
        if (craftOpen) RefreshCraft();
    }

    // ------------------------------------------------------------------ build UI
    void Build()
    {
        var canvasGo = new GameObject("HUD");
        canvasGo.transform.SetParent(transform, false);
        var canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;
        canvasGo.AddComponent<GraphicRaycaster>();

        var root = canvasGo.transform;

        // Touch look area sits underneath everything else (sibling 0).
        var look = MakeImage(root, "LookArea", new Color(0f, 0f, 0f, 0f), null);
        var lookRect = look.rectTransform;
        lookRect.anchorMin = Vector2.zero;
        lookRect.anchorMax = Vector2.one;
        lookRect.offsetMin = Vector2.zero;
        lookRect.offsetMax = Vector2.zero;
        look.gameObject.AddComponent<TouchLookArea>();

        // HUD line
        hud = MakeText(root, "HUD", "", 26, TextAnchor.UpperLeft);
        Anchor(hud.rectTransform, new Vector2(0f, 1f), new Vector2(24f, -18f), new Vector2(920f, 40f), new Vector2(0f, 1f));

        // Harvest prompt
        prompt = MakeText(root, "Prompt", "", 30, TextAnchor.MiddleCenter);
        Anchor(prompt.rectTransform, new Vector2(0.5f, 0f), new Vector2(0f, 240f), new Vector2(700f, 44f), new Vector2(0.5f, 0f));

        // Joystick
        var stickBg = MakeImage(root, "JoystickArea", new Color(1f, 1f, 1f, 0.12f), circle);
        var stickRect = stickBg.rectTransform;
        Anchor(stickRect, new Vector2(0f, 0f), new Vector2(230f, 230f), new Vector2(240f, 240f), new Vector2(0f, 0f));

        var knob = MakeImage(stickRect, "Knob", new Color(1f, 1f, 1f, 0.55f), circle);
        Anchor(knob.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(100f, 100f), new Vector2(0.5f, 0.5f));

        var joystick = stickBg.gameObject.AddComponent<VirtualJoystick>();
        joystick.background = stickRect;
        joystick.knob = knob.rectTransform;
        joystick.radius = 90f;

        // Jump + interact
        // Action buttons: one right-hand column, evenly spaced (no overlap).
        MakeButton(root, "JumpBtn", "Jump", new Vector2(1f, 0f), new Vector2(-100f, 150f), 130f, TouchButton.Kind.Jump);
        MakeButton(root, "UseBtn", "Use", new Vector2(1f, 0f), new Vector2(-100f, 340f), 130f, TouchButton.Kind.Interact);
        MakeButton(root, "BuildBtn", "Build", new Vector2(1f, 0f), new Vector2(-100f, 530f), 130f, TouchButton.Kind.Build);

        // Menu buttons top-right
        MakeButton(root, "BagBtn", "Bag", new Vector2(1f, 1f), new Vector2(-90f, -90f), 130f, TouchButton.Kind.Inventory);
        MakeButton(root, "CraftBtn", "Craft", new Vector2(1f, 1f), new Vector2(-90f, -230f), 130f, TouchButton.Kind.Craft);

        // Inventory panel
        invPanel = MakePanel(root, "InventoryPanel", "INVENTORY", out invText);
        invPanel.SetActive(false);

        // Crafting panel
        craftPanel = MakePanel(root, "CraftPanel", "CRAFTING", out craftText);
        craftPanel.SetActive(false);
        MakeCraftButtons(craftPanel.transform);

        RefreshCursor();
    }

    GameObject MakePanel(Transform parent, string name, string title, out Text body)
    {
        var bg = MakeImage(parent, name, new Color(0.05f, 0.06f, 0.08f, 0.92f), null);
        var rect = bg.rectTransform;
        Anchor(rect, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(760f, 520f), new Vector2(0.5f, 0.5f));

        var t = MakeText(rect, "Title", title, 34, TextAnchor.UpperCenter);
        Anchor(t.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -26f), new Vector2(700f, 50f), new Vector2(0.5f, 1f));

        body = MakeText(rect, "Body", "", 26, TextAnchor.UpperLeft);
        Anchor(body.rectTransform, new Vector2(0f, 1f), new Vector2(30f, -90f), new Vector2(700f, 300f), new Vector2(0f, 1f));

        return bg.gameObject;
    }

    void MakeCraftButtons(Transform panel)
    {
        MakeCraftRow(panel, "Plank", 0);
        MakeCraftRow(panel, "Wall", 1);
    }

    void MakeCraftRow(Transform panel, string label, int index)
    {
        var img = MakeImage(panel, "Craft" + label, new Color(1f, 1f, 1f, 0.14f), circle);
        Anchor(img.rectTransform, new Vector2(0.5f, 0f), new Vector2(0f, 70f + index * 100f), new Vector2(300f, 78f), new Vector2(0.5f, 0f));

        var t = MakeText(img.rectTransform, "Label", label, 26, TextAnchor.MiddleCenter);
        Anchor(t.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(280f, 70f), new Vector2(0.5f, 0.5f));

        var btn = img.gameObject.AddComponent<CraftButton>();
        btn.recipeName = label;
    }

    Image MakeButton(Transform parent, string name, string label, Vector2 anchor, Vector2 offset, float size, TouchButton.Kind kind)
    {
        var img = MakeImage(parent, name, new Color(1f, 1f, 1f, 0.18f), circle);
        Anchor(img.rectTransform, anchor, offset, new Vector2(size, size), anchor);

        var t = MakeText(img.rectTransform, "Label", label, 26, TextAnchor.MiddleCenter);
        Anchor(t.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(size, size), new Vector2(0.5f, 0.5f));

        var touch = img.gameObject.AddComponent<TouchButton>();
        touch.kind = kind;
        return img;
    }

    // ------------------------------------------------------------------ helpers
    static void Anchor(RectTransform rt, Vector2 anchor, Vector2 offset, Vector2 size, Vector2 pivot)
    {
        rt.anchorMin = anchor;
        rt.anchorMax = anchor;
        rt.pivot = pivot;
        rt.sizeDelta = size;
        rt.anchoredPosition = offset;
    }

    Image MakeImage(Transform parent, string name, Color color, Sprite sprite)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var img = go.AddComponent<Image>();
        img.color = color;
        img.sprite = sprite;
        return img;
    }

    Text MakeText(Transform parent, string name, string content, int size, TextAnchor anchor)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var t = go.AddComponent<Text>();
        t.font = font;
        t.fontSize = size;
        t.text = content;
        t.alignment = anchor;
        t.color = Color.white;
        t.raycastTarget = false;
        t.horizontalOverflow = HorizontalWrapMode.Wrap;
        t.verticalOverflow = VerticalWrapMode.Overflow;
        return t;
    }

    void Toggle(ref bool state, GameObject panel)
    {
        state = !state;
        if (panel != null) panel.SetActive(state);
        RefreshCursor();
    }

    void RefreshCursor()
    {
        PanelOpen = invOpen || craftOpen;
        Cursor.lockState = PanelOpen ? CursorLockMode.None : CursorLockMode.Locked;
        Cursor.visible = PanelOpen;
    }

    /// <summary>Test hook: opens/closes the crafting panel exactly like pressing C.</summary>
    public void DebugToggleCraft() { Toggle(ref craftOpen, craftPanel); }

    void RefreshInventory()
    {
        if (invText == null || Inventory.Instance == null) return;
        var sb = new System.Text.StringBuilder();
        if (Inventory.Instance.items.Count == 0) sb.Append("(nothing yet)");
        foreach (var s in Inventory.Instance.items)
            sb.Append(ItemDatabase.Display(s.id)).Append("    x").Append(s.count).Append("\n\n");
        invText.text = sb.ToString();
    }

    void RefreshCraft()
    {
        if (craftText == null || Inventory.Instance == null) return;
        craftText.text =
            "Plank   3 Wood  ->  2 Plank\n\n" +
            "Wall    2 Plank ->  1 Wall\n\n" +
            "Wood: " + Inventory.Instance.Count("wood") +
            "   Plank: " + Inventory.Instance.Count("plank") +
            "   Wall: " + Inventory.Instance.Count("wall");
    }

    static void EnsureEventSystem()
    {
        if (EventSystem.current != null) return;
        var go = new GameObject("EventSystem");
        go.AddComponent<EventSystem>();
        go.AddComponent<StandaloneInputModule>();
    }

    static Sprite MakeCircle(int size)
    {
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        float r = size * 0.5f;
        var px = new Color32[size * size];
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = x + 0.5f - r, dy = y + 0.5f - r;
                float d = Mathf.Sqrt(dx * dx + dy * dy);
                float a = Mathf.Clamp01((r - d) / 2f);
                px[y * size + x] = new Color32(255, 255, 255, (byte)(a * 255));
            }
        }
        tex.SetPixels32(px);
        tex.Apply();
        tex.wrapMode = TextureWrapMode.Clamp;
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
    }
}
