using System.Collections.Generic;
using UnityEngine;

// Draws the player-ball chain as artist chain-segment sprites laid along a sagging curve.
public class JamChain : MonoBehaviour
{
    const float MaxLength = 3f;   // matches the DistanceJoint2D max distance

    Transform a, b;
    readonly List<(SpriteRenderer sr, float baseAngle, float diag)> segs = new List<(SpriteRenderer, float, float)>();

    Sprite spriteA, spriteB;
    Material material;
    float maxLen = MaxLength;
    const float SegLen = 1.7f;   // world length of one chain-art segment

    public void Init(Transform player, Transform ball, Sprite linkA, Sprite linkB, Material mat)
    {
        a = player; b = ball; spriteA = linkA; spriteB = linkB ?? linkA; material = mat;
        var j = ball.GetComponent<DistanceJoint2D>() ?? player.GetComponent<DistanceJoint2D>();
        maxLen = j != null && j.distance > 0.1f ? j.distance : MaxLength;
    }

    void Ensure(int n)
    {
        while (segs.Count < n)
        {
            int i = segs.Count;
            var s = i % 2 == 0 ? spriteA : spriteB;
            var go = new GameObject("Link" + i);
            go.transform.SetParent(transform, false);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = s;
            if (material != null) sr.sharedMaterial = material;
            sr.sortingOrder = 1;
            Vector2 size = s.bounds.size;
            // Sprite A runs bottom-left -> top-right, B top-left -> bottom-right.
            float ang = Mathf.Atan2(size.y, size.x) * Mathf.Rad2Deg;
            segs.Add((sr, s == spriteA ? ang : -ang, size.magnitude * 0.82f));
        }
        for (int i = 0; i < segs.Count; i++) segs[i].sr.enabled = i < n;
    }

    Vector2 Bez(Vector2 p0, Vector2 c, Vector2 p1, float t) => (1 - t) * (1 - t) * p0 + 2 * (1 - t) * t * c + t * t * p1;

    void LateUpdate()
    {
        if (a == null || b == null) { gameObject.SetActive(false); return; }
        Vector2 p0 = a.position, p1 = b.position;
        float d = Vector2.Distance(p0, p1);
        float slack = Mathf.Max(0f, maxLen - d);
        Vector2 ctrl = (p0 + p1) * 0.5f + Vector2.down * (slack * 0.75f + 0.05f);
        float arc = Mathf.Max(maxLen, d);
        int n = Mathf.Clamp(Mathf.RoundToInt(arc / SegLen), 2, 16);
        Ensure(n);
        for (int i = 0; i < n; i++)
        {
            Vector2 s0 = Bez(p0, ctrl, p1, i / (float)n), s1 = Bez(p0, ctrl, p1, (i + 1) / (float)n);
            Vector2 dir = s1 - s0;
            // Uniform link size so the chain never looks stretched; overlap hides gaps on the curve.
            float scale = (arc / n) * 1.15f / segs[i].diag;
            var t = segs[i].sr.transform;
            t.position = new Vector3((s0.x + s1.x) * 0.5f, (s0.y + s1.y) * 0.5f, 0.5f);
            t.rotation = Quaternion.Euler(0, 0, Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg - segs[i].baseAngle);
            t.localScale = Vector3.one * scale;
        }
    }
}
