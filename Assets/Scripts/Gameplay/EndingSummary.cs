using System.Linq;
using UnityEngine;
using UnityEngine.UI;

// Ending screen summary: gems, best chain, total time across the run (this session's clears, falling back to
// saved bests), plus a secret line for finding every gem.
public class EndingSummary : MonoBehaviour
{
    RectTransform card;
    float t;

    void Start()
    {
        // The shell's release form already shows the run stats as prison paperwork.
        if (FindAnyObjectByType<EndingController>() != null) { Destroy(gameObject); return; }
        int coll = 0, total = 0, best = 0;
        float time = 0f;
        if (LevelStats.Session.Count > 0)
        {
            foreach (var r in LevelStats.Session.Values)
            {
                coll += r.collected; total += r.collectTotal; best = Mathf.Max(best, r.bestChain); time += r.seconds;
            }
        }
        else
        {
            for (int i = 1; i <= LevelStats.TotalLevels; i++)
            {
                string k = "gp.Level" + i;
                coll += PlayerPrefs.GetInt(k + ".coll", 0);
                total += PlayerPrefs.GetInt(k + ".collTotal", 0);
                best = Mathf.Max(best, PlayerPrefs.GetInt(k + ".chain", 0));
                time += PlayerPrefs.GetFloat(k + ".time", 0f);
            }
        }
        total = Mathf.Max(total, LevelStats.CollectiblesPerLevel * LevelStats.TotalLevels);

        var canvas = ComicUI.MakeCanvas("EndingSummary", 300);
        canvas.transform.SetParent(transform, false);
        canvas.GetComponent<GraphicRaycaster>().enabled = false;
        bool all = coll >= total;
        card = ComicUI.Panel(canvas.transform, new Vector2(0f, 0f), new Vector2(250, 190), new Vector2(420, all ? 300 : 250), ComicUI.Charcoal);
        card.localRotation = Quaternion.Euler(0, 0, 2f);
        ComicUI.Text(card, "YOUR ESCAPE", 40, new Vector2(0.5f, 1f), new Vector2(0, -44), new Vector2(400, 50), ComicUI.Cyan);
        string[] lines = { $"GEMS  {coll}/{total}", $"BEST CHAIN  x{Mathf.Max(1, best)}", $"TIME  {LevelStats.FormatTime(time)}" };
        for (int i = 0; i < lines.Length; i++)
            ComicUI.Text(card, lines[i], 32, new Vector2(0.5f, 1f), new Vector2(0, -100 - i * 46), new Vector2(400, 44), Color.white, ComicUI.Pink, 3f);
        if (all)
        {
            var s = ComicUI.Text(card, "EVERY GEM! TRUE MASTERMIND", 26, new Vector2(0.5f, 0f), new Vector2(0, 34), new Vector2(400, 36), ComicUI.Pink, ComicUI.Cyan, 3f);
            s.root.localRotation = Quaternion.Euler(0, 0, -3f);
        }
        card.localScale = Vector3.zero;
    }

    void Update()
    {
        if (card == null) return;
        t += Mathf.Min(Time.unscaledDeltaTime, 0.05f);
        float lt = t - 1.5f;
        card.localScale = Vector3.one * (lt <= 0 ? 0f : lt < 0.15f ? 1.2f * lt / 0.15f : Mathf.Lerp(1.2f, 1f, Mathf.Clamp01((lt - 0.15f) / 0.12f)));
    }
}
