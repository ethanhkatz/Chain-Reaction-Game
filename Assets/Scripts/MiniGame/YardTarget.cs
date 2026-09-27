using System.Collections;
using UnityEngine;

// A cardboard guard cutout for Yard Break. Springs up from the floor (or slides in from the side), waits, then ducks
// away. A fast enough hit from the ball tears it in half and sends the pieces spinning off.
public class YardTarget : MonoBehaviour
{
    public const float SmashSpeed = 2.6f;
    static readonly Color Gold = new Color(1f, 0.82f, 0.25f);

    YardBreakGame game;
    SpriteRenderer sr;
    Transform body;
    BoxCollider2D box;
    public bool IsGold { get; private set; }
    public int Slot { get; private set; }
    bool alive, smashed;
    float lifetime;
    float wobble;

    public void Init(YardBreakGame game, Sprite sprite, float height, int slot, bool gold, float life, float slideFrom)
    {
        this.game = game;
        Slot = slot;
        IsGold = gold;
        lifetime = life;

        body = new GameObject("Cutout").transform;
        body.SetParent(transform, false);
        sr = body.gameObject.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.sortingOrder = 2;
        float scale = height / sprite.bounds.size.y;
        body.localScale = Vector3.one * scale;
        body.localPosition = new Vector3(0, height * 0.5f, 0);
        if (gold) sr.color = Gold;

        box = gameObject.AddComponent<BoxCollider2D>();
        box.isTrigger = true;
        box.size = new Vector2(sprite.bounds.size.x * scale * 0.8f, height * 0.9f);
        box.offset = new Vector2(0, height * 0.48f);

        StartCoroutine(slideFrom == 0 ? PopUp() : SlideIn(slideFrom));
    }

    IEnumerator PopUp()
    {
        // spring out of the floor: squash -> overshoot -> settle
        transform.localScale = new Vector3(1.2f, 0f, 1f);
        const float dur = 0.38f;
        for (float t = 0; t < dur; t += Time.deltaTime)
        {
            float k = t / dur;
            float y = 1f + Mathf.Sin(k * Mathf.PI * 2.2f) * (1f - k) * 0.35f;
            y = Mathf.Min(y, Mathf.SmoothStep(0, 1.4f, k * 2.2f));
            transform.localScale = new Vector3(Mathf.Lerp(1.25f, 1f, k), y, 1f);
            yield return null;
        }
        transform.localScale = Vector3.one;
        alive = true;
        yield return Hold();
    }

    IEnumerator SlideIn(float fromX)
    {
        Vector3 end = transform.position, start = new Vector3(fromX, end.y, 0);
        const float dur = 0.45f;
        alive = true;
        for (float t = 0; t < dur && !smashed; t += Time.deltaTime)
        {
            float k = 1f - Mathf.Pow(1f - t / dur, 3f);
            transform.position = Vector3.LerpUnclamped(start, end, k);
            body.localRotation = Quaternion.Euler(0, 0, Mathf.Sign(end.x - fromX) * -12f * (1f - k));
            yield return null;
        }
        if (smashed) yield break;
        transform.position = end;
        body.localRotation = Quaternion.identity;
        yield return Hold();
    }

    IEnumerator Hold()
    {
        for (float t = 0; t < lifetime && !smashed; t += Time.deltaTime)
        {
            // idle sway; a warning shiver in the last half second before it ducks away
            float shiver = t > lifetime - 0.5f ? Mathf.Sin(Time.time * 60f) * 3f : 0f;
            wobble = Mathf.MoveTowards(wobble, 0, Time.deltaTime * 40f);
            body.localRotation = Quaternion.Euler(0, 0, Mathf.Sin(Time.time * 2.3f + Slot) * 2f + shiver + Mathf.Sin(Time.time * 30f) * wobble);
            if (IsGold) sr.color = Color.Lerp(Gold, Color.white, 0.25f + 0.25f * Mathf.Sin(Time.time * 8f));
            yield return null;
        }
        if (smashed) yield break;
        alive = false;
        game.Release(this);
        const float dur = 0.22f;
        for (float t = 0; t < dur; t += Time.deltaTime)
        {
            transform.localScale = new Vector3(1f, 1f - t / dur, 1f);
            yield return null;
        }
        Destroy(gameObject);
    }

    void OnTriggerEnter2D(Collider2D other) => Check(other);
    void OnTriggerStay2D(Collider2D other) => Check(other);

    void Check(Collider2D other)
    {
        if (!alive || smashed || game == null || !game.Running) return;
        if (!other.CompareTag("Ball")) return;
        var rb = other.attachedRigidbody;
        float v = rb != null ? rb.linearVelocity.magnitude : 0f;
        if (v < SmashSpeed) { wobble = Mathf.Max(wobble, 6f); return; }
        Smash(other.transform.position, rb != null ? rb.linearVelocity : Vector2.right);
    }

    void Smash(Vector2 ballPos, Vector2 ballVel)
    {
        smashed = true;
        alive = false;
        box.enabled = false;
        Vector2 center = sr.bounds.center;
        game.OnSmash(this, center);

        // tear the cutout into a top and bottom half that tumble away in different directions
        var tex = sr.sprite.texture;
        var r = sr.sprite.rect;
        float ppu = sr.sprite.pixelsPerUnit;
        float dir = Mathf.Sign(ballVel.x == 0 ? center.x - ballPos.x : ballVel.x);
        float tear = 0.52f;
        for (int half = 0; half < 2; half++)
        {
            var rect = half == 0 ? new Rect(r.x, r.y + r.height * tear, r.width, r.height * (1 - tear)) : new Rect(r.x, r.y, r.width, r.height * tear);
            var piece = new GameObject(half == 0 ? "Torn Top" : "Torn Bottom");
            var psr = piece.AddComponent<SpriteRenderer>();
            psr.sprite = Sprite.Create(tex, rect, new Vector2(0.5f, 0.5f), ppu);
            psr.color = sr.color;
            psr.sortingOrder = 4;
            piece.transform.localScale = body.lossyScale;
            float h = sr.bounds.size.y;
            float yOff = half == 0 ? h * (tear + (1 - tear) * 0.5f) : h * tear * 0.5f;
            piece.transform.position = new Vector3(transform.position.x, transform.position.y + yOff, 0);
            piece.transform.rotation = body.rotation;
            var prb = piece.AddComponent<Rigidbody2D>();
            prb.gravityScale = 2.2f;
            prb.linearVelocity = new Vector2(dir * Random.Range(5f, 9f) * (half == 0 ? 1.1f : 0.7f), half == 0 ? Random.Range(8f, 11f) : Random.Range(4f, 6f));
            prb.angularVelocity = -dir * Random.Range(500f, 900f) * (half == 0 ? 1f : -0.6f);
            piece.AddComponent<YardDebris>();
        }
        Destroy(gameObject);
    }
}

// Torn cardboard: falls off-screen, fades, cleans itself up. Collides with nothing.
public class YardDebris : MonoBehaviour
{
    float t;
    SpriteRenderer sr;
    void Start() => sr = GetComponent<SpriteRenderer>();
    void Update()
    {
        t += Time.deltaTime;
        if (t > 1.1f && sr != null) { var c = sr.color; c.a = Mathf.Max(0, 1f - (t - 1.1f) / 0.5f); sr.color = c; }
        if (t > 1.7f) Destroy(gameObject);
    }
}
