using System.Collections.Generic;
using UnityEngine;

// High-contrast mode: darkens the painted background, pushes hazards to hot orange, the exit to pure white,
// and the ball/interactive rocks to bright cyan so the play space reads at a glance.
public static class HighContrast
{
    static readonly Dictionary<SpriteRenderer, Color> original = new Dictionary<SpriteRenderer, Color>();
    static Color? camColor;

    public static void Apply(bool on)
    {
        int lava = LayerMask.NameToLayer("Lava");
        int rock = LayerMask.NameToLayer("Rock");
        int ball = LayerMask.NameToLayer("Ball");
        foreach (var sr in Object.FindObjectsByType<SpriteRenderer>(FindObjectsInactive.Include))
        {
            if (!original.ContainsKey(sr)) original[sr] = sr.color;
            var baseColor = original[sr];
            if (!on) { sr.color = baseColor; continue; }
            var go = sr.gameObject;
            string n = (go.name + " " + (sr.sprite != null ? sr.sprite.name : "")).ToLowerInvariant();
            if (n.Contains("background") || n.Contains("backdrop")) sr.color = baseColor * new Color(0.3f, 0.3f, 0.33f, 1f);
            else if (go.layer == lava || n.Contains("lava") || n.Contains("laser")) sr.color = new Color(1f, 0.45f, 0.05f, baseColor.a);
            else if (go.CompareTag("Finish")) sr.color = new Color(1f, 1f, 1f, baseColor.a);
            else if (go.layer == rock || go.layer == ball || go.CompareTag("Rock") || go.CompareTag("Ball")) sr.color = new Color(0.55f, 1f, 0.92f, baseColor.a);
            else sr.color = baseColor;
        }
        var cam = Camera.main;
        if (cam != null)
        {
            if (camColor == null) camColor = cam.backgroundColor;
            cam.backgroundColor = on ? Color.black : camColor.Value;
        }
    }

    public static void ResetCache() { original.Clear(); camColor = null; }
}
