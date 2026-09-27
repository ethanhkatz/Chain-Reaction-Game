using UnityEngine;

// Added to the Ball at runtime: turns hard hits into shake, dust, hit-pause and a crash pop.
public class JamBallImpact : MonoBehaviour
{
    public float puffSpeed = 4f;
    public float bigSpeed = 9f;
    float cooldown;

    void OnCollisionEnter2D(Collision2D c)
    {
        if (c.collider.GetComponentInParent<PlayerController>() != null) return;
        float v = c.relativeVelocity.magnitude;
        if (Application.isBatchMode && v > 2f) Debug.Log($"JAM: ball impact v={v:0.0} with {c.collider.name}");
        if (v < puffSpeed || Time.unscaledTime < cooldown) return;
        cooldown = Time.unscaledTime + 0.08f;
        Vector2 p = c.contactCount > 0 ? c.GetContact(0).point : (Vector2)transform.position;
        JamAtmosphere.Puff(p, Mathf.Clamp((int)(v * 1.2f), 4, 18), Mathf.Clamp(v * 0.3f, 1f, 4f));
        JamAtmosphere.Shake(Mathf.Clamp(v * 0.025f, 0.05f, 0.35f));
        if (v >= bigSpeed)
        {
            JamAtmosphere.Shake(Mathf.Clamp(v * 0.04f, 0.4f, 0.8f));
            JamAtmosphere.HitPause(0.04f);
            JamAtmosphere.Crash(p);
        }
    }
}
