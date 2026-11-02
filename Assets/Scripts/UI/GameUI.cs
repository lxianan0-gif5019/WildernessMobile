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

    const int InvRowCount = 10;
    readonly Text[] invLabels = new Text[InvRowCount];
    readonly GameObject[] invRows = new GameObject[InvRowCount];
    readonly string[] invRowIds = new string[InvRowCount];

    Text[] craftRowLabels;

    Image damageFlash;
    Text warning;
    Text objective;
    float flashAlpha;
    float lastHealth = -1f;

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

        if (GetComponent<CraftingController>() == null) gameObject.AddComponent<CraftingController>();
        if (GetComponent<WolfNight>() == null) gameObject.AddComponent<WolfNight>();
        if (GetComponent<AudioManager>() == null) gameObject.AddComponent<AudioManager>();
        if (GetComponent<Diagnostics>() == null) gameObject.AddComponent<Diagnostics>();
        if (GetComponent<MenuUI>() == null) gameObject.AddComponent<MenuUI>();
        if (GetComponent<Tutorial>() == null) gameObject.AddComponent<Tutorial>();
        if (GetComponent<ToolWear>() == null) gameObject.AddComponent<ToolWear>();

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
            hud.text = sb.Length == 0 ? Loc.T("hud.empty") : sb.ToString();

            var cc = CraftingController.Instance;
            if (cc != null && cc.Busy)
                hud.text += "      " + Loc.F("hud.crafting", ItemDatabase.Display(cc.active.outputId), Mathf.RoundToInt(cc.Progress * 100f));

            var tw = ToolWear.Instance;
            if (tw != null)
            {
                string best = tw.BestTool();
                if (best != "")
                    hud.text += "      " + Loc.ItemName(best) + " " + Mathf.CeilToInt(tw.Left(best)) + "/" + Mathf.CeilToInt(tw.MaxFor(best));
            }
        }

        if (prompt != null)
        {
            if (BuildSystem.Instance != null && BuildSystem.Instance.Active)
            {
                var type = BuildSystem.Instance.buildType;
                int have = Inventory.Instance != null ? Inventory.Instance.Count(type) : 0;
                prompt.text = have > 0
                    ? Loc.F("hud.build_on", ItemDatabase.Display(type), have)
                    : Loc.F("hud.build_none", ItemDatabase.Display(type));
            }
            else
            {
                prompt.text = PlayerInteractor.Instance != null ? PlayerInteractor.Instance.Prompt() : "";
            }
        }

        UpdateStatBars();
        UpdateDamageFeedback();
        UpdateFireAudio();

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

        // red hit flash: first child so the rest of the HUD draws on top of it
        damageFlash = MakeImage(root, "DamageFlash", new Color(0.7f, 0.05f, 0.05f, 0f), null);
        var frt = damageFlash.rectTransform;
        frt.anchorMin = Vector2.zero;
        frt.anchorMax = Vector2.one;
        frt.offsetMin = Vector2.zero;
        frt.offsetMax = Vector2.zero;

        // HUD line
        hud = MakeText(root, "HUD", "", 26, TextAnchor.UpperLeft);
        Anchor(hud.rectTransform, new Vector2(0f, 1f), new Vector2(24f, -18f), new Vector2(920f, 40f), new Vector2(0f, 1f));

        // wolf warning
        warning = MakeText(root, "Warning", "", 32, TextAnchor.MiddleCenter);
        Anchor(warning.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -60f), new Vector2(600f, 44f), new Vector2(0.5f, 1f));
        warning.color = new Color(1f, 0.35f, 0.30f);

        // --- survival bars, stacked under the HUD line
        MakeStatBar(root, 0, "HP", new Color(0.85f, 0.22f, 0.22f), -64f);
        MakeStatBar(root, 1, "FOOD", new Color(0.90f, 0.60f, 0.15f), -104f);
        MakeStatBar(root, 2, "WATER", new Color(0.22f, 0.55f, 0.90f), -144f);
        MakeStatBar(root, 3, "STAMINA", new Color(0.35f, 0.80f, 0.35f), -184f);

        // tutorial objective, under the bars
        objective = MakeText(root, "Objective", "", 24, TextAnchor.UpperLeft);
        Anchor(objective.rectTransform, new Vector2(0f, 1f), new Vector2(24f, -228f), new Vector2(760f, 34f), new Vector2(0f, 1f));
        objective.color = new Color(1f, 0.93f, 0.55f);

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
        MakeButton(root, "PauseBtn", "II", new Vector2(1f, 0.98f), 110f, () => { if (MenuUI.Instance != null) MenuUI.Instance.OpenPause(); });

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

    void UpdateDamageFeedback()
    {
        var st = PlayerStats.Instance;
        if (st != null)
        {
            if (lastHealth < 0f) lastHealth = st.health;
            if (st.health < lastHealth - 0.01f) flashAlpha = Mathf.Min(0.5f, flashAlpha + 0.4f);
            lastHealth = st.health;
        }

        if (flashAlpha > 0f)
        {
            flashAlpha = Mathf.Max(0f, flashAlpha - Time.deltaTime * 1.6f);
            if (damageFlash != null) damageFlash.color = new Color(0.7f, 0.05f, 0.05f, flashAlpha);
        }

        if (warning != null)
        {
            var w = WolfNight.Instance;
            if (w != null && w.AliveCount > 0) warning.text = Loc.F("hud.wolves", w.AliveCount);
            else warning.text = "";
        }

        if (objective != null)
        {
            var t = Tutorial.Instance;
            objective.text = t != null ? Loc.T("tut.title") + ":  " + t.Objective() : "";
            objective.color = (t != null && t.Done) ? new Color(0.6f, 0.95f, 0.6f) : new Color(1f, 0.93f, 0.55f);
        }
    }

    void UpdateFireAudio()
    {
        if (AudioManager.Instance == null || PlayerStats.Instance == null) return;

        var pos = PlayerStats.Instance.transform.position;
        var fire = Campfire.Nearest(pos);
        bool near = fire != null && fire.IsBurning && Vector3.Distance(fire.transform.position, pos) < 15f;
        AudioManager.Instance.SetFireLoop(near, near ? fire.transform : null);
    }

    void UpdateStatBars()
    {
        var s = PlayerStats.Instance;
        if (s == null) return;

        SetBar(0, s.health, s.maxHealth, Loc.T("stat.hp"));
        SetBar(1, s.hunger, s.maxHunger, Loc.T("stat.food"));
        SetBar(2, s.thirst, s.maxThirst, Loc.T("stat.water"));
        SetBar(3, s.stamina, s.maxStamina, Loc.T("stat.stamina"));
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
        for (int i = 0; i < InvRowCount; i++)
        {
            var img = MakeImage(panel, "Row" + i, new Color(1f, 1f, 1f, 0.10f), circle);
            Anchor(img.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -104f - i * 44f), new Vector2(640f, 40f), new Vector2(0.5f, 1f));

            var label = MakeText(img.rectTransform, "Label", "", 22, TextAnchor.MiddleLeft);
            Anchor(label.rectTransform, new Vector2(0f, 0.5f), new Vector2(26f, 0f), new Vector2(580f, 38f), new Vector2(0f, 0.5f));

            invLabels[i] = label;
            invRows[i] = img.gameObject;
            img.gameObject.SetActive(false);

            int captured = i;
            router.RegisterButton(img.rectTransform, () => OnInventoryRow(captured));
        }
    }

    void OnInventoryRow(int index)
    {
        if (index < 0 || index >= InvRowCount) return;
        var id = invRowIds[index];
        if (!string.IsNullOrEmpty(id)) OnItemClicked(id);
    }

    void MakeCraftRows(Transform panel)
    {
        craftRowLabels = new Text[RecipeCatalog.All.Count];
        for (int i = 0; i < RecipeCatalog.All.Count; i++)
        {
            var r = RecipeCatalog.All[i];
            var img = MakeImage(panel, "Craft" + r.outputId, new Color(1f, 1f, 1f, 0.12f), circle);
            Anchor(img.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -100f - i * 40f), new Vector2(700f, 36f), new Vector2(0.5f, 1f));

            var label = MakeText(img.rectTransform, "Label", "", 20, TextAnchor.MiddleLeft);
            Anchor(label.rectTransform, new Vector2(0f, 0.5f), new Vector2(18f, 0f), new Vector2(670f, 34f), new Vector2(0f, 0.5f));

            craftRowLabels[i] = label;

            string id = r.outputId;
            router.RegisterButton(img.rectTransform, () => Craft(id));
        }
    }

    string RecipeLine(CraftingRecipe r, string reason)
    {
        var sb = new System.Text.StringBuilder();
        sb.Append(ItemDatabase.Display(r.outputId).PadRight(13));
        for (int i = 0; i < r.inputIds.Length; i++)
        {
            if (i > 0) sb.Append(" + ");
            sb.Append(r.inputCounts[i]).Append(' ').Append(ItemDatabase.Display(r.inputIds[i]));
        }
        if (!string.IsNullOrEmpty(r.requiresStation))
            sb.Append("   [").Append(RecipeCatalog.StationLabel(r.requiresStation)).Append(']');
        sb.Append("   ").Append(r.seconds.ToString("F0")).Append("s");
        if (!string.IsNullOrEmpty(reason)) sb.Append("   <- ").Append(reason);
        return sb.ToString();
    }

    // ------------------------------------------------------------------ actions
    public void Craft(string outputId)
    {
        var r = RecipeCatalog.Find(outputId);
        if (r == null || CraftingController.Instance == null) return;
        CraftingController.Instance.StartCraft(r);
        RefreshCraft();
    }

    public void OnItemClicked(string id)
    {
        if (id == "cooked_meat")
        {
            if (Inventory.Instance != null && Inventory.Instance.Spend("cooked_meat", 1) && PlayerStats.Instance != null)
            {
                PlayerStats.Instance.Eat(45f, 0f);
                Debug.Log("[UI] ate cooked meat");
            }
            return;
        }

        if (id == "wall" || id == "campfire" || id == "workbench" || id == "furnace" || id == "bed" || id == "hay_bed")
        {
            if (BuildSystem.Instance != null) BuildSystem.Instance.EnterBuildMode(id);
            invOpen = false; if (invPanel != null) invPanel.SetActive(false);
            craftOpen = false; if (craftPanel != null) craftPanel.SetActive(false);
            RefreshCursor();
            Debug.Log("[UI] took a " + ItemDatabase.Display(id) + " out of the backpack - place it");
            return;
        }

        if (id == "meat")
        {
            if (Inventory.Instance != null && Inventory.Instance.Spend("meat", 1) && PlayerStats.Instance != null)
            {
                PlayerStats.Instance.Eat(26f, 0f);
                Debug.Log("[UI] ate raw meat");
            }
            return;
        }

        if (id == "berry")
        {
            if (Inventory.Instance != null && Inventory.Instance.Spend("berry", 1) && PlayerStats.Instance != null)
            {
                PlayerStats.Instance.Eat(12f, 6f);
                Debug.Log("[UI] ate a berry");
            }
            return;
        }

        if (id == "wall" || id == "campfire" || id == "workbench" || id == "furnace")
        {
            // handled above
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

        int row = 0;
        var list = Inventory.Instance.items;
        for (int i = 0; i < list.Count && row < InvRowCount; i++)
        {
            var s = list[i];
            if (s.count <= 0) continue;
            invRowIds[row] = s.id;
            invRows[row].SetActive(true);
            invLabels[row].text = ItemDatabase.Display(s.id).PadRight(16) + " x" + s.count;
            row++;
        }

        for (int i = row; i < InvRowCount; i++)
        {
            invRowIds[i] = null;
            invRows[i].SetActive(false);
        }

        if (invText != null)
            invText.text = row == 0 ? Loc.T("hud.nothing") : Loc.T("hud.take_out");
    }

    void RefreshCraft()
    {
        if (craftRowLabels == null) return;

        var cc = CraftingController.Instance;
        for (int i = 0; i < RecipeCatalog.All.Count && i < craftRowLabels.Length; i++)
        {
            var r = RecipeCatalog.All[i];
            string reason = cc != null ? cc.BlockReason(r) : "";
            craftRowLabels[i].text = RecipeLine(r, reason);
            craftRowLabels[i].color = string.IsNullOrEmpty(reason) ? Color.white : new Color(1f, 1f, 1f, 0.45f);
        }

        if (craftText != null && Inventory.Instance != null)
        {
            if (cc != null && cc.Busy)
                craftText.text = "Crafting " + cc.active.displayName + "   " + Mathf.RoundToInt(cc.Progress * 100f) + "%";
            else
                craftText.text = "Wood " + Inventory.Instance.Count("wood")
                               + "   Stone " + Inventory.Instance.Count("stone")
                               + "   Stick " + Inventory.Instance.Count("stick")
                               + "   Iron " + Inventory.Instance.Count("iron_ingot");
        }
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
