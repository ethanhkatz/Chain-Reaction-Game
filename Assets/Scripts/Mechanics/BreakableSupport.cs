using System.Collections;
using UnityEngine;

// A cyan pillar that holds up a FallingPlatform. The ball smashes it (hitsToBreak hits); when it breaks it
// tumbles away and tells its platform to let go. The player walks through supports so only the ball matters.
[RequireComponent(typeof(Collider2D))]
public class BreakableSupport : MonoBehaviour
{
    [SerializeField] private FallingPlatform platform;
    [SerializeField] private int hitsToBreak = 1;
    [SerializeField] private float minImpactSpeed = 1.2f;
    [SerializeField] private bool ignorePlayer = true;
    [SerializeField] private Color crackedTint = new Color(0.6f, 0.75f, 0.75f);

    private int hits;
    private bool broken;
    private float lastHitTime = -1f;

    public bool IsBroken => broken;
    public FallingPlatform GetPlatform() => platform;

    private void Start()
    {
        if (ignorePlayer)
        {
            var player = FindAnyObjectByType<PlayerController>();
            if (player != null)
                foreach (var pc in player.GetComponentsInChildren<Collider2D>())
                    foreach (var mine in GetComponentsInChildren<Collider2D>())
                        Physics2D.IgnoreCollision(pc, mine);
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (broken || !collision.gameObject.CompareTag("Ball")) return;
        if (collision.relativeVelocity.magnitude < minImpactSpeed) return;
        if (Time.time - lastHitTime < 0.25f) return;
        lastHitTime = Time.time;
        hits++;
        if (hits >= hitsToBreak) Break(collision.transform.position.x < transform.position.x ? 1f : -1f);
        else StartCoroutine(Shake());
    }

    public void Break(float direction = 1f)
    {
        if (broken) return;
        broken = true;
        // Debris keeps its collider so it lands on the floor, but never blocks the player, ball or its platform.
        var mine = GetComponentsInChildren<Collider2D>();
        var others = new System.Collections.Generic.List<Collider2D>();
        var ball = GameObject.FindWithTag("Ball");
        if (ball != null) others.AddRange(ball.GetComponentsInChildren<Collider2D>());
        if (platform != null) others.AddRange(platform.GetComponentsInChildren<Collider2D>());
        foreach (var m in mine) foreach (var o in others) Physics2D.IgnoreCollision(m, o);
        if (platform != null) platform.Release();
        var rb = GetComponent<Rigidbody2D>();
        if (rb == null) rb = gameObject.AddComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Dynamic;
        rb.gravityScale = 2f;
        rb.linearVelocity = new Vector2(3f * direction, 2f);
        rb.angularVelocity = -200f * direction;
        StartCoroutine(FadeAndDie());
    }

    private IEnumerator Shake()
    {
        foreach (var r in GetComponentsInChildren<SpriteRenderer>()) r.color = crackedTint;
        Vector3 home = transform.position;
        for (float t = 0; t < 0.2f; t += Time.deltaTime)
        {
            transform.position = home + (Vector3)(Random.insideUnitCircle * 0.08f);
            yield return null;
        }
        transform.position = home;
    }

    private IEnumerator FadeAndDie()
    {
        var renderers = GetComponentsInChildren<SpriteRenderer>();
        for (float t = 0; t < 1f; t += Time.deltaTime)
        {
            foreach (var r in renderers) { var c = r.color; c.a = 1f - t; r.color = c; }
            yield return null;
        }
        Destroy(gameObject);
    }
}
