using System.Collections.Generic;
using UnityEngine;

// All game audio, synthesised at runtime so the project needs no audio assets.
// One place to call: AudioManager.Play(AudioManager.Sfx.Chop);
public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance;

    public enum Sfx
    {
        Footstep,
        Chop,
        Pickup,
        Eat,
        CraftDone,
        UiClick,
        WolfHowl,
        WolfBite,
        Hurt,
        Place,
        FireLoop
    }

    [Range(0f, 1f)] public float masterVolume = 0.8f;
    [Range(0f, 1f)] public float sfxVolume = 0.9f;

    const int VoiceCount = 10;
    AudioSource[] voices;
    int nextVoice;

    readonly Dictionary<Sfx, AudioClip> clips = new Dictionary<Sfx, AudioClip>();
    AudioSource fireSource;

    void Awake()
    {
        Instance = this;

        voices = new AudioSource[VoiceCount];
        for (int i = 0; i < VoiceCount; i++)
        {
            var go = new GameObject("Voice" + i);
            go.transform.SetParent(transform, false);
            var src = go.AddComponent<AudioSource>();
            src.playOnAwake = false;
            src.spatialBlend = 0f;
            voices[i] = src;
        }

        BuildClips();
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    void BuildClips()
    {
        clips[Sfx.Footstep] = Noise(0.10f, 0.35f, 900f, 0.55f);
        clips[Sfx.Chop] = Thud(0.16f, 160f, 60f, 0.75f);
        clips[Sfx.Pickup] = Blip(0.09f, 880f, 1320f, 0.45f);
        clips[Sfx.Eat] = Blip(0.16f, 320f, 220f, 0.5f);
        clips[Sfx.CraftDone] = Blip(0.28f, 660f, 990f, 0.5f);
        clips[Sfx.UiClick] = Blip(0.06f, 1200f, 1200f, 0.35f);
        clips[Sfx.WolfHowl] = Howl(1.5f, 420f, 240f, 0.6f);
        clips[Sfx.WolfBite] = Thud(0.18f, 220f, 90f, 0.8f);
        clips[Sfx.Hurt] = Thud(0.22f, 120f, 55f, 0.85f);
        clips[Sfx.Place] = Thud(0.14f, 200f, 110f, 0.6f);
        clips[Sfx.FireLoop] = FireCrackle(2.0f, 0.35f);
    }

    public void Play(Sfx sfx, float volume = 1f, float pitchJitter = 0.08f)
    {
        AudioClip clip;
        if (!clips.TryGetValue(sfx, out clip) || clip == null) return;

        var src = voices[nextVoice];
        nextVoice = (nextVoice + 1) % VoiceCount;

        src.pitch = 1f + Random.Range(-pitchJitter, pitchJitter);
        src.volume = masterVolume * sfxVolume * volume;
        src.clip = clip;
        src.Play();
    }

    /// <summary>Starts/stops the looping campfire crackle on the player.</summary>
    public void SetFireLoop(bool on, Transform follow)
    {
        if (clips[Sfx.FireLoop] == null) return;

        if (fireSource == null && on)
        {
            var go = new GameObject("FireLoop");
            fireSource = go.AddComponent<AudioSource>();
            fireSource.clip = clips[Sfx.FireLoop];
            fireSource.loop = true;
            fireSource.spatialBlend = 1f;
            fireSource.rolloffMode = AudioRolloffMode.Linear;
            fireSource.minDistance = 2f;
            fireSource.maxDistance = 14f;
        }

        if (fireSource == null) return;

        if (follow != null) fireSource.transform.position = follow.position;
        fireSource.volume = masterVolume * sfxVolume * 0.5f;

        if (on && !fireSource.isPlaying) fireSource.Play();
        else if (!on && fireSource.isPlaying) fireSource.Stop();
    }

    // ------------------------------------------------------------- synthesis
    static AudioClip Make(string name, float seconds, System.Func<float, float> sample)
    {
        const int rate = 44100;
        int count = Mathf.Max(8, Mathf.RoundToInt(rate * seconds));
        var data = new float[count];
        for (int i = 0; i < count; i++)
            data[i] = Mathf.Clamp(sample(i / (float)rate), -1f, 1f);

        var clip = AudioClip.Create(name, count, 1, rate, false);
        clip.SetData(data, 0);
        return clip;
    }

    static AudioClip Noise(float seconds, float amp, float cutoff, float decay)
    {
        var rnd = new System.Random(12345);
        float lp = 0f;
        float k = Mathf.Clamp01(cutoff / 22050f);
        return Make("noise", seconds, t =>
        {
            float white = (float)(rnd.NextDouble() * 2.0 - 1.0);
            lp += (white - lp) * k;
            float env = Mathf.Exp(-t / Mathf.Max(0.001f, seconds * decay));
            return lp * amp * env;
        });
    }

    static AudioClip Thud(float seconds, float startHz, float endHz, float amp)
    {
        float phase = 0f;
        return Make("thud", seconds, t =>
        {
            float f = Mathf.Lerp(startHz, endHz, t / seconds);
            phase += 2f * Mathf.PI * f / 44100f;
            float env = Mathf.Exp(-t / Mathf.Max(0.001f, seconds * 0.35f));
            return Mathf.Sin(phase) * amp * env;
        });
    }

    static AudioClip Blip(float seconds, float startHz, float endHz, float amp)
    {
        float phase = 0f;
        return Make("blip", seconds, t =>
        {
            float f = Mathf.Lerp(startHz, endHz, t / seconds);
            phase += 2f * Mathf.PI * f / 44100f;
            float env = Mathf.Sin(Mathf.PI * Mathf.Clamp01(t / seconds));
            return Mathf.Sin(phase) * amp * env;
        });
    }

    static AudioClip Howl(float seconds, float startHz, float endHz, float amp)
    {
        float phase = 0f;
        return Make("howl", seconds, t =>
        {
            float p = t / seconds;
            float f = Mathf.Lerp(startHz, endHz, p) + Mathf.Sin(t * 7f) * 12f;
            phase += 2f * Mathf.PI * f / 44100f;
            float env = Mathf.Sin(Mathf.PI * Mathf.Clamp01(p));
            return Mathf.Sin(phase) * amp * env;
        });
    }

    static AudioClip FireCrackle(float seconds, float amp)
    {
        var rnd = new System.Random(777);
        float lp = 0f;
        return Make("fire", seconds, t =>
        {
            float white = (float)(rnd.NextDouble() * 2.0 - 1.0);
            lp += (white - lp) * 0.04f;
            float pop = rnd.NextDouble() < 0.002 ? (float)rnd.NextDouble() : 0f;
            return (lp * 0.6f + pop) * amp;
        });
    }
}
