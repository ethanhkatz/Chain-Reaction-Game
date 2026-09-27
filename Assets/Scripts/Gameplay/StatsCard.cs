using UnityEngine;
using UnityEngine.UI;

// Level Clear stats card: slides in beside the Level Clear panel, counts the rows in, then a rank stamp
// (S/A/B/C) slams down like a rubber stamp. Runs on unscaled time (the level is frozen).
public class StatsCard : MonoBehaviour
{
    RectTransform card, stamp;
    CanvasGroup stampGroup;
    ComicUI.Label[] rows;
    ComicUI.Label bestTag;
    float t;
    bool stamped;
    LevelStats.Result r;
    Vector2 cardHome;

    const float SlideIn = 0.35f, RowGap = 0.12f, StampAt = 1.1f;

    public static StatsCard Show(Transform parent, LevelStats.Result result)
    {
        var canvas = ComicUI.MakeCanvas("StatsCard", 200);
        canvas.transform.SetParent(parent, false);
        canvas.GetComponent<GraphicRaycaster>().enabled = false;
        var c = canvas.gameObject.AddComponent<StatsCard>();
        c.Build(canvas.transform, result);
        return c;
    }

    void Build(Transform root, LevelStats.Result result)
    {
        r = result;
        cardHome = new Vector2(-330, -10);
        card = ComicUI.Panel(root, new Vector2(1f, 0.5f), cardHome, new Vector2(480, 560), ComicUI.Charcoal);
        card.localRotation = Quaternion.Euler(0, 0, -2.5f);

        var title = ComicUI.Text(card, "CELL CLEARED", 50, new Vector2(0.5f, 1f), new Vector2(0, -62), new Vector2(440, 60), ComicUI.Cyan);
        title.root.localRotation = Quaternion.Euler(0, 0, 2f);

        string coll = r.collectTotal > 0 ? $"{r.collected}/{r.collectTotal}" : "-";
        string[,] data =
        {
            { "TIME", LevelStats.FormatTime(r.seconds) },
            { "DEATHS", r.deaths.ToString() },
            { "BEST CHAIN", r.bestChain > 1 ? "x" + r.bestChain : r.bestChain.ToString() },
            { "GEMS", coll },
            { "SCORE", r.points.ToString("N0") },
        };
        int n = data.GetLength(0);
        rows = new ComicUI.Label[n * 2];
        for (int i = 0; i < n; i++)
        {
            float y = -140 - i * 66;
            bool last = i == n - 1;
            var k = ComicUI.Text(card, data[i, 0], 32, new Vector2(0f, 1f), new Vector2(170, y), new Vector2(260, 50), Color.white, ComicUI.Ink, 3f, TMPro.TextAlignmentOptions.Left);
            var v = ComicUI.Text(card, data[i, 1], last ? 46 : 40, new Vector2(1f, 1f), new Vector2(-150, y), new Vector2(240, 56), last ? ComicUI.Cyan : Color.white, ComicUI.Pink, -1f, TMPro.TextAlignmentOptions.Right);
            k.root.localScale = v.root.localScale = Vector3.zero;
            rows[i * 2] = k; rows[i * 2 + 1] = v;
        }

        // Rank stamp: pink ring + letter, over the card's lower-left corner.
        stamp = ComicUI.Rect("RankStamp", card, new Vector2(0f, 0f), new Vector2(40, 40), new Vector2(230, 230));
        stampGroup = stamp.gameObject.AddComponent<CanvasGroup>();
        var ringShadow = ComicUI.Rect("RingInk", stamp, new Vector2(0.5f, 0.5f), new Vector2(6, -6), new Vector2(230, 230));
        ComicUI.Img(ringShadow, ComicUI.RingSprite, ComicUI.Ink);
        var ring = ComicUI.Rect("Ring", stamp, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(230, 230));
        ComicUI.Img(ring, ComicUI.RingSprite, RankColor(r.rank));
        var letter = ComicUI.Text(stamp, r.rank, 150, new Vector2(0.5f, 0.5f), new Vector2(0, 6), new Vector2(200, 180), RankColor(r.rank), ComicUI.Ink, 7f);
        var word = ComicUI.Text(stamp, "RANK", 26, new Vector2(0.5f, 0f), new Vector2(0, 38), new Vector2(200, 30), Color.white, ComicUI.Ink, 2f);
        stamp.localRotation = Quaternion.Euler(0, 0, 14f);
        stamp.gameObject.SetActive(false);

        bestTag = ComicUI.Text(card, "NEW BEST!", 34, new Vector2(1f, 0f), new Vector2(-110, 50), new Vector2(220, 44), ComicUI.Pink, ComicUI.Ink, 3f);
        bestTag.root.localRotation = Quaternion.Euler(0, 0, 8f);
        bestTag.root.gameObject.SetActive(false);
        if (!r.newBest)
        {
            string prev = LevelStats.BestRank(r.level);
            bestTag.text = "BEST: " + prev;
            bestTag.SetColors(Color.white, ComicUI.Ink);
        }

        Update();
    }

    public static Color RankColor(string rank) =>
        rank == "S" ? new Color(1f, 0.84f, 0.25f) : rank == "A" ? ComicUI.Pink : rank == "B" ? ComicUI.Cyan : new Color(0.85f, 0.85f, 0.85f);

    void Update()
    {
        t += Mathf.Min(Time.unscaledDeltaTime, 0.05f);

        // Card slides in from the right with a little overshoot.
        float k = Mathf.Clamp01(t / SlideIn);
        float ease = 1f + 2.2f * Mathf.Pow(k - 1f, 3) + 1.2f * Mathf.Pow(k - 1f, 2);
        card.anchoredPosition = cardHome + new Vector2((1f - ease) * 700f, 0);

        for (int i = 0; i < rows.Length / 2; i++)
        {
            float lt = t - SlideIn - i * RowGap;
            float s = lt <= 0 ? 0f : lt < 0.1f ? Mathf.Lerp(0f, 1.25f, lt / 0.1f) : lt < 0.2f ? Mathf.Lerp(1.25f, 1f, (lt - 0.1f) / 0.1f) : 1f;
            rows[i * 2].root.localScale = rows[i * 2 + 1].root.localScale = Vector3.one * s;
        }

        // Stamp: drops from big and transparent, hits at StampAt with a thwack and a jolt of the card.
        float st = t - StampAt;
        if (st > -0.18f)
        {
            if (!stamp.gameObject.activeSelf) stamp.gameObject.SetActive(true);
            if (st < 0f)
            {
                float a = 1f + st / 0.18f; // 0..1
                stamp.localScale = Vector3.one * Mathf.Lerp(3f, 1f, a * a);
                stampGroup.alpha = a;
            }
            else
            {
                if (!stamped)
                {
                    stamped = true;
                    GameplayAudio.Stamp();
                    ShellAudio.Play(ShellAudio.Sfx.Thud, 0.8f, 0f);
                    bestTag.root.gameObject.SetActive(true);
                }
                stamp.localScale = Vector3.one * (st < 0.08f ? Mathf.Lerp(0.9f, 1.05f, st / 0.08f) : st < 0.16f ? Mathf.Lerp(1.05f, 1f, (st - 0.08f) / 0.08f) : 1f);
                stampGroup.alpha = 1f;
                float jolt = Mathf.Max(0f, 1f - st / 0.25f);
                card.anchoredPosition += new Vector2(Mathf.Sin(st * 90f) * 10f * jolt, -8f * jolt);
                if (bestTag != null)
                {
                    float bt = st - 0.2f;
                    bestTag.root.localScale = Vector3.one * (bt <= 0 ? 0f : bt < 0.1f ? 1.3f * bt / 0.1f : Mathf.Lerp(1.3f, 1f, Mathf.Clamp01((bt - 0.1f) / 0.1f)));
                }
            }
        }
        if (stamped && t > StampAt + 1f) enabled = false;
    }
}
