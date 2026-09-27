using UnityEngine;

public class TippyRockController : MonoBehaviour 
{
    private Rigidbody2D rb;
    private bool isLocked = false;
    [HideInInspector] public bool ballHasHit = false;
    
    [Header("Tip Settings")]
    [SerializeField] private float immediateTipSpeed = -5f; // Negative tips right, Positive tips left

    void Start() 
    {
        rb = GetComponent<Rigidbody2D>();
        rb.constraints = RigidbodyConstraints2D.FreezePositionX | RigidbodyConstraints2D.FreezeRotation;
    }

    void Update() 
    {
        // Lock the rock if it hits the ground before the ball touches it
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
            // Calculate hit side: negative if ball is to the left, positive if to the right
            float direction = collision.transform.position.x < transform.position.x ? 1f : -1f;
            UnfreezeRock(direction);
            return;
        }

        TippyRockController otherRock = collision.gameObject.GetComponent<TippyRockController>();
        if (otherRock != null && otherRock.ballHasHit) 
        {
            // For domino chain reactions, tip in the same direction as the falling rock
            float direction = Mathf.Sign(otherRock.rb.angularVelocity);
            UnfreezeRock(direction);
        }
    }

    private void UnfreezeRock(float directionSign) 
    {
        if (ballHasHit) return; 
        ballHasHit = true;
        
        rb.constraints = RigidbodyConstraints2D.None; // Free all physics
        
        // Force the rotation speed directly so it breaks its balance immediately
        rb.angularVelocity = Mathf.Abs(immediateTipSpeed) * directionSign;
    }
}