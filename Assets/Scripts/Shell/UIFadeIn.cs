using UnityEngine;

// Fades a CanvasGroup from 0 to 1 on start (unscaled time).
[RequireComponent(typeof(CanvasGroup))]
public class UIFadeIn : MonoBehaviour
{
    public float delay = 0f;
    public float duration = 1f;
    CanvasGroup group;
    float t;

    void Awake() { group = GetComponent<CanvasGroup>(); group.alpha = 0f; }

    void Update()
    {
        t += Time.unscaledDeltaTime;
        group.alpha = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((t - delay) / duration));
        if (t > delay + duration) enabled = false;
    }
}
