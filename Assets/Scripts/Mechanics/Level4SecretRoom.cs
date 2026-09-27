using System.Collections;
using UnityEngine;

// Hidden room behind a fake wall. When the player walks in, the cover rock fades away and an alert "!" pops over
// the player's head. Standing behind the cardboard box hides you, and the "!" goes away.
[RequireComponent(typeof(Collider2D))]
public class Level4SecretRoom : MonoBehaviour
{
    [SerializeField] private SpriteRenderer[] cover;
    [SerializeField] private Transform alert;
    [SerializeField] private Collider2D hideZone;
    [SerializeField] private float fadeTime = 0.6f;

    private Transform player;
    private bool revealed;
    private float popTime;

    private void Start()
    {
        if (alert != null) alert.gameObject.SetActive(false);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        var pc = other.GetComponentInParent<PlayerController>();
        if (pc == null) return;
        player = pc.transform;
        if (revealed) return;
        revealed = true;
        StartCoroutine(Fade());
        popTime = Time.time;
        if (alert != null) alert.gameObject.SetActive(true);
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.GetComponentInParent<PlayerController>() != null && alert != null) alert.gameObject.SetActive(false);
    }

    private void LateUpdate()
    {
        if (!revealed || alert == null || player == null) return;
        bool inRoom = GetComponent<Collider2D>().OverlapPoint(player.position);
        bool hidden = hideZone != null && hideZone.OverlapPoint(player.position);
        alert.gameObject.SetActive(inRoom && !hidden);
        if (!alert.gameObject.activeSelf) { popTime = Time.time; return; }
        // pop in over the player's head, then bob
        float t = Time.time - popTime;
        float s = t < 0.15f ? Mathf.Lerp(0.2f, 1.3f, t / 0.15f) : Mathf.Lerp(1.3f, 1f, Mathf.Clamp01((t - 0.15f) / 0.1f));
        alert.localScale = Vector3.one * s;
        alert.position = player.position + new Vector3(0f, 2.1f + Mathf.Sin(t * 6f) * 0.05f, 0f);
    }

    private IEnumerator Fade()
    {
        for (float t = 0; t < fadeTime; t += Time.deltaTime)
        {
            foreach (var r in cover) { var c = r.color; c.a = 1f - t / fadeTime; r.color = c; }
            yield return null;
        }
        foreach (var r in cover) r.gameObject.SetActive(false);
    }
}
