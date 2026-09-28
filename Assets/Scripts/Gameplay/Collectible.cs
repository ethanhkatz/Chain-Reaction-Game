using System.Collections;
using UnityEngine;

// Added at runtime to every "Collectible_*" trigger object. Bobs and glows; the player (or the swinging ball)
// picks it up for points, with a sparkle burst and chime.
public class Collectible : MonoBehaviour
{
    Vector3 home;
    float phase;
    bool taken;
    SpriteRenderer sr, halo;
    Rigidbody2D rb;
    Color baseColor = Color.white;

    void Start()
    {
        home = transform.position;
        phase = Random.value * 10f;
        sr = GetComponent<SpriteRenderer>();
        if (sr != null) baseColor = sr.color;

        // Kinematic body so the bobbing trigger moves cleanly instead of rebuilding a static collider every frame.
        rb = GetComponent<Rigidbody2D>();
        if (rb == null) rb = gameObject.AddComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;

        var h = new GameObject("Glow");
        h.transform.SetParent(transform, false);
        halo = h.AddComponent<SpriteRenderer>();
        halo.sprite = ComicUI.GlowSprite;
        var unlit = ComicUI.UnlitSprite;
        if (unlit != null) halo.sharedMaterial = unlit;
        if (sr != null) { halo.sortingLayerID = sr.sortingLayerID; halo.sortingOrder = sr.sortingOrder - 1; }
        // Glow ~1.6 world units across regardless of the collectible's own scale.
        float ls = Mathf.Max(0.01f, Mathf.Abs(transform.lossyScale.x));
        h.transform.localScale = Vector3.one * (1.6f / ls);
    }

    void Update()
    {
        if (taken) return;
        float t = Time.time + phase;
        rb.MovePosition(home + Vector3.up * Mathf.Sin(t * 2.4f) * 0.12f);
        float pulse = 0.5f + 0.5f * Mathf.Sin(t * 4f);
        if (halo != null) halo.color = new Color(ComicUI.Cyan.r, ComicUI.Cyan.g, ComicUI.Cyan.b, 0.35f + 0.3f * pulse);
        if (sr != null) sr.color = Color.Lerp(baseColor, Color.white, 0.35f * pulse);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (taken) return;
        bool player = other.GetComponentInParent<PlayerController>() != null;
        bool ball = other.CompareTag("Ball");
        if (!player && !ball) return;
        if (GameManager.Frozen) return;
        taken = true;
        foreach (var c in GetComponents<Collider2D>()) c.enabled = false;
        LevelStats.AddCollectible();
        GameplayAudio.Pickup();
        if (ComboMeter.Instance != null)
            ComboMeter.Instance.Pop(transform.position, "+" + LevelStats.CollectiblePoints, 52, ComicUI.Cyan, ComicUI.Pink, 0.9f);
        StartCoroutine(Burst());
    }

    IEnumerator Burst()
    {
        const int n = 10;
        var unlit = ComicUI.UnlitSprite;
        var sparks = new Transform[n];
        var vel = new Vector2[n];
        var srs = new SpriteRenderer[n];
        for (int i = 0; i < n; i++)
        {
            var g = new GameObject("Spark");
            g.transform.position = transform.position;
            var s = g.AddComponent<SpriteRenderer>();
            s.sprite = ComicUI.StarSprite;
            if (unlit != null) s.sharedMaterial = unlit;
            s.sortingOrder = 40;
            s.color = i % 3 == 0 ? ComicUI.Pink : i % 3 == 1 ? ComicUI.Cyan : Color.white;
            float a = (i / (float)n) * Mathf.PI * 2f + Random.Range(-0.2f, 0.2f);
            vel[i] = new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * Random.Range(2.5f, 4.5f);
            g.transform.localScale = Vector3.one * Random.Range(0.35f, 0.6f);
            sparks[i] = g.transform; srs[i] = s;
        }
        Vector3 s0 = transform.localScale;
        for (float t = 0; t < 0.55f; t += Time.deltaTime)
        {
            float k = t / 0.55f;
            transform.localScale = s0 * (k < 0.15f ? 1f + k * 3f : Mathf.Max(0f, 1.45f * (1f - (k - 0.15f) / 0.5f)));
            for (int i = 0; i < n; i++)
            {
                sparks[i].position += (Vector3)(vel[i] * Time.deltaTime);
                vel[i] *= 0.9f;
                sparks[i].Rotate(0, 0, 300f * Time.deltaTime);
                var c = srs[i].color; c.a = 1f - k; srs[i].color = c;
            }
            yield return null;
        }
        foreach (var s in sparks) Destroy(s.gameObject);
        gameObject.SetActive(false);
    }
}
