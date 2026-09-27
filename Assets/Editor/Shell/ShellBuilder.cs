using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Builds the game shell: Resources/ShellArt.asset, the EndingScene (from scratch) and MainMenu polish wiring.
// Run: Tools/jam.sh run ShellBuilder.Build
public static class ShellBuilder
{
    const string Cat = "Assets/Art/Catalog/";
    static readonly Color Charcoal = new Color(0.08f, 0.085f, 0.1f);

    public static void Build()
    {
        try
        {
            BuildArt();
            BuildEnding();
            PolishMainMenu();
            Debug.Log("JAM: shell built");
            EditorApplication.Exit(0);
        }
        catch (System.Exception e)
        {
            Debug.LogError("JAM: shell build failed " + e);
            EditorApplication.Exit(1);
        }
    }

    static Sprite S(string name)
    {
        var s = AssetDatabase.LoadAllAssetsAtPath(Cat + name + ".png").OfType<Sprite>().FirstOrDefault();
        if (s == null) throw new System.Exception("missing sprite " + name);
        return s;
    }

    static void BuildArt()
    {
        const string path = "Assets/Resources/ShellArt.asset";
        var art = AssetDatabase.LoadAssetAtPath<ShellArt>(path);
        if (art == null) { art = ScriptableObject.CreateInstance<ShellArt>(); AssetDatabase.CreateAsset(art, path); }
        art.idleFront1 = S("player_idle_front_1");
        art.idleFront2 = S("player_idle_front_2");
        art.buttonReturnTitle = S("ui_btn_returntitle_cyan");
        art.footsteps = Clip("sfx_footsteps_run.wav", false);
        art.jump = Clip("sfx_jump.wav", false);
        art.deathExplosion = Clip("sfx_death_explosion.wav", false);
        art.gameOverSting = Clip("sting_gameover_yarukizero.mp3", false);
        art.musicMenu = Clip("music_escape.mp3", true);
        art.musicLevel = Clip("music_vivid_anti_ray.mp3", true);
        art.musicEnding = Clip("music_engram.mp3", true);
        EditorUtility.SetDirty(art);
        AssetDatabase.SaveAssets();
    }

    // Music streams (no big decompress hitch on load); short SFX stay decompressed for instant playback.
    static AudioClip Clip(string file, bool isMusic)
    {
        string path = "Assets/Audio/" + file;
        var imp = AssetImporter.GetAtPath(path) as AudioImporter;
        if (imp == null) { Debug.LogWarning("JAM: missing audio " + path); return null; }
        var st = imp.defaultSampleSettings;
        st.loadType = isMusic ? AudioClipLoadType.Streaming : AudioClipLoadType.DecompressOnLoad;
        st.compressionFormat = AudioCompressionFormat.Vorbis;
        st.quality = isMusic ? 0.6f : 0.8f;
        imp.defaultSampleSettings = st;
        imp.forceToMono = !isMusic;
        imp.loadInBackground = isMusic;
        imp.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<AudioClip>(path);
    }

    static Camera MakeCamera()
    {
        var go = new GameObject("Main Camera");
        go.tag = "MainCamera";
        var cam = go.AddComponent<Camera>();
        cam.orthographic = true;
        cam.orthographicSize = 5f;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = Charcoal;
        go.transform.position = new Vector3(0, 0, -10);
        go.AddComponent<AudioListener>();
        return cam;
    }

    static RectTransform Rect(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.layer = 5;
        var rt = (RectTransform)go.transform;
        rt.SetParent(parent, false);
        return rt;
    }

    static void BuildEnding()
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        var cam = MakeCamera();

        var canvasGo = new GameObject("Canvas", typeof(RectTransform));
        canvasGo.layer = 5;
        var canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceCamera;
        canvas.worldCamera = cam;
        canvas.planeDistance = 5f;
        var scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;
        canvasGo.AddComponent<GraphicRaycaster>();
        canvasGo.AddComponent<CanvasGroup>();
        var fade = canvasGo.AddComponent<UIFadeIn>();
        fade.duration = 1.8f;
        var ctrl = canvasGo.AddComponent<EndingController>();

        var bg = Rect("Background", canvasGo.transform);
        bg.anchorMin = Vector2.zero; bg.anchorMax = Vector2.one; bg.offsetMin = bg.offsetMax = Vector2.zero;
        bg.gameObject.AddComponent<Image>().color = Charcoal;

        var poster = Rect("Poster", canvasGo.transform);
        poster.anchorMin = poster.anchorMax = new Vector2(0.5f, 0.5f);
        poster.anchoredPosition = new Vector2(-400, 0);
        poster.sizeDelta = new Vector2(900, 852);
        var pimg = poster.gameObject.AddComponent<Image>();
        pimg.sprite = S("story_ending_adieu_poster");
        pimg.preserveAspect = true;
        pimg.raycastTarget = false;
        ctrl.poster = poster;

        var textGroup = Rect("Story", canvasGo.transform);
        textGroup.anchorMin = textGroup.anchorMax = new Vector2(0.5f, 0.5f);
        textGroup.anchoredPosition = new Vector2(500, 150);
        textGroup.sizeDelta = new Vector2(760, 420);
        var tg = textGroup.gameObject.AddComponent<CanvasGroup>();
        var tf = textGroup.gameObject.AddComponent<UIFadeIn>();
        tf.delay = 1.4f; tf.duration = 1.4f;

        var head = Text(textGroup, "You broke out.", 84, new Vector2(0, 110), new Vector2(760, 110), new Color(0.36f, 0.95f, 0.84f));
        head.fontStyle = FontStyles.Bold;
        Text(textGroup, "The kingdom will have to find another soldier.\nYou kept the ball. It seems to like you.", 36,
            new Vector2(0, -40), new Vector2(760, 160), new Color(0.92f, 0.92f, 0.94f));

        var btnRt = Rect("ReturnToTitleButton", canvasGo.transform);
        btnRt.anchorMin = btnRt.anchorMax = new Vector2(0.5f, 0.5f);
        btnRt.anchoredPosition = new Vector2(500, -120);
        btnRt.sizeDelta = new Vector2(520, 164);
        var bimg = btnRt.gameObject.AddComponent<Image>();
        bimg.sprite = S("ui_btn_returntitle_cyan");
        bimg.preserveAspect = true;
        var btn = btnRt.gameObject.AddComponent<Button>();
        btn.targetGraphic = bimg;
        UnityEventTools.AddPersistentListener(btn.onClick, ctrl.ReturnToTitle);
        btnRt.gameObject.AddComponent<CanvasGroup>();
        var bf = btnRt.gameObject.AddComponent<UIFadeIn>();
        bf.delay = 2.6f; bf.duration = 1f;

        var credits = "Assets/Audio/CREDITS.txt";
        if (System.IO.File.Exists(credits))
        {
            var cr = Text(canvasGo.transform, System.IO.File.ReadAllText(credits).Trim().Replace("\u3084\u308b\u6c17\u30bc\u30ed", "Yaruki Zero"), 15, new Vector2(500, -390), new Vector2(820, 200), new Color(1f, 1f, 1f, 0.45f));
            cr.name = "Credits";
            cr.alignment = TextAlignmentOptions.Bottom;
            cr.textWrappingMode = TextWrappingModes.Normal;
        }

        var es = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
        es.GetComponent<EventSystem>().firstSelectedGameObject = btnRt.gameObject;

        EditorSceneManager.SaveScene(scene, "Assets/Scenes/EndingScene.unity");
    }

    static TextMeshProUGUI Text(Transform parent, string s, float size, Vector2 pos, Vector2 box, Color c)
    {
        var rt = Rect("Text", parent);
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = box;
        var t = rt.gameObject.AddComponent<TextMeshProUGUI>();
        t.text = s;
        t.fontSize = size;
        t.color = c;
        t.alignment = TextAlignmentOptions.Center;
        t.raycastTarget = false;
        return t;
    }

    static void PolishMainMenu()
    {
        var scene = EditorSceneManager.OpenScene("Assets/Scenes/MainMenu.unity", OpenSceneMode.Single);
        var cam = Object.FindAnyObjectByType<Camera>();
        if (cam == null) cam = MakeCamera();

        var canvas = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<Canvas>(true)).First();
        canvas.renderMode = RenderMode.ScreenSpaceCamera;
        canvas.worldCamera = cam;
        canvas.planeDistance = 5f;
        var group = canvas.GetComponent<CanvasGroup>();
        if (group == null) group = canvas.gameObject.AddComponent<CanvasGroup>();

        var menu = Object.FindAnyObjectByType<MainMenuController>();
        var logo = GameObject.Find("BreakOut");
        var so = new SerializedObject(menu);
        so.FindProperty("logo").objectReferenceValue = logo != null ? logo.transform : null;
        so.FindProperty("fadeGroup").objectReferenceValue = group;
        so.ApplyModifiedPropertiesWithoutUndo();

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
    }
}
