using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

// Small helpers for building runtime UI from code.
public static class ShellUI
{
    public static readonly Color Cyan = new Color(0.36f, 0.95f, 0.84f);
    public static readonly Color Charcoal = new Color(0.08f, 0.085f, 0.1f);

    public static Canvas MakeCanvas(string name, int sortingOrder)
    {
        var go = new GameObject(name, typeof(RectTransform));
        var c = go.AddComponent<Canvas>();
        // Camera-space (in front of everything) when there is a camera, so screenshots/captures include it.
        var cam = Camera.main;
        if (cam != null)
        {
            c.renderMode = RenderMode.ScreenSpaceCamera;
            c.worldCamera = cam;
            c.planeDistance = Mathf.Max(cam.nearClipPlane + 0.05f, 0.5f);
        }
        else c.renderMode = RenderMode.ScreenSpaceOverlay;
        c.sortingLayerName = "Default";
        c.sortingOrder = sortingOrder + 1000;
        var s = go.AddComponent<CanvasScaler>();
        s.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        s.referenceResolution = new Vector2(1920, 1080);
        s.matchWidthOrHeight = 0.5f;
        go.AddComponent<GraphicRaycaster>();
        return c;
    }

    public static RectTransform Rect(string name, Transform parent, Vector2 anchor, Vector2 pos, Vector2 size)
    {
        var go = new GameObject(name, typeof(RectTransform));
        var rt = (RectTransform)go.transform;
        rt.SetParent(parent, false);
        rt.anchorMin = rt.anchorMax = anchor;
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
        return rt;
    }

    public static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
    }

    public static TextMeshProUGUI Text(Transform parent, string text, float size, Vector2 anchor, Vector2 pos, Vector2 box, Color color)
    {
        var rt = Rect("Text", parent, anchor, pos, box);
        var t = rt.gameObject.AddComponent<TextMeshProUGUI>();
        t.text = text;
        t.fontSize = size;
        t.color = color;
        t.alignment = TextAlignmentOptions.Center;
        t.raycastTarget = false;
        return t;
    }

    public static Button TextButton(Transform parent, string label, Vector2 pos, UnityEngine.Events.UnityAction onClick)
    {
        var rt = Rect("Button " + label, parent, new Vector2(0.5f, 0.5f), pos, new Vector2(440, 70));
        var img = rt.gameObject.AddComponent<Image>();
        img.color = new Color(0.14f, 0.15f, 0.17f, 0.95f);
        var outline = rt.gameObject.AddComponent<Outline>();
        outline.effectColor = Cyan;
        outline.effectDistance = new Vector2(2, -2);
        var b = rt.gameObject.AddComponent<Button>();
        var colors = b.colors;
        colors.highlightedColor = new Color(1.4f, 1.4f, 1.4f);
        colors.selectedColor = new Color(1.3f, 1.3f, 1.3f);
        b.colors = colors;
        b.onClick.AddListener(onClick);
        var t = Text(rt, label, 34, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(440, 70), Color.white);
        t.name = "Label";
        rt.gameObject.AddComponent<ButtonFx>();
        return b;
    }

    public static void EnsureEventSystem()
    {
        if (Object.FindAnyObjectByType<EventSystem>() != null) return;
        var go = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
    }

    // Comic-style button: dark ink-bordered box with a pink misprint shadow and outlined cyan lettering.
    public static Button ComicButton(Transform parent, string label, Vector2 anchor, Vector2 pos, Vector2 size, UnityEngine.Events.UnityAction onClick, float fontSize = 44f)
    {
        var root = ComicUI.Panel(parent, anchor, pos, size, ComicUI.Charcoal);
        root.name = "Button " + label;
        var face = root.Find("Face").GetComponent<Image>();
        face.raycastTarget = true;
        var b = root.gameObject.AddComponent<Button>();
        b.targetGraphic = face;
        var colors = b.colors;
        colors.highlightedColor = new Color(1.9f, 1.9f, 2.1f);
        colors.selectedColor = new Color(1.9f, 1.9f, 2.1f);
        colors.pressedColor = new Color(0.7f, 0.7f, 0.7f);
        b.colors = colors;
        if (onClick != null) b.onClick.AddListener(onClick);
        var t = ComicUI.Text(root, label, fontSize, new Vector2(0.5f, 0.5f), new Vector2(0, 2), size, ComicUI.Cyan, new Color(0.878f, 0.271f, 0.482f), 4f);
        t.root.name = "Label";
        root.gameObject.AddComponent<ButtonFx>();
        return b;
    }
}
