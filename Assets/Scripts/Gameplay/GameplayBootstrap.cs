using UnityEngine;
using UnityEngine.SceneManagement;

// Wires the score layer (combo meter, collectibles, HUD, stats card, ending summary) into scenes at runtime,
// the same way the shell and visuals bootstraps do, so level scenes don't need editing.
public static class GameplayBootstrap
{
    static GameObject persistent;
    static Scene lastHandled;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Init()
    {
        persistent = new GameObject("[Gameplay]");
        Object.DontDestroyOnLoad(persistent);
        persistent.AddComponent<GameplayAudio>();
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
        GameManager.GameOverRaised -= OnGameOver;
        GameManager.GameOverRaised += OnGameOver;
        GameManager.LevelClearRaised -= OnLevelClear;
        GameManager.LevelClearRaised += OnLevelClear;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void AfterFirstScene()
    {
        var s = SceneManager.GetActiveScene();
        if (s != lastHandled) OnSceneLoaded(s, LoadSceneMode.Single);
    }

    static GameObject levelRoot;

    static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (mode != LoadSceneMode.Single || scene == lastHandled) return;
        lastHandled = scene;

        if (scene.name == "EndingScene")
        {
            new GameObject("[Gameplay Ending]").AddComponent<EndingSummary>();
            return;
        }
        if (Object.FindAnyObjectByType<GameManager>() == null) return;

        LevelStats.BeginAttempt(scene.name);
        levelRoot = new GameObject("[Gameplay Level]");
        levelRoot.AddComponent<ComboMeter>();
        levelRoot.AddComponent<GameplayHud>();

        int total = 0;
        foreach (var col in Object.FindObjectsByType<Collider2D>(FindObjectsSortMode.None))
        {
            if (!col.isTrigger || !col.gameObject.name.StartsWith("Collectible_")) continue;
            if (col.GetComponent<Collectible>() != null) continue;
            col.gameObject.AddComponent<Collectible>();
            total++;
        }
        LevelStats.CollectTotal = total;
    }

    static void OnGameOver() => LevelStats.AddDeath();

    static void OnLevelClear()
    {
        var r = LevelStats.Finish();
        if (levelRoot != null) StatsCard.Show(levelRoot.transform, r);
    }
}
