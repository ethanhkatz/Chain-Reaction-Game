using UnityEngine;

// Added to the Player at runtime: a dust puff when landing from a real fall.
public class JamLandingFx : MonoBehaviour
{
    Rigidbody2D rb;
    float lastVy;

    void Awake() => rb = GetComponent<Rigidbody2D>();

    void FixedUpdate() { if (rb != null) lastVy = rb.linearVelocity.y; }

    void OnCollisionEnter2D(Collision2D c)
    {
        if (lastVy > -4f || c.contactCount == 0 || c.GetContact(0).normal.y < 0.5f) return;
        if (c.collider.CompareTag("Ball")) return;
        JamAtmosphere.Puff(c.GetContact(0).point, Mathf.Clamp((int)(-lastVy), 4, 10), 1.2f);
        if (lastVy < -12f) JamAtmosphere.Shake(0.12f);
    }
}
