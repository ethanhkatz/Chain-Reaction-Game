using UnityEngine;

// Companion to RockShape: when the rock turns into stairs, RockShape adds a PolygonCollider2D traced from the sprite
// outline. Its bevelled corners can hold the player just off a step (not grounded, so no jump). This swaps it for
// three clean box steps matching the stairs art (tall column on the sprite's left; flip with negative X scale).
[RequireComponent(typeof(RockShape))]
public class RockStairsSteps : MonoBehaviour
{
    [Tooltip("Physics material for the steps (frictionless avoids wall-sticking on risers).")]
    [SerializeField] private PhysicsMaterial2D stepMaterial;

    // Column boundaries (fraction of width from the sprite's left) and step heights (fraction of height).
    static readonly float[] ColumnEdges = { 0f, 0.31f, 0.61f, 1f };
    static readonly float[] Heights = { 1f, 0.625f, 0.29f };

    private bool done;

    void FixedUpdate()
    {
        if (done) return;
        var poly = GetComponent<PolygonCollider2D>();
        if (poly == null) return;
        done = true;

        var sprite = GetComponent<SpriteRenderer>().sprite;
        Bounds b = sprite.bounds; // local space
        Destroy(poly);
        for (int i = 0; i < 3; i++)
        {
            float x0 = b.min.x + b.size.x * ColumnEdges[i];
            float x1 = b.min.x + b.size.x * ColumnEdges[i + 1];
            float h = b.size.y * Heights[i];
            var box = gameObject.AddComponent<BoxCollider2D>();
            box.size = new Vector2(x1 - x0, h);
            box.offset = new Vector2((x0 + x1) * 0.5f, b.min.y + h * 0.5f);
            if (stepMaterial != null) box.sharedMaterial = stepMaterial;
        }
    }
}
