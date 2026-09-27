using System.Collections;
using UnityEngine;

// A cyan weak point on the Warden. The ball just clangs off it: only a WardenStriker that has been set in
// motion (a toppled domino, a dropped stalactite, a released crusher) cracks it.
public class WardenCore : MonoBehaviour
{
    public WardenBoss boss;
    public float padding = 0.35f;
    public Sprite crackFx;
    public Sprite brokenSprite;

    public bool Broken { get; private set; }
    public bool Vulnerable;   // shielded (dim) until the boss arms this core's setup; then it pulses
    Vector3 baseScale;
    bool flashing;

    Collider2D col;
    SpriteRenderer[] renderers;
    Color[] baseColors;
    float lastClang = -1f;
    readonly Collider2D[] hits = new Collider2D[16];

    void Awake()
    {
        col = GetComponent<Collider2D>();
        renderers = GetComponentsInChildren<SpriteRenderer>();
        baseColors = new Color[renderers.Length];
        for (int i = 0; i < renderers.Length; i++) baseColors[i] = renderers[i].color;
        baseScale = transform.localScale;
    }

    void Update()
    {
        if (Broken || flashing) return;
        float pulse = Vulnerable ? 0.5f + 0.5f * Mathf.Sin(Time.time * 7f) : 0f;
        transform.localScale = baseScale * (Vulnerable ? 1f + 0.12f * pulse : 0.92f);
        for (int i = 0; i < renderers.Length; i++)
            renderers[i].color = Vulnerable ? Color.Lerp(baseColors[i], Color.white, 0.35f * pulse) : baseColors[i] * new Color(0.3f, 0.33f, 0.38f, 1f);
    }

    void FixedUpdate()
    {
        if (Broken || col == null) return;
        var b = col.bounds;
        int n = Physics2D.OverlapBox(b.center, (Vector2)b.size + Vector2.one * padding * 2f, 0f, new ContactFilter2D().NoFilter(), hits);
        for (int i = 0; i < n; i++)
        {
            var s = hits[i] != null ? hits[i].GetComponentInParent<WardenStriker>() : null;
            if (s == null || s.Used || !s.Armed || !Vulnerable) continue;
            s.Used = true;
            Break(s);
            return;
        }
    }

    void OnCollisionEnter2D(Collision2D c)
    {
        if (Broken || !c.gameObject.CompareTag("Ball") || Time.time - lastClang < 0.3f) return;
        lastClang = Time.time;
        StartCoroutine(Clang());
    }

    IEnumerator Clang()
    {
        flashing = true;
        ShellAudio.Play(ShellAudio.Sfx.Click, 0.8f);
        for (float t = 0; t < 0.18f; t += Time.deltaTime)
        {
            foreach (var r in renderers) r.color = Color.white;
            yield return null;
        }
        for (int i = 0; i < renderers.Length; i++) renderers[i].color = baseColors[i];
        flashing = false;
    }

    void Break(WardenStriker s)
    {
        Broken = true;
        ShellAudio.Play(ShellAudio.Sfx.Crack);
        JamAtmosphere.Shake(0.55f);
        JamAtmosphere.HitPause(0.08f);
        WardenBoss.Pop(crackFx, col.bounds.center, 0.3f, 0.5f, new Color(0.36f, 0.95f, 0.84f));
        if (s.consumeOnHit) Destroy(s.gameObject);
        foreach (var c in GetComponentsInChildren<Collider2D>()) c.enabled = false;
        StartCoroutine(Shatter());
        if (boss != null) boss.CoreBroken(this);
    }

    IEnumerator Shatter()
    {
        Vector3 s0 = baseScale;
        for (float t = 0; t < 0.35f; t += Time.deltaTime)
        {
            float k = t / 0.35f;
            transform.localScale = s0 * (1f + 0.3f * k);
            foreach (var r in renderers) r.color = new Color(1, 1, 1, 1 - k);
            yield return null;
        }
        // the smashed plate stays behind (dark socket if there is no broken art)
        transform.localScale = s0;
        var sr = GetComponent<SpriteRenderer>();
        bool hasArt = brokenSprite != null && sr != null && brokenSprite != sr.sprite;
        if (hasArt) sr.sprite = brokenSprite;
        foreach (var r in renderers) r.color = hasArt ? Color.white : new Color(0.12f, 0.12f, 0.14f, 1f);
    }
}
