using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// "The Warden": Level 5's end boss. It never takes a direct hit - its three cyan cores (WardenCore) only crack
// when a chain reaction lands on them. Between hits it telegraphs two attacks, faster each phase:
//   Slam  - rears up, warning marks flash on the floor, then orange shards drop from the ceiling there.
//   Sweep - the eye charges, then a low laser runs along the floor toward the arena door (jump it).
// Losing all three cores collapses it into the floor and opens the exit gate.
public class WardenBoss : MonoBehaviour
{
    public static WardenBoss Instance { get; private set; }

    [Header("Parts")]
    public WardenCore[] cores;
    public SpriteRenderer[] pips;
    public Transform body;          // everything that sinks when it dies
    public Transform tower;         // the part that rears up for a slam
    public SpriteRenderer eye;
    public SpriteRenderer bodyArt;
    public Sprite defeatedSprite;   // the collapsed wreck left behind
    public Gate exitGate;

    [Header("Arena")]
    public float wakeX = 106f;
    public float arenaMinX = 104f, arenaMaxX = 128.5f;
    public float floorY = 9f, ceilY = 22.5f;
    public float laserStartX = 129f;
    public Vector2 ledgeX;          // x range of a raised ledge in the arena (shards land on it)
    public float ledgeTop = 10.5f;

    [Header("Art")]
    public Sprite shardSprite, warnSprite, laserSprite, crashSprite;

    static readonly Color Orange = new Color(1f, 0.55f, 0.18f);
    static readonly Color Cyan = new Color(0.36f, 0.95f, 0.84f);
    static readonly Color PipOff = new Color(0.16f, 0.17f, 0.19f);

    // per phase (index = cores already broken)
    static readonly float[] Cooldown = { 2.6f, 1.9f, 1.3f };
    static readonly float[] Telegraph = { 1.2f, 0.95f, 0.75f };
    static readonly int[] Shards = { 2, 3, 4 };
    static readonly float[] SweepSpeed = { 9f, 12f, 15f };

    PlayerController player;
    int broken;
    bool awake, dead, staggered;
    Color eyeBase;
    readonly List<GameObject> hazards = new List<GameObject>();
    int lavaLayer;

    void Awake()
    {
        Instance = this;
        lavaLayer = LayerMask.NameToLayer("Lava");
        if (eye != null) eyeBase = eye.color;
    }

    void Start()
    {
        player = FindAnyObjectByType<PlayerController>();
        foreach (var c in cores) if (c != null) c.boss = this;
    }

    void Update()
    {
        if (!awake && !dead && player != null && player.transform.position.x > wakeX)
        {
            awake = true;
            StartCoroutine(Fight());
        }
    }

    int Phase => Mathf.Clamp(broken, 0, 2);

    IEnumerator Fight()
    {
        Debug.Log("JAM: warden awake");
        ShellAudio.Play(ShellAudio.Sfx.Alert);
        JamAtmosphere.Shake(0.4f);
        yield return Flash(eye, Color.white, 0.6f, 4);
        yield return new WaitForSeconds(1.0f);
        bool slam = true;
        while (!dead)
        {
            while (staggered && !dead) yield return null;
            if (dead) break;
            if (slam) yield return Slam(); else yield return Sweep();
            slam = !slam;
            float wait = Cooldown[Phase];
            for (float t = 0; t < wait && !dead && !staggered; t += Time.deltaTime) yield return null;
        }
    }

    // ------------------------------------------------------------------ attacks
    IEnumerator Slam()
    {
        int phase = Phase;
        float px = player != null ? player.transform.position.x : (arenaMinX + arenaMaxX) / 2;
        var xs = new List<float>();
        for (int i = 0; i < Shards[phase]; i++)
        {
            float off = i == 0 ? 0 : ((i + 1) / 2) * 3.6f * (i % 2 == 1 ? 1 : -1);
            xs.Add(Mathf.Clamp(px + off, arenaMinX + 0.8f, arenaMaxX - 0.5f));
        }

        // rear up
        Vector3 home = tower != null ? tower.localPosition : Vector3.zero;
        var marks = new List<SpriteRenderer>();
        var shards = new List<GameObject>();
        foreach (float x in xs)
        {
            var m = MakeSprite("SlamWarning", warnSprite, new Vector3(x, LandY(x) + 0.08f, 0), new Vector3(0.45f, 0.12f, 1), Orange, 6);
            marks.Add(m);
            shards.Add(Hazard("Shard", shardSprite, new Vector3(x, ceilY - 0.75f, 0), Vector3.one * 0.6f, Orange, 5, new Vector2(0.55f, 0.8f), false));
        }
        ShellAudio.Play(ShellAudio.Sfx.Alert, 0.7f);
        float tel = Telegraph[phase];
        for (float t = 0; t < tel; t += Time.deltaTime)
        {
            if (dead) yield break;
            float k = t / tel;
            if (tower != null) tower.localPosition = home + Vector3.up * 1.2f * Mathf.SmoothStep(0, 1, k);
            bool on = Mathf.Repeat(t * (6f + 6f * k), 1f) < 0.5f;
            foreach (var m in marks) if (m != null) m.color = on ? Orange : new Color(1f, 0.55f, 0.18f, 0.25f);
            foreach (var s in shards) if (s != null) s.transform.position = new Vector3(s.transform.position.x, ceilY - 0.75f, 0) + (Vector3)(Random.insideUnitCircle * 0.05f * k);
            yield return null;
        }
        if (tower != null) tower.localPosition = home;
        JamAtmosphere.Shake(0.45f);
        ShellAudio.Play(ShellAudio.Sfx.Thud);
        foreach (var m in marks) if (m != null) Destroy(m.gameObject);
        foreach (var s in shards) if (s != null) StartCoroutine(Fall(s));
        yield return new WaitForSeconds(0.6f);
    }

    float LandY(float x) => (x >= ledgeX.x && x <= ledgeX.y) ? ledgeTop : floorY;

    IEnumerator Fall(GameObject s)
    {
        var rb = s.GetComponent<Rigidbody2D>();
        float v = 2f, land = LandY(s.transform.position.x) + 0.55f;
        while (s != null && s.transform.position.y > land)
        {
            v += 45f * Time.deltaTime;
            var p = s.transform.position + Vector3.down * v * Time.deltaTime;
            p.y = Mathf.Max(p.y, land);
            rb.MovePosition(p);
            s.transform.position = p;
            yield return null;
        }
        if (s == null) yield break;
        Pop(crashSprite, s.transform.position + Vector3.down * 0.4f, 0.12f, 0.3f, Orange);
        hazards.Remove(s);
        Destroy(s);
    }

    IEnumerator Sweep()
    {
        int phase = Phase;
        float tel = Telegraph[phase];
        ShellAudio.Play(ShellAudio.Sfx.Alert, 0.7f);
        var line = MakeSprite("SweepWarning", warnSprite, new Vector3((arenaMinX + laserStartX) / 2, floorY + 0.06f, 0),
            new Vector3((laserStartX - arenaMinX) / Mathf.Max(0.01f, warnSprite.bounds.size.x), 0.06f, 1), Orange, 6);
        for (float t = 0; t < tel; t += Time.deltaTime)
        {
            if (dead) { Destroy(line.gameObject); yield break; }
            float k = t / tel;
            bool on = Mathf.Repeat(t * (5f + 7f * k), 1f) < 0.5f;
            line.color = new Color(1f, 0.55f, 0.18f, on ? 0.8f : 0.15f);
            if (eye != null) eye.color = Color.Lerp(eyeBase, Color.white, on ? k : 0);
            yield return null;
        }
        Destroy(line.gameObject);
        if (eye != null) eye.color = eyeBase;
        float h = 1.3f;
        float sy = h / laserSprite.bounds.size.y;
        var beam = Hazard("SweepLaser", laserSprite, new Vector3(laserStartX, floorY + h / 2, 0), new Vector3(0.5f, sy, 1), Color.white, 5,
            new Vector2(laserSprite.bounds.size.x * 0.5f, laserSprite.bounds.size.y * 0.9f), true);
        ShellAudio.Play(ShellAudio.Sfx.Click);
        var rb = beam.GetComponent<Rigidbody2D>();
        float speed = SweepSpeed[phase];
        while (beam != null && beam.transform.position.x > arenaMinX - 1f && !dead)
        {
            var p = beam.transform.position + Vector3.left * speed * Time.deltaTime;
            rb.MovePosition(p);
            beam.transform.position = p;
            yield return null;
        }
        if (beam != null) { hazards.Remove(beam); Destroy(beam); }
    }

    // ------------------------------------------------------------------ damage & death
    public void CoreBroken(WardenCore core)
    {
        if (dead) return;
        broken++;
        Debug.Log("JAM: warden core broken " + broken + "/" + cores.Length);
        if (broken - 1 < pips.Length && pips[broken - 1] != null) StartCoroutine(KillPip(pips[broken - 1]));
        if (broken >= cores.Length) StartCoroutine(Die());
        else StartCoroutine(Stagger());
    }

    IEnumerator KillPip(SpriteRenderer pip)
    {
        Vector3 s0 = pip.transform.localScale;
        for (float t = 0; t < 0.3f; t += Time.deltaTime)
        {
            pip.transform.localScale = s0 * (1f + t * 2f);
            pip.color = Color.Lerp(Color.white, PipOff, t / 0.3f);
            yield return null;
        }
        pip.transform.localScale = s0;
        pip.color = PipOff;
    }

    IEnumerator Stagger()
    {
        staggered = true;
        ClearHazards();
        yield return Shudder(0.9f, 0.18f);
        staggered = false;
    }

    IEnumerator Shudder(float time, float amp)
    {
        if (body == null) yield break;
        Vector3 home = body.localPosition;
        for (float t = 0; t < time; t += Time.deltaTime)
        {
            body.localPosition = home + (Vector3)(Random.insideUnitCircle * amp);
            yield return null;
        }
        body.localPosition = home;
    }

    IEnumerator Die()
    {
        dead = true;
        Debug.Log("JAM: warden defeated");
        ClearHazards();
        StartCoroutine(Flash(eye, Color.white, 0.8f, 6));
        if (body != null) foreach (var c in body.GetComponentsInChildren<Collider2D>()) c.enabled = false;
        Vector3 center = body != null ? body.position + new Vector3(0, 4.5f, 0) : transform.position;
        JamAtmosphere.Shake(0.9f);
        JamAtmosphere.HitPause(0.12f);
        ShellAudio.Play(ShellAudio.Sfx.Crack);
        ShellAudio.Play(ShellAudio.Sfx.Thud);
        Pop(crashSprite, center, 0.4f, 1.1f, Color.white, 0.9f);
        yield return Shudder(0.9f, 0.3f);
        // collapse into the floor (drawn behind it), popping as it goes
        foreach (var r in body.GetComponentsInChildren<SpriteRenderer>()) r.sortingOrder = -11;
        Vector3 home = body.localPosition;
        float dur = 2.2f;
        for (float t = 0; t < dur; t += Time.deltaTime)
        {
            float k = t / dur;
            body.localPosition = home + Vector3.down * 11.5f * k * k + (Vector3)(Random.insideUnitCircle * 0.12f);
            body.localRotation = Quaternion.Euler(0, 0, -7f * k);
            JamAtmosphere.Shake(0.35f);
            if (Random.value < Time.deltaTime * 4f)
                Pop(crashSprite, center + new Vector3(Random.Range(-5f, 5f), Random.Range(-4f, 2f), 0), 0.12f, 0.35f, Orange);
            yield return null;
        }
        if (defeatedSprite != null && bodyArt != null)
        {
            // leave the wreck slumped on the floor, behind the player
            float h = bodyArt.bounds.size.y;
            body.localPosition = home;
            body.localRotation = Quaternion.identity;
            foreach (Transform c in body) if (c != bodyArt.transform) c.gameObject.SetActive(false);
            bodyArt.sprite = defeatedSprite;
            bodyArt.transform.localScale = Vector3.one * (h * 0.45f / defeatedSprite.bounds.size.y);
            bodyArt.transform.position = new Vector3(bodyArt.transform.position.x, floorY + defeatedSprite.bounds.size.y * bodyArt.transform.localScale.y / 2f, 0);
            bodyArt.sortingOrder = -8;
            Pop(crashSprite, bodyArt.transform.position, 0.3f, 0.9f, Color.white);
        }
        else body.gameObject.SetActive(false);
        JamAtmosphere.Shake(0.7f);
        ShellAudio.Play(ShellAudio.Sfx.Thud);
        yield return new WaitForSeconds(0.4f);
        if (exitGate != null) exitGate.Activate();
        ShellAudio.Play(ShellAudio.Sfx.Clear, 0.6f);
        Debug.Log("JAM: exit gate open");
    }

    void ClearHazards()
    {
        foreach (var h in hazards) if (h != null) Destroy(h);
        hazards.Clear();
    }

    IEnumerator Flash(SpriteRenderer r, Color c, float time, int blinks)
    {
        if (r == null) yield break;
        Color b = r.color;
        for (float t = 0; t < time; t += Time.deltaTime)
        {
            r.color = Mathf.Repeat(t / time * blinks, 1f) < 0.5f ? c : b;
            yield return null;
        }
        r.color = b;
    }

    // ------------------------------------------------------------------ helpers
    SpriteRenderer MakeSprite(string n, Sprite s, Vector3 pos, Vector3 scale, Color c, int order)
    {
        var go = new GameObject(n);
        go.transform.position = pos;
        go.transform.localScale = scale;
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = s;
        sr.color = c;
        sr.sortingOrder = order;
        hazards.Add(go);
        return sr;
    }

    GameObject Hazard(string n, Sprite s, Vector3 pos, Vector3 scale, Color c, int order, Vector2 colSize, bool colliderInLocal)
    {
        var sr = MakeSprite(n, s, pos, scale, c, order);
        var go = sr.gameObject;
        go.layer = lavaLayer;
        var rb = go.AddComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;
        var bc = go.AddComponent<BoxCollider2D>();
        bc.isTrigger = true;
        bc.size = colliderInLocal ? colSize : new Vector2(colSize.x / scale.x, colSize.y / scale.y);
        return go;
    }

    // An expanding, fading sprite burst (the comic "CRASH").
    public static void Pop(Sprite s, Vector3 pos, float fromScale, float toScale, Color c, float time = 0.45f)
    {
        if (Instance == null || s == null) return;
        Instance.StartCoroutine(Instance.PopRoutine(s, pos, fromScale, toScale, c, time));
    }

    IEnumerator PopRoutine(Sprite s, Vector3 pos, float a, float b, Color c, float time)
    {
        var go = new GameObject("WardenPop");
        go.transform.position = pos + Vector3.back * 0.2f;
        go.transform.rotation = Quaternion.Euler(0, 0, Random.Range(-10f, 10f));
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = s;
        sr.sortingOrder = 20;
        for (float t = 0; t < time; t += Time.unscaledDeltaTime)
        {
            float k = t / time;
            go.transform.localScale = Vector3.one * Mathf.Lerp(a, b, 1 - (1 - k) * (1 - k));
            sr.color = new Color(c.r, c.g, c.b, k < 0.6f ? 1f : 1f - (k - 0.6f) / 0.4f);
            yield return null;
        }
        Destroy(go);
    }
}
