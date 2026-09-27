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
    public int frameFps; // >0: record every frame as JPG at a fixed game-time step (trailer footage)
    public List<float> shots;

    float start = -1, nextLog;
    int errors;
    string outcome = "none";
    Keyboard keyboard;
    bool done;
    int frameIndex;

    // When recording, the clock is the frame count: with Time.captureFramerate set, the game advances exactly 1/fps per
    // rendered frame, so footage is smooth no matter how slowly the headless editor renders.
    float Now() => frameFps > 0 ? (float)frameIndex / frameFps : Time.realtimeSinceStartup;

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
        if (start < 0)
        {
            if (frameFps > 0) { Time.captureFramerate = frameFps; Directory.CreateDirectory(Path.Combine(outDir, "frames")); }
            start = Now();
            Directory.CreateDirectory(outDir);
        }
        if (keyboard == null || !keyboard.added) { keyboard = InputSystem.AddDevice<Keyboard>(); keyboard.MakeCurrent(); }
        float t = Now() - start;
        if (frameFps > 0) Capture(Path.Combine(outDir, "frames", $"f{frameIndex++:00000}.jpg"), 1920, 1080, true);

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
            Capture(Path.Combine(outDir, $"{Path.GetFileNameWithoutExtension(scene)}_t{shots[0]:0.0}.png"), 1600, 900, false);
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

    static void Capture(string file, int w, int h, bool jpg)
    {
        var cam = Camera.main;
        if (cam == null) return;
        var rt = RenderTexture.GetTemporary(w, h, 24, RenderTextureFormat.ARGB32);
        var prev = cam.targetTexture;
        cam.targetTexture = rt;
        cam.Render();
        cam.targetTexture = prev;
        var active = RenderTexture.active;
        RenderTexture.active = rt;
        var tex = new Texture2D(w, h, TextureFormat.RGB24, false);
        tex.ReadPixels(new Rect(0, 0, w, h), 0, 0);
        tex.Apply();
        RenderTexture.active = active;
        RenderTexture.ReleaseTemporary(rt);
        File.WriteAllBytes(file, jpg ? tex.EncodeToJPG(90) : tex.EncodeToPNG());
        Destroy(tex);
        if (!jpg) Debug.Log("JAM: captured " + Path.GetFullPath(file));
    }
}
#endif
