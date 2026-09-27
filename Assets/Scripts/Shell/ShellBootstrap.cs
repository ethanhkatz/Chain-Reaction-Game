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

    static void OnGameOver() => ShellAudio.Play(ShellAudio.Sfx.GameOver, 0.7f, 0f);
    static void OnLevelClear() => ShellAudio.Play(ShellAudio.Sfx.Clear, 0.7f, 0f);

    static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (mode != LoadSceneMode.Single) return;
        ShellAudio.Instance?.RefreshListener();
        HighContrast.ResetCache();

        foreach (var b in Object.FindObjectsByType<Button>(FindObjectsInactive.Include))
            if (b.GetComponent<ButtonFx>() == null) b.gameObject.AddComponent<ButtonFx>();

        var gm = Object.FindAnyObjectByType<GameManager>();
        bool isLevel = gm != null;
        ShellAudio.SetDrone(true);
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
