using UnityEngine;
using UnityEngine.UI;

// Corner HUD: collectible counter (inked cyan diamond + "1/3") and running score. Punches when a value changes.
public class GameplayHud : MonoBehaviour
{
    ComicUI.Label count, score;
    RectTransform countRoot, scoreRoot;
    int lastCollected = -1, lastPoints = -1;
    float countPunch, scorePunch;

    void Awake()
    {
        var canvas = ComicUI.MakeCanvas("GameplayHud", 40);
        canvas.transform.SetParent(transform, false);
        canvas.GetComponent<GraphicRaycaster>().enabled = false;

        countRoot = ComicUI.Rect("Collectibles", canvas.transform, new Vector2(1f, 1f), new Vector2(-150, -70), new Vector2(220, 80));
        var diamond = ComicUI.Rect("Diamond", countRoot, new Vector2(0f, 0.5f), new Vector2(34, 0), new Vector2(44, 44));
        diamond.localRotation = Quaternion.Euler(0, 0, 45);
        var dsh = ComicUI.Rect("DiamondShadow", countRoot, new Vector2(0f, 0.5f), new Vector2(39, -5), new Vector2(44, 44));
        dsh.localRotation = Quaternion.Euler(0, 0, 45);
        dsh.SetAsFirstSibling();
        ComicUI.Img(dsh, ComicUI.BoxSprite, ComicUI.Pink);
        ComicUI.Img(diamond, ComicUI.BoxSprite, ComicUI.Cyan);
        count = ComicUI.Text(countRoot, "0/0", 54, new Vector2(0f, 0.5f), new Vector2(150, 0), new Vector2(160, 70), Color.white, null, -1f, TMPro.TextAlignmentOptions.Left);

        scoreRoot = ComicUI.Rect("Score", canvas.transform, new Vector2(1f, 1f), new Vector2(-150, -140), new Vector2(260, 50));
        score = ComicUI.Text(scoreRoot, "0", 38, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(260, 50), ComicUI.Cyan, null, -1f, TMPro.TextAlignmentOptions.Right);
        scoreRoot.localRotation = Quaternion.Euler(0, 0, 2f);
    }

    void Update()
    {
        countRoot.gameObject.SetActive(LevelStats.CollectTotal > 0);
        if (LevelStats.Collected != lastCollected)
        {
            if (lastCollected >= 0) countPunch = 1f;
            lastCollected = LevelStats.Collected;
            count.text = LevelStats.Collected + "/" + LevelStats.CollectTotal;
        }
        if (LevelStats.Points != lastPoints)
        {
            if (lastPoints >= 0) scorePunch = 1f;
            lastPoints = LevelStats.Points;
            score.text = LevelStats.Points.ToString("N0") + " PTS";
        }
        float dt = Time.unscaledDeltaTime;
        countPunch = Mathf.MoveTowards(countPunch, 0f, dt * 4f);
        scorePunch = Mathf.MoveTowards(scorePunch, 0f, dt * 5f);
        countRoot.localScale = Vector3.one * (1f + 0.35f * countPunch * countPunch);
        scoreRoot.localScale = Vector3.one * (1f + 0.25f * scorePunch * scorePunch);
        // Tuck the HUD away once the level ends so the stats card owns the screen.
        bool show = !GameManager.Frozen;
        if (countRoot.parent.gameObject.activeSelf != show) countRoot.parent.gameObject.SetActive(show);
    }
}
