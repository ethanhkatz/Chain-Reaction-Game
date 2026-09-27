using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// Hover/selection scale-up and a click sound for any Button (added to every scene button by ShellBootstrap).
public class ButtonFx : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, ISelectHandler, IDeselectHandler
{
    public float hoverScale = 1.08f;
    public Color hotTint = new Color(0.62f, 1f, 0.9f, 1f);
    Vector3 baseScale;
    bool hovered, selected;
    bool hot => hovered || selected || (EventSystem.current != null && EventSystem.current.currentSelectedGameObject == gameObject);
    Graphic graphic;
    Color baseColor;
    float heat;

    void Awake()
    {
        baseScale = transform.localScale;
        var b = GetComponent<Button>();
        // Drive the tint ourselves so hover and keyboard selection look identical on every button.
        if (b != null) { graphic = b.targetGraphic; b.transition = Selectable.Transition.None; }
        if (graphic != null) baseColor = graphic.color;
        if (b != null) b.onClick.AddListener(() => ShellAudio.Play(ShellAudio.Sfx.Click, 0.6f, 0.02f));
    }

    // Light art gets tinted cyan; dark panels get mixed toward cyan so the change is visible either way.
    Color HotColor()
    {
        var c = baseColor.maxColorComponent > 0.5f ? baseColor * hotTint : Color.Lerp(baseColor, new Color(0.36f, 0.95f, 0.84f), 0.35f);
        c.a = baseColor.a;
        return c;
    }

    void OnDisable()
    {
        hovered = selected = false; heat = 0f;
        transform.localScale = baseScale;
        if (graphic != null) graphic.color = baseColor;
    }

    public void OnPointerEnter(PointerEventData e) => hovered = true;
    public void OnPointerExit(PointerEventData e) => hovered = false;
    public void OnSelect(BaseEventData e) => selected = true;
    public void OnDeselect(BaseEventData e) => selected = false;

    void Update()
    {
        heat = Mathf.MoveTowards(heat, hot ? 1f : 0f, Time.unscaledDeltaTime * 8f);
        transform.localScale = baseScale * Mathf.Lerp(1f, hoverScale, heat);
        if (graphic != null) graphic.color = Color.Lerp(baseColor, HotColor(), heat);
    }
}
