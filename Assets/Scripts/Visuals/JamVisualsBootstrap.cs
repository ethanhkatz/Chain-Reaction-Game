using UnityEngine;
using UnityEngine.SceneManagement;

// Spawns the atmosphere (parallax, lights, particles, chain, juice) into any scene that has a Player.
// Runs at runtime only, so level scenes get the look without being edited.
public static class JamVisualsBootstrap
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Init()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
        Apply();
    }

    static void OnSceneLoaded(Scene scene, LoadSceneMode mode) => Apply();

    static void Apply()
    {
        if (JamAtmosphere.Instance != null) return;
        var player = Object.FindFirstObjectByType<PlayerController>();
        var cam = Camera.main;
        if (player == null || cam == null) return;
        var cfg = Resources.Load<JamVisualsConfig>("JamVisualsConfig");
        if (cfg == null) { Debug.LogWarning("JamVisualsConfig missing from Resources"); return; }

        GameObject ballGo = null;
        try { ballGo = GameObject.FindWithTag("Ball"); } catch (UnityException) { }

        var go = new GameObject("JamAtmosphere");
        go.AddComponent<JamAtmosphere>().Build(cfg, cam, player.transform, ballGo != null ? ballGo.transform : null);
    }
}
