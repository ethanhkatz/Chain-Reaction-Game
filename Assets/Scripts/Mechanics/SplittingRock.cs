using System.Collections;
using UnityEngine;

// Big cracked rock. A hard enough hit from the ball (relative impact speed >= breakSpeed) splits it into
// several smaller dynamic rocks (layer Rock, tag Rock) that the player can push around. Weak hits just wobble it.
[RequireComponent(typeof(Collider2D))]
public class SplittingRock : MonoBehaviour
{
    [SerializeField] private float breakSpeed = 4.5f;
    [SerializeField] private Sprite[] pieceSprites;
    [SerializeField] private int pieceCount = 3;
    [SerializeField] private float pieceScale = 0.4f;
    [SerializeField] private float pieceMass = 1f;
    [SerializeField] private Color pieceColor = new Color(0.55f, 1f, 0.9f);
    [SerializeField] private Sprite crashSprite;
    [SerializeField] private float crashScale = 0.35f;
    [SerializeField] private float crashTime = 0.45f;

    private bool split;
    private Vector3 restPos;

    private Rigidbody2D ballBody;
    private float ballSpeed; // ball speed from the last physics step; contact relativeVelocity is post-solve (~0)

    private void Awake() => restPos = transform.position;

    private void Start()
    {
        var ball = GameObject.FindWithTag("Ball");
        if (ball != null) ballBody = ball.GetComponent<Rigidbody2D>();
    }

    private void FixedUpdate()
    {
        if (ballBody != null) ballSpeed = ballBody.linearVelocity.magnitude;
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (split || !collision.collider.CompareTag("Ball")) return;
        float speed = Mathf.Max(collision.relativeVelocity.magnitude, ballSpeed);
        if (speed >= breakSpeed) Split(collision.transform.position);
        else StartCoroutine(Wobble());
    }

    public void Split(Vector3 hitFrom)
    {
        if (split) return;
        split = true;
        ChainEvents.Report(transform.position, "split");
        StopAllCoroutines();
        var sr = GetComponent<SpriteRenderer>();
        foreach (var c in GetComponentsInChildren<Collider2D>()) c.enabled = false;

        Bounds b = sr != null ? sr.bounds : new Bounds(transform.position, Vector3.one * 2f);
        float away = hitFrom.x < transform.position.x ? 1f : -1f;
        int n = Mathf.Max(1, pieceCount);
        for (int i = 0; i < n; i++)
        {
            float t = n == 1 ? 0.5f : (float)i / (n - 1);
            var p = new GameObject("RockPiece" + i);
            p.layer = LayerMask.NameToLayer("Rock");
            p.tag = "Rock";
            p.transform.position = new Vector3(Mathf.Lerp(b.min.x + b.extents.x * 0.4f, b.max.x - b.extents.x * 0.4f, t),
                                               b.min.y + b.size.y * (0.25f + 0.25f * (i % 2)), transform.position.z);
            p.transform.localScale = Vector3.one * pieceScale;
            var psr = p.AddComponent<SpriteRenderer>();
            psr.sprite = pieceSprites != null && pieceSprites.Length > 0 ? pieceSprites[i % pieceSprites.Length] : (sr ? sr.sprite : null);
            psr.color = pieceColor;
            if (sr != null) { psr.sortingLayerID = sr.sortingLayerID; psr.sortingOrder = sr.sortingOrder; }
            var col = p.AddComponent<BoxCollider2D>();
            if (psr.sprite != null) col.size = (Vector2)psr.sprite.bounds.size * 0.95f;
            var rb = p.AddComponent<Rigidbody2D>();
            rb.mass = pieceMass;
            rb.angularDamping = 2f;
            rb.linearVelocity = new Vector2(away * (1.5f + 2f * t), 2f);
            rb.angularVelocity = Random.Range(-40f, 40f);
        }

        if (sr != null) sr.enabled = false;
        if (crashSprite != null)
        {
            var fx = new GameObject("CrashFx");
            fx.transform.position = new Vector3(b.center.x, b.center.y, transform.position.z - 0.1f);
            fx.transform.localScale = Vector3.one * crashScale;
            var fsr = fx.AddComponent<SpriteRenderer>();
            fsr.sprite = crashSprite;
            fsr.sortingOrder = (sr ? sr.sortingOrder : 0) + 5;
            StartCoroutine(FadeAndDestroy(fsr));
        }
        else Destroy(gameObject);
    }

    private IEnumerator FadeAndDestroy(SpriteRenderer fx)
    {
        Vector3 s0 = fx.transform.localScale;
        for (float t = 0; t < crashTime; t += Time.deltaTime)
        {
            float k = t / crashTime;
            fx.transform.localScale = s0 * (1f + 0.4f * k);
            var c = fx.color; c.a = 1f - k * k; fx.color = c;
            yield return null;
        }
        Destroy(fx.gameObject);
        Destroy(gameObject);
    }

    private IEnumerator Wobble()
    {
        for (float t = 0; t < 0.25f; t += Time.deltaTime)
        {
            transform.position = restPos + new Vector3(Mathf.Sin(t * 80f) * 0.08f * (1f - t / 0.25f), 0f, 0f);
            yield return null;
        }
        transform.position = restPos;
    }
}
