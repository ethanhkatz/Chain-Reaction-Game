using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class PlayerController : MonoBehaviour
{
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private LayerMask lavaLayer;
    [SerializeField] private Transform groundCheckPoint;
    [SerializeField] private float groundCheckRadius;
    [SerializeField] private float jumpVelocity;
    [SerializeField] private float walkSpeed;

    //Singleton pattern
    private static PlayerController instance;

    private bool isGrounded;
    private ContactFilter2D contactFilter;

    Rigidbody2D rb;
    private void Awake()
    {
        instance = this;
    }
    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        GameObject spawnPoint = GameObject.FindWithTag("SpawnPoint");
        if (spawnPoint != null)
        {
            transform.position = spawnPoint.transform.position;
            transform.rotation = spawnPoint.transform.rotation;
        }
        else
        {
            transform.position = Vector3.zero;
            Debug.LogWarning($"No GameObject with tag 'SpawnPoint' found in {scene.name}. Resetting to Vector3.zero.");
        }
    }

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

    private void OnTriggerEnter2D(Collider2D other)
    {
        if ((lavaLayer.value & (1 << other.gameObject.layer)) > 0)
        {
            GameManager.instance.GameOver();
        }
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
