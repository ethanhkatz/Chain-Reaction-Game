#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

// Runtime half of the headless playtest (see Assets/Editor/Jam/JamPlaytest.cs). Editor-only; never ships.
// Lives in the game loop because the editor update loop does not tick reliably in batchmode Play Mode.
public class JamPlaytestDriver : MonoBehaviour
{
    public string scene, input, outDir, expect = "any";
    public float seconds;
    public List<float> shots;

    float start = -1, nextLog;
    int errors;
    string outcome = "none";
    Keyboard keyboard;
    bool done;

    static readonly Dictionary<string, Key> Aliases = new Dictionary<string, Key>(StringComparer.OrdinalIgnoreCase)
    {
        { "right", Key.RightArrow }, { "left", Key.LeftArrow }, { "up", Key.UpArrow }, { "down", Key.DownArrow },
    };

    void Awake()
    {
        Application.logMessageReceived += OnLog;
    }

    void OnDestroy() => Application.logMessageReceived -= OnLog;

    void OnLog(string msg, string stack, LogType type)
    {
        if (type != LogType.Exception && type != LogType.Error) return;
        if (msg.StartsWith("JAM:") || (stack != null && stack.Contains("UnityEditor."))) return;
        errors++;
        Debug.Log("JAM: PLAY ERROR " + msg);
    }

    void Update()
    {
        if (done) return;
        if (start < 0) { start = Time.realtimeSinceStartup; Directory.CreateDirectory(outDir); }
        if (keyboard == null || !keyboard.added) { keyboard = InputSystem.AddDevice<Keyboard>(); keyboard.MakeCurrent(); }
        float t = Time.realtimeSinceStartup - start;

        FeedInput(t);

        if (t >= nextLog)
        {
            nextLog += 0.5f;
            var p = GameObject.Find("Player");
            var b = GameObject.FindWithTag("Ball");
            Debug.Log($"JAM: t={t:0.0} player={Fmt(p)} ball={Fmt(b)} timeScale={Time.timeScale}");
        }

        var over = GameObject.Find("GameOverPanel");
        var clear = GameObject.Find("LevelClearPanel");
        if (outcome == "none" && clear != null && clear.activeInHierarchy) { outcome = "clear"; Debug.Log($"JAM: t={t:0.0} LEVEL CLEAR"); }
        if (outcome == "none" && over != null && over.activeInHierarchy) { outcome = "gameover"; Debug.Log($"JAM: t={t:0.0} GAME OVER"); }

        while (shots.Count > 0 && t >= shots[0])
        {
            Capture(Path.Combine(outDir, $"{Path.GetFileNameWithoutExtension(scene)}_t{shots[0]:0.0}.png"));
            shots.RemoveAt(0);
        }

        if (t >= seconds)
        {
            done = true;
            InputSystem.QueueStateEvent(keyboard, new KeyboardState());
            bool ok = errors == 0 && (expect == "any" || expect == outcome);
            Debug.Log($"JAM: OUTCOME {outcome} (expected {expect}) errors={errors}");
            Debug.Log(ok ? "JAM: RESULT OK" : "JAM: RESULT FAIL");
            UnityEditor.SessionState.EraseString("JamPlaytest.Config");
            UnityEditor.EditorApplication.Exit(ok ? 0 : 1);
        }
    }

    static string Fmt(GameObject g) => g == null ? "none" : $"({g.transform.position.x:0.00},{g.transform.position.y:0.00})";

    static Key ParseKey(string k) => Aliases.TryGetValue(k, out var key) ? key : (Key)Enum.Parse(typeof(Key), k, true);

    void FeedInput(float t)
    {
        var held = new List<Key>();
        foreach (var seg in (input ?? "").Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries))
        {
            var parts = seg.Split(':');
            var range = parts[0].Split('-');
            float a = float.Parse(range[0], CultureInfo.InvariantCulture), b = float.Parse(range[1], CultureInfo.InvariantCulture);
            if (t >= a && t < b) held.AddRange(parts[1].Split('+').Select(ParseKey));
        }
        InputSystem.QueueStateEvent(keyboard, new KeyboardState(held.ToArray()));
    }

    static void Capture(string file)
    {
        var cam = Camera.main;
        if (cam == null) return;
        var rt = RenderTexture.GetTemporary(1600, 900, 24, RenderTextureFormat.ARGB32);
        var prev = cam.targetTexture;
        cam.targetTexture = rt;
        cam.Render();
        cam.targetTexture = prev;
        var active = RenderTexture.active;
        RenderTexture.active = rt;
        var tex = new Texture2D(1600, 900, TextureFormat.RGB24, false);
        tex.ReadPixels(new Rect(0, 0, 1600, 900), 0, 0);
        tex.Apply();
        RenderTexture.active = active;
        RenderTexture.ReleaseTemporary(rt);
        File.WriteAllBytes(file, tex.EncodeToPNG());
        Destroy(tex);
        Debug.Log("JAM: captured " + Path.GetFullPath(file));
    }
}
#endif
