using UnityEngine;

public class StalactiteController : MonoBehaviour
{
    [Header("Targeting")]
    [SerializeField] private string rockTag = "Rock"; 
    [SerializeField] private LayerMask detectionLayer;  
    [SerializeField] private string playerTag = "Player";

    [Header("Detection Settings")]
    [SerializeField] private float raycastWidth = 0.5f; 

    private GameObject targetObject; 
    private Rigidbody2D rb;
    private bool isFalling = false;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.linearVelocity = Vector2.zero;

        // Finds your local target rock by tag automatically
        targetObject = GameObject.FindWithTag(rockTag);
        
        if (targetObject == null)
        {
            Debug.LogError($"Stalactite could not find any object with the tag '{rockTag}'!");
        }
    }

    void Update()
    {
        if (isFalling) return;

        if (targetObject == null)
        {
            Fall();
            return;
        }

        CheckForPlayerInPath();
    }

    private void CheckForPlayerInPath()
    {
        Vector2 direction = targetObject.transform.position - transform.position;
        float distance = direction.magnitude;
        direction.Normalize();

        RaycastHit2D hit = Physics2D.CircleCast(transform.position, raycastWidth, direction, distance, detectionLayer);

        if (hit.collider != null && hit.collider.CompareTag(playerTag))
        {
            Fall();
        }
    }

    private void Fall()
    {
        isFalling = true;
        rb.bodyType = RigidbodyType2D.Dynamic;
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (!isFalling) return;

        if (collision.gameObject.CompareTag(playerTag))
        {
            Destroy(collision.gameObject); 
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
        Destroy(gameObject);
    }
}
