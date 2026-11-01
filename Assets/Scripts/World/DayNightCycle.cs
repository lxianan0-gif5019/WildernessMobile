using UnityEngine;

// Simple day/night cycle: rotates the sun, fades its colour and intensity, and
// drives ambient light, fog and sky tint so night actually looks like night.
public class DayNightCycle : MonoBehaviour
{
    public static DayNightCycle Instance;

    [Range(0f, 1f)]
    [Tooltip("0 = midnight, 0.25 = sunrise, 0.5 = noon, 0.75 = sunset")]
    public float timeOfDay = 0.32f;

    [Tooltip("Seconds for a full 24h cycle. 300 = 5 minutes.")]
    public float dayLengthSeconds = 300f;

    public bool running = true;

    Light sun;
    Material sky;

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
            if (timeOfDay >= 1f) timeOfDay -= 1f;
        }
        Apply(timeOfDay);
    }

    public void Apply(float t)
    {
        float elevation = Mathf.Sin((t - 0.25f) * Mathf.PI * 2f);      // -1 (midnight) .. 1 (noon)
        float daylight = Mathf.Clamp01(elevation * 1.25f + 0.18f);

        if (sun != null)
        {
            sun.transform.rotation = Quaternion.Euler(t * 360f - 90f, 35f, 0f);
            sun.intensity = Mathf.Lerp(0f, 1.15f, daylight);

            Color sunset = new Color(1f, 0.55f, 0.32f);
            Color noon = new Color(1f, 0.96f, 0.88f);
            sun.color = Color.Lerp(sunset, noon, Mathf.Clamp01(elevation * 1.6f));
        }

        RenderSettings.ambientLight = Color.Lerp(new Color(0.045f, 0.05f, 0.09f),
                                                 new Color(0.60f, 0.64f, 0.70f), daylight);

        RenderSettings.fogColor = Color.Lerp(NightSky, new Color(0.58f, 0.68f, 0.78f), daylight);

        if (sky != null)
        {
            sky.SetColor("_SkyTint", Color.Lerp(NightSky, DaySky, daylight));
            sky.SetFloat("_Exposure", Mathf.Lerp(0.22f, 1.05f, daylight));
        }
    }

    public bool IsNight { get { return timeOfDay < 0.22f || timeOfDay > 0.80f; } }

    public string Clock()
    {
        int totalMinutes = Mathf.FloorToInt(timeOfDay * 24f * 60f);
        return (totalMinutes / 60).ToString("00") + ":" + (totalMinutes % 60).ToString("00");
    }
}
