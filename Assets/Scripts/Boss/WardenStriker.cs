using UnityEngine;

// Something the level can drop, topple or swing into a WardenCore. It only counts once it has actually moved
// (a standing domino or a hanging stalactite resting against armor does nothing). Optionally respawns if it
// is spent without breaking anything, so a domino knocked the wrong way can't soft-lock the fight.
public class WardenStriker : MonoBehaviour
{
    public bool consumeOnHit;
    public bool respawnIfWasted;
    public float wastedAfter = 4f;

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
        if (respawnIfWasted && !isSpare)
        {
            spare = Instantiate(gameObject, transform.parent);
            spare.name = name;
            spare.GetComponent<WardenStriker>().isSpare = true;
            spare.SetActive(false);
        }
    }

    void Update()
    {
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
