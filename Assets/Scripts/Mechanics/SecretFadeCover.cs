using System.Collections;
using UnityEngine;

// A fake wall drawn in front of a hidden room. When the player steps inside its trigger it fades away,
// revealing the room; it fades back when they leave.
[RequireComponent(typeof(Collider2D))]
public class SecretFadeCover : MonoBehaviour
{
    [SerializeField] private float fadeTime = 0.35f;

    private SpriteRenderer[] renderers;
    private float target = 1f, alpha = 1f;
    private int inside;

    private void Awake() => renderers = GetComponentsInChildren<SpriteRenderer>();

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.GetComponentInParent<PlayerController>() == null) return;
        inside++;
        target = 0f;
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.GetComponentInParent<PlayerController>() == null) return;
        inside = Mathf.Max(0, inside - 1);
        if (inside == 0) target = 1f;
    }

    private void Update()
    {
        if (Mathf.Approximately(alpha, target)) return;
        alpha = Mathf.MoveTowards(alpha, target, Time.deltaTime / fadeTime);
        foreach (var r in renderers) { var c = r.color; c.a = alpha; r.color = c; }
    }
}
