using System.Collections;
using UnityEngine;

// Something the level can drop, topple or swing into a WardenCore. It only counts once it has actually moved
// (a standing domino or a hanging stalactite resting against armor does nothing). Optionally respawns if it
// is spent without breaking anything, so a domino knocked the wrong way can't soft-lock the fight.
public class WardenStriker : MonoBehaviour
{
    public bool consumeOnHit;
    public bool respawnIfWasted;
    public float wastedAfter = 4f;
    public bool ignorePlayer;
    [Header("Fling into a socket once toppled (domino)")]
    public Transform flingTo;
    public float flingDelay = 0.35f, flingArc = 3f, flingTime = 0.8f;
    bool launched;

    Vector3 homePos;
    float homeRot;
    float armedAt = -1f;
    GameObject spare;
    bool isSpare;

    public bool Armed
    {
        get
        {
            if (armedAt >= 0) return true;
            if ((transform.position - homePos).sqrMagnitude > 0.16f || Mathf.Abs(Mathf.DeltaAngle(transform.eulerAngles.z, homeRot)) > 8f)
                armedAt = Time.time;
            return armedAt >= 0;
        }
    }

    public bool Used { get; set; }

    void Start()
    {
        homePos = transform.position;
        homeRot = transform.eulerAngles.z;
        if (ignorePlayer)
        {
            var player = FindAnyObjectByType<PlayerController>();
            if (player != null)
                foreach (var pc in player.GetComponentsInChildren<Collider2D>())
                    foreach (var mine in GetComponentsInChildren<Collider2D>())
                        Physics2D.IgnoreCollision(pc, mine);
        }
        if (respawnIfWasted && !isSpare)
        {
            spare = Instantiate(gameObject, transform.parent);
            spare.name = name;
            spare.GetComponent<WardenStriker>().isSpare = true;
            spare.SetActive(false);
        }
    }

    // Flies along a parabola to the target (kinematic, so armor and the player can't knock it off course).
    public void Launch(Transform target, float arc, float time)
    {
        if (launched || target == null) return;
        launched = true;
        StartCoroutine(Fly(target, arc, time));
    }

    IEnumerator Fly(Transform target, float arc, float time)
    {
        var rbs = GetComponentsInChildren<Rigidbody2D>();
        Vector3 p0 = transform.position;
        float r0 = transform.eulerAngles.z;
        armedAt = Time.time;
        for (float t = 0; t <= time; t += Time.deltaTime)
        {
            foreach (var rb in rbs) { rb.bodyType = RigidbodyType2D.Kinematic; rb.linearVelocity = Vector2.zero; rb.angularVelocity = 0; }
            float k = Mathf.Clamp01(t / time);
            var p = Vector3.Lerp(p0, target.position, k) + Vector3.up * arc * 4f * k * (1 - k);
            transform.position = p;
            transform.rotation = Quaternion.Euler(0, 0, r0 - 200f * k);
            yield return null;
        }
        transform.position = target.position;
    }

    void Update()
    {
        if (flingTo != null && !launched && Armed && Time.time - armedAt > flingDelay) Launch(flingTo, flingArc, flingTime);
        if (!respawnIfWasted || Used || spare == null) return;
        if (Armed && Time.time - armedAt > wastedAfter) Respawn();
    }

    void Respawn()
    {
        var s = spare;
        spare = null;
        s.GetComponent<WardenStriker>().isSpare = false; // the new one makes its own spare
        s.SetActive(true);
        Destroy(gameObject);
    }
}
