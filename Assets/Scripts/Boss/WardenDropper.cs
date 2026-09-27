using UnityEngine;

// Holds a body in place until activated (by a PressButton), then lets gravity have it.
[RequireComponent(typeof(Rigidbody2D))]
public class WardenDropper : MonoBehaviour, IActivatable
{
    public float gravity = 3f;
    Rigidbody2D rb;
    bool dropped;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;
    }

    public void Activate()
    {
        if (dropped) return;
        dropped = true;
        rb.bodyType = RigidbodyType2D.Dynamic;
        rb.gravityScale = gravity;
        rb.linearVelocity = new Vector2(0, -2f);
        JamAtmosphere.Shake(0.15f);
    }
}
