using UnityEngine;

// Simple day/night cycle: rotates the sun, fades its colour and intensity, and
// drives ambient light, fog and sky tint so night actually looks like night.
public class DayNightCycle : MonoBehaviour
{
    public static DayNightCycle Instance;

    [Range(0f, 1f)]
    [Tooltip("0 = midnight, 0.25 = sunrise, 0.5 = noon, 0.75 = sunset")]
    public float timeOfDay = 0.32f;

    [Tooltip("Seconds for a full 24h cycle. 3600 = one hour (30 min day, 30 min night).")]
    public float dayLengthSeconds = 3600f;

    public bool running = true;

    Light sun;
    Light moon;
    Material sky;

    [Header("Moonlight")]
    [Tooltip("Moon brightness at full moon (十五). Halved or less on other nights.")]
    public float moonIntensity = 0.42f;
    [Tooltip("Ambient light floor at night - raise it if night is too dark to play.")]
    public Color nightAmbient = new Color(0.10f, 0.12f, 0.18f);

    [Header("Moon phase")]
    [Tooltip("One full lunar cycle every N in-game days. Day 1 = 初一 (new moon), day 15 = 十五 (full moon).")]
    public int lunarCycleDays = 30;
    [Tooltip("In-game day counter. Starts at 1.")]
    public int dayNumber = 1;

    static readonly Color DaySky = new Color(0.48f, 0.64f, 0.86f);
    static readonly Color NightSky = new Color(0.02f, 0.03f, 0.07f);

    void Awake()
    {
        Instance = this;

        foreach (var l in FindObjectsOfType<Light>())
        {
            if (l.type == LightType.Directional) { sun = l; break; }
        }
        if (sun == null) Debug.Log("[DayNight] no directional light found");

        // Moon: a second directional light that we drive ourselves. It sits opposite
        // the sun and only lights the world at night.
        var moonGo = new GameObject("MoonLight");
        moonGo.transform.SetParent(transform, false);
        moon = moonGo.AddComponent<Light>();
        moon.type = LightType.Directional;
        moon.shadows = LightShadows.None;
        moon.color = new Color(0.55f, 0.66f, 1f);
        moon.intensity = 0f;
        moon.transform.rotation = Quaternion.Euler(120f, -35f, 0f);

        var shader = Shader.Find("Skybox/Procedural");
        if (shader != null)
        {
            sky = new Material(shader);
            RenderSettings.skybox = sky;
        }

        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.ExponentialSquared;
        RenderSettings.fogDensity = 0.0022f;
    }

    void Update()
    {
        if (running)
        {
            timeOfDay += Time.deltaTime / Mathf.Max(10f, dayLengthSeconds);
            if (timeOfDay >= 1f)
            {
                timeOfDay -= 1f;
                dayNumber++;
                Debug.Log("[DayNight] day " + dayNumber + " (moon phase " + MoonName() + ")");
            }
        }
        Apply(timeOfDay);
    }

    public void Apply(float t)
    {
        float elevation = Mathf.Sin((t - 0.25f) * Mathf.PI * 2f);      // -1 (midnight) .. 1 (noon)
        float daylight = Mathf.Clamp01(elevation * 1.25f + 0.18f);

        float night = Mathf.Clamp01(-elevation * 1.6f);      // 1 at midnight

        if (sun != null)
        {
            sun.transform.rotation = Quaternion.Euler(t * 360f - 90f, 35f, 0f);
            sun.intensity = Mathf.Lerp(0f, 1.15f, daylight);

            Color sunset = new Color(1f, 0.55f, 0.32f);
            Color noon = new Color(1f, 0.96f, 0.88f);
            sun.color = Color.Lerp(sunset, noon, Mathf.Clamp01(elevation * 1.6f));
        }

        if (moon != null)
        {
            // opposite side of the sky from the sun, rising as the sun sets
            moon.transform.rotation = Quaternion.Euler((t * 360f - 90f) + 180f, -35f, 0f);
            // 初一 = 新月 (barely any light), 十五 = 满月 (brightest)
            moon.intensity = night * moonIntensity * Mathf.Lerp(0.10f, 1f, MoonFullness());
        }

        RenderSettings.ambientLight = Color.Lerp(nightAmbient,
                                                 new Color(0.60f, 0.64f, 0.70f), daylight);

        RenderSettings.fogColor = Color.Lerp(new Color(0.05f, 0.07f, 0.13f),
                                             new Color(0.58f, 0.68f, 0.78f), daylight);

        if (sky != null)
        {
            sky.SetColor("_SkyTint", Color.Lerp(new Color(0.06f, 0.08f, 0.16f), DaySky, daylight));
            // Exposure floor keeps the night sky readable instead of pure black.
            sky.SetFloat("_Exposure", Mathf.Lerp(0.45f, 1.05f, daylight));
        }
    }

    /// <summary>0 at 初一 (new moon), 1 at 十五 (full moon), back to 0 at 三十.</summary>
    public float MoonFullness()
    {
        int n = Mathf.Max(2, lunarCycleDays);
        int d = (int)Mathf.Repeat(dayNumber - 1, n);
        float phase = d / (float)n;
        return 0.5f * (1f - Mathf.Cos(phase * Mathf.PI * 2f));
    }

    public string MoonName()
    {
        int n = Mathf.Max(2, lunarCycleDays);
        int d = (int)Mathf.Repeat(dayNumber - 1, n) + 1;
        if (d == 1) return "初一 new moon";
        if (d == n / 2 || d == n / 2 + 1) return "十五 full moon";
        return "day " + d + "/" + n;
    }

    public bool IsNight { get { return timeOfDay < 0.22f || timeOfDay > 0.80f; } }

    public string Clock()
    {
        int totalMinutes = Mathf.FloorToInt(timeOfDay * 24f * 60f);
        return (totalMinutes / 60).ToString("00") + ":" + (totalMinutes % 60).ToString("00");
    }
}
