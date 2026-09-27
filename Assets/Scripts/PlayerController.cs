using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class PlayerController : MonoBehaviour
{
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private LayerMask lavaLayer;
    [SerializeField] private Transform groundCheckPoint;
    [SerializeField] private Vector2 groundCheckSize; // Changed from radius to Vector2 size
    [SerializeField] private float jumpVelocity;
    [SerializeField] private float walkSpeed;
    [SerializeField] private Sprite idleRightSprite;
    [SerializeField] private Sprite idleLeftSprite;
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

    private Rigidbody2D rb;
    private SpriteRenderer spriteRenderer;

    //Singleton pattern
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
    }

    void Update()
    {
        // 1. Check if the player is touching the floor using a box overlap
        Collider2D collider = Physics2D.OverlapBox(groundCheckPoint.position, groundCheckSize, 0f, groundLayer);
        isGrounded = collider != null;

        // 2. Catch the exact frame the jump key is pressed while on the ground
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
        // 3. Apply the jump force safely within the physics cycle
        if (jumpRequested)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0f);
            rb.AddForce(new Vector2(0f, jumpVelocity), ForceMode2D.Impulse);
            jumpRequested = false; 
        }

        // Horizontal Movement
        float horizontalVelocity = 0;
        if (Keyboard.current.rightArrowKey.isPressed)
        {
            horizontalVelocity += walkSpeed;
        }
        if (Keyboard.current.leftArrowKey.isPressed)
        {
            horizontalVelocity -= walkSpeed;
        }

        rb.linearVelocity = new Vector2(horizontalVelocity, rb.linearVelocity.y);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if ((lavaLayer.value & (1 << other.gameObject.layer)) > 0)
        {
            GameManager.instance.GameOver();
        }
    }

    private void AnimateCharacter()
    {
        if (!isGrounded)
        {
            spriteRenderer.sprite = facingRight ? jumpRightSprite : jumpLeftSprite;
            return;
        }

        bool isMoving = Keyboard.current.rightArrowKey.isPressed || Keyboard.current.leftArrowKey.isPressed;

        if (isMoving)
        {
            animationTimer += Time.deltaTime;
            if (animationTimer >= frameRate)
            {
                animationTimer = 0f;
                int frameCount = facingRight ? runRightFrames.Length : runLeftFrames.Length;
                if (frameCount > 0)
                {
                    currentFrameIndex = (currentFrameIndex + 1) % frameCount;
                    spriteRenderer.sprite = facingRight ? runRightFrames[currentFrameIndex] : runLeftFrames[currentFrameIndex];
                }
            }
            return;
        }

        spriteRenderer.sprite = facingRight ? idleRightSprite : idleLeftSprite;
        currentFrameIndex = 0;
    }

    private void OnDrawGizmosSelected()
    {
        if (groundCheckPoint != null)
        {
            Gizmos.color = Color.blue;
            // Changed to draw a wire cube matching the box dimensions
            Gizmos.DrawWireCube(groundCheckPoint.position, groundCheckSize);
        }
    }
}