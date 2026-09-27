using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// Hover/selection scale-up and a click sound for any Button (added to every scene button by ShellBootstrap).
public class ButtonFx : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, ISelectHandler, IDeselectHandler
{
    public float hoverScale = 1.08f;
    Vector3 baseScale;
    bool hot;

    void Awake()
    {
        baseScale = transform.localScale;
        var b = GetComponent<Button>();
        if (b != null) b.onClick.AddListener(() => ShellAudio.Play(ShellAudio.Sfx.Click, 0.6f, 0.02f));
    }

    void OnDisable() { hot = false; transform.localScale = baseScale; }

    public void OnPointerEnter(PointerEventData e) => hot = true;
    public void OnPointerExit(PointerEventData e) => hot = false;
    public void OnSelect(BaseEventData e) => hot = true;
    public void OnDeselect(BaseEventData e) => hot = false;

    void Update()
    {
        var target = hot ? baseScale * hoverScale : baseScale;
        transform.localScale = Vector3.Lerp(transform.localScale, target, 1f - Mathf.Exp(-Time.unscaledDeltaTime * 18f));
    }
}
