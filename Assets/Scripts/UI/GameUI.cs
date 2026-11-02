using System;
using UnityEngine;
using UnityEngine.UI;

// Builds the runtime HUD in code and wires every control into TouchRouter.
// No EventSystem: TouchRouter reads raw touches, which is reliable on device.
public class GameUI : MonoBehaviour
{
    public static GameUI Instance;

    /// <summary>True while a menu panel is open: cursor free, camera look frozen.</summary>
    public static bool PanelOpen { get; private set; }

    Font font;
    Sprite circle;
    TouchRouter router;

    Text hud, prompt, invText, craftText;
    GameObject invPanel, craftPanel;
    bool invOpen, craftOpen;

    static readonly string[] InvItems = { "wood", "stone", "fiber", "plank", "wall", "berry" };
    readonly Text[] invLabels = new Text[6];
    readonly GameObject[] invRows = new GameObject[6];

    // survival bars: health, hunger, thirst, stamina
    readonly Image[] statFills = new Image[4];
    readonly Text[] statLabels = new Text[4];

    void Awake()
    {
        Instance = this;
        font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (font == null) font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        circle = MakeCircle(128);

        router = GetComponent<TouchRouter>();
        if (router == null) router = gameObject.AddComponent<TouchRouter>();

        Build();
        RefreshCursor();
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
        {
            if (BuildSystem.Instance != null && BuildSystem.Instance.Active)
            {
                int walls = Inventory.Instance != null ? Inventory.Instance.Count("wall") : 0;
                prompt.text = walls > 0
                    ? "BUILD MODE   -   press B to exit,   E to place     Wall: " + walls
                    : "BUILD MODE   -   press B to exit     no Wall - craft one (C: 2 Plank)";
            }
            else
            {
                prompt.text = PlayerInteractor.Instance != null ? PlayerInteractor.Instance.Prompt() : "";
            }
        }

        UpdateStatBars();

        if (invOpen) RefreshInventory();
        if (craftOpen) RefreshCraft();
    }

    // ------------------------------------------------------------------ build UI
    void Build()
    {
        // The canvas must be a ROOT object: parented canvases are not stretched to
        // the screen by Unity, which pushes all UI off-screen.
        var canvasGo = new GameObject("HUD");
        var canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        // Unity does not reliably position a runtime-created root canvas: its size
        // gets set but it can stay pinned at (0,0), pushing the whole UI off-screen.
        // Place it explicitly in the middle of the screen.
        var crt = canvasGo.GetComponent<RectTransform>();
        crt.anchorMin = Vector2.zero;
        crt.anchorMax = Vector2.zero;
        crt.pivot = new Vector2(0.5f, 0.5f);
        crt.sizeDelta = new Vector2(Screen.width, Screen.height);
        crt.localPosition = new Vector3(Screen.width * 0.5f, Screen.height * 0.5f, 0f);
        var scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;
        canvasGo.AddComponent<GraphicRaycaster>();

        var root = canvasGo.transform;

        // HUD line
        hud = MakeText(root, "HUD", "", 26, TextAnchor.UpperLeft);
        Anchor(hud.rectTransform, new Vector2(0f, 1f), new Vector2(24f, -18f), new Vector2(920f, 40f), new Vector2(0f, 1f));

        // --- survival bars, stacked under the HUD line
        MakeStatBar(root, 0, "HP", new Color(0.85f, 0.22f, 0.22f), -64f);
        MakeStatBar(root, 1, "FOOD", new Color(0.90f, 0.60f, 0.15f), -104f);
        MakeStatBar(root, 2, "WATER", new Color(0.22f, 0.55f, 0.90f), -144f);
        MakeStatBar(root, 3, "STAMINA", new Color(0.35f, 0.80f, 0.35f), -184f);

        // Harvest / build prompt
        prompt = MakeText(root, "Prompt", "", 30, TextAnchor.MiddleCenter);
        Anchor(prompt.rectTransform, new Vector2(0.5f, 0f), new Vector2(0f, 250f), new Vector2(900f, 44f), new Vector2(0.5f, 0f));

        // --- joystick (pivot CENTRE: local coords are then centred on the stick)
        // --- joystick: a generous invisible touch pad with the visible stick inside it.
        // The pad is larger than the circle so a thumb landing slightly off still grabs
        // the stick instead of turning the camera.
        var stickPad = MakeImage(root, "JoystickPad", new Color(0f, 0f, 0f, 0f), null);
        Anchor(stickPad.rectTransform, new Vector2(0f, 0f), new Vector2(360f, 360f), new Vector2(400f, 400f), new Vector2(0.5f, 0.5f));

        var stickBg = MakeImage(stickPad.rectTransform, "JoystickArea", new Color(1f, 1f, 1f, 0.12f), circle);
        Anchor(stickBg.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(240f, 240f), new Vector2(0.5f, 0.5f));

        var knob = MakeImage(stickPad.rectTransform, "Knob", new Color(1f, 1f, 1f, 0.55f), circle);
        Anchor(knob.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(100f, 100f), new Vector2(0.5f, 0.5f));

        router.stickArea = stickPad.rectTransform;
        router.stickKnob = knob.rectTransform;
        router.stickRadius = 95f;

        // --- action buttons anchored by screen RATIO so a wide phone cannot overlap them
        MakeButton(root, "BagBtn", "Bag", new Vector2(1f, 0.88f), 140f, () => InputHub.InventoryDown = true);
        MakeButton(root, "CraftBtn", "Craft", new Vector2(1f, 0.72f), 140f, () => InputHub.CraftDown = true);
        MakeButton(root, "BuildBtn", "Build", new Vector2(1f, 0.56f), 140f, () => InputHub.BuildDown = true);
        MakeButton(root, "UseBtn", "Use", new Vector2(1f, 0.30f), 150f, () => InputHub.InteractDown = true);
        MakeButton(root, "JumpBtn", "Jump", new Vector2(1f, 0.14f), 150f, () => { InputHub.JumpHeld = true; InputHub.JumpDown = true; });

        // --- inventory panel
        invPanel = MakePanel(root, "InventoryPanel", "INVENTORY", out invText);
        invPanel.SetActive(false);
        MakeInventoryRows(invPanel.transform);
        Anchor(invText.rectTransform, new Vector2(0.5f, 0f), new Vector2(0f, 16f), new Vector2(700f, 42f), new Vector2(0.5f, 0f));
        invText.alignment = TextAnchor.MiddleCenter;
        invText.fontSize = 24;

        // --- crafting panel
        craftPanel = MakePanel(root, "CraftPanel", "CRAFTING", out craftText);
        craftPanel.SetActive(false);
        MakeCraftRows(craftPanel.transform);

        Debug.Log("[UI] screen=" + Screen.width + "x" + Screen.height
                  + " canvasPos=" + crt.position
                  + " stickScreen=" + RectTransformUtility.WorldToScreenPoint(null, router.stickArea.position)
                  + " children=" + root.childCount);
    }

    void MakeStatBar(Transform parent, int index, string label, Color color, float y)
    {
        var bg = MakeImage(parent, "StatBar" + label, new Color(0f, 0f, 0f, 0.45f), null);
        Anchor(bg.rectTransform, new Vector2(0f, 1f), new Vector2(24f, y), new Vector2(320f, 28f), new Vector2(0f, 1f));

        var fill = MakeImage(bg.rectTransform, "Fill", color, null);
        var rt = fill.rectTransform;
        rt.anchorMin = new Vector2(0f, 0f);
        rt.anchorMax = new Vector2(1f, 1f);
        rt.pivot = new Vector2(0f, 0.5f);
        rt.offsetMin = new Vector2(2f, 2f);
        rt.offsetMax = new Vector2(-2f, -2f);

        var t = MakeText(bg.rectTransform, "Label", label, 18, TextAnchor.MiddleLeft);
        Anchor(t.rectTransform, new Vector2(0f, 0.5f), new Vector2(8f, 0f), new Vector2(300f, 26f), new Vector2(0f, 0.5f));

        statFills[index] = fill;
        statLabels[index] = t;
    }

    void UpdateStatBars()
    {
        var s = PlayerStats.Instance;
        if (s == null) return;

        SetBar(0, s.health, s.maxHealth, "HP");
        SetBar(1, s.hunger, s.maxHunger, "FOOD");
        SetBar(2, s.thirst, s.maxThirst, "WATER");
        SetBar(3, s.stamina, s.maxStamina, "STAMINA");
    }

    void SetBar(int i, float value, float max, string label)
    {
        if (statFills[i] == null) return;
        float v = max <= 0f ? 0f : Mathf.Clamp(value / max, 0.001f, 1f);
        var rt = statFills[i].rectTransform;
        rt.anchorMax = new Vector2(v, 1f);
        rt.offsetMin = new Vector2(2f, 2f);
        rt.offsetMax = new Vector2(-2f, -2f);
        if (statLabels[i] != null) statLabels[i].text = label + "   " + Mathf.RoundToInt(value);

        if (i == 0 && !loggedBars)
        {
            loggedBars = true;
            Debug.Log("[UI] bars active, hp=" + Mathf.RoundToInt(value)
                      + " barScreen=" + RectTransformUtility.WorldToScreenPoint(null, rt.position)
                      + " barSize=" + rt.rect.size
                      + " fillColor=" + statFills[i].color);
        }
    }

    bool loggedBars;

    void MakeButton(Transform parent, string name, string label, Vector2 anchor, float size, Action action)    {
        var img = MakeImage(parent, name, new Color(1f, 1f, 1f, 0.18f), circle);
        var rt = img.rectTransform;
        rt.anchorMin = anchor;
        rt.anchorMax = anchor;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(size, size);
        rt.anchoredPosition = new Vector2(-100f, 0f);

        var t = MakeText(img.rectTransform, "Label", label, 27, TextAnchor.MiddleCenter);
        Anchor(t.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(size, size), new Vector2(0.5f, 0.5f));

        router.RegisterButton(img.rectTransform, action);
    }

    void MakeInventoryRows(Transform panel)
    {
        for (int i = 0; i < InvItems.Length; i++)
        {
            string id = InvItems[i];

            var img = MakeImage(panel, "Row_" + id, new Color(1f, 1f, 1f, 0.10f), circle);
            Anchor(img.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -110f - i * 66f), new Vector2(640f, 58f), new Vector2(0.5f, 1f));

            var label = MakeText(img.rectTransform, "Label", "", 26, TextAnchor.MiddleLeft);
            Anchor(label.rectTransform, new Vector2(0f, 0.5f), new Vector2(26f, 0f), new Vector2(580f, 52f), new Vector2(0f, 0.5f));

            invLabels[i] = label;
            invRows[i] = img.gameObject;

            router.RegisterButton(img.rectTransform, () => OnItemClicked(id));
        }
    }

    void MakeCraftRows(Transform panel)
    {
        MakeCraftRow(panel, "Plank", "Plank     3 Wood -> 2", 0);
        MakeCraftRow(panel, "Wall", "Wall      2 Plank -> 1", 1);
    }

    void MakeCraftRow(Transform panel, string recipe, string label, int index)
    {
        var img = MakeImage(panel, "Craft" + recipe, new Color(1f, 1f, 1f, 0.14f), circle);
        Anchor(img.rectTransform, new Vector2(0.5f, 0f), new Vector2(0f, 130f + index * 96f), new Vector2(520f, 76f), new Vector2(0.5f, 0f));

        var t = MakeText(img.rectTransform, "Label", label, 25, TextAnchor.MiddleCenter);
        Anchor(t.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(500f, 70f), new Vector2(0.5f, 0.5f));

        router.RegisterButton(img.rectTransform, () => Craft(recipe));
    }

    // ------------------------------------------------------------------ actions
    public void Craft(string recipe)
    {
        var inv = Inventory.Instance;
        if (inv == null) return;

        if (recipe == "Plank")
        {
            if (inv.Spend("wood", 3)) inv.Add("plank", 2);
        }
        else if (recipe == "Wall")
        {
            if (inv.Spend("plank", 2)) inv.Add("wall", 1);
        }
    }

    public void OnItemClicked(string id)
    {
        if (id == "berry")
        {
            if (Inventory.Instance != null && Inventory.Instance.Spend("berry", 1) && PlayerStats.Instance != null)
            {
                PlayerStats.Instance.Eat(12f, 6f);
                Debug.Log("[UI] ate a berry");
            }
            return;
        }

        if (id == "wall")
        {
            if (BuildSystem.Instance != null) BuildSystem.Instance.EnterBuildMode();
            invOpen = false; if (invPanel != null) invPanel.SetActive(false);
            craftOpen = false; if (craftPanel != null) craftPanel.SetActive(false);
            RefreshCursor();
            Debug.Log("[UI] took a Wall out of the backpack - build mode on");
        }
        else
        {
            Debug.Log("[UI] " + ItemDatabase.Display(id) + " is a material, not a placeable item");
        }
    }

    // ------------------------------------------------------------------ panels
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

    /// <summary>Test hook: same as pressing Tab.</summary>
    public void DebugToggleInventory() { Toggle(ref invOpen, invPanel); }

    /// <summary>Test hook: same as pressing C.</summary>
    public void DebugToggleCraft() { Toggle(ref craftOpen, craftPanel); }

    void RefreshInventory()
    {
        if (Inventory.Instance == null) return;

        bool any = false;
        for (int i = 0; i < InvItems.Length; i++)
        {
            int count = Inventory.Instance.Count(InvItems[i]);
            if (count > 0) any = true;
            if (invLabels[i] != null)
                invLabels[i].text = ItemDatabase.Display(InvItems[i]) + "        x" + count;
            if (invRows[i] != null)
                invRows[i].SetActive(count > 0);
        }

        if (invText != null)
            invText.text = any ? "Tap Wall to take it out and build"
                               : "(nothing yet - go harvest a tree with E)";
    }

    void RefreshCraft()
    {
        if (craftText == null || Inventory.Instance == null) return;
        craftText.text = "Wood: " + Inventory.Instance.Count("wood")
                       + "   Plank: " + Inventory.Instance.Count("plank")
                       + "   Wall: " + Inventory.Instance.Count("wall");
    }

    // ------------------------------------------------------------------ helpers
    GameObject MakePanel(Transform parent, string name, string title, out Text body)
    {
        var bg = MakeImage(parent, name, new Color(0.05f, 0.06f, 0.08f, 0.93f), null);
        Anchor(bg.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(760f, 560f), new Vector2(0.5f, 0.5f));

        var t = MakeText(bg.rectTransform, "Title", title, 34, TextAnchor.UpperCenter);
        Anchor(t.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -26f), new Vector2(700f, 50f), new Vector2(0.5f, 1f));

        body = MakeText(bg.rectTransform, "Body", "", 26, TextAnchor.UpperLeft);
        Anchor(body.rectTransform, new Vector2(0f, 1f), new Vector2(30f, -90f), new Vector2(700f, 60f), new Vector2(0f, 1f));

        return bg.gameObject;
    }

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
        img.raycastTarget = false;          // TouchRouter does the hit testing
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
