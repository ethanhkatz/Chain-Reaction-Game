using UnityEngine;

public class StalactiteController : MonoBehaviour
{
    [Header("Targeting")]
    [SerializeField] private string rockTag = "Rock";
    [SerializeField] private LayerMask detectionLayer;
    [SerializeField] private string playerTag = "Player";
    [Tooltip("Optional explicit target. If empty, the first object tagged rockTag is used; if none exists, the stalactite watches straight down.")]
    [SerializeField] private Transform target;
    [Tooltip("If false, only a ball hit knocks it loose (player walking underneath is safe).")]
    [SerializeField] private bool triggeredByPlayer = true;
    [Tooltip("How far straight down to watch for the player when there is no target.")]
    [SerializeField] private float fallbackDetectDistance = 12f;
    [SerializeField] private string ballTag = "Ball";

    [Header("Detection Settings")]
    [SerializeField] private float raycastWidth = 0.5f;

    private GameObject targetObject;
    private Rigidbody2D rb;
    private bool isFalling = false;
    private bool hadTarget;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.linearVelocity = Vector2.zero;

        // Finds your local target rock by tag automatically (unless one was assigned)
        targetObject = target != null ? target.gameObject : GameObject.FindWithTag(rockTag);
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
        Vector2 direction = Vector2.down;
        float distance = fallbackDetectDistance;
        if (targetObject != null)
        {
            direction = targetObject.transform.position - transform.position;
            distance = direction.magnitude;
            direction.Normalize();
        }

        RaycastHit2D hit = Physics2D.CircleCast(transform.position, raycastWidth, direction, distance, detectionLayer);

        if (hit.collider != null && hit.collider.CompareTag(playerTag))
        {
            Fall();
        }
    }

    public void Fall()
    {
        if (isFalling) return;
        isFalling = true;
        rb.bodyType = RigidbodyType2D.Dynamic;
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (!isFalling)
        {
            // A ball hit knocks it loose
            if (collision.gameObject.CompareTag(ballTag)) Fall();
            return;
        }

        if (collision.gameObject.CompareTag(playerTag))
        {
            if (GameManager.instance != null) GameManager.instance.GameOver();
            Destroy(gameObject);
        }
        else if (collision.gameObject.CompareTag(rockTag))
        {
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
