using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

// Runtime-built mood layer: parallax backdrop, fog, silhouettes, dust, 2D lights, camera shake, hit-pause, fades.
public class JamAtmosphere : MonoBehaviour
{
    public static JamAtmosphere Instance { get; private set; }

    // Palette (design doc): charcoal world, cyan interactables, orange hazards, white exit.
    static readonly Color Cyan = new Color(0.36f, 0.95f, 0.84f);
    static readonly Color Orange = new Color(1f, 0.52f, 0.16f);
    static readonly Color Charcoal = new Color(0.075f, 0.08f, 0.09f);

    const int LavaLayer = 9;

    JamVisualsConfig cfg;
    Camera cam;
    Transform player, ball;
    Vector3 camAnchor;

    // Serializable so the lists survive editor script reloads during Play Mode.
    [System.Serializable] public struct Parallax { public Transform t; public Vector2 factor; public Vector3 basePos; public bool lockY; public float yOffset; }
    [System.Serializable] public struct Flicker { public Light2D light; public float baseIntensity; public float seed; }
    [SerializeField] List<Parallax> layers = new List<Parallax>();
    [SerializeField] List<Flicker> flicker = new List<Flicker>();

    Transform playerLight, ballLight, playerHalo, ballHalo;
    Transform screenRoot;
    SpriteRenderer vignette, fog, fade, bottomDark;
    ParticleSystem dust, puffs;
    Material particleMat;

    float shakeAmp;
    Vector3 shakeOffset;
    float fadeAlpha = 1f, pausedFor;

    public void Build(JamVisualsConfig config, Camera camera, Transform playerT, Transform ballT)
    {
        Instance = this;
        cfg = config; cam = camera; player = playerT; ball = ballT;
        camAnchor = cam.transform.position;

        particleMat = new Material(cfg.unlitSprite) { mainTexture = SoftDot(64) };

        BuildBackdrop();
        BuildScreenOverlays();
        BuildLights();
        BuildParticles();
        BuildChain();

        if (ball != null && ball.GetComponent<JamBallImpact>() == null) ball.gameObject.AddComponent<JamBallImpact>();
        if (player.GetComponent<JamLandingFx>() == null) player.gameObject.AddComponent<JamLandingFx>();

    }

    void OnEnable()
    {
        Instance = this;
        RenderPipelineManager.beginCameraRendering += OnBeginCam;
        RenderPipelineManager.endCameraRendering += OnEndCam;
    }

    void OnDisable()
    {
        if (Instance == this) Instance = null;
        RenderPipelineManager.beginCameraRendering -= OnBeginCam;
        RenderPipelineManager.endCameraRendering -= OnEndCam;
    }

    // ---------------------------------------------------------------- backdrop
    void BuildBackdrop()
    {
        float h = cam.orthographicSize * 2f;
        float w = h * Mathf.Max(cam.aspect, 16f / 9f);

        // Far: blurred prison art pushed toward charcoal, nearly camera-locked.
        if (cfg.background != null)
        {
            var bg = MakeSprite("BG_Far", cfg.background, cfg.unlitSprite, -100, new Color(0.27f, 0.31f, 0.34f));
            Vector2 s = cfg.background.bounds.size;
            float k = Mathf.Max(w * 1.35f / s.x, h * 1.25f / s.y);
            bg.transform.localScale = Vector3.one * k;
            AddLayer(bg.transform, new Vector2(0.92f, 0.95f), Vector3.zero);
        }

        // Charcoal wash between the far art and everything else: keeps the backdrop from competing with gameplay.
        var wash = MakeSprite("BG_Wash", WhiteSprite(), cfg.unlitSprite, -95, new Color(0.13f, 0.15f, 0.17f, 0.5f));
        wash.transform.localScale = new Vector3(w * 1.5f, h * 1.5f, 1f);
        AddLayer(wash.transform, Vector2.one, Vector3.zero);

        // Soft light shafts falling from above.
        var shaftSprite = Sprite.Create(ShaftTex(), new Rect(0, 0, 64, 256), new Vector2(0.5f, 1f), 16f);
        float[] xs = { -0.28f, 0.05f, 0.33f };
        for (int i = 0; i < xs.Length; i++)
        {
            var sh = MakeSprite("Shaft" + i, shaftSprite, cfg.unlitSprite, -92, new Color(0.75f, 0.92f, 0.95f, i == 1 ? 0.10f : 0.06f));
            sh.transform.localScale = new Vector3(1.4f + i * 0.4f, h * 1.1f / 16f, 1f);
            sh.transform.rotation = Quaternion.Euler(0, 0, 12f);
            AddLayer(sh.transform, new Vector2(0.8f, 1f), new Vector3(xs[i] * w, h * 0.55f, 0));
        }

        // Mid: dark industrial silhouettes (pipes, girders, hanging chains), slow parallax, tiled wide.
        var midSprite = Sprite.Create(MidSilhouetteTex(), new Rect(0, 0, 512, 512), new Vector2(0.5f, 0.5f), 512f / (h * 1.05f), 0, SpriteMeshType.FullRect);
        var mid = MakeSprite("BG_Mid", midSprite, cfg.unlitSprite, -90, new Color(0.05f, 0.055f, 0.065f, 0.8f));
        mid.drawMode = SpriteDrawMode.Tiled;
        mid.size = new Vector2(w * 8f, h * 1.05f);
        AddLayer(mid.transform, new Vector2(0.55f, 0.9f), Vector3.zero);

        // Bottom haze band over the mid layer (depth fog).
        var fogSprite = Sprite.Create(GradientTex(false), new Rect(0, 0, 4, 128), new Vector2(0.5f, 0f), 4f);
        fog = MakeSprite("Fog", fogSprite, cfg.unlitSprite, -85, new Color(0.38f, 0.46f, 0.5f, 0.28f));
        fog.transform.localScale = new Vector3(w * 1.6f, h * 0.6f / 32f, 1f);
        AddLayer(fog.transform, Vector2.one, new Vector3(0, -h * 0.5f, 0));

        // Foreground: near-black hanging chains/beams across the top of the screen, faster than the world.
        var fgSprite = Sprite.Create(ForegroundTex(), new Rect(0, 0, 512, 128), new Vector2(0.5f, 1f), 16f, 0, SpriteMeshType.FullRect);
        var fg = MakeSprite("FG_Top", fgSprite, cfg.unlitSprite, 40, new Color(0.02f, 0.02f, 0.025f, 1f));
        fg.drawMode = SpriteDrawMode.Tiled;
        fg.size = new Vector2(w * 10f, 8f);
        layers.Add(new Parallax { t = fg.transform, factor = new Vector2(-0.3f, 1f), basePos = Vector3.zero, lockY = true, yOffset = h * 0.5f + 0.4f });
    }

    void AddLayer(Transform t, Vector2 factor, Vector3 offset)
    {
        t.SetParent(transform, false);
        layers.Add(new Parallax { t = t, factor = factor, basePos = offset });
    }

    void BuildScreenOverlays()
    {
        screenRoot = new GameObject("ScreenFx").transform;
        screenRoot.SetParent(transform, false);
        var vigSprite = Sprite.Create(VignetteTex(128), new Rect(0, 0, 128, 128), new Vector2(0.5f, 0.5f), 128f);
        vignette = MakeSprite("Vignette", vigSprite, cfg.unlitSprite, 90, new Color(0.01f, 0.012f, 0.015f, 0.9f));
        vignette.transform.SetParent(screenRoot, false);
        // Darken the lower quarter of the frame so floor masses and voids sink into shadow.
        var bottomSprite = Sprite.Create(GradientTex(false), new Rect(0, 0, 4, 128), new Vector2(0.5f, 0f), 128f);
        bottomDark = MakeSprite("BottomDark", bottomSprite, cfg.unlitSprite, 85, new Color(0.01f, 0.012f, 0.015f, 0.85f));
        bottomDark.transform.SetParent(screenRoot, false);
        fade = MakeSprite("Fade", WhiteSprite(), cfg.unlitSprite, 100, Color.black);
        fade.transform.SetParent(screenRoot, false);
    }

    // ---------------------------------------------------------------- lights
    void BuildLights()
    {
        foreach (var l in FindObjectsByType<Light2D>(FindObjectsSortMode.None))
        {
            if (l.lightType != Light2D.LightType.Global) continue;
            l.intensity = 0.45f;
            l.color = new Color(0.72f, 0.78f, 0.88f);
        }

        playerLight = MakeLight("PlayerLight", Color.Lerp(Cyan, Color.white, 0.45f), 1.1f, 1.5f, 9f, 0.5f).transform;
        if (ball != null) ballLight = MakeLight("BallLight", Cyan, 1.0f, 0.8f, 4.5f, 0.6f).transform;

        // Soft halos behind player and ball so their silhouettes pop off the dark backdrop.
        var dot = Sprite.Create((Texture2D)particleMat.mainTexture, new Rect(0, 0, 64, 64), new Vector2(0.5f, 0.5f), 64f);
        var ph = MakeSprite("PlayerHalo", dot, cfg.unlitSprite, -1, new Color(0.85f, 0.95f, 1f, 0.16f));
        ph.transform.localScale = Vector3.one * 4.5f;
        playerHalo = ph.transform;
        if (ball != null)
        {
            var bh = MakeSprite("BallHalo", dot, cfg.unlitSprite, -1, new Color(Cyan.r, Cyan.g, Cyan.b, 0.28f));
            var br = ball.GetComponentInChildren<SpriteRenderer>();
            bh.transform.localScale = Vector3.one * (br != null ? br.bounds.size.x * 2.1f : 3f);
            ballHalo = bh.transform;
            if (br != null)
            {
                // Ball draws above chain (4/3) and halo; a solid disc behind it keeps its body opaque.
                br.sortingOrder = 6;
                var disc = MakeSprite("BallBody", Sprite.Create(HardDisc(64), new Rect(0, 0, 64, 64), new Vector2(0.5f, 0.5f), 64f),
                    cfg.litSprite, 5, new Color(0.2f, 0.22f, 0.23f, 1f));
                disc.transform.SetParent(br.transform, false);
                disc.transform.localPosition = Vector3.zero;
                float d = br.sprite != null ? Mathf.Min(br.sprite.bounds.size.x, br.sprite.bounds.size.y) * 0.9f : 1f;
                disc.transform.localScale = Vector3.one * d;
            }
        }

        foreach (var r in FindObjectsByType<Renderer>(FindObjectsSortMode.None))
        {
            if (r.gameObject.layer != LavaLayer || r is ParticleSystemRenderer) continue;
            if (r.GetComponentInParent<PlayerController>() != null) continue;
            var b = r.bounds;
            float rad = Mathf.Clamp(Mathf.Max(b.size.x, b.size.y) * 0.8f + 2f, 3f, 14f);
            var l = MakeLight("HazardGlow", Orange, 1.8f, rad * 0.3f, rad, 0.5f);
            l.transform.position = new Vector3(b.center.x, b.max.y, 0);
            flicker.Add(new Flicker { light = l, baseIntensity = l.intensity, seed = Random.value * 10f });
            SpawnEmbers(b);
        }

        foreach (var st in FindObjectsByType<StalactiteController>(FindObjectsSortMode.None))
        {
            var r = st.GetComponentInChildren<Renderer>();
            var l = MakeLight("StalactiteGlow", Orange, 1.3f, 0.3f, 2.6f, 0.6f);
            l.transform.SetParent(st.transform, true);
            l.transform.position = r != null ? r.bounds.center : st.transform.position;
        }

        foreach (var go in GameObjectsWithTag("Finish"))
        {
            // Keep the exit behind player (2) and ball (1) so the ball never vanishes behind the door.
            foreach (var fsr in go.GetComponentsInChildren<SpriteRenderer>()) fsr.sortingOrder = Mathf.Min(fsr.sortingOrder, 0);
            var r = go.GetComponentInChildren<Renderer>();
            Vector3 c = r != null ? r.bounds.center : go.transform.position;
            var l = MakeLight("ExitGlow", new Color(0.95f, 0.97f, 1f), 1.0f, 1f, 7f, 0.6f);
            l.transform.position = c;
            flicker.Add(new Flicker { light = l, baseIntensity = l.intensity, seed = Random.value * 10f });
        }
    }

    Light2D MakeLight(string name, Color c, float intensity, float inner, float outer, float falloff)
    {
        var go = new GameObject(name);
        go.transform.SetParent(transform, false);
        var l = go.AddComponent<Light2D>();
        l.lightType = Light2D.LightType.Point;
        l.color = c;
        l.intensity = intensity;
        l.pointLightInnerRadius = inner;
        l.pointLightOuterRadius = outer;
        l.falloffIntensity = falloff;
        l.shadowsEnabled = false;
        return l;
    }

    static IEnumerable<GameObject> GameObjectsWithTag(string tag)
    {
        try { return GameObject.FindGameObjectsWithTag(tag); } catch (UnityException) { return new GameObject[0]; }
    }

    // ---------------------------------------------------------------- particles
    void BuildParticles()
    {
        float h = cam.orthographicSize * 2f;
        float w = h * Mathf.Max(cam.aspect, 16f / 9f);

        dust = NewSystem("Dust", 6, 90);
        var main = dust.main;
        main.startLifetime = new ParticleSystem.MinMaxCurve(7f, 12f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.05f, 0.3f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.1f, 0.3f);
        main.startColor = new ParticleSystem.MinMaxGradient(new Color(0.85f, 0.88f, 0.9f, 0.45f), new Color(0.55f, 1f, 0.92f, 0.6f));
        main.gravityModifier = -0.004f;
        main.prewarm = true;
        var em = dust.emission; em.rateOverTime = 9f;
        var shape = dust.shape; shape.enabled = true; shape.shapeType = ParticleSystemShapeType.Box; shape.scale = new Vector3(w * 1.2f, h * 1.1f, 1f);
        var noise = dust.noise; noise.enabled = true; noise.strength = 0.25f; noise.frequency = 0.15f; noise.quality = ParticleSystemNoiseQuality.Low;
        FadeInOut(dust);
        AddLayer(dust.transform, Vector2.one, Vector3.zero);
        dust.Play();

        puffs = NewSystem("ImpactPuffs", 3, 160);
        var pm = puffs.main;
        pm.startLifetime = new ParticleSystem.MinMaxCurve(0.35f, 0.7f);
        pm.startSpeed = 0f;
        pm.gravityModifier = -0.05f;
        pm.startColor = new Color(0.72f, 0.75f, 0.77f, 0.85f);
        var pem = puffs.emission; pem.enabled = false;
        var sol = puffs.sizeOverLifetime; sol.enabled = true; sol.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.EaseInOut(0, 0.5f, 1, 1.6f));
        var lim = puffs.limitVelocityOverLifetime; lim.enabled = true; lim.dampen = 0.25f; lim.limit = 0.3f;
        FadeInOut(puffs, 0.05f);
        puffs.Play();
    }

    void SpawnEmbers(Bounds b)
    {
        var e = NewSystem("Embers", 4, 30);
        var m = e.main;
        m.startLifetime = new ParticleSystem.MinMaxCurve(1.5f, 3f);
        m.startSpeed = new ParticleSystem.MinMaxCurve(0.6f, 1.6f);
        m.startSize = new ParticleSystem.MinMaxCurve(0.06f, 0.16f);
        m.startColor = new ParticleSystem.MinMaxGradient(new Color(1f, 0.45f, 0.1f, 0.9f), new Color(1f, 0.8f, 0.35f, 0.9f));
        var em = e.emission; em.rateOverTime = Mathf.Clamp(b.size.x * 1.2f, 2f, 10f);
        var s = e.shape; s.enabled = true; s.shapeType = ParticleSystemShapeType.Box; s.scale = new Vector3(Mathf.Max(0.5f, b.size.x), 0.1f, 0.1f);
        s.rotation = new Vector3(-90f, 0, 0);
        var noise = e.noise; noise.enabled = true; noise.strength = 0.6f; noise.frequency = 0.8f; noise.quality = ParticleSystemNoiseQuality.Low;
        FadeInOut(e);
        e.transform.position = new Vector3(b.center.x, b.max.y, 0);
        e.Play();
    }

    ParticleSystem NewSystem(string name, int order, int max)
    {
        var go = new GameObject(name);
        go.transform.SetParent(transform, false);
        var ps = go.AddComponent<ParticleSystem>();
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        var m = ps.main;
        m.simulationSpace = ParticleSystemSimulationSpace.World;
        m.maxParticles = max;
        m.playOnAwake = false;
        var r = go.GetComponent<ParticleSystemRenderer>();
        r.sharedMaterial = particleMat;
        r.sortingOrder = order;
        return ps;
    }

    static void FadeInOut(ParticleSystem ps, float inT = 0.2f)
    {
        var col = ps.colorOverLifetime; col.enabled = true;
        var g = new Gradient();
        g.SetKeys(new[] { new GradientColorKey(Color.white, 0), new GradientColorKey(Color.white, 1) },
                  new[] { new GradientAlphaKey(0, 0), new GradientAlphaKey(1, inT), new GradientAlphaKey(1, 0.7f), new GradientAlphaKey(0, 1) });
        col.color = g;
    }

    // ---------------------------------------------------------------- chain
    void BuildChain()
    {
        foreach (var cb in FindObjectsByType<ChainBridge>(FindObjectsSortMode.None))
        {
            var lr = cb.GetComponent<LineRenderer>();
            if (lr != null) lr.enabled = false;
        }
        if (ball == null || cfg.chainLinkA == null) return;
        var go = new GameObject("JamChain");
        go.transform.SetParent(transform, false);
        go.AddComponent<JamChain>().Init(player, ball, cfg.chainLinkA, cfg.chainLinkB, cfg.litSprite, cfg.unlitSprite);
    }

    // ---------------------------------------------------------------- juice API
    public static void Shake(float amount)
    {
        if (Instance != null) Instance.shakeAmp = Mathf.Min(0.9f, Mathf.Max(Instance.shakeAmp, amount));
    }

    public static void HitPause(float seconds = 0.04f)
    {
        if (Instance != null && Time.timeScale == 1f) Instance.StartCoroutine(Instance.HitPauseRoutine(seconds));
    }

    IEnumerator HitPauseRoutine(float seconds)
    {
        const float paused = 0.02f;
        Time.timeScale = paused;
        yield return new WaitForSecondsRealtime(seconds);
        if (Time.timeScale == paused) Time.timeScale = 1f;
    }

    public static void Puff(Vector2 pos, int count, float strength)
    {
        if (Instance == null || Instance.puffs == null) return;
        var ep = new ParticleSystem.EmitParams();
        for (int i = 0; i < count; i++)
        {
            float a = Random.Range(0.1f, Mathf.PI - 0.1f);
            ep.position = pos + Random.insideUnitCircle * 0.15f;
            ep.velocity = new Vector3(Mathf.Cos(a), Mathf.Sin(a) * 0.6f, 0) * Random.Range(0.5f, 1.2f) * strength;
            ep.startSize = Random.Range(0.5f, 1.0f) * Mathf.Lerp(0.8f, 1.8f, Mathf.Clamp01(strength / 4f));
            ep.startLifetime = Random.Range(0.35f, 0.75f);
            Instance.puffs.Emit(ep, 1);
        }
    }

    public static void Crash(Vector2 pos)
    {
        if (Instance == null || Instance.cfg.crash == null) return;
        Instance.StartCoroutine(Instance.CrashRoutine(pos));
    }

    IEnumerator CrashRoutine(Vector2 pos)
    {
        var sr = MakeSprite("Crash", cfg.crash, cfg.unlitSprite, 30, Color.white);
        sr.transform.position = new Vector3(pos.x, pos.y, 0);
        sr.transform.rotation = Quaternion.Euler(0, 0, Random.Range(-15f, 15f));
        float target = 3.4f / Mathf.Max(0.01f, cfg.crash.bounds.size.x);
        const float dur = 0.32f;
        for (float t = 0; t < dur; t += Time.unscaledDeltaTime)
        {
            float k = t / dur;
            float s = k < 0.25f ? Mathf.Lerp(0.3f, 1.15f, k / 0.25f) : Mathf.Lerp(1.15f, 1f, (k - 0.25f) / 0.75f);
            sr.transform.localScale = Vector3.one * target * s;
            sr.color = new Color(1, 1, 1, k < 0.55f ? 1f : 1f - (k - 0.55f) / 0.45f);
            yield return null;
        }
        Destroy(sr.gameObject);
    }

    // ---------------------------------------------------------------- per-frame
    void LateUpdate()
    {
        Vector3 cp = cam.transform.position;
        if (cam == null || player == null) return;
        foreach (var p in layers)
        {
            if (p.t == null) continue;
            float x = camAnchor.x + (cp.x - camAnchor.x) * p.factor.x + p.basePos.x;
            float y = p.lockY ? cp.y + p.yOffset : camAnchor.y + (cp.y - camAnchor.y) * p.factor.y + p.basePos.y;
            p.t.position = new Vector3(x, y, p.t.position.z);
        }

        if (playerLight != null) playerLight.position = player.position + Vector3.up * 0.5f;
        if (ballLight != null && ball != null) ballLight.position = ball.position;
        if (playerHalo != null) playerHalo.position = player.position + Vector3.up * 0.3f;
        if (ballHalo != null && ball != null) ballHalo.position = ball.position;

        float time = Time.unscaledTime;
        foreach (var f in flicker)
            if (f.light != null)
                f.light.intensity = f.baseIntensity * (0.88f + 0.12f * Mathf.PerlinNoise(time * 2.2f, f.seed));

        // Screen-space overlays (world sprites parented to the camera so they also show in captures).
        float h = cam.orthographicSize * 2f, w = h * Mathf.Max(cam.aspect, 16f / 9f);
        screenRoot.position = new Vector3(cp.x, cp.y, 0);
        vignette.transform.localScale = new Vector3(w * 1.02f, h * 1.02f, 1);
        bottomDark.transform.localPosition = new Vector3(0, -h * 0.5f - 0.1f, 0);
        bottomDark.transform.localScale = new Vector3(w * 1.1f, h * 0.32f, 1);
        fade.transform.localScale = new Vector3(w * 2f, h * 1.5f, 1);

        float dt = Mathf.Min(Time.unscaledDeltaTime, 0.1f);
        pausedFor = Time.timeScale < 0.01f ? pausedFor + dt : 0f;
        float target = pausedFor > 0.2f ? 0.55f : 0f;
        fadeAlpha = Mathf.MoveTowards(fadeAlpha, target, dt / (target > fadeAlpha ? 0.5f : 0.8f));
        fade.color = new Color(0, 0, 0, fadeAlpha);

        shakeAmp = Mathf.MoveTowards(shakeAmp, 0f, Time.unscaledDeltaTime * 2.5f);
        shakeOffset = shakeAmp > 0.001f
            ? new Vector3((Mathf.PerlinNoise(time * 30f, 1f) - 0.5f) * 2f, (Mathf.PerlinNoise(1f, time * 30f) - 0.5f) * 2f, 0) * shakeAmp
            : Vector3.zero;
    }

    Vector3 appliedShake;
    void OnBeginCam(ScriptableRenderContext ctx, Camera c)
    {
        if (c != cam) return;
        appliedShake = shakeOffset;
        c.transform.position += appliedShake;
    }

    void OnEndCam(ScriptableRenderContext ctx, Camera c)
    {
        if (c != cam) return;
        c.transform.position -= appliedShake;
        appliedShake = Vector3.zero;
    }

    // ---------------------------------------------------------------- helpers
    SpriteRenderer MakeSprite(string name, Sprite s, Material m, int order, Color c)
    {
        var go = new GameObject(name);
        go.transform.SetParent(transform, false);
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = s;
        if (m != null) sr.sharedMaterial = m;
        sr.sortingOrder = order;
        sr.color = c;
        return sr;
    }

    static Sprite white;
    static Sprite WhiteSprite()
    {
        if (white == null) white = Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f), 4f);
        return white;
    }

    static Texture2D NewTex(int w, int h, TextureWrapMode wrap = TextureWrapMode.Clamp)
    {
        return new Texture2D(w, h, TextureFormat.RGBA32, false) { wrapMode = wrap, filterMode = FilterMode.Bilinear };
    }

    static Texture2D SoftDot(int n)
    {
        var t = NewTex(n, n);
        var px = new Color32[n * n];
        for (int y = 0; y < n; y++)
            for (int x = 0; x < n; x++)
            {
                float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(n / 2f, n / 2f)) / (n / 2f);
                float a = Mathf.Clamp01(1f - d); a *= a;
                px[y * n + x] = new Color(1, 1, 1, a);
            }
        t.SetPixels32(px); t.Apply();
        return t;
    }

    static Texture2D HardDisc(int n)
    {
        var t = NewTex(n, n);
        var px = new Color32[n * n];
        for (int y = 0; y < n; y++)
            for (int x = 0; x < n; x++)
            {
                float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(n / 2f, n / 2f));
                px[y * n + x] = new Color(1, 1, 1, Mathf.Clamp01(n / 2f - d));
            }
        t.SetPixels32(px); t.Apply();
        return t;
    }

    static Texture2D VignetteTex(int n)
    {
        var t = NewTex(n, n);
        var px = new Color32[n * n];
        for (int y = 0; y < n; y++)
            for (int x = 0; x < n; x++)
            {
                float u = (x + 0.5f) / n * 2f - 1f, v = (y + 0.5f) / n * 2f - 1f;
                float d = Mathf.Sqrt(u * u * 0.8f + v * v);
                float a = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.55f, 1.35f, d));
                px[y * n + x] = new Color(1, 1, 1, a);
            }
        t.SetPixels32(px); t.Apply();
        return t;
    }

    static Texture2D GradientTex(bool topHeavy)
    {
        var t = NewTex(4, 128);
        var px = new Color32[4 * 128];
        for (int y = 0; y < 128; y++)
        {
            float k = y / 127f; if (topHeavy) k = 1f - k;
            float a = Mathf.Pow(1f - k, 2.2f);
            for (int x = 0; x < 4; x++) px[y * 4 + x] = new Color(1, 1, 1, a);
        }
        t.SetPixels32(px); t.Apply();
        return t;
    }

    static Texture2D ShaftTex()
    {
        var t = NewTex(64, 256);
        var px = new Color32[64 * 256];
        for (int y = 0; y < 256; y++)
            for (int x = 0; x < 64; x++)
            {
                float u = Mathf.Abs((x + 0.5f) / 64f * 2f - 1f);
                float a = Mathf.Pow(1f - u, 2f) * Mathf.Pow(y / 255f, 1.5f);
                px[y * 64 + x] = new Color(1, 1, 1, a);
            }
        t.SetPixels32(px); t.Apply();
        return t;
    }

    // Tileable 512x512 industrial silhouette: pipes, a girder with rivets, cell bars, hanging chains.
    static Texture2D MidSilhouetteTex()
    {
        const int N = 512;
        var t = NewTex(N, N, TextureWrapMode.Repeat);
        t.wrapModeV = TextureWrapMode.Clamp;
        var a = new float[N * N];
        void Fill(int x0, int y0, int x1, int y1, float v = 1f)
        {
            for (int y = Mathf.Max(0, y0); y < Mathf.Min(N, y1); y++)
                for (int x = x0; x < x1; x++) a[y * N + ((x % N) + N) % N] = Mathf.Max(a[y * N + ((x % N) + N) % N], v);
        }
        // Vertical pipes of varying width with flanges.
        int[] pipeX = { 30, 44, 300, 318 }; int[] pipeW = { 10, 6, 16, 8 };
        for (int i = 0; i < pipeX.Length; i++)
        {
            Fill(pipeX[i], 0, pipeX[i] + pipeW[i], N);
            for (int y = 40; y < N; y += 150) Fill(pipeX[i] - 3, y, pipeX[i] + pipeW[i] + 3, y + 6);
        }
        // Girder across the top with rivets and diagonal braces.
        Fill(0, 440, N, 462); Fill(0, 470, N, 478);
        for (int x = 0; x < N; x += 32)
            for (int k = 0; k < 8; k++) Fill(x + k * 2, 462 + k, x + k * 2 + 3, 463 + k);
        // Cell bars block.
        Fill(120, 120, 250, 128); Fill(120, 320, 250, 328);
        for (int x = 124; x < 250; x += 16) Fill(x, 128, x + 4, 320);
        // Hanging chains from the girder.
        foreach (int cx in new[] { 90, 380, 440 })
        {
            int len = 120 + (cx * 7) % 140;
            for (int y = 440 - len; y < 440; y += 12) { Fill(cx - 3, y, cx + 3, y + 9); Fill(cx - 1, y + 3, cx + 1, y + 6, 0f); }
            Fill(cx - 8, 440 - len - 14, cx + 8, 440 - len);
        }
        // Low broken wall line.
        for (int x = 0; x < N; x++) { int top = 40 + (int)(10 * Mathf.PerlinNoise(x * 0.05f, 3f)); Fill(x, 0, x + 1, top); }
        var px = new Color32[N * N];
        for (int i = 0; i < px.Length; i++) px[i] = new Color(1, 1, 1, a[i]);
        t.SetPixels32(px); t.Apply();
        return t;
    }

    // Tileable 512x128 foreground: ceiling beam with hanging chain loops and cables (top-anchored).
    static Texture2D ForegroundTex()
    {
        const int W = 512, H = 128;
        var t = NewTex(W, H, TextureWrapMode.Repeat);
        t.wrapModeV = TextureWrapMode.Clamp;
        var a = new float[W * H];
        void Dot(float cx, float cy, float r)
        {
            for (int y = (int)(cy - r); y <= cy + r; y++)
                for (int x = (int)(cx - r); x <= cx + r; x++)
                {
                    if (y < 0 || y >= H) continue;
                    float d = Mathf.Sqrt((x - cx) * (x - cx) + (y - cy) * (y - cy));
                    float v = Mathf.Clamp01(r - d + 0.5f);
                    int xi = ((x % W) + W) % W;
                    a[y * W + xi] = Mathf.Max(a[y * W + xi], v);
                }
        }
        // Beam along the top edge.
        for (int x = 0; x < W; x++) for (int y = H - 14; y < H; y++) a[y * W + x] = 1f;
        // Sagging cables.
        void Cable(float x0, float x1, float sag, float r)
        {
            for (float s = 0; s <= 1f; s += 0.002f)
            {
                float x = Mathf.Lerp(x0, x1, s);
                float y = (H - 14) - sag * 4f * s * (1 - s);
                Dot(x, y, r);
            }
        }
        Cable(20, 230, 60, 2.2f); Cable(180, 470, 85, 3f); Cable(400, 600, 40, 1.6f);
        // Hanging chain strands.
        foreach (var (cx, len) in new[] { (110f, 70f), (330f, 100f), (345f, 55f) })
            for (float y = H - 14; y > H - 14 - len; y -= 7f) { Dot(cx, y, 3.2f); }
        var px = new Color32[W * H];
        for (int i = 0; i < px.Length; i++) px[i] = new Color(1, 1, 1, a[i]);
        t.SetPixels32(px); t.Apply();
        return t;
    }
}
