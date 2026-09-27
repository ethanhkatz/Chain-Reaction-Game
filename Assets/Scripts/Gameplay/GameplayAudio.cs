using UnityEngine;

// Small synthesized sounds for the score layer: combo blip (pitch climbs with the chain), pickup sparkle,
// stamp slam. Lives on the persistent [Gameplay] object; follows the shell's SFX volume.
public class GameplayAudio : MonoBehaviour
{
    public static GameplayAudio Instance { get; private set; }

    const int Rate = 44100;
    AudioClip blip, sparkle, stamp;
    AudioSource[] pool;
    int next;

    void Awake()
    {
        Instance = this;
        pool = new AudioSource[6];
        for (int i = 0; i < pool.Length; i++)
        {
            var a = gameObject.AddComponent<AudioSource>();
            a.playOnAwake = false;
            a.spatialBlend = 0f;
            pool[i] = a;
        }
        blip = Make("combo_blip", 0.16f, Blip);
        sparkle = Make("pickup_sparkle", 0.6f, Sparkle);
        stamp = Make("rank_stamp", 0.4f, Stamp);
    }

    // Major-scale steps so a long chain climbs like an arpeggio.
    static readonly int[] Steps = { 0, 2, 4, 5, 7, 9, 11, 12, 14, 16, 17, 19, 21, 23, 24 };

    public static void Combo(int chain)
    {
        int i = Mathf.Clamp(chain - 1, 0, Steps.Length - 1);
        Play(Instance?.blip, 0.55f, Mathf.Pow(2f, Steps[i] / 12f));
    }

    public static void Pickup() => Play(Instance?.sparkle, 0.6f, 1f);
    public static void Stamp() => Play(Instance?.stamp, 0.9f, 1f);

    static void Play(AudioClip c, float vol, float pitch)
    {
        if (Instance == null || c == null) return;
        var src = Instance.pool[Instance.next];
        Instance.next = (Instance.next + 1) % Instance.pool.Length;
        src.pitch = pitch;
        src.PlayOneShot(c, vol * 0.8f * ShellSettings.SfxVolume);
    }

    // ---------- synthesis ----------
    delegate float Gen(float t, float d);
    const float TwoPi = Mathf.PI * 2f;
    static uint seed = 90210;
    static float Noise() { seed ^= seed << 13; seed ^= seed >> 17; seed ^= seed << 5; return (seed / (float)uint.MaxValue) * 2f - 1f; }

    static AudioClip Make(string name, float dur, Gen g)
    {
        int n = Mathf.CeilToInt(dur * Rate);
        var data = new float[n];
        float peak = 0.0001f;
        for (int i = 0; i < n; i++) { data[i] = g(i / (float)Rate, dur); peak = Mathf.Max(peak, Mathf.Abs(data[i])); }
        float norm = 0.9f / peak;
        for (int i = 0; i < n; i++) data[i] *= norm;
        var clip = AudioClip.Create(name, n, 1, Rate, false);
        clip.SetData(data, 0);
        return clip;
    }

    static float Env(float t, float a, float d) => (t < a ? t / a : 1f) * Mathf.Exp(-t / d);

    // Bright click + short square-ish tone.
    static float Blip(float t, float d)
    {
        float f = 660f;
        float tone = Mathf.Sin(TwoPi * f * t) + 0.3f * Mathf.Sign(Mathf.Sin(TwoPi * f * t)) + 0.25f * Mathf.Sin(TwoPi * f * 2f * t);
        return 0.5f * tone * Env(t, 0.002f, 0.05f) + 0.3f * Noise() * Env(t, 0.0005f, 0.004f);
    }

    // Three quick high bell notes.
    static float Sparkle(float t, float d)
    {
        float[] notes = { 1318.5f, 1760f, 2637f };
        float s = 0;
        for (int i = 0; i < notes.Length; i++)
        {
            float st = i * 0.06f;
            if (t < st) continue;
            float lt = t - st;
            s += (Mathf.Sin(TwoPi * notes[i] * lt) + 0.3f * Mathf.Sin(TwoPi * notes[i] * 2.76f * lt)) * Env(lt, 0.002f, 0.12f + i * 0.06f);
        }
        return s;
    }

    // Rubber-stamp thwack: low knock + papery noise burst.
    static float lp;
    static float Stamp(float t, float d)
    {
        if (t == 0) lp = 0;
        lp += (Noise() - lp) * 0.25f;
        float knock = Mathf.Sin(TwoPi * (70f + 90f * Mathf.Exp(-t * 25f)) * t) * Env(t, 0.001f, 0.08f);
        return 0.9f * knock + 0.8f * lp * Env(t, 0.001f, 0.035f);
    }
}
