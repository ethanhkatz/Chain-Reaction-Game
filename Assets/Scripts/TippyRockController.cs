using UnityEngine;

public class TippyRockController : MonoBehaviour 
{
    private Rigidbody2D rb;
    private bool isLocked = false;
    [HideInInspector] public bool ballHasHit = false;
    private float originalGravityScale; // Remembers your default gravity setting
    
    [Header("Tip Settings")]
    [SerializeField] private float immediateTipSpeed = -15f; // Increase this value to spin faster (e.g., -15f or -20f)
    [SerializeField] private float fallGravityMultiplier = 3f;  // Multiplies gravity while tipping to snap down faster

    void Start() 
    {
        rb = GetComponent<Rigidbody2D>();
        rb.constraints = RigidbodyConstraints2D.FreezePositionX | RigidbodyConstraints2D.FreezeRotation;
        
        // Save the rock's starting gravity scale
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
            float direction = collision.transform.position.x < transform.position.x ? 1f : -1f;
            UnfreezeRock(direction);
            return;
        }

        TippyRockController otherRock = collision.gameObject.GetComponent<TippyRockController>();
        if (otherRock != null && otherRock.ballHasHit) 
        {
            float direction = Mathf.Sign(otherRock.rb.angularVelocity);
            UnfreezeRock(direction);
        }
    }

    private void UnfreezeRock(float directionSign) 
    {
        if (ballHasHit) return; 
        ballHasHit = true;
        
        rb.constraints = RigidbodyConstraints2D.None; 
        
        // 1. Instantly set a much higher rotation speed
        rb.angularVelocity = Mathf.Abs(immediateTipSpeed) * directionSign;

        // 2. Turn up gravity so it pulls the falling rock downward faster
        rb.gravityScale = originalGravityScale * fallGravityMultiplier;
    }
    
    // Optional: Reset gravity if the rock lands and you want it heavy/stable
    private void OnCollisionStay2D(Collision2D collision)
    {
        if (ballHasHit && collision.gameObject.CompareTag("Ground"))
        {
            rb.gravityScale = originalGravityScale;
        }
    }
}