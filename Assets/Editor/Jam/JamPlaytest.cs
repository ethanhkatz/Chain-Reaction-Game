using System;
using System.Globalization;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;

// Headless playtest: opens a scene, enters Play Mode, feeds a scripted keyboard timeline, captures frames,
// and reports the outcome. Usually run through Tools/jam.sh play. Direct form:
//   Unity -batchmode -projectPath <wt> -executeMethod JamPlaytest.Run -logFile <log>
//     -jamScene Assets/Scenes/Level1.unity
//     -jamInput "0-2:right;1.5-1.6:space;2-4:left"   (seconds from start, hold keys: right,left,up,down,space or any Key name; '+' joins keys)
//     -jamShots "0.5,2,4"                              (capture times in seconds)
//     -jamSeconds 6                                    (total run time)
//     -jamExpect clear|gameover|any                    (default any)
//     -jamOut JamCaptures/level1
// Output: "JAM: t=.. player=(x,y) ball=(x,y)" every 0.5s, "JAM: OUTCOME ..", "JAM: RESULT OK|FAIL".
// The per-frame work happens in JamPlaytestDriver, because the editor update loop does not tick in batchmode Play Mode.
[InitializeOnLoad]
public static class JamPlaytest
{
    const string Key = "JamPlaytest.Config";

    [Serializable]
    class Config
    {
        public string scene, input, shots, expect, outDir;
        public float seconds;
        public string outcome;
        public int errors = -1;
    }

    static JamPlaytest()
    {
        if (string.IsNullOrEmpty(SessionState.GetString(Key, ""))) return;
        EditorApplication.playModeStateChanged += OnPlayModeChanged;
    }

    public static void Run()
    {
        var c = new Config
        {
            scene = JamCli.Arg("-jamScene"),
            input = JamCli.Arg("-jamInput", ""),
            shots = JamCli.Arg("-jamShots", "1,3"),
            expect = JamCli.Arg("-jamExpect", "any"),
            outDir = JamCli.Arg("-jamOut", "JamCaptures"),
            seconds = float.Parse(JamCli.Arg("-jamSeconds", "5"), CultureInfo.InvariantCulture),
        };
        SessionState.SetString(Key, JsonUtility.ToJson(c));
        EditorSceneManager.OpenScene(c.scene, OpenSceneMode.Single);
        EditorApplication.playModeStateChanged -= OnPlayModeChanged;
        EditorApplication.playModeStateChanged += OnPlayModeChanged;
        EditorApplication.EnterPlaymode();
    }

    static Config Load() => JsonUtility.FromJson<Config>(SessionState.GetString(Key, "{}"));

    static void OnPlayModeChanged(PlayModeStateChange s)
    {
        var c = Load();
        if (c.scene == null) return;
        if (s == PlayModeStateChange.EnteredPlayMode)
        {
            var settings = ScriptableObject.Instantiate(InputSystem.settings);
            settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
            settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            InputSystem.settings = settings;

            var go = new GameObject("__JamPlaytestDriver");
            go.hideFlags = HideFlags.HideAndDontSave;
            UnityEngine.Object.DontDestroyOnLoad(go);
            var d = go.AddComponent<JamPlaytestDriver>();
            d.scene = c.scene;
            d.input = c.input;
            d.outDir = c.outDir;
            d.seconds = c.seconds;
            d.shots = c.shots.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries).Select(x => float.Parse(x, CultureInfo.InvariantCulture)).OrderBy(x => x).ToList();
            d.expect = c.expect;
        }
    }
}
