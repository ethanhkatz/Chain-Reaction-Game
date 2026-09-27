// using UnityEngine;

// public class StalactiteController : MonoBehaviour
// {
//     [Header("Targeting")]
//     [SerializeField] private string rockTag = "Rock"; 
//     [SerializeField] private LayerMask detectionLayer;  
//     [SerializeField] private string playerTag = "Player";

//     [Header("Detection Settings")]
//     [SerializeField] private float raycastWidth = 0.5f; 

//     private GameObject targetObject; 
//     private Rigidbody2D rb;
//     private bool isFalling = false;

//     void Start()
//     {
//         rb = GetComponent<Rigidbody2D>();
//         rb.bodyType = RigidbodyType2D.Kinematic;
//         rb.linearVelocity = Vector2.zero;

//         // Finds your local target rock by tag automatically
//         targetObject = GameObject.FindWithTag(rockTag);
        
//         if (targetObject == null)
//         {
//             Debug.LogError($"Stalactite could not find any object with the tag '{rockTag}'!");
//         }
//     }

//     void Update()
//     {
//         if (isFalling) return;

//         if (targetObject == null)
//         {
//             Fall();
//             return;
//         }

//         CheckForPlayerInPath();
//     }

//     private void CheckForPlayerInPath()
//     {
//         Vector2 direction = targetObject.transform.position - transform.position;
//         float distance = direction.magnitude;
//         direction.Normalize();

//         RaycastHit2D hit = Physics2D.CircleCast(transform.position, raycastWidth, direction, distance, detectionLayer);

//         if (hit.collider != null && hit.collider.CompareTag(playerTag))
//         {
//             Fall();
//         }
//     }

//     private void Fall()
//     {
//         isFalling = true;
//         rb.bodyType = RigidbodyType2D.Dynamic;
//     }

//     private void OnCollisionEnter2D(Collision2D collision)
//     {
//         if (!isFalling) return;

//         if (collision.gameObject.CompareTag(playerTag))
//         {
//             Destroy(collision.gameObject); 
//             Destroy(gameObject); 
//         }
//         else if (collision.gameObject.CompareTag(rockTag))
//         {
//             Destroy(collision.gameObject);
//             Destroy(gameObject);
//         }
//         else
//         {
//             Destroy(gameObject);
//         }
//     }

//     private void OnBecameInvisible()
//     {
//         Destroy(gameObject);
//     }
// }
using UnityEngine;

public class StalactiteController : MonoBehaviour
{
    [Header("Targeting")]
    [SerializeField] private string rockTag = "Rock";
    [SerializeField] private LayerMask detectionLayer;
    [SerializeField] private string playerTag = "Player";

    [Header("Detection Settings")]
    [SerializeField] private float raycastWidth = 0.5f;
    [SerializeField] private float detectionDistance = 10f; // How far down to look for the player

    private GameObject targetObject;
    private Rigidbody2D rb;
    private bool isFalling = false;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.linearVelocity = Vector2.zero;

        // AUTOMATICALLY find the CLOSEST rock instead of a random global one
        targetObject = FindClosestRock();

        if (targetObject == null)
        {
            Debug.LogWarning($"{gameObject.name} could not find any nearby object with the tag {rockTag}!");
        }
    }

    void Update()
    {
        if (isFalling) return;

        // If its individual connected rock is destroyed, fall automatically
        if (targetObject == null)
        {
            Fall();
            return;
        }

        CheckForPlayerUnderneath();
    }

    private void CheckForPlayerUnderneath()
    {
        // Casts a circle straight down from the stalactite's position
        RaycastHit2D hit = Physics2D.CircleCast(transform.position, raycastWidth, Vector2.down, detectionDistance, detectionLayer);
        
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
