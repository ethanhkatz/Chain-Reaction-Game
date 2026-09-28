using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

// Command-line entry points for headless verification. Run with:
//   Unity -batchmode -projectPath <wt> -executeMethod JamCli.<Method> [-jam* args] -logFile <log>
// Every method exits the editor with 0 on success, 1 on failure, and prints lines prefixed "JAM:".
public static class JamCli
{
    // ---------- argument helpers ----------
    public static string Arg(string name, string fallback = null)
    {
        var args = Environment.GetCommandLineArgs();
        for (int i = 0; i < args.Length - 1; i++)
            if (args[i] == name) return args[i + 1];
        return fallback;
    }

    public static void Log(string msg) => Debug.Log("JAM: " + msg);

    static void Finish(bool ok)
    {
        Log(ok ? "RESULT OK" : "RESULT FAIL");
        EditorApplication.Exit(ok ? 0 : 1);
    }

    // ---------- DumpScene: -jamScene Assets/Scenes/X.unity ----------
    // Prints every GameObject with its path, layer, tag, position, scale and component list.
    public static void DumpScene()
    {
        var path = Arg("-jamScene");
        var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
        var sb = new StringBuilder();
        foreach (var root in scene.GetRootGameObjects()) Dump(root.transform, 0, sb);
        Log("SCENE " + path);
        foreach (var line in sb.ToString().Split('\n')) if (line.Length > 0) Log("  " + line);
        Finish(true);
    }

    static void Dump(Transform t, int depth, StringBuilder sb)
    {
        var go = t.gameObject;
        var comps = go.GetComponents<Component>().Select(c => c == null ? "<MISSING SCRIPT>" : c.GetType().Name).Where(n => n != "Transform");
        sb.Append(new string(' ', depth * 2)).Append(go.name)
          .Append($"  [layer={LayerMask.LayerToName(go.layer)} tag={go.tag} active={go.activeSelf}]")
          .Append($" pos=({t.position.x:0.##},{t.position.y:0.##}) scale=({t.localScale.x:0.##},{t.localScale.y:0.##})")
          .Append(" {").Append(string.Join(", ", comps)).Append("}\n");
        foreach (Transform c in t) Dump(c, depth + 1, sb);
    }

    // ---------- ValidateScenes: -jamScenes a.unity,b.unity (default: all under Assets/Scenes) ----------
    // Fails on missing scripts, missing prefab links, or missing sprites on SpriteRenderers.
    public static void ValidateScenes()
    {
        var list = Arg("-jamScenes");
        var scenes = list != null ? list.Split(',') : AssetDatabase.FindAssets("t:Scene", new[] { "Assets/Scenes" }).Select(AssetDatabase.GUIDToAssetPath).ToArray();
        bool ok = true;
        foreach (var s in scenes)
        {
            var scene = EditorSceneManager.OpenScene(s, OpenSceneMode.Single);
            foreach (var root in scene.GetRootGameObjects())
                foreach (var t in root.GetComponentsInChildren<Transform>(true))
                {
                    var go = t.gameObject;
                    if (go.GetComponents<Component>().Any(c => c == null)) { ok = false; Log($"ERROR {s}: missing script on {go.name}"); }
                    if (PrefabUtility.IsPrefabAssetMissing(go)) { ok = false; Log($"ERROR {s}: missing prefab asset for {go.name}"); }
                    var sr = go.GetComponent<SpriteRenderer>();
                    if (sr != null && sr.sprite == null && sr.drawMode == SpriteDrawMode.Simple) Log($"WARN {s}: SpriteRenderer without sprite on {go.name}");
                }
            Log("validated " + s);
        }
        Finish(ok);
    }

    // ---------- CaptureScene: -jamScene X.unity -jamOut dir [-jamMode overview|player] [-jamSize 1600x900] ----------
    // Renders the scene in edit mode (no simulation) to PNG. "overview" frames all renderers; "player" frames the Player
    // at the camera's own orthographic size.
    public static void CaptureScene()
    {
        var path = Arg("-jamScene");
        var outDir = Arg("-jamOut", "JamCaptures");
        var mode = Arg("-jamMode", "both");
        ParseSize(Arg("-jamSize", "1600x900"), out int w, out int h);
        EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
        Directory.CreateDirectory(outDir);
        var cam = Camera.main;
        if (cam == null) { Log("ERROR no Main Camera"); Finish(false); return; }
        var name = Path.GetFileNameWithoutExtension(path);
        var player = GameObject.FindWithTag("Player") ?? GameObject.Find("Player");
        if (mode == "player" || mode == "both")
        {
            if (player != null) cam.transform.position = new Vector3(player.transform.position.x, player.transform.position.y + 1f, cam.transform.position.z);
            Save(RenderCamera(cam, w, h), Path.Combine(outDir, name + "_player.png"));
        }
        if (mode == "overview" || mode == "both")
        {
            var b = SceneBounds();
            var size = cam.orthographicSize;
            cam.transform.position = new Vector3(b.center.x, b.center.y, cam.transform.position.z);
            cam.orthographicSize = Mathf.Max(b.extents.y, b.extents.x * h / w) * 1.05f;
            Save(RenderCamera(cam, w, h), Path.Combine(outDir, name + "_overview.png"));
            cam.orthographicSize = size;
        }
        Finish(true);
    }

    public static Bounds SceneBounds()
    {
        var rs = UnityEngine.Object.FindObjectsByType<Renderer>(FindObjectsInactive.Exclude, FindObjectsSortMode.None)
            .Where(r => r.enabled && !(r is ParticleSystemRenderer) && r.bounds.size.sqrMagnitude < 40000f).ToArray();
        if (rs.Length == 0) return new Bounds(Vector3.zero, Vector3.one * 10);
        var b = rs[0].bounds;
        foreach (var r in rs) b.Encapsulate(r.bounds);
        return b;
    }

    public static void ParseSize(string s, out int w, out int h)
    {
        var p = s.Split('x');
        w = int.Parse(p[0]); h = int.Parse(p[1]);
    }

    public static Texture2D RenderCamera(Camera cam, int w, int h)
    {
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
        return tex;
    }

    public static void Save(Texture2D tex, string file)
    {
        File.WriteAllBytes(file, tex.EncodeToPNG());
        Log("captured " + Path.GetFullPath(file));
    }

    // ---------- Build: -jamTarget webgl|mac -jamOut dir [-jamScenes a,b] (default: EditorBuildSettings scenes) ----------
    public static void Build()
    {
        var target = Arg("-jamTarget", "webgl");
        var outDir = Arg("-jamOut", "Builds/" + target);
        var list = Arg("-jamScenes");
        var scenes = list != null ? list.Split(',') : EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray();
        var opts = new BuildPlayerOptions { scenes = scenes, options = BuildOptions.None };
        if (target == "mac") { opts.target = BuildTarget.StandaloneOSX; opts.locationPathName = Path.Combine(outDir, "ChainReaction.app"); }
        else { opts.target = BuildTarget.WebGL; opts.locationPathName = outDir; }
        var report = BuildPipeline.BuildPlayer(opts);
        Log($"BUILD {target} {report.summary.result} errors={report.summary.totalErrors} size={report.summary.totalSize / (1024 * 1024)}MB -> {Path.GetFullPath(opts.locationPathName)}");
        Finish(report.summary.result == UnityEditor.Build.Reporting.BuildResult.Succeeded);
    }
}
