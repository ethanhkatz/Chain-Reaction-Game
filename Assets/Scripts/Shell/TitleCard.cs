using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Level intro: a riveted sign drops in on two chains, swings to rest, holds, then the ball smashes it and the
// halves tumble away. Screen-space UI with raycasts off, so it never blocks input.
public class TitleCard : MonoBehaviour
{
    // Index = level number. Edit here to retitle levels.
    public static readonly string[] Titles = { "", "Break Stuff", "Knock-On Effect", "Dead Weight", "Look Up", "The Way Out" };

    const float SignW = 680f;
    const float ChainLen = 215f;        // rig pivot (above the screen) to sign top
    const float RigY = 100f;            // pivot height above the top edge
    const float DropTime = 0.32f, SwingTime = 1.25f, HoldTime = 1.6f;
    const float SmashAt = DropTime + SwingTime + HoldTime;
    const float BallFlight = 0.2f;
    static readonly Color Ink = new Color(0.07f, 0.075f, 0.09f);

    RectTransform canvasRt, rig, ball, crash;
    RectTransform[] halves;
    Vector2[] vel;
    float[] spin;
    Vector2 ballFrom, ballTo;
    float t, signH;
    bool landed, smashed;

    public static void Show(Transform parent, int level)
    {
        var art = ShellArt.Get();
        if (art == null || art.sign == null) return;
        string title = level > 0 && level < Titles.Length ? Titles[level] : $"Level {level}";
        var canvas = ShellUI.MakeCanvas("TitleCard", 150);
        canvas.transform.SetParent(parent, false);
        canvas.GetComponent<GraphicRaycaster>().enabled = false;
        var card = canvas.gameObject.AddComponent<TitleCard>();
        card.Build((RectTransform)canvas.transform, art, "CELL BLOCK " + level, title);
    }

    void Build(RectTransform root, ShellArt art, string sub, string title)
    {
        canvasRt = root;
        halves = new RectTransform[2]; vel = new Vector2[2]; spin = new float[2];
        signH = SignW * art.sign.rect.height / art.sign.rect.width;
        rig = ShellUI.Rect("Rig", root, new Vector2(0.5f, 1f), new Vector2(0, RigY + 900f), Vector2.zero);
        // Two halves, each clipping its own full copy of the sign, so the crack is seamless until the smash.
        for (int h = 0; h < 2; h++)
        {
            var half = ShellUI.Rect(h == 0 ? "LeftHalf" : "RightHalf", rig, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            halves[h] = half;
            float ringX = (h == 0 ? -0.33f : 0.33f) * SignW;
            BuildChain(half, art, ringX);
            var mask = ShellUI.Rect("Mask", half, new Vector2(0.5f, 0.5f),
                new Vector2((h == 0 ? -0.25f : 0.25f) * (SignW + 40f), -ChainLen - signH * 0.5f), new Vector2((SignW + 40f) * 0.5f, signH + 40f));
            var mimg = mask.gameObject.AddComponent<Image>();
            mimg.raycastTarget = false;
            mask.gameObject.AddComponent<Mask>().showMaskGraphic = false;
            var sign = ShellUI.Rect("Sign", mask, new Vector2(0.5f, 0.5f), new Vector2((h == 0 ? 0.25f : -0.25f) * (SignW + 40f), 0), new Vector2(SignW, signH));
            Img(sign, art.sign);
            // Plate spans ~31%..98% of the sign height; letter it in the middle of that.
            var s = ShellUI.Text(sign, sub, 30, new Vector2(0.5f, 1f), new Vector2(0, -signH * 0.43f), new Vector2(SignW * 0.8f, 40), Ink);
            s.font = ComicUI.Font;
            s.characterSpacing = 12f;
            var l = ComicUI.Text(sign, title.ToUpperInvariant(), 84, new Vector2(0.5f, 1f), new Vector2(0, -signH * 0.70f), new Vector2(SignW * 0.84f, 110), ComicUI.Cyan, new Color(0.878f, 0.271f, 0.482f), 7f);
            foreach (var tmp in new[] { l.face, l.shadow })
            {
                tmp.enableAutoSizing = true;
                tmp.fontSizeMin = 40; tmp.fontSizeMax = 84;
                tmp.textWrappingMode = TextWrappingModes.NoWrap;
                tmp.overflowMode = TextOverflowModes.Truncate;
            }
        }

        var sprite = BallSkin.CurrentSprite ?? (art.ballSkins != null && art.ballSkins.Length > 0 ? art.ballSkins[0] : null);
        if (sprite != null)
        {
            ball = ShellUI.Rect("Ball", root, new Vector2(0.5f, 0.5f), new Vector2(-1300, -700), new Vector2(190, 190));
            Img(ball, sprite);
            ball.gameObject.SetActive(false);
        }
        if (art.crash != null)
        {
            crash = ShellUI.Rect("Crash", root, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(520, 520 * art.crash.rect.height / art.crash.rect.width));
            Img(crash, art.crash);
            crash.gameObject.SetActive(false);
        }
        Apply(0f);
    }

    static void BuildChain(RectTransform parent, ShellArt art, float x)
    {
        // The link sprite is a short diagonal run of links (~37 deg); turn it upright and stack with overlap.
        const float step = 96f;
        int n = Mathf.CeilToInt((ChainLen + RigY + 300f) / step);
        for (int i = 0; i < n; i++)
        {
            var link = ShellUI.Rect("Link", parent, new Vector2(0.5f, 0.5f), new Vector2(x, -ChainLen + 22f + i * step), new Vector2(203, 153) * 0.45f);
            link.localRotation = Quaternion.Euler(0, 0, 53f);
            Img(link, art.chainLink1);
            link.SetAsFirstSibling();
        }
    }

    static Image Img(RectTransform rt, Sprite s)
    {
        var img = rt.gameObject.AddComponent<Image>();
        img.sprite = s;
        img.preserveAspect = true;
        img.raycastTarget = false;
        return img;
    }

    void Update()
    {
        float dt = Time.unscaledDeltaTime;
        if (dt > 0.12f) dt = 1f / 30f; // ignore load hitches
        t += dt;
        Apply(dt);
    }

    void Apply(float dt)
    {
        if (!smashed)
        {
            float y, ang;
            if (t < DropTime)
            {
                float k = t / DropTime;
                y = Mathf.Lerp(RigY + 900f, RigY, k * k);
                ang = 7f * k;
            }
            else
            {
                if (!landed) { landed = true; ShellAudio.Play(ShellAudio.Sfx.Thud, 0.9f, 0.02f); ShellAudio.Play(ShellAudio.Sfx.Crack, 0.25f); }
                float u = t - DropTime;
                y = RigY - 26f * Mathf.Exp(-7f * u) * Mathf.Sin(u * 22f);
                ang = 7f * Mathf.Exp(-3.8f * u) * Mathf.Cos(u * Mathf.PI * 2f / 0.62f);
            }
            rig.anchoredPosition = new Vector2(0, y);
            rig.localRotation = Quaternion.Euler(0, 0, ang);
        }

        // Ball flight: from bottom-left into the sign's centre, then on out the top-right.
        Vector2 signCentre = new Vector2(0, canvasRt.rect.height * 0.5f + RigY - ChainLen - signH * 0.5f);
        float bt = t - (SmashAt - BallFlight);
        if (ball != null && bt > 0)
        {
            ball.gameObject.SetActive(true);
            Vector2 from = new Vector2(-canvasRt.rect.width * 0.5f - 200f, -canvasRt.rect.height * 0.5f - 100f);
            Vector2 exit = new Vector2(canvasRt.rect.width * 0.5f + 300f, canvasRt.rect.height * 0.5f + 300f);
            ball.anchoredPosition = bt < BallFlight ? Vector2.Lerp(from, signCentre, bt / BallFlight) : Vector2.Lerp(signCentre, exit, (bt - BallFlight) / 0.35f);
            ball.localRotation = Quaternion.Euler(0, 0, -bt * 900f);
            if (bt > BallFlight + 0.4f) ball.gameObject.SetActive(false);
        }

        if (!smashed && t >= SmashAt)
        {
            smashed = true;
            ShellAudio.Play(ShellAudio.Sfx.Crack, 1f, 0.05f);
            ShellAudio.Play(ShellAudio.Sfx.Thud, 0.8f);
            vel[0] = new Vector2(-520f, 420f); spin[0] = 160f;
            vel[1] = new Vector2(640f, 620f); spin[1] = -220f;
            if (crash != null) { crash.gameObject.SetActive(true); crash.anchoredPosition = signCentre; }
        }
        if (smashed)
        {
            float st = t - SmashAt;
            for (int h = 0; h < 2; h++)
            {
                vel[h].y -= 3200f * dt;
                halves[h].anchoredPosition += vel[h] * dt;
                halves[h].localRotation = Quaternion.Euler(0, 0, halves[h].localEulerAngles.z + spin[h] * dt);
            }
            if (crash != null)
            {
                float s = st < 0.1f ? Mathf.Lerp(0.4f, 1.25f, st / 0.1f) : Mathf.Lerp(1.25f, 1.05f, (st - 0.1f) / 0.3f);
                crash.localScale = Vector3.one * s;
                crash.localRotation = Quaternion.Euler(0, 0, -8f + st * 20f);
                crash.GetComponent<Image>().color = new Color(1, 1, 1, Mathf.Clamp01(1f - (st - 0.25f) / 0.25f));
            }
            if (st > 2.2f) Destroy(gameObject);
        }
    }
}
