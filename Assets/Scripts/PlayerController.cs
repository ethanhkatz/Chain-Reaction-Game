using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private Transform groundCheckPoint;
    [SerializeField] private float groundCheckRadius;
    [SerializeField] private float jumpVelocity;
    [SerializeField] private float walkSpeed;

    private bool isGrounded;
    private ContactFilter2D contactFilter;

    Rigidbody2D rb;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        contactFilter = new ContactFilter2D();
        contactFilter.layerMask = groundLayer;
        contactFilter.useLayerMask = true;
    }

    // Update is called once per frame
    void Update()
    {
        Collider2D collider = Physics2D.OverlapCircle(groundCheckPoint.position, groundCheckRadius, groundLayer);
        isGrounded = collider != null;
    }

    private void FixedUpdate()
    {
        float verticalVelocity = rb.linearVelocity.y;
        if (isGrounded && Keyboard.current.spaceKey.isPressed)
        {
            rb.AddForce(new Vector2(0f, jumpVelocity), ForceMode2D.Impulse);
        }
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

    private void OnDrawGizmosSelected()
    {
        if (groundCheckPoint != null)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawSphere(groundCheckPoint.position, groundCheckRadius);
        }
    }
}
