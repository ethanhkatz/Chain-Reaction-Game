using UnityEngine;

public class TippyRockController : MonoBehaviour 
{
    private Rigidbody2D rb;
    private bool isLocked = false;
    [HideInInspector] public bool ballHasHit = false;
    private float originalGravityScale;
    
    [Header("Tip Settings")]
    [SerializeField] private float immediateTipSpeed = -15f; 
    [SerializeField] private float fallGravityMultiplier = 3f;  

    void Start() 
    {
        rb = GetComponent<Rigidbody2D>();
        rb.constraints = RigidbodyConstraints2D.FreezePositionX | RigidbodyConstraints2D.FreezeRotation;
        originalGravityScale = rb.gravityScale;
    }

    void Update() 
    {
        if (!isLocked && !ballHasHit && Mathf.Abs(rb.linearVelocity.y) < 0.01f) 
        {
            rb.linearVelocity = Vector2.zero;
            rb.constraints = RigidbodyConstraints2D.FreezeAll;
            isLocked = true;
        }
    }

    private void OnCollisionEnter2D(Collision2D collision) 
    {
        if (collision.gameObject.CompareTag("Ball")) 
        {
            // INVERTED LOGIC: If ball is to the left, tip RIGHT (-1f). If ball is to the right, tip LEFT (1f).
            float direction = collision.transform.position.x < transform.position.x ? -1f : 1f;
            UnfreezeRock(direction);
            return;
        }

        TippyRockController otherRock = collision.gameObject.GetComponent<TippyRockController>();
        if (otherRock != null && otherRock.ballHasHit) 
        {
            // Domino chain reaction follows the same flow direction
            float direction = Mathf.Sign(otherRock.rb.angularVelocity);
            UnfreezeRock(direction);
        }
    }

    private void UnfreezeRock(float directionSign) 
    {
        if (ballHasHit) return; 
        ballHasHit = true;
        
        rb.constraints = RigidbodyConstraints2D.None; 
        
        // Sets the speed instantly away from the impact direction
        rb.angularVelocity = Mathf.Abs(immediateTipSpeed) * directionSign;
        rb.gravityScale = originalGravityScale * fallGravityMultiplier;
    }
    
    private void OnCollisionStay2D(Collision2D collision)
    {
        if (ballHasHit && collision.gameObject.CompareTag("Ground"))
        {
            rb.gravityScale = originalGravityScale;
        }
    }
}
