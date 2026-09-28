using System.Collections;
using UnityEngine;

// A thin wall the ball smashes in one hit. It bursts into fading chunks that never collide with anything,
// so rubble can't trap the player or the ball. With ignorePlayer on, the player can walk straight through it
// (a fake/cracked wall) and the trailing ball smashes it on the way.
[RequireComponent(typeof(Collider2D))]
public class SmashWall : MonoBehaviour
{
    [SerializeField] private float breakSpeed = 1.5f;
    [SerializeField] private bool ignorePlayer;
    [SerializeField] private Sprite[] chunkSprites;
    [SerializeField] private int chunkCount = 6;
    [SerializeField] private float chunkScale = 0.25f;
    [SerializeField] private Color chunkColor = Color.white;
    [SerializeField] private Sprite crashSprite;
    [SerializeField] private float crashScale = 0.3f;
    [SerializeField] private string reportKind = "smash";

    private bool smashed;
    private Rigidbody2D ballBody;
    private float ballSpeed;

    private void Start()
    {
        var ball = GameObject.FindWithTag("Ball");
        if (ball != null) ballBody = ball.GetComponent<Rigidbody2D>();
        if (ignorePlayer)
        {
            var player = FindAnyObjectByType<PlayerController>();
            if (player != null)
                foreach (var pc in player.GetComponentsInChildren<Collider2D>())
                    foreach (var mine in GetComponentsInChildren<Collider2D>())
                        Physics2D.IgnoreCollision(pc, mine);
        }
    }

    private void FixedUpdate()
    {
        if (ballBody != null) ballSpeed = ballBody.linearVelocity.magnitude;
    }

    private void OnCollisionEnter2D(Collision2D collision) => TryHit(collision);
    private void OnCollisionStay2D(Collision2D collision) => TryHit(collision);

    private void TryHit(Collision2D collision)
    {
        if (smashed || !collision.collider.CompareTag("Ball")) return;
        float speed = Mathf.Max(collision.relativeVelocity.magnitude, ballSpeed);
        if (speed >= breakSpeed) Smash(collision.transform.position);
    }

    public void Smash(Vector3 hitFrom)
    {
        if (smashed) return;
        smashed = true;
        var sr = GetComponent<SpriteRenderer>();
        Bounds b = sr != null ? sr.bounds : new Bounds(transform.position, Vector3.one);
        foreach (var c in GetComponentsInChildren<Collider2D>()) c.enabled = false;
        foreach (var r in GetComponentsInChildren<SpriteRenderer>()) r.enabled = false;
        float away = hitFrom.x < transform.position.x ? 1f : -1f;
        int order = sr != null ? sr.sortingOrder + 1 : 5;

        for (int i = 0; i < chunkCount; i++)
        {
            var p = new GameObject("WallChunk");
            p.transform.position = new Vector3(b.center.x + Random.Range(-b.extents.x, b.extents.x),
                                               b.min.y + b.size.y * (i + 0.5f) / chunkCount, transform.position.z);
            p.transform.localScale = Vector3.one * chunkScale * Random.Range(0.7f, 1.2f);
            var psr = p.AddComponent<SpriteRenderer>();
            psr.sprite = chunkSprites != null && chunkSprites.Length > 0 ? chunkSprites[i % chunkSprites.Length] : (sr ? sr.sprite : null);
            psr.color = chunkColor;
            psr.sortingOrder = order;
            var rb = p.AddComponent<Rigidbody2D>();
            rb.gravityScale = 2.5f;
            rb.linearVelocity = new Vector2(away * Random.Range(2f, 6f), Random.Range(1f, 6f));
            rb.angularVelocity = Random.Range(-400f, 400f);
            StartCoroutine(FadeAndDestroy(psr, 0.9f, 1f));
        }

        if (crashSprite != null)
        {
            var fx = new GameObject("SmashFx");
            fx.transform.position = new Vector3(b.center.x, b.center.y, transform.position.z - 0.1f);
            fx.transform.localScale = Vector3.one * crashScale;
            var fsr = fx.AddComponent<SpriteRenderer>();
            fsr.sprite = crashSprite;
            fsr.sortingOrder = order + 5;
            StartCoroutine(FadeAndDestroy(fsr, 0.4f, 1.5f));
        }

        ReportChainEvent(b.center);
        StartCoroutine(DieLater());
    }

    // Shared chain-event hook, if the project has one (looked up by name so this file compiles either way).
    private void ReportChainEvent(Vector3 pos)
    {
        var t = System.Type.GetType("ChainEvents");
        var m = t?.GetMethod("Report", new[] { typeof(Vector3), typeof(string) });
        m?.Invoke(null, new object[] { pos, reportKind });
    }

    private IEnumerator FadeAndDestroy(SpriteRenderer r, float time, float grow)
    {
        Vector3 s0 = r.transform.localScale;
        for (float t = 0; t < time && r != null; t += Time.deltaTime)
        {
            float k = t / time;
            r.transform.localScale = s0 * (1f + (grow - 1f) * k);
            var c = r.color; c.a = 1f - k * k; r.color = c;
            yield return null;
        }
        if (r != null) Destroy(r.gameObject);
    }

    private IEnumerator DieLater()
    {
        yield return new WaitForSeconds(1.6f);
        Destroy(gameObject);
    }
}
