using System.Collections;
using UnityEngine;

// Turns ChainEvents into combos: each report within ChainWindow seconds of the previous one extends the chain.
// Shows comic pop-ups at the event, climbs the blip pitch, scores BasePoints x chain, and gives big chains a
// short slow-mo. One per level scene (added by GameplayBootstrap).
public class ComboMeter : MonoBehaviour
{
    public const float ChainWindow = 1.5f;
    public const int SlowMoChain = 5;
    const float SlowMoSeconds = 0.15f, SlowMoScale = 0.3f;

    public static ComboMeter Instance { get; private set; }
    public int Chain { get; private set; }

    Canvas canvas;
    float lastEventTime = -99f;
    bool slowMo;
    float lastSlowMo = -99f;
    const float SlowMoCooldown = 2f; // real seconds; keeps repeated big chains from compounding

    void Awake()
    {
        Instance = this;
        canvas = ComicUI.MakeCanvas("ComboPopups", 60);
        canvas.transform.SetParent(transform, false);
        canvas.GetComponent<UnityEngine.UI.GraphicRaycaster>().enabled = false;
    }

    void OnEnable() => ChainEvents.Reported += OnReport;
    void OnDisable()
    {
        ChainEvents.Reported -= OnReport;
        if (Instance == this) Instance = null;
    }

    void Update()
    {
        if (!GameManager.Frozen && Time.timeScale > 0f)
            LevelStats.Seconds += Mathf.Min(Time.unscaledDeltaTime, 0.1f);
        if (Chain > 0 && Time.time - lastEventTime > ChainWindow) Chain = 0;
    }

    void OnReport(Vector3 pos, string kind)
    {
        if (GameManager.Frozen) return;
        Chain = Time.time - lastEventTime <= ChainWindow ? Chain + 1 : 1;
        lastEventTime = Time.time;
        int points = LevelStats.BasePoints * Chain;
        LevelStats.AddChain(Chain, points);
        GameplayAudio.Combo(Chain);

        if (Chain == 1) Pop(pos, "+" + points, 52, Color.white, ComicUI.Pink, 0.8f);
        else
        {
            bool big = Chain >= SlowMoChain;
            string txt = "CHAIN x" + Chain + (big ? "!!" : "!");
            Pop(pos, txt, Mathf.Min(80 + Chain * 10, 140), big ? ComicUI.Pink : ComicUI.Cyan, big ? ComicUI.Cyan : ComicUI.Pink, 1.1f,
                "+" + points);
            if (big) StartSlowMo();
        }
    }

    // Comic pop-up anchored to a world point: punch-scale in, slight tilt, drift up, fade.
    public void Pop(Vector3 world, string text, float size, Color face, Color shadow, float life = 1f, string sub = null)
    {
        var l = ComicUI.Text(canvas.transform, text, size, new Vector2(0.5f, 0.5f), ComicUI.WorldToCanvas(canvas, world), new Vector2(600, size * 1.2f), face, shadow);
        if (!string.IsNullOrEmpty(sub))
        {
            var s = ComicUI.Text(l.root, sub, size * 0.45f, new Vector2(0.5f, 0f), new Vector2(0, -size * 0.25f), new Vector2(300, size * 0.6f), Color.white, ComicUI.Ink, 3f);
        }
        var p = l.root.gameObject.AddComponent<ComicPopup>();
        p.Init(canvas, world + Vector3.up * 1.2f, life);
    }

    void StartSlowMo()
    {
        if (slowMo || GameManager.Frozen || Time.timeScale < ShellSettings.GameSpeed * 0.99f) return;
        if (Time.unscaledTime - lastSlowMo < SlowMoCooldown) return;
        lastSlowMo = Time.unscaledTime;
        StartCoroutine(SlowMo());
    }

    IEnumerator SlowMo()
    {
        slowMo = true;
        float scale = ShellSettings.GameSpeed * SlowMoScale;
        Time.timeScale = scale;
        yield return new WaitForSecondsRealtime(SlowMoSeconds);
        // Only restore if nobody else (pause menu, Game Over / Level Clear freeze) changed time meanwhile.
        if (!GameManager.Frozen && Mathf.Approximately(Time.timeScale, scale)) Time.timeScale = ShellSettings.PlayTimeScale;
        slowMo = false;
    }
}

// Animation for a single pop-up (unscaled time so it still plays during freezes).
public class ComicPopup : MonoBehaviour
{
    Canvas canvas;
    Vector3 world;
    float life, t, tilt;
    RectTransform rt;
    CanvasGroup group;
    static int stack;

    public void Init(Canvas c, Vector3 w, float lifeSeconds)
    {
        canvas = c; world = w; life = lifeSeconds;
        rt = (RectTransform)transform;
        group = gameObject.AddComponent<CanvasGroup>();
        group.blocksRaycasts = false;
        tilt = Random.Range(-12f, 12f);
        // Nudge simultaneous pop-ups apart so they don't print on top of each other.
        world += Vector3.up * 0.6f * (stack++ % 3);
        rt.localScale = Vector3.zero;
        Update();
    }

    void OnDestroy() => stack = Mathf.Max(0, stack - 1);

    void Update()
    {
        t += Mathf.Min(Time.unscaledDeltaTime, 0.05f);
        float s = t < 0.08f ? Mathf.Lerp(0.2f, 1.35f, t / 0.08f)
                : t < 0.2f ? Mathf.Lerp(1.35f, 1f, (t - 0.08f) / 0.12f)
                : 1f;
        rt.localScale = Vector3.one * s;
        rt.localRotation = Quaternion.Euler(0, 0, tilt * (t < 0.2f ? 1.6f - 3f * t : 1f));
        rt.anchoredPosition = ComicUI.WorldToCanvas(canvas, world) + Vector2.up * (t * 40f);
        float fadeStart = life * 0.7f;
        group.alpha = t < fadeStart ? 1f : 1f - (t - fadeStart) / (life - fadeStart);
        if (t >= life) Destroy(gameObject);
    }
}
