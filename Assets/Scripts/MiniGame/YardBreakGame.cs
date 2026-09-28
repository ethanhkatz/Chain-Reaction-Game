using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// "YARD BREAK": 60-second bonus round. Cardboard guard cutouts pop up around the exercise yard; smash them with the
// ball for points, chain hits within 1.5 s for a multiplier, gold cutouts are worth 5x. At zero: freeze + results.
public class YardBreakGame : MonoBehaviour
{
    public const float RoundSeconds = 60f;
    const int BasePoints = 100;
    const string BestKey = "yardBest";

    [SerializeField] Sprite targetSprite;
    [SerializeField] Sprite crashSprite;
    [SerializeField] Transform background;
    [SerializeField] Vector2[] spawnPoints;      // x, base y (the surface the cutout stands on)
    [SerializeField] float arenaHalfWidth = 13f;
    [SerializeField] float targetHeight = 2.5f;

    public bool Running { get; private set; }
    float timeLeft = RoundSeconds, nextSpawn, lastSmash = -99f, bgStartX;
    int score, chain, bestChain, smashed;
    bool finished;
    readonly Dictionary<int, YardTarget> live = new Dictionary<int, YardTarget>();
    Transform player;

    Canvas hud;
    ComicUI.Label timerLabel, scoreLabel;
    RectTransform timerRoot, scoreRoot;
    float timerPunch, scorePunch;
    int lastWholeSecond = -1;

    void Start()
    {
        var pc = FindAnyObjectByType<PlayerController>();
        if (pc != null) player = pc.transform;
        if (background != null) bgStartX = background.position.x;
        BuildHud();
        StartCoroutine(Intro());
    }

    IEnumerator Intro()
    {
        yield return null;
        RemoveLevelShell();
        var big = ComicUI.Text(hud.transform, "SMASH THE GUARDS!", 110, new Vector2(0.5f, 0.5f), new Vector2(0, 60), new Vector2(1400, 160), ComicUI.Cyan);
        yield return Punch(big.root, 1.1f);
        var go = ComicUI.Text(hud.transform, "GO!", 200, new Vector2(0.5f, 0.5f), new Vector2(0, 40), new Vector2(800, 260), Color.white);
        Running = true;
        nextSpawn = Time.time;
        yield return Punch(go.root, 0.6f);
    }

    // The level shell's pause menu owns Esc and the gameplay HUD shows level stats; this mode has its own.
    void RemoveLevelShell()
    {
        foreach (var pm in FindObjectsByType<PauseMenu>(FindObjectsSortMode.None)) Destroy(pm);
        foreach (var h in FindObjectsByType<GameplayHud>(FindObjectsSortMode.None))
        {
            var c = h.transform.Find("GameplayHud");
            if (c != null) Destroy(c.gameObject);
            Destroy(h);
        }
    }

    IEnumerator Punch(RectTransform rt, float life)
    {
        var g = rt.gameObject.AddComponent<CanvasGroup>();
        for (float t = 0; t < life; t += Dt)
        {
            float s = t < 0.1f ? Mathf.Lerp(0.2f, 1.3f, t / 0.1f) : t < 0.22f ? Mathf.Lerp(1.3f, 1f, (t - 0.1f) / 0.12f) : 1f + (t - 0.22f) * 0.05f;
            rt.localScale = Vector3.one * s;
            rt.localRotation = Quaternion.Euler(0, 0, -4f);
            g.alpha = t > life - 0.2f ? (life - t) / 0.2f : 1f;
            yield return null;
        }
        Destroy(rt.gameObject);
    }

    void Update()
    {
        var k = Keyboard.current;
        if (k != null && k.escapeKey.wasPressedThisFrame) { ToTitle(); return; }
        if (finished) { Time.timeScale = 0f; return; }
        if (timerLabel == null || timerLabel.face == null)
        {
            // a script reload mid-play drops the runtime UI and coroutines; rebuild and carry on
            if (hud != null) Destroy(hud.gameObject);
            if (player == null) { var pc = FindAnyObjectByType<PlayerController>(); if (pc != null) player = pc.transform; }
            BuildHud();
            if (!Running) StartCoroutine(Intro());
        }

        if (player != null && background != null)
            background.position = new Vector3(bgStartX - player.position.x * 0.04f, background.position.y, background.position.z);

        UpdateHud();
        if (!Running) return;

        timeLeft -= Time.deltaTime;
        if (timeLeft <= 0f) { timeLeft = 0f; UpdateHud(); Finish(); return; }

        float progress = 1f - timeLeft / RoundSeconds;
        int maxLive = progress < 0.3f ? 1 : progress < 0.65f ? 2 : 3;
        if (Time.time >= nextSpawn && live.Count < maxLive)
        {
            Spawn(progress);
            nextSpawn = Time.time + Mathf.Lerp(1.3f, 0.45f, progress) + Random.Range(0f, 0.3f);
        }
        if (chain > 0 && Time.time - lastSmash > ComboMeter.ChainWindow) chain = 0;
    }

    void Spawn(float progress)
    {
        var free = new List<int>();
        for (int i = 0; i < spawnPoints.Length; i++)
        {
            if (live.ContainsKey(i)) continue;
            if (player != null && Mathf.Abs(spawnPoints[i].x - player.position.x) < 2.2f && Mathf.Abs(spawnPoints[i].y - player.position.y) < 2.5f) continue;
            free.Add(i);
        }
        if (free.Count == 0) return;
        int slot = free[Random.Range(0, free.Count)];
        Vector2 p = spawnPoints[slot];
        bool gold = smashed >= 2 && Random.value < 0.13f;
        // cutouts at the yard edges sometimes slide in on a track instead of springing up
        float slideFrom = Mathf.Abs(p.x) > arenaHalfWidth * 0.6f && Random.value < 0.6f ? Mathf.Sign(p.x) * (arenaHalfWidth + 2f) : 0f;
        var go = new GameObject(gold ? "Target Gold" : "Target");
        go.transform.position = new Vector3(p.x, p.y, 0);
        var t = go.AddComponent<YardTarget>();
        float life = Mathf.Lerp(4.2f, 2.4f, progress) * (gold ? 0.75f : 1f);
        t.Init(this, targetSprite, targetHeight * (gold ? 0.9f : 1f), slot, gold, life, slideFrom);
        live[slot] = t;
    }

    public void Release(YardTarget t)
    {
        if (live.TryGetValue(t.Slot, out var cur) && cur == t) live.Remove(t.Slot);
    }

    public void OnSmash(YardTarget t, Vector2 at)
    {
        Release(t);
        smashed++;
        chain = Time.time - lastSmash <= ComboMeter.ChainWindow ? chain + 1 : 1;
        lastSmash = Time.time;
        bestChain = Mathf.Max(bestChain, chain);
        int points = BasePoints * chain * (t.IsGold ? 5 : 1);
        score += points;
        scorePunch = 1f;
        if (Application.isBatchMode) Debug.Log($"JAM: yard smash #{smashed} chain={chain} gold={t.IsGold} +{points} score={score} t={timeLeft:0.0}");

        StartCoroutine(CrashFx(at));
        JamAtmosphere.Shake(t.IsGold ? 0.45f : 0.28f);
        JamAtmosphere.Puff(at, 10, 2.5f);
        GameplayAudio.Combo(chain);

        var cm = ComboMeter.Instance;
        if (cm == null)
        {
            // lost after a script reload mid-play; replace the stale one
            foreach (var old in FindObjectsByType<ComboMeter>(FindObjectsSortMode.None)) Destroy(old);
            cm = gameObject.AddComponent<ComboMeter>();
        }
        if (t.IsGold)
            cm.Pop(at, "GOLD x5!", 96, new Color(1f, 0.84f, 0.3f), ComicUI.Pink, 1.1f, "+" + points);
        else if (chain > 1)
            cm.Pop(at, "CHAIN x" + chain + (chain >= 4 ? "!!" : "!"), Mathf.Min(80 + chain * 10, 140), chain >= 4 ? ComicUI.Pink : ComicUI.Cyan,
                   chain >= 4 ? ComicUI.Cyan : ComicUI.Pink, 1.1f, "+" + points);
        else
            cm.Pop(at, "+" + points, 56, Color.white, ComicUI.Pink, 0.8f);
    }

    IEnumerator CrashFx(Vector2 at)
    {
        if (crashSprite == null) yield break;
        var go = new GameObject("Crash");
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = crashSprite;
        sr.sortingOrder = 30;
        go.transform.position = at;
        go.transform.rotation = Quaternion.Euler(0, 0, Random.Range(-18f, 18f));
        float target = 3.2f / Mathf.Max(0.01f, crashSprite.bounds.size.x);
        const float dur = 0.4f;
        for (float t = 0; t < dur; t += Dt)
        {
            float k = t / dur;
            float s = k < 0.2f ? Mathf.Lerp(0.3f, 1.2f, k / 0.2f) : Mathf.Lerp(1.2f, 1f, (k - 0.2f) / 0.8f);
            go.transform.localScale = Vector3.one * target * s;
            sr.color = new Color(1, 1, 1, k < 0.6f ? 1f : 1f - (k - 0.6f) / 0.4f);
            yield return null;
        }
        Destroy(go);
    }

    // ---------------------------------------------------------------- HUD

    void BuildHud()
    {
        hud = ComicUI.MakeCanvas("YardHud", 50);
        hud.transform.SetParent(transform, false);
        timerRoot = ComicUI.Rect("Timer", hud.transform, new Vector2(0.5f, 1f), new Vector2(0, -80), new Vector2(400, 130));
        timerLabel = ComicUI.Text(timerRoot, "60", 120, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(400, 130), ComicUI.Cyan);
        scoreRoot = ComicUI.Rect("Score", hud.transform, new Vector2(0.5f, 1f), new Vector2(0, -175), new Vector2(600, 70));
        scoreRoot.localRotation = Quaternion.Euler(0, 0, -2f);
        scoreLabel = ComicUI.Text(scoreRoot, "SCORE 0", 60, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(600, 70), ComicUI.Cyan);
        var tag = ComicUI.Text(hud.transform, "YARD BREAK", 40, new Vector2(0f, 1f), new Vector2(190, -55), new Vector2(340, 50), Color.white);
        tag.root.localRotation = Quaternion.Euler(0, 0, 3f);
    }

    void UpdateHud()
    {
        int whole = Mathf.CeilToInt(timeLeft);
        if (whole != lastWholeSecond)
        {
            lastWholeSecond = whole;
            timerLabel.text = whole.ToString();
            if (whole <= 10 && Running) { timerPunch = 1f; timerLabel.SetColors(ComicUI.Pink, ComicUI.Ink); }
        }
        scoreLabel.text = "SCORE " + score.ToString("N0");
        timerPunch = Mathf.MoveTowards(timerPunch, 0, Time.unscaledDeltaTime * 4f);
        scorePunch = Mathf.MoveTowards(scorePunch, 0, Time.unscaledDeltaTime * 5f);
        timerRoot.localScale = Vector3.one * (1f + 0.25f * timerPunch);
        scoreRoot.localScale = Vector3.one * (1f + 0.2f * scorePunch);
    }

    // ---------------------------------------------------------------- results

    void Finish()
    {
        Running = false;
        finished = true;
        Time.timeScale = 0f;
        int best = PlayerPrefs.GetInt(BestKey, 0);
        bool newBest = score > best && score > 0;
        if (newBest) { best = score; PlayerPrefs.SetInt(BestKey, best); PlayerPrefs.Save(); }
        if (Application.isBatchMode) Debug.Log($"JAM: yard results score={score} best={best} newBest={newBest} smashed={smashed} bestChain={bestChain}");
        StartCoroutine(Results(best, newBest));
    }

    IEnumerator Results(int best, bool newBest)
    {
        var timeUp = ComicUI.Text(hud.transform, "TIME!", 200, new Vector2(0.5f, 0.5f), new Vector2(0, 40), new Vector2(900, 260), Color.white);
        yield return Punch(timeUp.root, 0.9f);

        var dim = ComicUI.Rect("Dim", hud.transform, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
        dim.anchorMin = Vector2.zero; dim.anchorMax = Vector2.one; dim.offsetMin = dim.offsetMax = Vector2.zero;
        var dimImg = dim.gameObject.AddComponent<Image>();
        dimImg.color = new Color(0, 0, 0, 0.55f);

        var card = ComicUI.Panel(hud.transform, new Vector2(0.5f, 0.5f), new Vector2(0, -10), new Vector2(820, 640), ComicUI.Charcoal);
        card.localRotation = Quaternion.Euler(0, 0, -1.5f);
        ComicUI.Text(card, "YARD BREAK!", 110, new Vector2(0.5f, 1f), new Vector2(0, -95), new Vector2(800, 130), ComicUI.Cyan);
        ComicUI.Text(card, "SCORE", 44, new Vector2(0.5f, 1f), new Vector2(0, -190), new Vector2(600, 50), Color.white, ComicUI.Ink);
        var scoreText = ComicUI.Text(card, "0", 120, new Vector2(0.5f, 1f), new Vector2(0, -270), new Vector2(700, 130), Color.white);
        ComicUI.Text(card, $"BEST {best:N0}     GUARDS {smashed}     CHAIN x{Mathf.Max(1, bestChain)}", 38, new Vector2(0.5f, 1f), new Vector2(0, -365), new Vector2(780, 50), ComicUI.Cyan, ComicUI.Ink);

        var again = ShellUI.TextButton(card, "PLAY AGAIN", new Vector2(0, -120), () => { Time.timeScale = 1f; SceneManager.LoadScene(SceneManager.GetActiveScene().name); });
        var title = ShellUI.TextButton(card, "TITLE", new Vector2(0, -210), ToTitle);
        StyleButton(again); StyleButton(title);
        again.Select();

        // slide the card in, then count the score up
        for (float t = 0; t < 0.3f; t += Dt)
        {
            float k = 1f - Mathf.Pow(1f - t / 0.3f, 3f);
            card.anchoredPosition = new Vector2(0, Mathf.Lerp(-900, -10, k));
            yield return null;
        }
        card.anchoredPosition = new Vector2(0, -10);
        for (float t = 0; t < 0.8f; t += Dt)
        {
            scoreText.text = Mathf.RoundToInt(score * (t / 0.8f)).ToString("N0");
            yield return null;
        }
        scoreText.text = score.ToString("N0");

        if (newBest)
        {
            var stamp = ComicUI.Rect("NewBest", card, new Vector2(1f, 0f), new Vector2(-105, 125), new Vector2(200, 200));
            var ringInk = ComicUI.Rect("Ink", stamp, new Vector2(0.5f, 0.5f), new Vector2(5, -5), new Vector2(200, 200));
            ComicUI.Img(ringInk, ComicUI.RingSprite, ComicUI.Ink);
            var ring = ComicUI.Rect("Ring", stamp, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(200, 200));
            ComicUI.Img(ring, ComicUI.RingSprite, ComicUI.Pink);
            ComicUI.Text(stamp, "NEW\nBEST!", 50, new Vector2(0.5f, 0.5f), new Vector2(0, 0), new Vector2(190, 120), ComicUI.Pink, ComicUI.Ink);
            GameplayAudio.Stamp();
            for (float t = 0; t < 0.25f; t += Dt)
            {
                float k = t / 0.25f;
                stamp.localScale = Vector3.one * Mathf.Lerp(2.4f, 1f, k * k);
                stamp.localRotation = Quaternion.Euler(0, 0, Mathf.Lerp(-40f, 14f, k));
                yield return null;
            }
            stamp.localScale = Vector3.one;
            JamAtmosphere.Shake(0.3f);
        }
    }

    static void StyleButton(Button b)
    {
        var label = b.GetComponentInChildren<TMPro.TextMeshProUGUI>();
        if (label != null) { label.font = ComicUI.Font; label.fontSharedMaterial = ComicUI.OutlineMaterial; label.fontSize = 44; }
        var img = b.GetComponent<Image>();
        img.sprite = ComicUI.BoxSprite;
        img.type = Image.Type.Sliced;
        img.color = new Color(0.2f, 0.21f, 0.24f);
    }

    // Real-time step for UI animations; floored so a hitching (or headless) frame rate can't stall them.
    static float Dt => Mathf.Max(Time.unscaledDeltaTime, 1f / 60f);

    void ToTitle()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(0);
    }
}
