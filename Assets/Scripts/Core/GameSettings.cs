using UnityEngine;

// Player-facing settings, persisted with PlayerPrefs and applied to the systems.
public static class GameSettings
{
    public static float MasterVolume = 0.8f;
    public static float SfxVolume = 0.9f;
    public static float LookSensitivity = 0.085f;
    public static float StickRadius = 95f;
    public static int Quality = 1;          // 0 low, 1 medium, 2 high
    public static bool InvertLook = false;
    public static int Language = 0;         // 0 English, 1 Chinese

    static bool loaded;

    public static void Load()
    {
        if (loaded) return;
        loaded = true;

        MasterVolume = PlayerPrefs.GetFloat("masterVolume", 0.8f);
        SfxVolume = PlayerPrefs.GetFloat("sfxVolume", 0.9f);
        LookSensitivity = PlayerPrefs.GetFloat("lookSensitivity", 0.085f);
        StickRadius = PlayerPrefs.GetFloat("stickRadius", 95f);
        Quality = PlayerPrefs.GetInt("quality", 1);
        InvertLook = PlayerPrefs.GetInt("invertLook", 0) == 1;
        Language = PlayerPrefs.GetInt("language", 1);      // default Chinese
        Loc.Current = Language == 1 ? Loc.Lang.Chinese : Loc.Lang.English;
    }

    public static void Save()
    {
        PlayerPrefs.SetFloat("masterVolume", MasterVolume);
        PlayerPrefs.SetFloat("sfxVolume", SfxVolume);
        PlayerPrefs.SetFloat("lookSensitivity", LookSensitivity);
        PlayerPrefs.SetFloat("stickRadius", StickRadius);
        PlayerPrefs.SetInt("quality", Quality);
        PlayerPrefs.SetInt("invertLook", InvertLook ? 1 : 0);
        PlayerPrefs.SetInt("language", Language);
        PlayerPrefs.Save();
    }

    /// <summary>Pushes the values into the live systems.</summary>
    public static void Apply()
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.masterVolume = MasterVolume;
            AudioManager.Instance.sfxVolume = SfxVolume;
        }

        if (TouchRouter.Instance != null)
        {
            TouchRouter.Instance.lookSensitivity = LookSensitivity;
            TouchRouter.Instance.stickRadius = StickRadius;
        }

        switch (Quality)
        {
            case 0:
                QualitySettings.shadowDistance = 20f;
                QualitySettings.shadows = ShadowQuality.Disable;
                break;
            case 1:
                QualitySettings.shadowDistance = 40f;
                QualitySettings.shadows = ShadowQuality.HardOnly;
                break;
            default:
                QualitySettings.shadowDistance = 70f;
                QualitySettings.shadows = ShadowQuality.All;
                break;
        }
    }

    public static string[] QualityNames = { "Low", "Medium", "High" };
}
