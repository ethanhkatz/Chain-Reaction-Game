using UnityEngine;

public class StalactiteController : MonoBehaviour
{
    [Header("Targeting")]
    [SerializeField] private string rockTag = "Rock";
    [SerializeField] private LayerMask detectionLayer;
    [SerializeField] private string playerTag = "Player";
    [Tooltip("Optional explicit target. If empty, the closest object tagged rockTag is used.")]
    [SerializeField] private Transform target;
    [Tooltip("If false and no target is assigned, ignore rockTag objects and just watch straight down.")]
    [SerializeField] private bool findTargetByTag = true;
    [Tooltip("If false, only a ball hit knocks it loose (player walking underneath is safe).")]
    [SerializeField] private bool triggeredByPlayer = true;
    [SerializeField] private string ballTag = "Ball";
    [Tooltip("Fraction of the ball's velocity passed on when the ball knocks it loose.")]
    [SerializeField] private float knockMomentum = 0.6f;
    [Tooltip("When knocked loose by the ball, arc onto the target instead of just dropping.")]
    [SerializeField] private bool aimAtTargetWhenKnocked = false;
    [SerializeField] private float launchUpSpeed = 5f;

    [Header("Detection Settings")]
    [SerializeField] private float raycastWidth = 0.5f;
    [SerializeField] private float detectionDistance = 10f; // How far down to look for the player

    private GameObject targetObject;
    private Rigidbody2D rb;
    private bool isFalling = false;
    private bool hadTarget;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.linearVelocity = Vector2.zero;

        // Use the assigned target, otherwise the closest rock (not a random global one)
        targetObject = target != null ? target.gameObject : findTargetByTag ? FindClosestRock() : null;
        hadTarget = targetObject != null;
    }

    void Update()
    {
        if (isFalling) return;

        // The target was destroyed: nothing left to hold this one up
        if (hadTarget && targetObject == null)
        {
            Fall();
            return;
        }

        if (triggeredByPlayer) CheckForPlayerInPath();
    }

    private void CheckForPlayerInPath()
    {
        // Casts a circle straight down from the stalactite's position
        RaycastHit2D hit = Physics2D.CircleCast(transform.position, raycastWidth, Vector2.down, detectionDistance, detectionLayer);

        if (hit.collider != null && hit.collider.CompareTag(playerTag))
        {
            Fall();
        }
    }

    public void Fall()
    {
        if (isFalling) return;
        isFalling = true;
        ChainEvents.Report(transform.position, "stalactite");
        rb.bodyType = RigidbodyType2D.Dynamic;
    }

    private GameObject FindClosestRock()
    {
        GameObject[] rocks = GameObject.FindGameObjectsWithTag(rockTag);
        GameObject closest = null;
        float shortestDistance = Mathf.Infinity;
        Vector3 currentPosition = transform.position;

        foreach (GameObject rock in rocks)
        {
            float distance = (rock.transform.position - currentPosition).sqrMagnitude;
            if (distance < shortestDistance)
            {
                shortestDistance = distance;
                closest = rock;
            }
        }
        return closest;
    }

    // Horizontal launch that lands on top of the target
    private Vector2 LaunchToward(GameObject t)
    {
        Vector2 aim = t.transform.position;
        if (t.TryGetComponent(out Collider2D c)) aim = new Vector2(c.bounds.center.x, c.bounds.max.y);
        float drop = Mathf.Max(0.5f, transform.position.y - aim.y);
        float g = Mathf.Abs(Physics2D.gravity.y * rb.gravityScale);
        // pop up first so it arcs over the player's head, then lands on the target
        float time = (launchUpSpeed + Mathf.Sqrt(launchUpSpeed * launchUpSpeed + 2f * g * drop)) / g;
        return new Vector2((aim.x - transform.position.x) / time, launchUpSpeed);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (!isFalling)
        {
            // A ball hit knocks it loose and carries some of the ball's momentum
            if (collision.gameObject.CompareTag(ballTag))
            {
                Fall();
                // let it fly free of the ball that knocked it
                if (TryGetComponent(out Collider2D mine)) Physics2D.IgnoreCollision(mine, collision.collider);
                if (aimAtTargetWhenKnocked && targetObject != null) rb.linearVelocity = LaunchToward(targetObject);
                else if (collision.rigidbody != null) rb.linearVelocity = collision.rigidbody.linearVelocity * knockMomentum;
            }
            return;
        }

        // The ball that knocked it loose keeps chasing it; don't shatter mid-air on it
        if (collision.gameObject.CompareTag(ballTag)) return;

        if (collision.gameObject.CompareTag(playerTag))
        {
            if (GameManager.instance != null) GameManager.instance.GameOver();
            Destroy(gameObject);
        }
        else if (collision.gameObject.CompareTag(rockTag))
        {
            ChainEvents.Report(collision.transform.position, "smash");
            Destroy(collision.gameObject);
            Destroy(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void OnBecameInvisible()
    {
        // Only clean up stalactites that already fell; hanging ones must survive the camera panning away
        if (isFalling) Destroy(gameObject);
    }
}
