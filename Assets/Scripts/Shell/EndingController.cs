using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

// Ending: the ADIEU poster and an inmate release form filled in with this run's stats. The RELEASED stamp slams
// down, then CONTINUE opens the team credits, and any key from there returns to the title.
public class EndingController : MonoBehaviour
{
    public RectTransform poster;
    public RectTransform content;   // shaken by the stamp
    public RectTransform stamp;
    public RectTransform headlineAnchor;
    public TextMeshProUGUI[] rowValues = new TextMeshProUGUI[5];

    const float StampAt = 1.9f, StampDrop = 0.13f;
    float t;
    bool stamped;
    RectTransform headline, continueBtn;
    CanvasGroup stampGroup;

    void Start()
    {
        Time.timeScale = 1f;
        FillStats();
        if (headlineAnchor != null)
        {
            var l = ComicUI.Text(headlineAnchor, "YOU BROKE OUT!", 88, new Vector2(0.5f, 0.5f), Vector2.zero, headlineAnchor.sizeDelta,
                ComicUI.Cyan, new Color(0.878f, 0.271f, 0.482f), 9f);
            headline = l.root;
            headline.localScale = Vector3.zero;
        }
        if (stamp != null)
        {
            stampGroup = stamp.GetComponent<CanvasGroup>() ?? stamp.gameObject.AddComponent<CanvasGroup>();
            stampGroup.alpha = 0f;
        }
    }

    void FillStats()
    {
        // This session's clears first; otherwise saved per-level bests; otherwise a dash.
        var runs = LevelStats.Session.Values.ToList();
        string time = "—", damage = "—", contraband = "—", chain = "—", rating = "—";
        if (runs.Count > 0)
        {
            time = Clock(runs.Sum(r => r.seconds));
            damage = runs.Sum(r => r.points).ToString("N0") + " pts";
            contraband = $"{runs.Sum(r => r.collected)} / {runs.Sum(r => r.collectTotal)}";
            chain = "x" + Mathf.Max(1, runs.Max(r => r.bestChain));
            rating = Letter(runs.Average(r => (float)LevelStats.RankValue(r.rank)));
        }
        else
        {
            float secs = 0f; int coll = 0, tot = 0, best = 0, ranks = 0, rankSum = 0, timed = 0;
            for (int i = 1; i <= LevelStats.TotalLevels; i++)
            {
                string k = "gp.Level" + i;
                float s = PlayerPrefs.GetFloat(k + ".time", 0f);
                if (s > 0f) { secs += s; timed++; }
                coll += PlayerPrefs.GetInt(k + ".coll", 0);
                tot += PlayerPrefs.GetInt(k + ".collTotal", 0);
                best = Mathf.Max(best, PlayerPrefs.GetInt(k + ".chain", 0));
                if (PlayerPrefs.HasKey(k + ".rank")) { rankSum += PlayerPrefs.GetInt(k + ".rank"); ranks++; }
            }
            if (timed > 0) time = Clock(secs);
            if (tot > 0) contraband = $"{coll} / {tot}";
            if (best > 0) chain = "x" + best;
            if (ranks > 0) rating = Letter((float)rankSum / ranks);
        }
        string[] v = { time, damage, contraband, chain, rating };
        for (int i = 0; i < rowValues.Length && i < v.Length; i++)
            if (rowValues[i] != null) rowValues[i].text = v[i];
    }

    static string Clock(float s) { int m = Mathf.FloorToInt(s / 60f); return $"{m:00}:{Mathf.FloorToInt(s - m * 60f):00}"; }
    static string Letter(float v) => v >= 3.5f ? "S" : v >= 2.5f ? "A" : v >= 1.5f ? "B" : v >= 0.5f ? "C" : "D";

    void Update()
    {
        float dt = Time.unscaledDeltaTime;
        t += dt > 0.25f ? 1f / 30f : dt; // skip load hitches
        if (poster != null)
        {
            float s = Mathf.Lerp(1.04f, 1f, Mathf.SmoothStep(0, 1, Mathf.Clamp01(t / 2.5f)));
            poster.localScale = Vector3.one * (s + 0.006f * Mathf.Sin(t * 0.9f));
        }
        if (headline != null)
        {
            float h = t - 0.9f;
            headline.localScale = Vector3.one * (h <= 0 ? 0f : h < 0.14f ? 1.18f * h / 0.14f : Mathf.Lerp(1.18f, 1f, Mathf.Clamp01((h - 0.14f) / 0.12f)));
        }
        if (stamp != null)
        {
            float st = t - StampAt;
            if (st > -StampDrop)
            {
                float k = Mathf.Clamp01((st + StampDrop) / StampDrop);
                stamp.localScale = Vector3.one * Mathf.Lerp(3f, 1f, k * k);
                stampGroup.alpha = Mathf.Clamp01(k * 2.5f);
            }
            if (!stamped && st >= 0f)
            {
                stamped = true;
                ShellAudio.Play(ShellAudio.Sfx.Thud, 1f, 0.02f);
                ShellAudio.Play(ShellAudio.Sfx.Crack, 0.35f);
                ShowContinue();
            }
            if (content != null)
            {
                float shake = st >= 0f ? Mathf.Max(0f, 1f - st / 0.28f) * 12f : 0f;
                content.anchoredPosition = shake > 0 ? Random.insideUnitCircle * shake : Vector2.zero;
            }
        }
        if (continueBtn != null)
        {
            float c = t - StampAt - 0.5f;
            continueBtn.localScale = Vector3.one * (c <= 0 ? 0f : c < 0.14f ? 1.15f * c / 0.14f : Mathf.Lerp(1.15f, 1f, Mathf.Clamp01((c - 0.14f) / 0.12f)));
            var k = UnityEngine.InputSystem.Keyboard.current;
            if (c > 0.3f && !CreditsScreen.IsOpen && k != null && (k.spaceKey.wasPressedThisFrame || k.enterKey.wasPressedThisFrame))
                OpenCredits();
        }
    }

    void ShowContinue()
    {
        var b = ShellUI.ComicButton(transform, "CONTINUE", new Vector2(1f, 0f), new Vector2(-190, 90), new Vector2(300, 96), OpenCredits);
        continueBtn = (RectTransform)b.transform;
        continueBtn.localScale = Vector3.zero;
        if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(b.gameObject);
    }

    public void OpenCredits()
    {
        if (CreditsScreen.IsOpen) return;
        CreditsScreen.Show(ReturnToTitle);
    }

    public void ReturnToTitle()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(0);
    }
}
