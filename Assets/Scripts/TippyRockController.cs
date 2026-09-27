// using UnityEngine;

// public class TippyRockController : MonoBehaviour
// {
//     private Rigidbody2D rb;
//     // Start is called once before the first execution of Update after the MonoBehaviour is created
//     void Start()
//     {
//         rb = GetComponent<Rigidbody2D>();
//     }

//     // Update is called once per frame
//     void Update()
//     {
        
//     }
// }
using UnityEngine;

public class TippyRockController : MonoBehaviour
{
    private Rigidbody2D rb;
    private bool isLocked = false;
    private bool ballHasHit = false;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        
        // Start out frozen horizontally so the player can't push it on startup
        rb.constraints = RigidbodyConstraints2D.FreezePositionX | RigidbodyConstraints2D.FreezeRotation;
    }

    void Update()
    {
        // Once the rock hits the floor and stops moving down, lock it vertically too
        // This stops the player's downward weight from glitching or moving the rock
        if (!isLocked && !ballHasHit && Mathf.Abs(rb.linearVelocity.y) < 0.01f)
        {
            rb.linearVelocity = Vector2.zero;
            rb.constraints = RigidbodyConstraints2D.FreezeAll;
            isLocked = true;
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        // ONLY unfreeze if the colliding object has the "Ball" tag
        if (collision.gameObject.CompareTag("Ball"))
        {
            ballHasHit = true;
            rb.constraints = RigidbodyConstraints2D.None; // Free all physics
        }
    }
}
