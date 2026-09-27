using UnityEngine;

// Sprites the runtime shell needs without scene references. Lives at Resources/ShellArt (built by ShellBuilder).
[CreateAssetMenu(menuName = "Jam/Shell Art")]
public class ShellArt : ScriptableObject
{
    public Sprite idleFront1;
    public Sprite idleFront2;
    public Sprite buttonReturnTitle;

    static ShellArt cached;
    public static ShellArt Get()
    {
        if (cached == null) cached = Resources.Load<ShellArt>("ShellArt");
        return cached;
    }
}
