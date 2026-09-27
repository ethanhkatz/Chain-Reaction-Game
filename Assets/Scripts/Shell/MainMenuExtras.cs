using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Extra main-menu buttons built at runtime in the comic style: SKINS (ball skin picker), CREDITS, and BONUS
// (the YardBreak mini-game, shown only when that scene is in the build).
public class MainMenuExtras : MonoBehaviour
{
    const string BonusScene = "YardBreak";

    RectTransform panel;
    CanvasGroup panelGroup;
    RectTransform[] tiles, balls;
    Image[] rings;
    float t, panelT;
    CanvasGroup fade;
    bool panelOpen;
    GameObject returnFocus;

    void Start()
    {
        var canvas = ShellUI.MakeCanvas("MenuExtras", 20);
        canvas.transform.SetParent(transform, false);
        var root = canvas.transform;
        fade = canvas.gameObject.AddComponent<CanvasGroup>();
        fade.alpha = 0f;
        // A row along the bottom-right, under New Game / Exit.
        float x = -160f;
        if (Application.CanStreamedLevelBeLoaded(BonusScene))
        {
            ShellUI.ComicButton(root, "BONUS", new Vector2(1f, 0f), new Vector2(x, 80), new Vector2(230, 78), () => SceneManager.LoadScene(BonusScene), 36f);
            x -= 256f;
        }
        ShellUI.ComicButton(root, "CREDITS", new Vector2(1f, 0f), new Vector2(x, 80), new Vector2(230, 78), OpenCredits, 36f);
        x -= 256f;
        ShellUI.ComicButton(root, "SKINS", new Vector2(1f, 0f), new Vector2(x, 80), new Vector2(230, 78), OpenSkins, 36f);
        BuildPanel(root);
        if (!string.IsNullOrEmpty(System.Environment.GetEnvironmentVariable("JAM_OPEN_SKINS"))) OpenSkins();
    }

    void OpenCredits()
    {
        if (CreditsScreen.IsOpen) return;
        var es = EventSystem.current;
        returnFocus = es != null ? es.currentSelectedGameObject : null;
        CreditsScreen.Show(() => { if (returnFocus != null && EventSystem.current != null) EventSystem.current.SetSelectedGameObject(returnFocus); });
    }

    void BuildPanel(Transform root)
    {
        tiles = new RectTransform[BallSkin.Count]; balls = new RectTransform[BallSkin.Count]; rings = new Image[BallSkin.Count];
        var holder = ShellUI.Rect("SkinsPanel", root, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
        ShellUI.Stretch(holder);
        panelGroup = holder.gameObject.AddComponent<CanvasGroup>();
        var dim = ShellUI.Rect("Dim", holder, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
        ShellUI.Stretch(dim);
        dim.gameObject.AddComponent<Image>().color = new Color(0.02f, 0.02f, 0.03f, 0.72f);

        panel = ComicUI.Panel(holder, new Vector2(0.5f, 0.5f), new Vector2(0, -10), new Vector2(1180, 600), ComicUI.Charcoal);
        ComicUI.Text(panel, "BALL SKINS", 76, new Vector2(0.5f, 1f), new Vector2(0, -78), new Vector2(900, 100), ComicUI.Cyan, new Color(0.878f, 0.271f, 0.482f), 7f);

        for (int i = 0; i < BallSkin.Count; i++)
        {
            int idx = i;
            float x = (i - 2) * 214f;
            var tile = ComicUI.Panel(panel, new Vector2(0.5f, 0.5f), new Vector2(x, -10), new Vector2(190, 250), new Color(0.17f, 0.18f, 0.21f), new Color(0.05f, 0.05f, 0.06f));
            tile.name = "Tile " + BallSkin.Names[i];
            tiles[i] = tile;
            var face = tile.Find("Face").GetComponent<Image>();
            face.raycastTarget = true;
            var b = tile.gameObject.AddComponent<Button>();
            b.targetGraphic = face;
            b.onClick.AddListener(() => Select(idx));
            tile.gameObject.AddComponent<ButtonFx>();

            var ring = ComicUI.Rect("Ring", tile, new Vector2(0.5f, 0.5f), new Vector2(0, 28), new Vector2(178, 178));
            rings[i] = ComicUI.Img(ring, ComicUI.RingSprite, ComicUI.Cyan);
            var ball = ComicUI.Rect("Ball", tile, new Vector2(0.5f, 0.5f), new Vector2(0, 28), new Vector2(138, 138));
            var img = ComicUI.Img(ball, BallSkin.SpriteFor(i), Color.white);
            img.preserveAspect = true;
            balls[i] = ball;
            ComicUI.Text(tile, BallSkin.Names[i].ToUpperInvariant(), 30, new Vector2(0.5f, 0f), new Vector2(0, 38), new Vector2(180, 40), Color.white, new Color(0.878f, 0.271f, 0.482f), 3f);
        }
        ShellUI.ComicButton(panel, "DONE", new Vector2(0.5f, 0f), new Vector2(0, 70), new Vector2(240, 80), CloseSkins, 38f);
        holder.gameObject.SetActive(false);
    }

    void OpenSkins()
    {
        var es = EventSystem.current;
        returnFocus = es != null ? es.currentSelectedGameObject : null;
        panelGroup.gameObject.SetActive(true);
        panelOpen = true;
        panelT = 0f;
        if (es != null) es.SetSelectedGameObject(tiles[BallSkin.Current].gameObject);
    }

    void CloseSkins()
    {
        panelOpen = false;
        panelGroup.gameObject.SetActive(false);
        if (returnFocus != null && EventSystem.current != null) EventSystem.current.SetSelectedGameObject(returnFocus);
    }

    void Select(int i)
    {
        BallSkin.Current = i;
        ShellAudio.Play(ShellAudio.Sfx.Thud, 0.5f);
        balls[i].localScale = Vector3.one * 1.3f; // little squash-pop; Update eases it back
    }

    void Update()
    {
        float dt = Mathf.Min(Time.unscaledDeltaTime, 0.05f);
        t += dt;
        if (fade != null) fade.alpha = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((t - 0.3f) / 1.2f));
        if (!panelOpen) return;
        panelT += dt;
        float k = Mathf.Clamp01(panelT / 0.18f);
        panelGroup.alpha = k;
        panel.localScale = Vector3.one * (k < 1f ? Mathf.Lerp(0.85f, 1.03f, k) : Mathf.Lerp(1.03f, 1f, Mathf.Clamp01((panelT - 0.18f) / 0.1f)));
        int sel = BallSkin.Current;
        for (int i = 0; i < balls.Length; i++)
        {
            bool on = i == sel;
            rings[i].enabled = on;
            float bob = on ? 9f * Mathf.Sin(t * 5f) : 0f;
            balls[i].anchoredPosition = new Vector2(0, 28 + bob);
            rings[i].rectTransform.localRotation = Quaternion.Euler(0, 0, t * 40f);
            balls[i].localScale = Vector3.Lerp(balls[i].localScale, Vector3.one * (on ? 1.08f : 0.9f), dt * 12f);
            if (on) balls[i].localRotation = Quaternion.Euler(0, 0, 6f * Mathf.Sin(t * 2.5f));
            else balls[i].localRotation = Quaternion.identity;
        }
        var kb = Keyboard.current;
        if (kb != null && kb.escapeKey.wasPressedThisFrame) CloseSkins();
    }
}
