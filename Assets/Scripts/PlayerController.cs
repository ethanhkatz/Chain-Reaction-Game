// using UnityEngine;
// using UnityEngine.InputSystem;
// using UnityEngine.SceneManagement;

// public class PlayerController : MonoBehaviour
// {
//     [SerializeField] private LayerMask groundLayer;
//     [SerializeField] private LayerMask lavaLayer;
//     [SerializeField] private Transform groundCheckPoint;
//     [SerializeField] private Vector2 groundCheckSize; 
//     [SerializeField] private float jumpVelocity;
//     [SerializeField] private float walkSpeed;

//     // Single regular idle frame setup
//     [SerializeField] private Sprite idleRightSprite;
//     [SerializeField] private Sprite idleLeftSprite;
    
//     // --- Drop your 8 drawing sprites into this array in the Inspector ---
//     [SerializeField] private Sprite[] longIdleFrames; 
    
//     [SerializeField] private Sprite jumpRightSprite;
//     [SerializeField] private Sprite jumpLeftSprite;
//     [SerializeField] private Sprite[] runRightFrames;
//     [SerializeField] private Sprite[] runLeftFrames;
//     [SerializeField] private float frameRate = 0.1f;

//     private bool isGrounded;
//     private bool jumpRequested;
//     private bool facingRight = true;
//     private float animationTimer;
//     private int currentFrameIndex;

//     // Inactivity Tracking
//     private float inactivityTimer;
//     private const float LongIdleThreshold = 30f; 

//     private Rigidbody2D rb;
//     private SpriteRenderer spriteRenderer;
    
//     private static PlayerController instance;
//     private ContactFilter2D contactFilter;

//     private void Awake()
//     {
//         instance = this;
//     }

//     void Start()
//     {
//         rb = GetComponent<Rigidbody2D>();
//         spriteRenderer = GetComponent<SpriteRenderer>();
//     }

//     void Update()
//     {
//         isGrounded = Physics2D.OverlapBox(groundCheckPoint.position, groundCheckSize, 0f, groundLayer) != null;

//         if (isGrounded && Keyboard.current.spaceKey.wasPressedThisFrame)
//         {
//             jumpRequested = true;
//         }

//         if (Keyboard.current.rightArrowKey.isPressed)
//         {
//             facingRight = true;
//         }
//         else if (Keyboard.current.leftArrowKey.isPressed)
//         {
//             facingRight = false;
//         }

//         AnimateCharacter();
//     }

//     private void FixedUpdate()
//     {
//         if (jumpRequested)
//         {
//             rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0f);
//             rb.AddForce(new Vector2(0f, jumpVelocity), ForceMode2D.Impulse);
//             jumpRequested = false;
//         }

//         float horizontalVelocity = 0;
//         if (Keyboard.current.rightArrowKey.isPressed) { horizontalVelocity += walkSpeed; }
//         if (Keyboard.current.leftArrowKey.isPressed) { horizontalVelocity -= walkSpeed; }

//         rb.linearVelocity = new Vector2(horizontalVelocity, rb.linearVelocity.y);
//     }

//     private void OnTriggerEnter2D(Collider2D other)
//     {
//         if ((lavaLayer.value & (1 << other.gameObject.layer)) > 0)
//         {
//             GameManager.instance.GameOver();
//         }
//         else if (other.CompareTag("Finish"))
//         {
//             GameManager.instance.LevelClear();
//         }
//     }

//     private void AnimateCharacter()
//     {
//         // 1. Air handling (Resets inactivity)
//         if (!isGrounded)
//         {
//             spriteRenderer.sprite = facingRight ? jumpRightSprite : jumpLeftSprite;
//             inactivityTimer = 0f; 
//             return;
//         }

//         bool isMoving = Keyboard.current.rightArrowKey.isPressed || Keyboard.current.leftArrowKey.isPressed;

//         // 2. Running state (Resets inactivity)
//         if (isMoving)
//         {
//             inactivityTimer = 0f; 
//             PlayLoopingAnimation(facingRight ? runRightFrames : runLeftFrames);
//             return;
//         }

//         // 3. Grounded & Standing Still
//         inactivityTimer += Time.deltaTime;

//         if (inactivityTimer >= LongIdleThreshold)
//         {
//             // Loops through your 8 special drawings automatically
//             PlayLoopingAnimation(longIdleFrames);
//         }
//         else
//         {
//             // Regular 1-frame idle state
//             spriteRenderer.sprite = facingRight ? idleRightSprite : idleLeftSprite;
//             currentFrameIndex = 0;
//             animationTimer = 0f;
//         }
//     }

//     private void PlayLoopingAnimation(Sprite[] frames)
//     {
//         if (frames == null || frames.Length == 0) return;

//         animationTimer += Time.deltaTime;
//         if (animationTimer >= frameRate)
//         {
//             animationTimer = 0f;
//             currentFrameIndex = (currentFrameIndex + 1) % frames.Length;
//             spriteRenderer.sprite = frames[currentFrameIndex];
//         }
//     }

//     private void OnDrawGizmosSelected()
//     {
//         if (groundCheckPoint != null)
//         {
//             Gizmos.color = Color.blue;
//             Gizmos.DrawWireCube(groundCheckPoint.position, groundCheckSize);
//         }
//     }
// }
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class PlayerController : MonoBehaviour
{
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private LayerMask lavaLayer;
    [SerializeField] private Transform groundCheckPoint;
    [SerializeField] private Vector2 groundCheckSize; 
    [SerializeField] private float jumpVelocity;
    [SerializeField] private float walkSpeed;

    [SerializeField] private Sprite idleRightSprite;
    [SerializeField] private Sprite idleLeftSprite;
    
    // Drop your 8 drawing sprites into this array in the Inspector
    [SerializeField] private Sprite[] longIdleFrames; 
    
    [SerializeField] private Sprite jumpRightSprite;
    [SerializeField] private Sprite jumpLeftSprite;
    [SerializeField] private Sprite[] runRightFrames;
    [SerializeField] private Sprite[] runLeftFrames;
    [SerializeField] private float frameRate = 0.1f;

    private bool isGrounded;
    private bool jumpRequested;
    private bool facingRight = true;
    private float animationTimer;
    private int currentFrameIndex;

    // Inactivity Tracking
    private float inactivityTimer;
    private const float LongIdleThreshold = 30f; 

    private Rigidbody2D rb;
    private SpriteRenderer spriteRenderer;
    private BoxCollider2D bodyCollider;
    
    private static PlayerController instance;
    private ContactFilter2D contactFilter;

    private void Awake()
    {
        instance = this;
    }

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        bodyCollider = GetComponent<BoxCollider2D>();
    }

    void Update()
    {
        isGrounded = Physics2D.OverlapBox(groundCheckPoint.position, groundCheckSize, 0f, groundLayer) != null
            || IsStandingOnSomething();
        PassThroughBallAbove();

        if (isGrounded && Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            jumpRequested = true;
        }

        if (Keyboard.current.rightArrowKey.isPressed)
        {
            facingRight = true;
        }
        else if (Keyboard.current.leftArrowKey.isPressed)
        {
            facingRight = false;
        }

        AnimateCharacter();
    }

    private void FixedUpdate()
    {
        if (jumpRequested)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0f);
            rb.AddForce(new Vector2(0f, jumpVelocity), ForceMode2D.Impulse);
            jumpRequested = false;
        }

        float horizontalVelocity = 0;
        if (Keyboard.current.rightArrowKey.isPressed) { horizontalVelocity += walkSpeed; }
        if (Keyboard.current.leftArrowKey.isPressed) { horizontalVelocity -= walkSpeed; }

        rb.linearVelocity = new Vector2(horizontalVelocity, rb.linearVelocity.y);
    }

    private Collider2D ballCollider;

    // Jumping up into the ball when it rests on a ledge overhead used to bounce the player back down. Let the player
    // pass through the ball while it is above them; at the same height it still collides so it can be shoved.
    private void PassThroughBallAbove()
    {
        if (ballCollider == null)
        {
            var joint = GetComponent<DistanceJoint2D>();
            if (joint == null || joint.connectedBody == null) return;
            ballCollider = joint.connectedBody.GetComponent<Collider2D>();
            if (ballCollider == null || bodyCollider == null) return;
        }
        bool above = ballCollider.bounds.center.y > bodyCollider.bounds.center.y + 0.8f;
        Physics2D.IgnoreCollision(bodyCollider, ballCollider, above);
    }

    private readonly ContactPoint2D[] contacts = new ContactPoint2D[16];

    // The feet box only covers the middle of the player, so standing on a stair edge or the ball left the player
    // unable to jump. Also count any solid contact along the bottom of the body collider.
    private bool IsStandingOnSomething()
    {
        if (bodyCollider == null) return false;
        float feet = bodyCollider.bounds.min.y + 0.15f;
        int n = rb.GetContacts(contacts);
        for (int i = 0; i < n; i++)
        {
            var other = contacts[i].collider == bodyCollider ? contacts[i].otherCollider : contacts[i].collider;
            if (other == null || other.isTrigger) continue;
            if (contacts[i].point.y <= feet && rb.linearVelocity.y <= 0.1f) return true;
        }
        return false;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if ((lavaLayer.value & (1 << other.gameObject.layer)) > 0)
        {
            GameManager.instance.GameOver();
        }
        else if (other.CompareTag("Finish"))
        {
            GameManager.instance.LevelClear();
        }
    }

    private void AnimateCharacter()
    {
        // 1. Air handling (Resets inactivity)
        if (!isGrounded)
        {
            spriteRenderer.sprite = facingRight ? jumpRightSprite : jumpLeftSprite;
            inactivityTimer = 0f; 
            return;
        }

        bool isMoving = Keyboard.current.rightArrowKey.isPressed || Keyboard.current.leftArrowKey.isPressed;

        // 2. Running state (Resets inactivity)
        if (isMoving)
        {
            inactivityTimer = 0f; 
            PlayLoopingAnimation(facingRight ? runRightFrames : runLeftFrames);
            return;
        }

        // 3. Grounded & Standing Still
        inactivityTimer += Time.deltaTime;

        if (inactivityTimer >= LongIdleThreshold)
        {
            // Plays the special animation and holds the last frame
            PlayLongIdleAnimation();
        }
        else
        {
            // Regular 1-frame idle state
            spriteRenderer.sprite = facingRight ? idleRightSprite : idleLeftSprite;
            currentFrameIndex = 0;
            animationTimer = 0f;
        }
    }

    private void PlayLoopingAnimation(Sprite[] frames)
    {
        if (frames == null || frames.Length == 0) return;

        animationTimer += Time.deltaTime;
        if (animationTimer >= frameRate)
        {
            animationTimer = 0f;
            currentFrameIndex = (currentFrameIndex + 1) % frames.Length;
            spriteRenderer.sprite = frames[currentFrameIndex];
        }
    }

    // --- NEW: Custom animation loop that freezes at the last index ---
    private void PlayLongIdleAnimation()
    {
        if (longIdleFrames == null || longIdleFrames.Length == 0) return;

        // If we haven't reached the final frame yet, update the timer and cycle forward
        if (currentFrameIndex < longIdleFrames.Length - 1)
        {
            animationTimer += Time.deltaTime;
            if (animationTimer >= frameRate)
            {
                animationTimer = 0f;
                currentFrameIndex++;
            }
        }

        // Always apply the current frame index to keep rendering the frozen sprite
        spriteRenderer.sprite = longIdleFrames[currentFrameIndex];
    }

    private void OnDrawGizmosSelected()
    {
        if (groundCheckPoint != null)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawWireCube(groundCheckPoint.position, groundCheckSize);
        }
    }
}

