using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class RockShape : MonoBehaviour
{
    [Header("Visual Stages")]
    
    [Tooltip("Sprite for Stage 1 (Moderately broken)")]
    [SerializeField] private Sprite stage1Sprite;
    
    [Tooltip("Sprite for Stage 2 (Very broken)")]
    [SerializeField] private Sprite stage2Sprite;

    [Tooltip("Sprite for Stage 3 (Stairs shape)")]
    [SerializeField] private Sprite stairsSprite;

    [Tooltip("Whether this changes into stairs after destruction")]
    [SerializeField] private bool isStairs;

    [Tooltip("Whether the stairs are facing left or right")]
    [SerializeField] private bool stairsIsRight;

    [Tooltip("Keep the stairs the same size and facing as the rock instead of applying the stairs scale below")]
    [SerializeField] private bool matchSizeOnStairs;

    private int currentStage = 0; // Tracks hits: 0 = Intact, 1 = Stage 1, 2 = Stage 2, 3 = Stage 3, 4 = Stairs
    private SpriteRenderer spriteRenderer;

    private Vector2 targetWorldSize; 

    void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();

        if (spriteRenderer.sprite != null)
        {
            Vector3 spriteSize = spriteRenderer.sprite.bounds.size;
            targetWorldSize = new Vector2(spriteSize.x * transform.localScale.x, spriteSize.y * transform.localScale.y);
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        // Check if the colliding object has the BallController script attached
        if (collision.gameObject.TryGetComponent<BallController>(out _))
        {
            TakeDamage();
        }
    }

    private void TakeDamage()
    {
        // If it's already reached the stairs stage, do nothing more
        if (currentStage >= 3) return;

        currentStage++;

        switch (currentStage)
        {
            case 1:
                if (stage1Sprite != null) ChangeSpriteAndMatchScale(stage1Sprite);
                break;
            case 2:
                if (stage2Sprite != null) ChangeSpriteAndMatchScale(stage2Sprite);
                break;
            case 3:
                if (isStairs) ConvertToStairs();
                else Destroy(gameObject);
                break;
        }
    }

    private void ChangeSpriteAndMatchScale(Sprite newSprite)
    {
        spriteRenderer.sprite = newSprite;

        // Prevent division by zero if a sprite layout is empty
        if (newSprite.bounds.size.x == 0 || newSprite.bounds.size.y == 0) return;

        // Dynamically adjust scale to match the target size we captured at Start()
        float newScaleX = targetWorldSize.x / newSprite.bounds.size.x;
        float newScaleY = targetWorldSize.y / newSprite.bounds.size.y;

        transform.localScale = new Vector3(newScaleX, newScaleY, transform.localScale.z);
    }

    private void ConvertToStairs()
    {
        if (stairsSprite != null)
        {
            if (matchSizeOnStairs) ChangeSpriteAndMatchScale(stairsSprite);
            else spriteRenderer.sprite = stairsSprite;
        }

        // Remove existing 2D primitive colliders so they don't block the player like a solid block
        if (TryGetComponent<BoxCollider2D>(out BoxCollider2D box)) Destroy(box);
        if (TryGetComponent<CircleCollider2D>(out CircleCollider2D circle)) Destroy(circle);

        // Add a PolygonCollider2D which automatically shapes itself tightly around your new stair sprite transparency
        PolygonCollider2D polygonCollider = gameObject.AddComponent<PolygonCollider2D>();
        
        // Ensure the stairs are on your player's ground layer so they can jump off of it
        gameObject.layer = LayerMask.NameToLayer("Ground");

        if (matchSizeOnStairs) return;

        // Scale the stairs and reflect if necessary
        float newScale = transform.localScale.x / 2f;
        if (stairsIsRight)
        {
            transform.localScale = new Vector3(newScale, newScale * 2.5f, newScale);
        }
        else
        {
            transform.localScale = new Vector3(-newScale, newScale * 2.5f, newScale);
        }
    }
}
