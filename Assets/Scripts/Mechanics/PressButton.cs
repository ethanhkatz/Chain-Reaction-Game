using UnityEngine;

// Pressure button. Pressed by the ball, a rock, or any rigidbody at least minMass heavy landing on its trigger.
// Latches once pressed and activates every target that implements IActivatable.
[RequireComponent(typeof(Collider2D))]
public class PressButton : MonoBehaviour
{
    [SerializeField] private MonoBehaviour[] targets;
    [SerializeField] private float minMass = 0.4f;
    [SerializeField] private bool playerCanPress = false;
    [SerializeField] private Sprite pressedSprite;

    private bool pressed;

    private void OnTriggerEnter2D(Collider2D other) => TryPress(other);
    private void OnCollisionEnter2D(Collision2D collision) => TryPress(collision.collider);

    private void TryPress(Collider2D other)
    {
        if (pressed) return;
        var body = other.attachedRigidbody;
        bool isPlayer = other.GetComponentInParent<PlayerController>() != null;
        if (isPlayer && !playerCanPress) return;
        bool heavy = body != null && body.mass >= minMass;
        if (!heavy && !other.CompareTag("Ball")) return;
        Press();
    }

    public void Press()
    {
        pressed = true;
        ChainEvents.Report(transform.position, "button");
        if (pressedSprite != null && TryGetComponent(out SpriteRenderer sr)) sr.sprite = pressedSprite;
        foreach (var t in targets)
            if (t is IActivatable a) a.Activate();
    }
}
