using UnityEngine;

// Sprites and clips the runtime shell needs without scene references. Lives at Resources/ShellArt (built by ShellBuilder).
[CreateAssetMenu(menuName = "Jam/Shell Art")]
public class ShellArt : ScriptableObject
{
    public Sprite idleFront1;
    public Sprite idleFront2;
    public Sprite buttonReturnTitle;

    [Header("Level intro sign")]
    public Sprite sign;
    public Sprite chainLink1;
    public Sprite chainLink2;
    public Sprite crash;

    [Header("Ball skins (index = PlayerPrefs ballSkin)")]
    public Sprite[] ballSkins;

    [Header("Team audio (Assets/Audio)")]
    public AudioClip footsteps;
    public AudioClip jump;
    public AudioClip deathExplosion;
    public AudioClip gameOverSting;
    public AudioClip musicMenu;
    public AudioClip musicLevel;
    public AudioClip musicEnding;

    static ShellArt cached;
    public static ShellArt Get()
    {
        if (cached == null) cached = Resources.Load<ShellArt>("ShellArt");
        return cached;
    }
}
