using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Wires the game shell into every scene without touching the level scenes themselves:
// audio, button feedback, pause/accessibility menu, title cards, idle pose, alert "!", hazard watch.
public static class ShellBootstrap
{
    static GameObject persistent;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Init()
    {
        persistent = new GameObject("[Shell]");
        Object.DontDestroyOnLoad(persistent);
        persistent.AddComponent<ShellAudio>();
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
        GameManager.GameOverRaised -= OnGameOver;
        GameManager.GameOverRaised += OnGameOver;
        GameManager.LevelClearRaised -= OnLevelClear;
        GameManager.LevelClearRaised += OnLevelClear;
    }

    // In the editor the first scene can already be loaded before sceneLoaded is hooked; catch it here.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void AfterFirstScene()
    {
        var s = SceneManager.GetActiveScene();
        if (s != lastHandled) OnSceneLoaded(s, LoadSceneMode.Single);
    }

    static Scene lastHandled;

    static void OnGameOver() => ShellAudio.PlayGameOver();
    static void OnLevelClear() => ShellAudio.PlayLevelClear();

    static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (mode != LoadSceneMode.Single) return;
        if (scene == lastHandled) return;
        lastHandled = scene;
        ShellAudio.Instance?.RefreshListener();
        HighContrast.ResetCache();

        foreach (var b in Object.FindObjectsByType<Button>(FindObjectsInactive.Include))
            if (b.GetComponent<ButtonFx>() == null) b.gameObject.AddComponent<ButtonFx>();

        var gm = Object.FindAnyObjectByType<GameManager>();
        bool isLevel = gm != null;
        bool finale = scene.name == "EndingScene" || Regex.IsMatch(scene.name, @"^Level\s*5$", RegexOptions.IgnoreCase);
        ShellAudio.SetFootsteps(false);
        ShellAudio.SetMusic(finale ? ShellAudio.Music.Ending : isLevel ? ShellAudio.Music.Level : ShellAudio.Music.Menu);
        if (!isLevel) return;

        Time.timeScale = ShellSettings.PlayTimeScale;

        var player = GameObject.FindWithTag("Player");
        var pc = Object.FindAnyObjectByType<PlayerController>();
        if (pc != null) player = pc.gameObject;
        if (player != null && player.GetComponent<PlayerShellFx>() == null) player.AddComponent<PlayerShellFx>();

        var ball = GameObject.FindWithTag("Ball");
        if (ball == null) ball = GameObject.Find("Ball");
        if (ball != null && ball.GetComponent<BallShellFx>() == null) ball.AddComponent<BallShellFx>();

        var sceneRoot = new GameObject("[Shell Level]");
        sceneRoot.AddComponent<PauseMenu>();
        sceneRoot.AddComponent<HazardWatcher>();

        var m = Regex.Match(scene.name, @"^Level\s*(\d+)$", RegexOptions.IgnoreCase);
        if (m.Success) TitleCard.Show(sceneRoot.transform, int.Parse(m.Groups[1].Value));

        if (ShellSettings.HighContrast) HighContrast.Apply(true);
    }
}
