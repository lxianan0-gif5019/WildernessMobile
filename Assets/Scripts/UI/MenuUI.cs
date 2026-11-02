using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Main menu, pause menu and settings. Built at runtime like the HUD.
// Buttons use TouchRouter (layer 1) so they work by touch and mouse alike.
public class MenuUI : MonoBehaviour
{
    public static MenuUI Instance;

    public bool IsOpen { get; private set; }

    Canvas canvas;
    Font font;
    GameObject mainPanel;
    GameObject pausePanel;
    GameObject settingsPanel;

    Text masterValue, sfxValue, lookValue, stickValue, qualityValue, invertValue;
    bool started;

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

        pausePanel.SetActive(false);
        settingsPanel.SetActive(false);
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

    GameObject Box(Transform parent, string title, float width, float height)
    {
        var box = MakeImage(parent, "Box", new Color(0.07f, 0.08f, 0.10f, 0.97f), null);
        var rt = box.rectTransform;
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(width, height);
        rt.anchoredPosition = Vector2.zero;

        var t = MakeText(box.rectTransform, "Title", title, 46, TextAnchor.UpperCenter);
        Anchor(t.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -30f), new Vector2(width - 40f, 60f), new Vector2(0.5f, 1f));

        return box.gameObject;
    }

    ButtonRow Row(Transform parent, string label, float y, Action onClick, float width = 520f)
    {
        var img = MakeImage(parent, "Btn_" + label, new Color(1f, 1f, 1f, 0.14f), null);
        Anchor(img.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, y), new Vector2(width, 76f), new Vector2(0.5f, 1f));

        var text = MakeText(img.rectTransform, "Label", label, 30, TextAnchor.MiddleCenter);
        Anchor(text.rectTransform, Vector2.zero, Vector2.zero, new Vector2(width, 76f), new Vector2(0.5f, 0.5f));
        text.rectTransform.anchorMin = new Vector2(0f, 0f);
        text.rectTransform.anchorMax = new Vector2(1f, 1f);
        text.rectTransform.offsetMin = Vector2.zero;
        text.rectTransform.offsetMax = Vector2.zero;

        var row = new ButtonRow { image = img, label = text };
        if (onClick != null)
            TouchRouter.Instance.RegisterButton(img.rectTransform, onClick, MenuLayer);
        return row;
    }

    class ButtonRow
    {
        public Image image;
        public Text label;
    }

    // ------------------------------------------------------------- main menu
    GameObject BuildMainMenu()
    {
        var panel = Panel("MainMenu", new Color(0.02f, 0.03f, 0.05f, 0.98f));
        var box = Box(panel.transform, "WILDERNESS", 760f, 640f);

        Row(box.transform, "Continue", -140f, () => { started = true; Close(); });
        Row(box.transform, "New Game", -236f, NewGame);
        Row(box.transform, "Settings", -332f, OpenSettings);
        Row(box.transform, "Quit", -428f, QuitGame);

        var hint = MakeText(box.transform, "Hint", "WASD move   E use   Tab bag   C craft   B build   Esc menu", 22, TextAnchor.MiddleCenter);
        Anchor(hint.rectTransform, new Vector2(0.5f, 0f), new Vector2(0f, 28f), new Vector2(700f, 60f), new Vector2(0.5f, 0f));
        hint.color = new Color(1f, 1f, 1f, 0.55f);

        return panel;
    }

    // ------------------------------------------------------------ pause menu
    GameObject BuildPauseMenu()
    {
        var panel = Panel("PauseMenu", new Color(0.02f, 0.03f, 0.05f, 0.80f));
        var box = Box(panel.transform, "PAUSED", 700f, 620f);

        Row(box.transform, "Resume", -140f, Close);
        Row(box.transform, "Save Game", -236f, () => { if (SaveSystem.Instance != null) SaveSystem.Instance.Save(); });
        Row(box.transform, "Settings", -332f, OpenSettings);
        Row(box.transform, "Main Menu", -428f, OpenMain);
        Row(box.transform, "Quit", -524f, QuitGame);

        return panel;
    }

    // -------------------------------------------------------------- settings
    GameObject BuildSettings()
    {
        var panel = Panel("Settings", new Color(0.02f, 0.03f, 0.05f, 0.94f));
        var box = Box(panel.transform, "SETTINGS", 820f, 760f);

        masterValue = SettingRow(box.transform, "Master Volume", -140f, () => { GameSettings.MasterVolume = Mathf.Clamp01(GameSettings.MasterVolume + 0.1f); Refresh(); },
                                                                                 () => { GameSettings.MasterVolume = Mathf.Clamp01(GameSettings.MasterVolume - 0.1f); Refresh(); });
        sfxValue = SettingRow(box.transform, "SFX Volume", -212f, () => { GameSettings.SfxVolume = Mathf.Clamp01(GameSettings.SfxVolume + 0.1f); Refresh(); },
                                                              () => { GameSettings.SfxVolume = Mathf.Clamp01(GameSettings.SfxVolume - 0.1f); Refresh(); });
        lookValue = SettingRow(box.transform, "Look Speed", -284f, () => { GameSettings.LookSensitivity = Mathf.Clamp(GameSettings.LookSensitivity * 1.25f, 0.02f, 0.6f); Refresh(); },
                                                               () => { GameSettings.LookSensitivity = Mathf.Clamp(GameSettings.LookSensitivity / 1.25f, 0.02f, 0.6f); Refresh(); });
        stickValue = SettingRow(box.transform, "Joystick Size", -356f, () => { GameSettings.StickRadius = Mathf.Clamp(GameSettings.StickRadius + 12f, 50f, 160f); Refresh(); },
                                                                () => { GameSettings.StickRadius = Mathf.Clamp(GameSettings.StickRadius - 12f, 50f, 160f); Refresh(); });
        qualityValue = SettingRow(box.transform, "Quality", -428f, () => { GameSettings.Quality = Mathf.Min(2, GameSettings.Quality + 1); Refresh(); },
                                                                 () => { GameSettings.Quality = Mathf.Max(0, GameSettings.Quality - 1); Refresh(); });
        invertValue = SettingRow(box.transform, "Invert Look", -500f, () => { GameSettings.InvertLook = !GameSettings.InvertLook; Refresh(); },
                                                                  () => { GameSettings.InvertLook = !GameSettings.InvertLook; Refresh(); });

        Row(box.transform, "Back", -620f, () => { GameSettings.Save(); GameSettings.Apply(); BackToPrevious(); });

        Refresh();
        return panel;
    }

    Text SettingRow(Transform parent, string label, float y, Action plus, Action minus)
    {
        var bg = MakeImage(parent, "Row_" + label, new Color(1f, 1f, 1f, 0.06f), null);
        Anchor(bg.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, y), new Vector2(700f, 64f), new Vector2(0.5f, 1f));

        var name = MakeText(bg.rectTransform, "Name", label, 26, TextAnchor.MiddleLeft);
        Anchor(name.rectTransform, new Vector2(0f, 0.5f), new Vector2(24f, 0f), new Vector2(360f, 60f), new Vector2(0f, 0.5f));

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

    void Refresh()
    {
        if (masterValue != null) masterValue.text = Mathf.RoundToInt(GameSettings.MasterVolume * 100f) + "%";
        if (sfxValue != null) sfxValue.text = Mathf.RoundToInt(GameSettings.SfxVolume * 100f) + "%";
        if (lookValue != null) lookValue.text = (GameSettings.LookSensitivity / 0.085f).ToString("F2") + "x";
        if (stickValue != null) stickValue.text = Mathf.RoundToInt(GameSettings.StickRadius) + "";
        if (qualityValue != null) qualityValue.text = GameSettings.QualityNames[Mathf.Clamp(GameSettings.Quality, 0, 2)];
        if (invertValue != null) invertValue.text = GameSettings.InvertLook ? "On" : "Off";

        GameSettings.Apply();
    }

    bool cameFromPause;

    // ----------------------------------------------------------------- state
    public void OpenMain()
    {
        started = false;
        mainPanel.SetActive(true);
        pausePanel.SetActive(false);
        settingsPanel.SetActive(false);
        SetOpen(true);
    }

    public void OpenPause()
    {
        cameFromPause = true;
        mainPanel.SetActive(false);
        pausePanel.SetActive(true);
        settingsPanel.SetActive(false);
        SetOpen(true);
    }

    public void OpenSettings()
    {
        mainPanel.SetActive(false);
        pausePanel.SetActive(false);
        settingsPanel.SetActive(true);
        SetOpen(true);
        Refresh();
    }

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
            try { if (System.IO.File.Exists(SaveSystem.Instance.FilePath)) System.IO.File.Delete(SaveSystem.Instance.FilePath); }
            catch (Exception e) { Debug.LogWarning("[Menu] could not delete save: " + e.Message); }
        }
        Time.timeScale = 1f;
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
