using UnityEngine;

// Game audio: team clips from Assets/Audio (via Resources/ShellArt) where they exist, synthesized clips for the gaps
// (ball thud, rock crack, click, level-clear sting, alert). Also owns the persistent music player.
public class ShellAudio : MonoBehaviour
{
    public enum Sfx { Thud, Crack, Jump, Land, Click, Clear, GameOver, Alert }
    public enum Music { None, Menu, Level, Ending }

    public static ShellAudio Instance { get; private set; }

    const int Rate = 44100;
    const float SfxBase = 0.8f;
    const float MusicBase = 0.35f;
    AudioClip[] clips;
    AudioSource[] pool;
    int next;
    AudioSource drone, music, steps, sting;
    float droneTarget;
    float musicDuck = 1f;
    Music currentMusic = Music.None;
    AudioListener fallbackListener;
    ShellArt art;

    public static void Play(Sfx s, float volume = 1f, float pitchJitter = 0.06f)
    {
        if (Instance != null) Instance.PlayInternal(s, volume, pitchJitter);
    }

    public static void SetDrone(bool on)
    {
        if (Instance != null) Instance.droneTarget = on ? 0.05f : 0f;
    }

    // Switches track only when the category changes, so music carries across level loads.
    public static void SetMusic(Music m)
    {
        if (Instance == null) return;
        Instance.musicDuck = 1f;
        if (m == Instance.currentMusic) return;
        Instance.currentMusic = m;
        var a = Instance.art;
        AudioClip clip = a == null ? null : m == Music.Menu ? a.musicMenu : m == Music.Level ? a.musicLevel : m == Music.Ending ? a.musicEnding : null;
        Instance.music.Stop();
        Instance.music.clip = clip;
        if (clip != null) Instance.music.Play();
        // The synthesized drone only fills in when there is no music track.
        Instance.droneTarget = clip == null && m != Music.None ? 0.05f : 0f;
    }

    public static void SetFootsteps(bool on)
    {
        if (Instance == null || Instance.steps.clip == null) return;
        if (on && !Instance.steps.isPlaying) Instance.steps.Play();
        else if (!on && Instance.steps.isPlaying) Instance.steps.Pause();
    }

    // Death explosion, then the game-over jingle over its tail; music ducks underneath.
    public static void PlayGameOver()
    {
        if (Instance == null) return;
        var a = Instance.art;
        SetFootsteps(false);
        Instance.musicDuck = 0.25f;
        if (a != null && a.deathExplosion != null)
        {
            Instance.pool[0].PlayOneShot(a.deathExplosion, 0.7f * SfxBase * ShellSettings.SfxVolume);
            if (a.gameOverSting != null)
            {
                Instance.sting.clip = a.gameOverSting;
                Instance.sting.volume = 0.8f * SfxBase * ShellSettings.SfxVolume;
                Instance.sting.PlayDelayed(1.4f);
            }
        }
        else Play(Sfx.GameOver, 0.7f, 0f);
    }

    public static void PlayLevelClear()
    {
        if (Instance == null) return;
        SetFootsteps(false);
        Instance.musicDuck = 0.4f;
        Play(Sfx.Clear, 0.7f, 0f);
    }

    void Awake()
    {
        Instance = this;
        art = ShellArt.Get();
        pool = new AudioSource[10];
        for (int i = 0; i < pool.Length; i++) pool[i] = NewSource(false);
        clips = new AudioClip[8];
        clips[(int)Sfx.Thud] = Make("thud", 0.35f, Thud);
        clips[(int)Sfx.Crack] = Make("crack", 0.45f, Crack);
        clips[(int)Sfx.Jump] = art != null && art.jump != null ? art.jump : Make("jump", 0.14f, Jump);
        clips[(int)Sfx.Land] = Make("land", 0.12f, Land);
        clips[(int)Sfx.Click] = Make("click", 0.06f, Click);
        clips[(int)Sfx.Clear] = Make("clear", 1.3f, ClearSting);
        clips[(int)Sfx.GameOver] = Make("gameover", 1.6f, GameOverSting);
        clips[(int)Sfx.Alert] = Make("alert", 0.35f, Alert);

        drone = NewSource(true);
        drone.clip = Make("drone", 8f, Drone);
        drone.volume = 0f;
        drone.Play();

        music = NewSource(true);
        music.volume = 0f;
        steps = NewSource(true);
        steps.clip = art != null ? art.footsteps : null;
        sting = NewSource(false);

        fallbackListener = gameObject.AddComponent<AudioListener>();
        fallbackListener.enabled = false;
    }

    AudioSource NewSource(bool loop)
    {
        var a = gameObject.AddComponent<AudioSource>();
        a.playOnAwake = false;
        a.loop = loop;
        a.spatialBlend = 0f;
        return a;
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
        float dt = Time.unscaledDeltaTime;
        drone.volume = Mathf.MoveTowards(drone.volume, droneTarget * SfxBase * ShellSettings.MusicVolume, dt * 0.03f);
        music.volume = Mathf.MoveTowards(music.volume, MusicBase * musicDuck * ShellSettings.MusicVolume, dt * 0.5f);
        steps.volume = 0.3f * SfxBase * ShellSettings.SfxVolume;
        // Footsteps shouldn't keep looping behind a pause/freeze.
        if (Time.timeScale == 0f && steps.isPlaying) steps.Pause();
    }

    void PlayInternal(Sfx s, float volume, float jitter)
    {
        var src = pool[next];
        next = (next + 1) % pool.Length;
        src.pitch = 1f + Random.Range(-jitter, jitter);
        src.PlayOneShot(clips[(int)s], Mathf.Clamp01(volume) * SfxBase * ShellSettings.SfxVolume);
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
