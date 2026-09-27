using TMPro;
using UnityEngine;

// Level-number card ("1 — Break Stuff") that fades in and out at level start.
public class TitleCard : MonoBehaviour
{
    // Index = level number. Edit here to retitle levels.
    public static readonly string[] Titles = { "", "Break Stuff", "Knock-On Effect", "Dead Weight", "Look Up", "The Way Out" };

    CanvasGroup group;
    float t;
    const float In = 0.5f, Hold = 1.6f, Out = 0.7f;

    public static void Show(Transform parent, int level)
    {
        string title = level > 0 && level < Titles.Length ? Titles[level] : "";
        var canvas = ShellUI.MakeCanvas("TitleCard", 150);
        canvas.transform.SetParent(parent, false);
        var card = canvas.gameObject.AddComponent<TitleCard>();
        card.group = canvas.gameObject.AddComponent<CanvasGroup>();
        card.group.alpha = 0f;
        card.group.blocksRaycasts = false;

        var band = ShellUI.Rect("Band", canvas.transform, new Vector2(0.5f, 0.72f), Vector2.zero, new Vector2(2400, 170));
        band.gameObject.AddComponent<UnityEngine.UI.Image>().color = new Color(0.03f, 0.03f, 0.04f, 0.6f);
        var num = ShellUI.Text(band, level.ToString(), 96, new Vector2(0.5f, 0.5f), new Vector2(0, 18), new Vector2(1600, 110), ShellUI.Cyan);
        num.fontStyle = FontStyles.Bold;
        num.text = string.IsNullOrEmpty(title) ? $"Level {level}" : $"{level} — {title}";
        var sub = ShellUI.Text(band, "EXPERIMENTAL PRISON  ·  CELL BLOCK " + level, 26, new Vector2(0.5f, 0.5f), new Vector2(0, -56), new Vector2(1600, 40), new Color(1, 1, 1, 0.6f));
        sub.characterSpacing = 8f;
    }

    void Update()
    {
        t += Mathf.Min(Time.unscaledDeltaTime, 0.05f); // ignore load hitches
        if (t < In) group.alpha = Mathf.SmoothStep(0, 1, t / In);
        else if (t < In + Hold) group.alpha = 1f;
        else if (t < In + Hold + Out) group.alpha = Mathf.SmoothStep(1, 0, (t - In - Hold) / Out);
        else Destroy(gameObject);
    }
}
