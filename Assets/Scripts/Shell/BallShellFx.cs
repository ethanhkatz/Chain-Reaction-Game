using UnityEngine;

// Added to the Ball at runtime: impact thuds, and a crack when the ball hits something breakable.
public class BallShellFx : MonoBehaviour
{
    float lastHit;
    int playerLayer, rockLayer;

    void Awake()
    {
        playerLayer = LayerMask.NameToLayer("Player");
        rockLayer = LayerMask.NameToLayer("Rock");
    }

    void OnCollisionEnter2D(Collision2D c)
    {
        if (c.gameObject.layer == playerLayer) return;
        float speed = c.relativeVelocity.magnitude;
        if (speed < 1.5f || Time.time - lastHit < 0.07f) return;
        lastHit = Time.time;
        float vol = Mathf.Clamp01((speed - 1.5f) / 9f);
        var go = c.gameObject;
        bool breakable = go.layer == rockLayer || go.CompareTag("Rock")
            || go.GetComponentInParent<RockShape>() != null || go.GetComponentInParent<TippyRockController>() != null;
        if (breakable)
        {
            ShellAudio.Play(ShellAudio.Sfx.Crack, 0.35f + 0.6f * vol, 0.1f);
            ShellAudio.Play(ShellAudio.Sfx.Thud, 0.3f * vol);
        }
        else
        {
            ShellAudio.Play(ShellAudio.Sfx.Thud, 0.15f + 0.6f * vol);
        }
    }
}
