// 职责：HUD 门面（状态条/按钮/提示/Toast），调度各面板
// 依赖：Inventory、PlayerStats、CraftingController、ToolWear、Tutorial、MenuUI
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

        // NOTE: Cinemachine rig (CameraRig) is installed but disabled for now: the
        // runtime-built vcam stays at the follow target instead of the arm offset,
        // so the hand-written ThirdPersonCamera remains the active camera.
        // Revisit with a scene-authored vcam instead of building it at runtime.

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
        UpdateToast();
        UpdateFps();

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

        // Everything except the damage flash lives inside the safe area, so nothing
        // hides under a notch or a rounded corner.
        var safeAreaGo = new GameObject("SafeArea");
        var safeArea = safeAreaGo.AddComponent<RectTransform>();
        safeArea.SetParent(root, false);
        Rect safe = Screen.safeArea;
        float sw = Mathf.Max(1f, Screen.width);
        float sh = Mathf.Max(1f, Screen.height);
        safeArea.anchorMin = new Vector2(safe.xMin / sw, safe.yMin / sh);
        safeArea.anchorMax = new Vector2(safe.xMax / sw, safe.yMax / sh);
        safeArea.offsetMin = Vector2.zero;
        safeArea.offsetMax = Vector2.zero;

        var content = safeArea;

        // HUD line
        hud = MakeText(content, "HUD", "", 30, TextAnchor.UpperLeft);
        Anchor(hud.rectTransform, new Vector2(0f, 1f), new Vector2(28f, -20f), new Vector2(1100f, 44f), new Vector2(0f, 1f));

        // wolf warning
        warning = MakeText(content, "Warning", "", 36, TextAnchor.MiddleCenter);
        Anchor(warning.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -70f), new Vector2(640f, 48f), new Vector2(0.5f, 1f));
        warning.color = new Color(1f, 0.35f, 0.30f);

        // --- survival bars
        MakeStatBar(content, 0, Loc.T("stat.hp"), new Color(0.85f, 0.22f, 0.22f), -76f);
        MakeStatBar(content, 1, Loc.T("stat.food"), new Color(0.90f, 0.60f, 0.15f), -126f);
        MakeStatBar(content, 2, Loc.T("stat.water"), new Color(0.22f, 0.55f, 0.90f), -176f);
        MakeStatBar(content, 3, Loc.T("stat.stamina"), new Color(0.35f, 0.80f, 0.35f), -226f);

        // tutorial objective
        objective = MakeText(content, "Objective", "", 28, TextAnchor.UpperLeft);
        Anchor(objective.rectTransform, new Vector2(0f, 1f), new Vector2(28f, -276f), new Vector2(820f, 38f), new Vector2(0f, 1f));
        objective.color = new Color(1f, 0.93f, 0.55f);

        // interactive prompt
        prompt = MakeText(content, "Prompt", "", 34, TextAnchor.MiddleCenter);
        Anchor(prompt.rectTransform, new Vector2(0.5f, 0f), new Vector2(0f, 270f), new Vector2(980f, 50f), new Vector2(0.5f, 0f));

        // --- joystick: generous invisible pad, visible stick inside it
        var stickPad = MakeImage(content, "JoystickPad", new Color(0f, 0f, 0f, 0f), null);
        Anchor(stickPad.rectTransform, new Vector2(0f, 0f), new Vector2(400f, 400f), new Vector2(460f, 460f), new Vector2(0.5f, 0.5f));

        var stickBg = MakeImage(stickPad.rectTransform, "JoystickArea", new Color(1f, 1f, 1f, 0.14f), circle);
        Anchor(stickBg.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(280f, 280f), new Vector2(0.5f, 0.5f));

        var knob = MakeImage(stickPad.rectTransform, "Knob", new Color(1f, 1f, 1f, 0.60f), circle);
        Anchor(knob.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(120f, 120f), new Vector2(0.5f, 0.5f));

        router.stickArea = stickPad.rectTransform;
        router.stickKnob = knob.rectTransform;
        router.stickRadius = 110f;

        // --- action buttons: bigger, further from the edge, Chinese labels
        MakeButton(content, "UseBtn", "使用", new Vector2(1f, 0.30f), 180f, () => InputHub.InteractDown = true);
        MakeButton(content, "JumpBtn", "跳", new Vector2(1f, 0.12f), 170f, () => { InputHub.JumpHeld = true; InputHub.JumpDown = true; });
        MakeButton(content, "BagBtn", "背包", new Vector2(1f, 0.86f), 170f, () => InputHub.InventoryDown = true);
        MakeButton(content, "CraftBtn", "制作", new Vector2(1f, 0.68f), 170f, () => InputHub.CraftDown = true);
        MakeButton(content, "BuildBtn", "建造", new Vector2(1f, 0.50f), 170f, () => InputHub.BuildDown = true);
        MakeButton(content, "PauseBtn", "❚❚", new Vector2(1f, 0.99f), 120f, () => { if (MenuUI.Instance != null) MenuUI.Instance.OpenPause(); });

        // --- inventory panel
        invPanel = MakePanel(content, "InventoryPanel", Loc.T("ui.inventory"), new Vector2(1000f, 840f), out invText,
                             () => { invOpen = false; invPanel.SetActive(false); RefreshCursor(); });
        invPanel.SetActive(false);
        MakeInventoryRows(invPanel.transform);

        // --- crafting panel
        craftPanel = MakePanel(content, "CraftPanel", Loc.T("ui.craft"), new Vector2(1460f, 900f), out craftText,
                               () => { craftOpen = false; craftPanel.SetActive(false); RefreshCursor(); });
        craftPanel.SetActive(false);
        MakeCraftRows(craftPanel.transform);

        // toast feedback line
        toast = MakeText(content, "Toast", "", 30, TextAnchor.MiddleCenter);
        Anchor(toast.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -330f), new Vector2(900f, 44f), new Vector2(0.5f, 1f));
        toast.color = new Color(1f, 0.95f, 0.6f);

        // optional FPS readout (F3)
        fpsText = MakeText(content, "FPS", "", 22, TextAnchor.UpperRight);
        Anchor(fpsText.rectTransform, new Vector2(1f, 1f), new Vector2(-28f, -20f), new Vector2(220f, 32f), new Vector2(1f, 1f));
        fpsText.color = new Color(0.7f, 0.95f, 0.7f);
        fpsText.gameObject.SetActive(false);

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

    Text toast;
    float toastUntil;
    Text fpsText;
    float fpsAccum;
    int fpsFrames;

    /// <summary>Short feedback line, e.g. "Wood +1" or "Need a Workbench".</summary>
    public void ShowToast(string message)
    {
        if (toast == null) return;
        toast.text = message;
        toastUntil = Time.unscaledTime + 2.2f;
        var c = toast.color;
        c.a = 1f;
        toast.color = c;
    }

    void UpdateFps()
    {
        if (fpsText == null) return;

        if (Input.GetKeyDown(KeyCode.F3)) fpsText.gameObject.SetActive(!fpsText.gameObject.activeSelf);
        if (!fpsText.gameObject.activeSelf) return;

        fpsAccum += Time.unscaledDeltaTime;
        fpsFrames++;
        if (fpsAccum >= 0.5f)
        {
            float fps = fpsFrames / fpsAccum;
            fpsText.text = Mathf.RoundToInt(fps) + " FPS   " + (1000f / Mathf.Max(1f, fps)).ToString("F1") + " ms";
            fpsAccum = 0f;
            fpsFrames = 0;
        }
    }

    void UpdateToast()
    {
        if (toast == null || toast.text == "") return;
        float left = toastUntil - Time.unscaledTime;
        if (left <= 0f) { toast.text = ""; return; }
        var c = toast.color;
        c.a = Mathf.Clamp01(left / 0.6f);
        toast.color = c;
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

    void MakeButton(Transform parent, string name, string label, Vector2 anchor, float size, Action action)
    {
        var img = MakeImage(parent, name, new Color(1f, 1f, 1f, 0.20f), circle);
        var rt = img.rectTransform;
        rt.anchorMin = anchor;
        rt.anchorMax = anchor;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(size, size);
        rt.anchoredPosition = new Vector2(-150f, 0f);          // keep clear of the edge

        var t = MakeText(img.rectTransform, "Label", label, Mathf.RoundToInt(size * 0.22f), TextAnchor.MiddleCenter);
        Anchor(t.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(size, size), new Vector2(0.5f, 0.5f));

        router.RegisterButton(img.rectTransform, action);
    }

    void MakeInventoryRows(Transform panel)
    {
        for (int i = 0; i < InvRowCount; i++)
        {
            var img = MakeImage(panel, "Row" + i, new Color(1f, 1f, 1f, 0.12f), circle);
            Anchor(img.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -120f - i * 54f), new Vector2(700f, 50f), new Vector2(0.5f, 1f));

            var label = MakeText(img.rectTransform, "Label", "", 27, TextAnchor.MiddleLeft);
            Anchor(label.rectTransform, new Vector2(0f, 0.5f), new Vector2(28f, 0f), new Vector2(640f, 48f), new Vector2(0f, 0.5f));

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
        const int perColumn = 7;

        for (int i = 0; i < RecipeCatalog.All.Count; i++)
        {
            var r = RecipeCatalog.All[i];
            int col = i / perColumn;
            int row = i % perColumn;

            var img = MakeImage(panel, "Craft" + r.outputId, new Color(1f, 1f, 1f, 0.12f), circle);
            Anchor(img.rectTransform, new Vector2(0.5f, 1f),
                   new Vector2(col == 0 ? -350f : 350f, -130f - row * 68f),
                   new Vector2(660f, 60f), new Vector2(0.5f, 1f));

            var label = MakeText(img.rectTransform, "Label", "", 24, TextAnchor.MiddleLeft);
            Anchor(label.rectTransform, new Vector2(0f, 0.5f), new Vector2(20f, 0f), new Vector2(620f, 56f), new Vector2(0f, 0.5f));

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

        string reason = CraftingController.Instance.BlockReason(r);
        if (!string.IsNullOrEmpty(reason))
        {
            ShowToast(reason);
            return;
        }

        CraftingController.Instance.StartCraft(r);
        ShowToast(Loc.F("ui.crafting_start", ItemDatabase.Display(r.outputId)));
        RefreshCraft();
    }

    public void OnItemClicked(string id)
    {
        if (id == ItemIds.CookedMeat)
        {
            if (Inventory.Instance != null && Inventory.Instance.Spend(ItemIds.CookedMeat, 1) && PlayerStats.Instance != null)
            {
                PlayerStats.Instance.Eat(45f, 0f);
                ShowToast("+" + 45 + " " + Loc.T("stat.food"));
                Debug.Log("[UI] ate cooked meat");
            }
            return;
        }

        if (id == ItemIds.Wall || id == ItemIds.Campfire || id == ItemIds.Workbench || id == ItemIds.Furnace || id == ItemIds.Bed || id == ItemIds.HayBed)
        {
            if (BuildSystem.Instance != null) BuildSystem.Instance.EnterBuildMode(id);
            invOpen = false; if (invPanel != null) invPanel.SetActive(false);
            craftOpen = false; if (craftPanel != null) craftPanel.SetActive(false);
            RefreshCursor();
            Debug.Log("[UI] took a " + ItemDatabase.Display(id) + " out of the backpack - place it");
            return;
        }

        if (id == ItemIds.Meat)
        {
            if (Inventory.Instance != null && Inventory.Instance.Spend(ItemIds.Meat, 1) && PlayerStats.Instance != null)
            {
                PlayerStats.Instance.Eat(26f, 0f);
                ShowToast("+" + 26 + " " + Loc.T("stat.food"));
                Debug.Log("[UI] ate raw meat");
            }
            return;
        }

        if (id == ItemIds.Berry)
        {
            if (Inventory.Instance != null && Inventory.Instance.Spend(ItemIds.Berry, 1) && PlayerStats.Instance != null)
            {
                PlayerStats.Instance.Eat(12f, 6f);
                Debug.Log("[UI] ate a berry");
            }
            return;
        }

        if (id == ItemIds.Wall || id == ItemIds.Campfire || id == ItemIds.Workbench || id == ItemIds.Furnace)
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
                craftText.text = "Wood " + Inventory.Instance.Count(ItemIds.Wood)
                               + "   Stone " + Inventory.Instance.Count(ItemIds.Stone)
                               + "   Stick " + Inventory.Instance.Count(ItemIds.Stick)
                               + "   Iron " + Inventory.Instance.Count(ItemIds.IronIngot);
        }
    }

    // ------------------------------------------------------------------ helpers
    GameObject MakePanel(Transform parent, string name, string title, Vector2 size, out Text body, Action onClose)
    {
        var bg = MakeImage(parent, name, new Color(0.05f, 0.06f, 0.08f, 0.95f), null);
        Anchor(bg.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, size, new Vector2(0.5f, 0.5f));

        var t = MakeText(bg.rectTransform, "Title", title, 44, TextAnchor.UpperCenter);
        Anchor(t.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -26f), new Vector2(size.x - 220f, 62f), new Vector2(0.5f, 1f));

        // big close button: the obvious way out on a phone
        var close = MakeImage(bg.rectTransform, "Close", new Color(1f, 1f, 1f, 0.20f), circle);
        Anchor(close.rectTransform, new Vector2(1f, 1f), new Vector2(-76f, -76f), new Vector2(100f, 100f), new Vector2(0.5f, 0.5f));
        var cl = MakeText(close.rectTransform, "L", "✕", 46, TextAnchor.MiddleCenter);
        Anchor(cl.rectTransform, Vector2.zero, Vector2.zero, new Vector2(100f, 100f), new Vector2(0.5f, 0.5f));
        router.RegisterButton(close.rectTransform, onClose);

        body = MakeText(bg.rectTransform, "Body", "", 28, TextAnchor.MiddleCenter);
        Anchor(body.rectTransform, new Vector2(0.5f, 0f), new Vector2(0f, 26f), new Vector2(size.x - 140f, 46f), new Vector2(0.5f, 0f));

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
