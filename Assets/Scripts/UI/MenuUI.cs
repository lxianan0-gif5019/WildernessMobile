// 职责：主菜单/暂停/设置/存档位
// 依赖：SaveSystem、GameSettings、Loc、TouchRouter
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Main menu, pause menu, settings and save slots. Built at runtime like the HUD.
// Buttons go through TouchRouter (layer 1) so touch and mouse both work.
public class MenuUI : MonoBehaviour
{
    public static MenuUI Instance;

    public bool IsOpen { get; private set; }

    Canvas canvas;
    Font font;

    GameObject mainPanel, pausePanel, settingsPanel, slotPanel;

    Text masterValue, sfxValue, lookValue, stickValue, qualityValue, invertValue, languageValue;
    Text slotTitle;
    readonly List<Text> slotLabels = new List<Text>();
    bool inSettings;
    bool saveMode;
    bool started;

    class Btn
    {
        public Text label;
        public string key;
    }

    readonly List<Btn> localizedButtons = new List<Btn>();

    const int MenuLayer = 1;

    void Awake()
    {
        Instance = this;
        GameSettings.Load();
    }

    void Start()
    {
        font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (font == null) font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        Build();
        OpenMain();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape) && started)
        {
            if (IsOpen) Close();
            else OpenPause();
        }
    }

    // ------------------------------------------------------------------ build
    void Build()
    {
        var go = new GameObject("MenuCanvas");
        canvas = go.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 50;

        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.zero;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(Screen.width, Screen.height);
        rt.localPosition = new Vector3(Screen.width * 0.5f, Screen.height * 0.5f, 0f);

        var scaler = go.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        mainPanel = BuildMainMenu();
        pausePanel = BuildPauseMenu();
        settingsPanel = BuildSettings();
        slotPanel = BuildSlots();

        pausePanel.SetActive(false);
        settingsPanel.SetActive(false);
        slotPanel.SetActive(false);
    }

    GameObject Panel(string name, Color dim)
    {
        var bg = MakeImage(canvas.transform, name, dim, null);
        var rt = bg.rectTransform;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        return bg.gameObject;
    }

    GameObject Box(Transform parent, string locKey, float width, float height)
    {
        var box = MakeImage(parent, "Box", new Color(0.07f, 0.08f, 0.10f, 0.97f), null);
        var rt = box.rectTransform;
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(width, height);
        rt.anchoredPosition = Vector2.zero;

        var t = MakeText(box.rectTransform, "Title", Loc.T(locKey), 46, TextAnchor.UpperCenter);
        Anchor(t.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -30f), new Vector2(width - 40f, 60f), new Vector2(0.5f, 1f));
        RegisterLocalized(t, locKey);

        return box.gameObject;
    }

    void Row(Transform parent, string locKey, float y, Action onClick, float width = 520f)
    {
        var img = MakeImage(parent, "Btn_" + locKey, new Color(1f, 1f, 1f, 0.14f), null);
        Anchor(img.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, y), new Vector2(width, 76f), new Vector2(0.5f, 1f));

        var text = MakeText(img.rectTransform, "Label", Loc.T(locKey), 30, TextAnchor.MiddleCenter);
        text.rectTransform.anchorMin = Vector2.zero;
        text.rectTransform.anchorMax = Vector2.one;
        text.rectTransform.offsetMin = Vector2.zero;
        text.rectTransform.offsetMax = Vector2.zero;
        RegisterLocalized(text, locKey);

        if (onClick != null)
            TouchRouter.Instance.RegisterButton(img.rectTransform, onClick, MenuLayer);
    }

    void RegisterLocalized(Text t, string key)
    {
        localizedButtons.Add(new Btn { label = t, key = key });
    }

    // ------------------------------------------------------------- main menu
    GameObject BuildMainMenu()
    {
        var panel = Panel("MainMenu", new Color(0.02f, 0.03f, 0.05f, 0.98f));
        var box = Box(panel.transform, "menu.title", 760f, 720f);

        Row(box.transform, "menu.continue", -140f, () => { started = true; Close(); });
        Row(box.transform, "menu.newgame", -236f, NewGame);
        Row(box.transform, "menu.load", -332f, () => OpenSlots(false));
        Row(box.transform, "menu.settings", -428f, () => OpenSettings(false));
        Row(box.transform, "menu.quit", -524f, QuitGame);

        // platform-appropriate controls hint
        string hintKey = Application.isMobilePlatform ? "menu.hint_mobile" : "menu.hint";
        var hint = MakeText(box.transform, "Hint", Loc.T(hintKey), 22, TextAnchor.MiddleCenter);
        Anchor(hint.rectTransform, new Vector2(0.5f, 0f), new Vector2(0f, 28f), new Vector2(720f, 60f), new Vector2(0.5f, 0f));
        hint.color = new Color(1f, 1f, 1f, 0.55f);
        RegisterLocalized(hint, hintKey);

        return panel;
    }

    // ------------------------------------------------------------ pause menu
    GameObject BuildPauseMenu()
    {
        var panel = Panel("PauseMenu", new Color(0.02f, 0.03f, 0.05f, 0.80f));
        var box = Box(panel.transform, "menu.paused", 700f, 700f);

        Row(box.transform, "menu.resume", -140f, () => { started = true; Close(); });
        Row(box.transform, "menu.save", -236f, () => OpenSlots(true));
        Row(box.transform, "menu.load", -332f, () => OpenSlots(false));
        Row(box.transform, "menu.settings", -428f, () => OpenSettings(true));
        Row(box.transform, "menu.mainmenu", -524f, OpenMain);
        Row(box.transform, "menu.quit", -620f, QuitGame);

        return panel;
    }

    // -------------------------------------------------------------- settings
    GameObject BuildSettings()
    {
        var panel = Panel("Settings", new Color(0.02f, 0.03f, 0.05f, 0.94f));
        var box = Box(panel.transform, "menu.settings", 820f, 820f);

        masterValue = SettingRow(box.transform, "set.master", -130f,
            () => { GameSettings.MasterVolume = Mathf.Clamp01(GameSettings.MasterVolume + 0.1f); Refresh(); },
            () => { GameSettings.MasterVolume = Mathf.Clamp01(GameSettings.MasterVolume - 0.1f); Refresh(); });

        sfxValue = SettingRow(box.transform, "set.sfx", -200f,
            () => { GameSettings.SfxVolume = Mathf.Clamp01(GameSettings.SfxVolume + 0.1f); Refresh(); },
            () => { GameSettings.SfxVolume = Mathf.Clamp01(GameSettings.SfxVolume - 0.1f); Refresh(); });

        lookValue = SettingRow(box.transform, "set.look", -270f,
            () => { GameSettings.LookSensitivity = Mathf.Clamp(GameSettings.LookSensitivity * 1.25f, 0.02f, 0.6f); Refresh(); },
            () => { GameSettings.LookSensitivity = Mathf.Clamp(GameSettings.LookSensitivity / 1.25f, 0.02f, 0.6f); Refresh(); });

        stickValue = SettingRow(box.transform, "set.stick", -340f,
            () => { GameSettings.StickRadius = Mathf.Clamp(GameSettings.StickRadius + 12f, 50f, 160f); Refresh(); },
            () => { GameSettings.StickRadius = Mathf.Clamp(GameSettings.StickRadius - 12f, 50f, 160f); Refresh(); });

        qualityValue = SettingRow(box.transform, "set.quality", -410f,
            () => { GameSettings.Quality = Mathf.Min(2, GameSettings.Quality + 1); Refresh(); },
            () => { GameSettings.Quality = Mathf.Max(0, GameSettings.Quality - 1); Refresh(); });

        invertValue = SettingRow(box.transform, "set.invert", -480f,
            () => { GameSettings.InvertLook = !GameSettings.InvertLook; Refresh(); },
            () => { GameSettings.InvertLook = !GameSettings.InvertLook; Refresh(); });

        languageValue = SettingRow(box.transform, "menu.language", -550f,
            () => { CycleLanguage(1); },
            () => { CycleLanguage(-1); });

        Row(box.transform, "menu.back", -680f, () => { GameSettings.Save(); GameSettings.Apply(); BackToPrevious(); });

        Refresh();
        return panel;
    }

    Text SettingRow(Transform parent, string locKey, float y, Action plus, Action minus)
    {
        var bg = MakeImage(parent, "Row_" + locKey, new Color(1f, 1f, 1f, 0.06f), null);
        Anchor(bg.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, y), new Vector2(700f, 64f), new Vector2(0.5f, 1f));

        var name = MakeText(bg.rectTransform, "Name", Loc.T(locKey), 26, TextAnchor.MiddleLeft);
        Anchor(name.rectTransform, new Vector2(0f, 0.5f), new Vector2(24f, 0f), new Vector2(360f, 60f), new Vector2(0f, 0.5f));
        RegisterLocalized(name, locKey);

        var minusBtn = MakeImage(bg.rectTransform, "Minus", new Color(1f, 1f, 1f, 0.16f), null);
        Anchor(minusBtn.rectTransform, new Vector2(1f, 0.5f), new Vector2(-250f, 0f), new Vector2(64f, 52f), new Vector2(0.5f, 0.5f));
        var minusText = MakeText(minusBtn.rectTransform, "L", "-", 34, TextAnchor.MiddleCenter);
        Anchor(minusText.rectTransform, Vector2.zero, Vector2.zero, new Vector2(64f, 52f), new Vector2(0.5f, 0.5f));
        TouchRouter.Instance.RegisterButton(minusBtn.rectTransform, minus, MenuLayer);

        var value = MakeText(bg.rectTransform, "Value", "", 26, TextAnchor.MiddleCenter);
        Anchor(value.rectTransform, new Vector2(1f, 0.5f), new Vector2(-150f, 0f), new Vector2(140f, 60f), new Vector2(0.5f, 0.5f));

        var plusBtn = MakeImage(bg.rectTransform, "Plus", new Color(1f, 1f, 1f, 0.16f), null);
        Anchor(plusBtn.rectTransform, new Vector2(1f, 0.5f), new Vector2(-50f, 0f), new Vector2(64f, 52f), new Vector2(0.5f, 0.5f));
        var plusText = MakeText(plusBtn.rectTransform, "L", "+", 34, TextAnchor.MiddleCenter);
        Anchor(plusText.rectTransform, Vector2.zero, Vector2.zero, new Vector2(64f, 52f), new Vector2(0.5f, 0.5f));
        TouchRouter.Instance.RegisterButton(plusBtn.rectTransform, plus, MenuLayer);

        return value;
    }

    // ----------------------------------------------------------------- slots
    GameObject BuildSlots()
    {
        var panel = Panel("Slots", new Color(0.02f, 0.03f, 0.05f, 0.94f));
        var box = Box(panel.transform, "menu.load", 760f, 620f);

        slotTitle = box.transform.Find("Title").GetComponent<Text>();

        for (int i = 0; i < 3; i++)
        {
            int slotIndex = i + 1;
            var img = MakeImage(box.transform, "Slot" + slotIndex, new Color(1f, 1f, 1f, 0.12f), null);
            Anchor(img.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -140f - i * 96f), new Vector2(600f, 76f), new Vector2(0.5f, 1f));

            var label = MakeText(img.rectTransform, "Label", "", 26, TextAnchor.MiddleCenter);
            label.rectTransform.anchorMin = Vector2.zero;
            label.rectTransform.anchorMax = Vector2.one;
            label.rectTransform.offsetMin = Vector2.zero;
            label.rectTransform.offsetMax = Vector2.zero;
            slotLabels.Add(label);

            TouchRouter.Instance.RegisterButton(img.rectTransform, () => OnSlotClicked(slotIndex), MenuLayer);
        }

        Row(box.transform, "menu.back", -470f, () => { slotPanel.SetActive(false); BackToPrevious(); });

        return panel;
    }

    void OnSlotClicked(int index)
    {
        var save = SaveSystem.Instance;
        if (save == null) return;

        if (saveMode)
        {
            // overwriting an existing save asks for a second tap
            if (SaveSystem.SlotExists(index) && pendingOverwriteSlot != index)
            {
                pendingOverwriteSlot = index;
                pendingOverwriteAt = Time.unscaledTime;
                RefreshSlots();
                return;
            }
        }
        else
        {
            if (!SaveSystem.SlotExists(index))
            {
                Debug.Log("[Menu] slot " + index + " is empty");
                return;
            }
            save.LoadFromSlot(index);
            started = true;
            Close();
            Debug.Log("[Menu] loaded slot " + index);
            return;
        }

        save.SaveToSlot(index);
        pendingOverwriteSlot = 0;
        RefreshSlots();
        Debug.Log("[Menu] saved to slot " + index);
    }

    int pendingOverwriteSlot;
    float pendingOverwriteAt;

    void OpenSlots(bool saving)
    {
        saveMode = saving;
        pendingOverwriteSlot = 0;
        slotTitle.text = Loc.T(saving ? "menu.save" : "menu.load");
        RefreshSlots();
        mainPanel.SetActive(false);
        pausePanel.SetActive(false);
        settingsPanel.SetActive(false);
        slotPanel.SetActive(true);
        SetOpen(true);
    }

    void RefreshSlots()
    {
        for (int i = 0; i < slotLabels.Count; i++)
        {
            int slotIndex = i + 1;
            bool exists = SaveSystem.SlotExists(slotIndex);

            if (pendingOverwriteSlot == slotIndex)
            {
                slotLabels[i].text = Loc.T("menu.slot") + " " + slotIndex + "   -   " + Loc.T("menu.confirm_overwrite");
                slotLabels[i].color = new Color(1f, 0.75f, 0.3f);
                continue;
            }

            string state;
            if (!exists) state = Loc.T("menu.empty");
            else state = SaveSystem.SlotTime(slotIndex).ToString("MM-dd HH:mm");

            slotLabels[i].text = Loc.T("menu.slot") + " " + slotIndex + "   -   " + state;
            slotLabels[i].color = exists ? Color.white : new Color(1f, 1f, 1f, 0.5f);
        }
    }

    // ------------------------------------------------------------------ state
    void CycleLanguage(int dir)
    {
        GameSettings.Language = (GameSettings.Language + dir + 2) % 2;
        Loc.Current = GameSettings.Language == 1 ? Loc.Lang.Chinese : Loc.Lang.English;
        GameSettings.Save();
        RefreshLanguage();
        Refresh();
    }

    void RefreshLanguage()
    {
        for (int i = 0; i < localizedButtons.Count; i++)
        {
            var b = localizedButtons[i];
            if (b.label != null) b.label.text = Loc.T(b.key);
        }
        if (slotTitle != null && slotPanel.activeSelf)
            slotTitle.text = Loc.T(saveMode ? "menu.save" : "menu.load");
    }

    void Refresh()
    {
        if (masterValue != null) masterValue.text = Mathf.RoundToInt(GameSettings.MasterVolume * 100f) + "%";
        if (sfxValue != null) sfxValue.text = Mathf.RoundToInt(GameSettings.SfxVolume * 100f) + "%";
        if (lookValue != null) lookValue.text = (GameSettings.LookSensitivity / 0.085f).ToString("F2") + "x";
        if (stickValue != null) stickValue.text = Mathf.RoundToInt(GameSettings.StickRadius) + "";
        if (qualityValue != null) qualityValue.text = Loc.QualityName(GameSettings.Quality);
        if (invertValue != null) invertValue.text = GameSettings.InvertLook ? Loc.T("set.on") : Loc.T("set.off");
        if (languageValue != null) languageValue.text = GameSettings.Language == 1 ? "中文" : "English";

        GameSettings.Apply();
    }

    public void OpenMain()
    {
        started = false;
        mainPanel.SetActive(true);
        pausePanel.SetActive(false);
        settingsPanel.SetActive(false);
        slotPanel.SetActive(false);
        SetOpen(true);
    }

    public void OpenPause()
    {
        inSettings = false;
        mainPanel.SetActive(false);
        pausePanel.SetActive(true);
        settingsPanel.SetActive(false);
        slotPanel.SetActive(false);
        SetOpen(true);
    }

    void OpenSettings(bool fromPause)
    {
        inSettings = true;
        cameFromPause = fromPause;
        mainPanel.SetActive(false);
        pausePanel.SetActive(false);
        slotPanel.SetActive(false);
        settingsPanel.SetActive(true);
        SetOpen(true);
        Refresh();
    }

    bool cameFromPause;

    void BackToPrevious()
    {
        settingsPanel.SetActive(false);
        if (cameFromPause) pausePanel.SetActive(true);
        else mainPanel.SetActive(true);
    }

    public void Close()
    {
        mainPanel.SetActive(false);
        pausePanel.SetActive(false);
        settingsPanel.SetActive(false);
        slotPanel.SetActive(false);
        SetOpen(false);
    }

    void SetOpen(bool open)
    {
        IsOpen = open;
        InputHub.MenuOpen = open;
        Time.timeScale = open ? 0f : 1f;
        Cursor.lockState = open ? CursorLockMode.None : CursorLockMode.Locked;
        Cursor.visible = open;
    }

    void NewGame()
    {
        if (SaveSystem.Instance != null)
        {
            try
            {
                if (System.IO.File.Exists(SaveSystem.Instance.FilePath)) System.IO.File.Delete(SaveSystem.Instance.FilePath);
            }
            catch (Exception e) { Debug.LogWarning("[Menu] could not delete save: " + e.Message); }
        }
        Time.timeScale = 1f;
        InputHub.MenuOpen = false;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    void QuitGame()
    {
        if (SaveSystem.Instance != null) SaveSystem.Instance.Save();
        GameSettings.Save();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    // ----------------------------------------------------------------- helpers
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
        img.raycastTarget = false;
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
        return t;
    }
}
