using UnityEngine;

// Procedural sound: every clip is synthesized at startup (the project ships no audio files).
public class ShellAudio : MonoBehaviour
{
    public enum Sfx { Thud, Crack, Jump, Land, Click, Clear, GameOver, Alert }

    public static ShellAudio Instance { get; private set; }

    const int Rate = 44100;
    const float MasterVolume = 0.8f;
    AudioClip[] clips;
    AudioSource[] pool;
    int next;
    AudioSource drone;
    float droneTarget;
    AudioListener fallbackListener;

    public static void Play(Sfx s, float volume = 1f, float pitchJitter = 0.06f)
    {
        if (Instance != null) Instance.PlayInternal(s, volume, pitchJitter);
    }

    public static void SetDrone(bool on)
    {
        if (Instance != null) Instance.droneTarget = on ? 0.05f : 0f;
    }

    void Awake()
    {
        Instance = this;
        pool = new AudioSource[10];
        for (int i = 0; i < pool.Length; i++)
        {
            pool[i] = gameObject.AddComponent<AudioSource>();
            pool[i].playOnAwake = false;
            pool[i].spatialBlend = 0f;
        }
        clips = new AudioClip[8];
        clips[(int)Sfx.Thud] = Make("thud", 0.35f, Thud);
        clips[(int)Sfx.Crack] = Make("crack", 0.45f, Crack);
        clips[(int)Sfx.Jump] = Make("jump", 0.14f, Jump);
        clips[(int)Sfx.Land] = Make("land", 0.12f, Land);
        clips[(int)Sfx.Click] = Make("click", 0.06f, Click);
        clips[(int)Sfx.Clear] = Make("clear", 1.3f, ClearSting);
        clips[(int)Sfx.GameOver] = Make("gameover", 1.6f, GameOverSting);
        clips[(int)Sfx.Alert] = Make("alert", 0.35f, Alert);

        drone = gameObject.AddComponent<AudioSource>();
        drone.clip = Make("drone", 8f, Drone);
        drone.loop = true;
        drone.volume = 0f;
        drone.spatialBlend = 0f;
        drone.Play();

        fallbackListener = gameObject.AddComponent<AudioListener>();
        fallbackListener.enabled = false;
    }

    // Keep exactly one listener: use ours only when the scene has none (menus without a camera listener).
    public void RefreshListener()
    {
        var all = FindObjectsByType<AudioListener>(FindObjectsInactive.Exclude);
        int others = 0;
        foreach (var l in all) if (l != fallbackListener && l.enabled) others++;
        fallbackListener.enabled = others == 0;
    }

    void Update()
    {
        drone.volume = Mathf.MoveTowards(drone.volume, droneTarget * MasterVolume, Time.unscaledDeltaTime * 0.03f);
    }

    void PlayInternal(Sfx s, float volume, float jitter)
    {
        var src = pool[next];
        next = (next + 1) % pool.Length;
        src.pitch = 1f + Random.Range(-jitter, jitter);
        src.PlayOneShot(clips[(int)s], Mathf.Clamp01(volume) * MasterVolume);
    }

    // ---------- synthesis ----------
    delegate float Gen(float t, float dur);
    static uint seed = 22222;
    static float Noise() { seed ^= seed << 13; seed ^= seed >> 17; seed ^= seed << 5; return (seed / (float)uint.MaxValue) * 2f - 1f; }
    const float TwoPi = Mathf.PI * 2f;

    static AudioClip Make(string name, float dur, Gen g)
    {
        int n = Mathf.CeilToInt(dur * Rate);
        var data = new float[n];
        float peak = 0.0001f;
        for (int i = 0; i < n; i++) { data[i] = g(i / (float)Rate, dur); peak = Mathf.Max(peak, Mathf.Abs(data[i])); }
        float norm = peak > 0.95f ? 0.95f / peak : 1f;
        for (int i = 0; i < n; i++) data[i] *= norm;
        var clip = AudioClip.Create("sfx_" + name, n, 1, Rate, false);
        clip.SetData(data, 0);
        return clip;
    }

    static float Env(float t, float attack, float decay) => (t < attack ? t / attack : 1f) * Mathf.Exp(-t / decay);

    // Low body thump with a falling pitch, like iron hitting stone.
    static float thudPhase;
    static float Thud(float t, float d)
    {
        if (t == 0) thudPhase = 0;
        float f = 38f + 70f * Mathf.Exp(-t * 18f);
        thudPhase += TwoPi * f / Rate;
        return 0.9f * Mathf.Sin(thudPhase) * Env(t, 0.002f, 0.09f) + 0.25f * Noise() * Env(t, 0.001f, 0.015f);
    }

    // Noisy crackle with a low knock: rock splitting.
    static float lp;
    static float Crack(float t, float d)
    {
        if (t == 0) lp = 0;
        float n = Noise();
        lp += (n - lp) * 0.35f;
        float grit = (Noise() > 0.93f - t * 0.2f ? Noise() : 0f) * Env(t, 0.001f, 0.12f);
        float knock = Mathf.Sin(TwoPi * (90f + 60f * Mathf.Exp(-t * 30f)) * t) * Env(t, 0.001f, 0.06f);
        return 0.55f * lp * Env(t, 0.001f, 0.07f) + 0.35f * grit + 0.6f * knock;
    }

    static float Jump(float t, float d)
    {
        float f = 260f + 420f * (t / d);
        return 0.35f * Mathf.Sin(TwoPi * f * t) * Env(t, 0.005f, 0.05f) + 0.1f * Noise() * Env(t, 0.001f, 0.02f);
    }

    static float Land(float t, float d)
    {
        if (t == 0) lp = 0;
        lp += (Noise() - lp) * 0.08f;
        return 1.2f * lp * Env(t, 0.001f, 0.03f) + 0.4f * Mathf.Sin(TwoPi * 70f * t) * Env(t, 0.001f, 0.04f);
    }

    static float Click(float t, float d) => 0.4f * Mathf.Sin(TwoPi * 1400f * t) * Env(t, 0.001f, 0.012f);

    static float Tone(float f, float t) => Mathf.Sin(TwoPi * f * t) + 0.35f * Mathf.Sin(TwoPi * f * 2f * t) + 0.12f * Mathf.Sin(TwoPi * f * 3f * t);

    // Rising major arpeggio with a held top note.
    static float ClearSting(float t, float d)
    {
        float[] notes = { 523.25f, 659.25f, 783.99f, 1046.5f };
        float s = 0;
        for (int i = 0; i < notes.Length; i++)
        {
            float start = i * 0.09f;
            if (t < start) continue;
            float lt = t - start;
            float decay = i == notes.Length - 1 ? 0.45f : 0.2f;
            s += 0.22f * Tone(notes[i], lt) * Env(lt, 0.004f, decay);
        }
        return s;
    }

    // Slow descending minor phrase, slightly detuned.
    static float GameOverSting(float t, float d)
    {
        float[] notes = { 392f, 311.13f, 261.63f };
        float s = 0;
        for (int i = 0; i < notes.Length; i++)
        {
            float start = i * 0.28f;
            if (t < start) continue;
            float lt = t - start;
            float decay = i == notes.Length - 1 ? 0.5f : 0.22f;
            s += 0.2f * (Tone(notes[i], lt) + 0.6f * Tone(notes[i] * 1.006f, lt)) * Env(lt, 0.01f, decay);
        }
        return s;
    }

    // Sharp bright stab for the "!" alert.
    static float Alert(float t, float d)
    {
        float s = 0;
        foreach (var f in new[] { 880f, 1318.5f, 1760f })
            s += Mathf.Sign(Mathf.Sin(TwoPi * f * t)) * 0.12f + Mathf.Sin(TwoPi * f * t) * 0.1f;
        return s * Env(t, 0.003f, 0.08f);
    }

    // Seamless 8 s loop: all partials are multiples of 1/8 Hz.
    static float Drone(float t, float d)
    {
        float lfo = 0.75f + 0.25f * Mathf.Sin(TwoPi * 0.25f * t);
        float lfo2 = 0.6f + 0.4f * Mathf.Sin(TwoPi * 0.125f * t + 1f);
        return 0.5f * Mathf.Sin(TwoPi * 55f * t) * lfo
             + 0.25f * Mathf.Sin(TwoPi * 82.5f * t) * lfo2
             + 0.15f * Mathf.Sin(TwoPi * 110.25f * t)
             + 0.05f * Mathf.Sin(TwoPi * 165f * t) * lfo2;
    }
}
