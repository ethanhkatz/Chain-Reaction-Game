using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Comic-book styled runtime UI pieces: outlined text with an offset pink shadow, hand-inked boxes, stamp ring,
// glow sprites and a few synthesized blips. Everything is generated once and shared (cheap on WebGL).
public static class ComicUI
{
    public static readonly Color Cyan = new Color(0.36f, 0.95f, 0.84f);
    public static readonly Color Pink = new Color(1f, 0.36f, 0.66f);
    public static readonly Color Ink = new Color(0.02f, 0.02f, 0.03f);
    public static readonly Color Paper = new Color(0.97f, 0.96f, 0.92f);
    public static readonly Color Charcoal = new Color(0.1f, 0.105f, 0.125f);

    static Material outlineMat;
    static TMP_FontAsset font;

    // Comic lettering: a TMP font asset at Resources/ComicFont (e.g. Luckiest Guy / Bangers SDF) if present,
    // else TMP's default. Assign ComicUI.Font before first use to force one.
    public static TMP_FontAsset Font
    {
        get
        {
            if (font == null) font = Resources.Load<TMP_FontAsset>("ComicFont");
            if (font == null) font = TMP_Settings.defaultFontAsset;
            return font;
        }
        set { font = value; outlineMat = null; }
    }
    static Sprite box, ring, glow, star;

    // Thick black outline via the SDF material (no shader keywords needed, so it survives build stripping).
    public static Material OutlineMaterial
    {
        get
        {
            if (outlineMat != null) return outlineMat;
            outlineMat = new Material(Font.material) { name = "ComicOutline" };
            outlineMat.SetFloat(ShaderUtilities.ID_FaceDilate, 0.3f);
            outlineMat.SetFloat(ShaderUtilities.ID_OutlineWidth, 0.45f);
            outlineMat.SetColor(ShaderUtilities.ID_OutlineColor, Ink);
            return outlineMat;
        }
    }

    public class Label
    {
        public RectTransform root;
        public TextMeshProUGUI face, shadow;
        public string text
        {
            set { face.text = value; shadow.text = value; }
        }
        public void SetColors(Color faceColor, Color shadowColor) { face.color = faceColor; shadow.color = shadowColor; }
    }

    // Outlined text with a pink drop shadow offset down-right (like the hand-lettered UI art).
    public static Label Text(Transform parent, string text, float size, Vector2 anchor, Vector2 pos, Vector2 box,
                             Color face, Color? shadowColor = null, float shadowOffset = -1f,
                             TextAlignmentOptions align = TextAlignmentOptions.Center)
    {
        var l = new Label();
        l.root = Rect("Comic " + text, parent, anchor, pos, box);
        float off = shadowOffset < 0 ? Mathf.Max(3f, size * 0.07f) : shadowOffset;
        l.shadow = MakeTmp(l.root, text, size, shadowColor ?? Pink, align, new Vector2(off, -off));
        l.face = MakeTmp(l.root, text, size, face, align, Vector2.zero);
        return l;
    }

    static TextMeshProUGUI MakeTmp(RectTransform parent, string text, float size, Color color, TextAlignmentOptions align, Vector2 offset)
    {
        var rt = Rect("t", parent, new Vector2(0.5f, 0.5f), offset, Vector2.zero);
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = offset; rt.offsetMax = offset;
        var t = rt.gameObject.AddComponent<TextMeshProUGUI>();
        t.font = Font;
        t.fontSharedMaterial = OutlineMaterial;
        t.text = text;
        t.fontSize = size;
        t.fontStyle = FontStyles.Bold;
        t.color = color;
        t.alignment = align;
        t.textWrappingMode = TextWrappingModes.NoWrap;
        t.overflowMode = TextOverflowModes.Overflow;
        t.raycastTarget = false;
        return t;
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

    // Ink-bordered panel with a pink offset "print misregistration" shadow behind it.
    public static RectTransform Panel(Transform parent, Vector2 anchor, Vector2 pos, Vector2 size, Color fill, Color? shadow = null)
    {
        var root = Rect("Panel", parent, anchor, pos, size);
        var sh = Rect("Shadow", root, new Vector2(0.5f, 0.5f), new Vector2(12, -12), size);
        Img(sh, BoxSprite, shadow ?? Pink);
        var face = Rect("Face", root, new Vector2(0.5f, 0.5f), Vector2.zero, size);
        Img(face, BoxSprite, fill);
        return root;
    }

    public static Image Img(RectTransform rt, Sprite s, Color c)
    {
        var img = rt.gameObject.AddComponent<Image>();
        img.sprite = s;
        img.type = s == box ? Image.Type.Sliced : Image.Type.Simple;
        img.color = c;
        img.raycastTarget = false;
        return img;
    }

    // White rounded box with a thick black border, 9-sliced. Tinting only changes the fill.
    public static Sprite BoxSprite
    {
        get
        {
            if (box != null) return box;
            const int n = 64, r = 18, border = 7;
            var tex = NewTex(n);
            var px = new Color32[n * n];
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    float d = RoundedDist(x + 0.5f, y + 0.5f, n, r); // <0 inside
                    float a = Mathf.Clamp01(0.5f - d);
                    bool ink = d > -border;
                    Color32 c = ink ? new Color(0, 0, 0, a) : new Color(1, 1, 1, a);
                    if (!ink && d > -border - 1) c = Color32.Lerp(new Color32(0, 0, 0, 255), new Color32(255, 255, 255, 255), -d - border);
                    px[y * n + x] = c;
                }
            tex.SetPixels32(px); tex.Apply();
            box = Sprite.Create(tex, new UnityEngine.Rect(0, 0, n, n), new Vector2(0.5f, 0.5f), 100, 0, SpriteMeshType.FullRect, new Vector4(r + 2, r + 2, r + 2, r + 2));
            return box;
        }
    }

    static float RoundedDist(float x, float y, int n, float r)
    {
        float cx = Mathf.Clamp(x, r, n - r), cy = Mathf.Clamp(y, r, n - r);
        return Vector2.Distance(new Vector2(x, y), new Vector2(cx, cy)) - r + 0.5f;
    }

    // Thick ring for the rank stamp (white, tint it).
    public static Sprite RingSprite
    {
        get
        {
            if (ring != null) return ring;
            const int n = 128;
            var tex = NewTex(n);
            var px = new Color32[n * n];
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(n / 2f, n / 2f));
                    float a = Mathf.Clamp01(Mathf.Min(d - 50f, 62f - d) + 0.5f);
                    // rough "inked stamp" breaks
                    float ang = Mathf.Atan2(y - n / 2f, x - n / 2f);
                    if (Mathf.Sin(ang * 7f + 1.3f) > 0.93f) a *= 0.35f;
                    px[y * n + x] = new Color(1, 1, 1, a);
                }
            tex.SetPixels32(px); tex.Apply();
            ring = Sprite.Create(tex, new UnityEngine.Rect(0, 0, n, n), new Vector2(0.5f, 0.5f), 100);
            return ring;
        }
    }

    // Soft radial glow (white, tint it).
    public static Sprite GlowSprite
    {
        get
        {
            if (glow != null) return glow;
            const int n = 64;
            var tex = NewTex(n);
            var px = new Color32[n * n];
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(n / 2f, n / 2f)) / (n / 2f);
                    float a = Mathf.Clamp01(1f - d);
                    px[y * n + x] = new Color(1, 1, 1, a * a);
                }
            tex.SetPixels32(px); tex.Apply();
            glow = Sprite.Create(tex, new UnityEngine.Rect(0, 0, n, n), new Vector2(0.5f, 0.5f), 64);
            return glow;
        }
    }

    // Four-point sparkle (white, tint it).
    public static Sprite StarSprite
    {
        get
        {
            if (star != null) return star;
            const int n = 48;
            var tex = NewTex(n);
            var px = new Color32[n * n];
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    float dx = Mathf.Abs(x + 0.5f - n / 2f) / (n / 2f), dy = Mathf.Abs(y + 0.5f - n / 2f) / (n / 2f);
                    float a = Mathf.Clamp01(1f - (Mathf.Sqrt(dx) + Mathf.Sqrt(dy)) * 0.9f) * 2f;
                    px[y * n + x] = new Color(1, 1, 1, Mathf.Clamp01(a));
                }
            tex.SetPixels32(px); tex.Apply();
            star = Sprite.Create(tex, new UnityEngine.Rect(0, 0, n, n), new Vector2(0.5f, 0.5f), 48);
            return star;
        }
    }

    static Texture2D NewTex(int n) => new Texture2D(n, n, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };

    // Canvas in front of everything; camera-space so headless captures include it.
    public static Canvas MakeCanvas(string name, int sortingOrder) => ShellUI.MakeCanvas(name, sortingOrder);

    // Maps a world point to a local position on a camera-space canvas.
    public static Vector2 WorldToCanvas(Canvas canvas, Vector3 world)
    {
        var cam = Camera.main;
        var rt = (RectTransform)canvas.transform;
        if (cam == null) return Vector2.zero;
        Vector2 screen = cam.WorldToScreenPoint(world);
        RectTransformUtility.ScreenPointToLocalPointInRectangle(rt, screen, canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera, out var local);
        return local;
    }

    // Unlit sprite material from the visuals config so glows read in dark lit scenes.
    public static Material UnlitSprite
    {
        get
        {
            var cfg = Resources.Load<JamVisualsConfig>("JamVisualsConfig");
            return cfg != null ? cfg.unlitSprite : null;
        }
    }
}
