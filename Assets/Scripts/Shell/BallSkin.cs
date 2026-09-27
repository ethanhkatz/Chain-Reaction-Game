using UnityEngine;

// Cosmetic ball skins. The choice lives in PlayerPrefs "ballSkin"; ShellBootstrap applies it to the Ball in every level.
// The skin sprite is re-created with a pixels-per-unit that gives it exactly the original sprite's bounds, so the
// ball's transform, collider and anything sized from the sprite stay unchanged.
public static class BallSkin
{
    public const string PrefKey = "ballSkin";
    public static readonly string[] Names = { "Classic", "Pink", "Gold", "Disco", "Magma" };

    public static int Count => Names.Length;

    public static int Current
    {
        get
        {
            // Headless captures can force a skin without touching saved prefs.
            var env = System.Environment.GetEnvironmentVariable("JAM_BALL_SKIN");
            if (!string.IsNullOrEmpty(env) && int.TryParse(env, out int e)) return Mathf.Clamp(e, 0, Count - 1);
            return Mathf.Clamp(PlayerPrefs.GetInt(PrefKey, 0), 0, Count - 1);
        }
        set { PlayerPrefs.SetInt(PrefKey, Mathf.Clamp(value, 0, Count - 1)); PlayerPrefs.Save(); }
    }

    public static Sprite SpriteFor(int i)
    {
        var art = ShellArt.Get();
        if (art == null || art.ballSkins == null || i < 0 || i >= art.ballSkins.Length) return null;
        return art.ballSkins[i];
    }

    public static Sprite CurrentSprite => SpriteFor(Current);

    public static void Apply(GameObject ball)
    {
        int i = Current;
        if (i == 0 || ball == null) return; // Classic = the level's own ball art
        var skin = SpriteFor(i);
        var sr = ball.GetComponentInChildren<SpriteRenderer>();
        if (skin == null || sr == null || sr.sprite == null) return;
        Vector2 want = sr.sprite.bounds.size;
        Rect r = skin.textureRect;
        float ppu = Mathf.Max(r.width / Mathf.Max(want.x, 0.0001f), r.height / Mathf.Max(want.y, 0.0001f));
        var s = Sprite.Create(skin.texture, r, new Vector2(0.5f, 0.5f), ppu, 0, SpriteMeshType.FullRect);
        s.name = skin.name + "_ballsized";
        sr.sprite = s;
    }
}
