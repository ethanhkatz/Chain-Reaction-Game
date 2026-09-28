using System.Collections;
using UnityEngine;

// Put on a FallingPlatform (Drop mode). The first time the player or the ball lands on it, it starts to shudder and
// gives way after `delay` seconds, whoever is still standing on it. Chain several for a bridge that collapses behind you.
[RequireComponent(typeof(FallingPlatform))]
public class CrumbleOnWeight : MonoBehaviour
{
    [SerializeField] private float delay = 1f;
    [SerializeField] private Color warnTint = new Color(1f, 0.55f, 0.25f);

    private bool triggered;

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (triggered) return;
        var other = collision.collider;
        bool heavy = other.CompareTag("Ball") || other.GetComponentInParent<PlayerController>() != null;
        if (!heavy) return;
        triggered = true;
        StartCoroutine(Crumble());
    }

    private IEnumerator Crumble()
    {
        var renderers = GetComponentsInChildren<SpriteRenderer>();
        var baseColors = new Color[renderers.Length];
        for (int i = 0; i < renderers.Length; i++) baseColors[i] = renderers[i].color;
        for (float t = 0; t < delay; t += Time.deltaTime)
        {
            // Flicker faster as it gets closer to letting go.
            float k = t / delay;
            float pulse = Mathf.PingPong(t * (4f + 16f * k), 1f) * k;
            for (int i = 0; i < renderers.Length; i++) renderers[i].color = Color.Lerp(baseColors[i], warnTint, pulse);
            yield return null;
        }
        for (int i = 0; i < renderers.Length; i++) renderers[i].color = Color.Lerp(baseColors[i], warnTint, 0.6f);
        GetComponent<FallingPlatform>().Release();
    }
}
