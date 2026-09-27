using System.Collections;
using UnityEngine;

// A cyan platform held in place until one of its BreakableSupports is smashed.
//  Drop  - becomes a dynamic body and falls (can land on a PressButton, crush things, etc).
//  Hinge - swings about a world-space pivot to targetAngle, like a drawbridge or trapdoor, then locks in place.
[RequireComponent(typeof(Rigidbody2D))]
public class FallingPlatform : MonoBehaviour
{
    public enum Mode { Drop, Hinge }

    [SerializeField] private Mode mode = Mode.Drop;
    [SerializeField] private float dropMass = 5f;
    [SerializeField] private bool freezeRotationWhenDropping = true;
    [SerializeField] private Vector2 hingePivot;
    [SerializeField] private float targetAngle = -90f;
    [SerializeField] private float angularAcceleration = 500f;
    [SerializeField] private Sprite impactFx;

    private Rigidbody2D rb;
    private bool released;
    private bool impacted;
    private bool swinging;
    private float startAngle, angle, angularSpeed;
    private Vector2 startOffset;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.useFullKinematicContacts = true;
    }

    public void Release()
    {
        if (released) return;
        released = true;
        // Every other support holding this platform gives way too.
        foreach (var s in FindObjectsByType<BreakableSupport>(FindObjectsSortMode.None))
            if (s.GetPlatform() == this) s.Break();

        if (mode == Mode.Drop)
        {
            rb.bodyType = RigidbodyType2D.Dynamic;
            rb.mass = dropMass;
            rb.gravityScale = 2f;
            if (freezeRotationWhenDropping) rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        }
        else
        {
            startAngle = angle = rb.rotation;
            startOffset = rb.position - hingePivot;
            swinging = true;
        }
    }

    private void FixedUpdate()
    {
        if (!swinging) return;
        float dir = Mathf.Sign(targetAngle - startAngle);
        angularSpeed += angularAcceleration * Time.fixedDeltaTime;
        angle += dir * angularSpeed * Time.fixedDeltaTime;
        bool done = dir > 0 ? angle >= targetAngle : angle <= targetAngle;
        if (done) angle = targetAngle;
        Vector2 pos = hingePivot + (Vector2)(Quaternion.Euler(0, 0, angle - startAngle) * startOffset);
        rb.MovePosition(pos);
        rb.MoveRotation(angle);
        if (done)
        {
            swinging = false;
            StartCoroutine(Impact());
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (mode == Mode.Drop && released && !impacted && collision.relativeVelocity.magnitude > 3f)
        {
            impacted = true;
            StartCoroutine(Impact());
        }
    }

    private IEnumerator Impact()
    {
        if (impactFx == null) yield break;
        var fx = new GameObject("ImpactFx");
        fx.transform.position = transform.position + Vector3.back * 0.1f;
        fx.transform.localScale = Vector3.one * 0.35f;
        var sr = fx.AddComponent<SpriteRenderer>();
        sr.sprite = impactFx;
        sr.sortingOrder = 10;
        for (float t = 0; t < 0.4f; t += Time.deltaTime)
        {
            sr.color = new Color(1, 1, 1, 1 - t / 0.4f);
            fx.transform.localScale = Vector3.one * (0.35f + t * 0.5f);
            yield return null;
        }
        Destroy(fx);
    }
}
