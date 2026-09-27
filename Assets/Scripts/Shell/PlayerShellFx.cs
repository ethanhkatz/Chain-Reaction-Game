using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

// Added to the Player at runtime by ShellBootstrap. Owns:
//  - "Where'd You Go": after 30 s without input the prisoner turns to face the camera (idle_front 1/2 loop).
//  - "Hideo Game": a red "!" pops over the head on Alert() (falling stalactites, near misses with lava).
//  - jump / land sounds.
public class PlayerShellFx : MonoBehaviour
{
    public static PlayerShellFx Instance { get; private set; }

    public float idleSeconds = 30f;
    public float alertSeconds = 0.6f;
    public float dangerRadius = 0.9f;

    SpriteRenderer sr;
    Rigidbody2D rb;
    float lastInput;
    TextMeshPro mark;
    float markUntil = -1f;
    float lastAlert = -10f;
    bool wasInDanger;
    int lavaMask;

    void Awake()
    {
        Instance = this;
        sr = GetComponent<SpriteRenderer>();
        rb = GetComponent<Rigidbody2D>();
        lastInput = Time.unscaledTime;
        lavaMask = 1 << LayerMask.NameToLayer("Lava");
        BuildMark();
    }

    void BuildMark()
    {
        var go = new GameObject("AlertMark");
        mark = go.AddComponent<TextMeshPro>();
        mark.text = "!";
        mark.fontSize = 9f;
        mark.fontStyle = FontStyles.Bold;
        mark.alignment = TextAlignmentOptions.Center;
        mark.color = new Color(1f, 0.12f, 0.1f);
        mark.outlineWidth = 0.3f;
        mark.outlineColor = Color.black;
        mark.rectTransform.sizeDelta = new Vector2(2f, 2f);
        mark.sortingOrder = 500;
        go.SetActive(false);
    }

    void OnDestroy()
    {
        if (mark != null) Destroy(mark.gameObject);
        if (Instance == this) Instance = null;
    }

    public static void Alert()
    {
        if (Instance != null) Instance.DoAlert();
    }

    void DoAlert()
    {
        if (Time.unscaledTime - lastAlert < 1.2f) return;
        lastAlert = Time.unscaledTime;
        markUntil = Time.unscaledTime + alertSeconds;
        mark.gameObject.SetActive(true);
        ShellAudio.Play(ShellAudio.Sfx.Alert, 0.45f, 0f);
    }

    bool AnyInput()
    {
        var k = Keyboard.current;
        if (k != null && k.anyKey.isPressed) return true;
        var m = Mouse.current;
        if (m != null && (m.leftButton.isPressed || m.delta.ReadValue().sqrMagnitude > 1f)) return true;
        var g = Gamepad.current;
        return g != null && g.leftStick.ReadValue().sqrMagnitude > 0.1f;
    }

    void Update()
    {
        if (AnyInput()) lastInput = Time.unscaledTime;

        var k = Keyboard.current;
        if (k != null && k.spaceKey.wasPressedThisFrame && rb != null && Mathf.Abs(rb.linearVelocity.y) < 0.6f && Time.timeScale > 0f)
            ShellAudio.Play(ShellAudio.Sfx.Jump, 0.35f);

        // Near miss: stepping right up to a lava edge.
        bool danger = lavaMask != 0 && Physics2D.OverlapCircle(transform.position, dangerRadius, lavaMask) != null;
        if (danger && !wasInDanger && !GameManager.Frozen) DoAlert();
        wasInDanger = danger;
    }

    void LateUpdate()
    {
        // Runs after PlayerController.Update has set its sprite, so the idle pose wins while it applies.
        if (Time.unscaledTime - lastInput > idleSeconds && Time.timeScale > 0f)
        {
            var art = ShellArt.Get();
            if (art != null && art.idleFront1 != null && sr != null)
                sr.sprite = (Mathf.FloorToInt(Time.unscaledTime / 0.6f) % 2 == 0) ? art.idleFront1 : art.idleFront2;
        }

        if (mark.gameObject.activeSelf)
        {
            float left = markUntil - Time.unscaledTime;
            if (left <= 0f) { mark.gameObject.SetActive(false); return; }
            float age = alertSeconds - left;
            float pop = age < 0.08f ? Mathf.Lerp(0.4f, 1.25f, age / 0.08f) : Mathf.Lerp(1.25f, 1f, Mathf.Clamp01((age - 0.08f) / 0.1f));
            mark.transform.localScale = Vector3.one * pop;
            mark.transform.position = transform.position + new Vector3(0f, 1.6f, 0f);
        }
    }

    void OnCollisionEnter2D(Collision2D c)
    {
        if (c.contactCount == 0) return;
        if (c.GetContact(0).normal.y > 0.5f && c.relativeVelocity.y > 3f)
            ShellAudio.Play(ShellAudio.Sfx.Land, Mathf.Clamp01(c.relativeVelocity.y / 12f) * 0.5f);
    }
}
