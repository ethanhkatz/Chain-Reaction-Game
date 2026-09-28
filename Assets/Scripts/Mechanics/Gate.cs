using System.Collections;
using UnityEngine;

// Blocking gate (e.g. the laser gate art). Activate() slides it by openOffset and disables its colliders.
public class Gate : MonoBehaviour, IActivatable
{
    [SerializeField] private Vector2 openOffset = new Vector2(0f, 4f);
    [SerializeField] private float openTime = 0.6f;
    [SerializeField] private bool fadeOut = true;

    private bool open;

    public void Activate()
    {
        if (open) return;
        open = true;
        ChainEvents.Report(transform.position, "gate");
        foreach (var c in GetComponentsInChildren<Collider2D>()) c.enabled = false;
        StartCoroutine(Open());
    }

    private IEnumerator Open()
    {
        Vector3 from = transform.position, to = from + (Vector3)openOffset;
        var renderers = GetComponentsInChildren<SpriteRenderer>();
        for (float t = 0; t < openTime; t += Time.deltaTime)
        {
            float k = Mathf.SmoothStep(0, 1, t / openTime);
            transform.position = Vector3.Lerp(from, to, k);
            if (fadeOut) foreach (var r in renderers) { var c = r.color; c.a = 1 - k; r.color = c; }
            yield return null;
        }
        transform.position = to;
        if (fadeOut) gameObject.SetActive(false);
    }
}
