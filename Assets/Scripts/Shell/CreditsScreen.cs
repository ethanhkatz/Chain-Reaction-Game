using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

// Full-screen team credits art (scale-to-fit, letterboxed in the art's own border colour). Any key/click/Esc closes it.
public class CreditsScreen : MonoBehaviour
{
    static readonly Color Letterbox = new Color32(0xAF, 0xC0, 0xBA, 0xFF);

    System.Action onClose;
    CanvasGroup group;
    RectTransform art;
    float t;
    bool closing;

    public static bool IsOpen { get; private set; }

    public static void Show(System.Action onClose)
    {
        var sprite = ShellArt.Get() != null ? ShellArt.Get().credits : null;
        if (sprite == null) { onClose?.Invoke(); return; }
        var canvas = ShellUI.MakeCanvas("Credits", 600);
        var c = canvas.gameObject.AddComponent<CreditsScreen>();
        c.onClose = onClose;
        c.group = canvas.gameObject.AddComponent<CanvasGroup>();
        c.group.alpha = 0f;
        var bg = ShellUI.Rect("Letterbox", canvas.transform, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
        ShellUI.Stretch(bg);
        bg.gameObject.AddComponent<Image>().color = Letterbox; // also swallows clicks meant for the menu below
        c.art = ShellUI.Rect("Art", canvas.transform, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
        ShellUI.Stretch(c.art);
        var img = c.art.gameObject.AddComponent<Image>();
        img.sprite = sprite;
        img.raycastTarget = false;
        var fit = c.art.gameObject.AddComponent<AspectRatioFitter>();
        fit.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
        fit.aspectRatio = sprite.rect.width / sprite.rect.height;
        IsOpen = true;
    }

    void Update()
    {
        float dt = Mathf.Min(Time.unscaledDeltaTime, 0.05f);
        t += closing ? -dt * 2.2f : dt;
        float k = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t / 0.5f));
        group.alpha = k;
        art.localScale = Vector3.one * Mathf.Lerp(1.04f, 1f, k);
        if (closing)
        {
            if (t <= 0f) { IsOpen = false; Destroy(gameObject); onClose?.Invoke(); }
            return;
        }
        if (t > 0.45f && AnyPress()) { closing = true; t = Mathf.Min(t, 0.5f); ShellAudio.Play(ShellAudio.Sfx.Click, 0.6f); }
    }

    static bool AnyPress()
    {
        var k = Keyboard.current; var m = Mouse.current; var g = Gamepad.current;
        return (k != null && k.anyKey.wasPressedThisFrame) || (m != null && (m.leftButton.wasPressedThisFrame || m.rightButton.wasPressedThisFrame))
            || (g != null && (g.buttonSouth.wasPressedThisFrame || g.buttonEast.wasPressedThisFrame || g.startButton.wasPressedThisFrame));
    }

    void OnDestroy() => IsOpen = false;
}
