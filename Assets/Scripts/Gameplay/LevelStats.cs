using System;
using System.Collections.Generic;
using UnityEngine;

// Score state for the current level attempt plus results for this play session.
// Deaths survive restarts of the same level (statics outlive scene loads); everything else resets per attempt.
public static class LevelStats
{
    public const int BasePoints = 100;
    public const int CollectiblePoints = 500;
    public const int CollectiblesPerLevel = 3;
    public const int TotalLevels = 5;
    // Par time per level index (seconds) for the rank; index 0 is the fallback.
    public static float[] ParSeconds = { 120f, 60f, 90f, 90f, 90f, 120f };

    public static string Level { get; private set; } = "";
    public static int Deaths { get; private set; }
    public static float Seconds;
    public static int Points { get; private set; }
    public static int BestChain { get; private set; }
    public static int Collected { get; private set; }
    public static int CollectTotal;

    public static event Action Changed;

    public struct Result
    {
        public string level;
        public float seconds;
        public int deaths, bestChain, collected, collectTotal, points;
        public string rank;
        public bool newBest;
    }

    // Results of levels cleared this session (latest clear per level), for the ending summary.
    public static readonly Dictionary<string, Result> Session = new Dictionary<string, Result>();
    public static Result? LastResult;

    public static void BeginAttempt(string level)
    {
        if (level != Level) { Level = level; Deaths = 0; }
        Seconds = 0f; Points = 0; BestChain = 0; Collected = 0; CollectTotal = 0;
        LastResult = null;
        Changed?.Invoke();
    }

    public static void AddDeath() { Deaths++; Changed?.Invoke(); }

    public static void AddChain(int chain, int points)
    {
        Points += points;
        if (chain > BestChain) BestChain = chain;
        Changed?.Invoke();
    }

    public static void AddCollectible()
    {
        Collected++;
        Points += CollectiblePoints;
        Changed?.Invoke();
    }

    public static int LevelNumber(string sceneName)
    {
        var m = System.Text.RegularExpressions.Regex.Match(sceneName ?? "", @"(\d+)$");
        return m.Success ? int.Parse(m.Groups[1].Value) : 0;
    }

    // One star each for: every collectible, no deaths, a 3+ chain, beating par. 4 = S, 3 = A, 2 = B, else C.
    public static string Rank(float seconds, int deaths, int bestChain, int collected, int total, int levelNumber)
    {
        float par = levelNumber > 0 && levelNumber < ParSeconds.Length ? ParSeconds[levelNumber] : ParSeconds[0];
        int stars = 0;
        if (collected >= total) stars++;
        if (deaths == 0) stars++;
        if (bestChain >= 3) stars++;
        if (seconds <= par) stars++;
        return stars >= 4 ? "S" : stars == 3 ? "A" : stars == 2 ? "B" : "C";
    }

    public static int RankValue(string r) => r == "S" ? 4 : r == "A" ? 3 : r == "B" ? 2 : r == "C" ? 1 : 0;

    // Called on Level Clear: scores the attempt, stores session + PlayerPrefs bests.
    public static Result Finish()
    {
        var r = new Result
        {
            level = Level, seconds = Seconds, deaths = Deaths, bestChain = BestChain,
            collected = Collected, collectTotal = CollectTotal,
            points = Points + Mathf.Max(0, Mathf.RoundToInt(3000f - Seconds * 20f)) - Deaths * 250,
        };
        r.points = Mathf.Max(0, r.points);
        r.rank = Rank(r.seconds, r.deaths, r.bestChain, r.collected, r.collectTotal, LevelNumber(Level));

        string k = "gp." + Level;
        int prevRank = PlayerPrefs.GetInt(k + ".rank", 0);
        r.newBest = RankValue(r.rank) > prevRank;
        if (r.newBest) PlayerPrefs.SetInt(k + ".rank", RankValue(r.rank));
        PlayerPrefs.SetInt(k + ".coll", Mathf.Max(PlayerPrefs.GetInt(k + ".coll", 0), r.collected));
        PlayerPrefs.SetInt(k + ".collTotal", r.collectTotal);
        PlayerPrefs.SetInt(k + ".chain", Mathf.Max(PlayerPrefs.GetInt(k + ".chain", 0), r.bestChain));
        float prevTime = PlayerPrefs.GetFloat(k + ".time", 0f);
        if (prevTime <= 0f || r.seconds < prevTime) PlayerPrefs.SetFloat(k + ".time", r.seconds);
        PlayerPrefs.Save();

        Session[Level] = r;
        LastResult = r;
        Deaths = 0; // a clear closes out this level's death count
        return r;
    }

    public static string BestRank(string level)
    {
        int v = PlayerPrefs.GetInt("gp." + level + ".rank", 0);
        return v == 4 ? "S" : v == 3 ? "A" : v == 2 ? "B" : v == 1 ? "C" : "-";
    }

    public static string FormatTime(float s)
    {
        int m = Mathf.FloorToInt(s / 60f);
        float sec = s - m * 60f;
        return $"{m}:{sec:00.0}";
    }
}
