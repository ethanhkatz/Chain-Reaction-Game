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

    // Generated art in Assets/Art/Gen: make sure it imports as a single sprite before loading it.
    static Sprite Gen(string name)
    {
        string path = "Assets/Art/Gen/" + name + ".png";
        var imp = AssetImporter.GetAtPath(path) as TextureImporter;
        if (imp == null) throw new System.Exception("missing art " + path);
        if (imp.textureType != TextureImporterType.Sprite || imp.textureShape != TextureImporterShape.Texture2D || imp.spriteImportMode != SpriteImportMode.Single || !imp.alphaIsTransparency)
        {
            imp.textureType = TextureImporterType.Sprite;
            imp.textureShape = TextureImporterShape.Texture2D;
            imp.spriteImportMode = SpriteImportMode.Single;
            imp.alphaIsTransparency = true;
            imp.mipmapEnabled = false;
            imp.SaveAndReimport();
        }
        var s = AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().FirstOrDefault();
        if (s == null)
        {
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport);
            s = AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().FirstOrDefault();
        }
        if (s == null) throw new System.Exception("missing sprite " + path + " objs=" + string.Join(",", AssetDatabase.LoadAllAssetsAtPath(path).Select(o => o.GetType().Name)));
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
        art.sign = Gen("ui_sign_blank");
        art.chainLink1 = S("obj_chain_link_1");
        art.chainLink2 = S("obj_chain_link_2");
        art.crash = S("fx_crash");
        art.ballSkins = new[] { S("ball_big_happy_cyan"), S("ball_big_pink"), Gen("skin_ball_gold"), Gen("skin_ball_disco"), Gen("skin_ball_magma") };
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

        // Blurred prison at low opacity, covering 16:9, then a radial vignette on top.
        var prison = Rect("PrisonBlur", canvasGo.transform);
        prison.anchorMin = Vector2.zero; prison.anchorMax = Vector2.one; prison.offsetMin = prison.offsetMax = Vector2.zero;
        var primg = prison.gameObject.AddComponent<Image>();
        primg.sprite = S("env_background_prison_blur");
        primg.color = new Color(1f, 1f, 1f, 0.2f);
        primg.raycastTarget = false;
        var fit = prison.gameObject.AddComponent<AspectRatioFitter>();
        fit.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
        fit.aspectRatio = 2732f / 2048f;

        var vig = Rect("Vignette", canvasGo.transform);
        vig.anchorMin = Vector2.zero; vig.anchorMax = Vector2.one; vig.offsetMin = vig.offsetMax = Vector2.zero;
        var vimg = vig.gameObject.AddComponent<Image>();
        vimg.sprite = VignetteSprite();
        vimg.raycastTarget = false;

        var poster = Rect("Poster", canvasGo.transform);
        poster.anchorMin = poster.anchorMax = new Vector2(0.5f, 0.5f);
        poster.anchoredPosition = new Vector2(-520, 10);
        poster.sizeDelta = new Vector2(780, 740);
        var pimg = poster.gameObject.AddComponent<Image>();
        pimg.sprite = S("story_ending_adieu_poster");
        pimg.preserveAspect = true;
        pimg.raycastTarget = false;
        var sh = poster.gameObject.AddComponent<Shadow>();
        sh.effectColor = new Color(0, 0, 0, 0.6f); sh.effectDistance = new Vector2(10, -12);
        ctrl.poster = poster;

        var shadowMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF - Drop Shadow.mat");

        var textGroup = Rect("Story", canvasGo.transform);
        textGroup.anchorMin = textGroup.anchorMax = new Vector2(0.5f, 0.5f);
        textGroup.anchoredPosition = new Vector2(190, 170);
        textGroup.sizeDelta = new Vector2(600, 460);
        textGroup.gameObject.AddComponent<CanvasGroup>();
        var tf = textGroup.gameObject.AddComponent<UIFadeIn>();
        tf.delay = 1.4f; tf.duration = 1.4f;

        var head = Text(textGroup, "YOU BROKE OUT.", 60, new Vector2(0, 150), new Vector2(600, 90), new Color(0.36f, 0.95f, 0.84f));
        head.fontStyle = FontStyles.Bold;
        head.characterSpacing = 2f;
        head.textWrappingMode = TextWrappingModes.NoWrap;
        if (shadowMat != null) head.fontSharedMaterial = shadowMat;
        var body = Text(textGroup, "The kingdom will have to find another soldier.\n\nYou kept the ball.\nIt seems to like you.", 38,
            new Vector2(0, -60), new Vector2(600, 260), new Color(0.95f, 0.95f, 0.97f));
        body.fontStyle = FontStyles.Bold;
        body.lineSpacing = 8f;
        if (shadowMat != null) body.fontSharedMaterial = shadowMat;

        var btnRt = Rect("ReturnToTitleButton", canvasGo.transform);
        btnRt.anchorMin = btnRt.anchorMax = new Vector2(0.5f, 0.5f);
        btnRt.anchoredPosition = new Vector2(190, -250);
        btnRt.sizeDelta = new Vector2(540, 170);
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
            var col = Rect("Credits", canvasGo.transform);
            col.anchorMin = col.anchorMax = new Vector2(0.5f, 0.5f);
            col.anchoredPosition = new Vector2(720, 0);
            col.sizeDelta = new Vector2(380, 760);
            col.gameObject.AddComponent<CanvasGroup>();
            var cf = col.gameObject.AddComponent<UIFadeIn>();
            cf.delay = 3.2f; cf.duration = 1.2f;
            var bar = Rect("Rule", col);
            bar.anchorMin = new Vector2(0, 0.1f); bar.anchorMax = new Vector2(0, 0.9f);
            bar.sizeDelta = new Vector2(3, 0); bar.anchoredPosition = new Vector2(-20, 0);
            bar.gameObject.AddComponent<Image>().color = new Color(0.36f, 0.95f, 0.84f, 0.5f);

            var lines = System.IO.File.ReadAllLines(credits).Skip(1).Where(l => l.Trim().Length > 0)
                .Select(l => System.Text.RegularExpressions.Regex.Replace(l.Trim().TrimStart('-', ' '), @"\w+\.(wav|mp3):?\s*", ""))
                .Select(l => l.Replace("\u3084\u308b\u6c17\u30bc\u30ed", "Yaruki Zero").Replace(", ", ",  ").Trim());
            var ct = Text(col, "<size=24><b><color=#5CF2D6>SOUND & MUSIC</color></b></size>\n\n" + string.Join("\n\n", lines), 18,
                Vector2.zero, new Vector2(380, 760), new Color(1f, 1f, 1f, 0.72f));
            ct.alignment = TextAlignmentOptions.MidlineLeft;
            ct.fontStyle = FontStyles.Bold;
            ct.textWrappingMode = TextWrappingModes.Normal;
        }

        var es = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
        es.GetComponent<EventSystem>().firstSelectedGameObject = btnRt.gameObject;

        EditorSceneManager.SaveScene(scene, "Assets/Scenes/EndingScene.unity");
    }

    // Radial charcoal vignette baked to a PNG so the scene can reference it.
    static Sprite VignetteSprite()
    {
        const string path = "Assets/Art/Shell/vignette.png";
        if (!System.IO.File.Exists(path))
        {
            System.IO.Directory.CreateDirectory("Assets/Art/Shell");
            const int w = 256, h = 144;
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    float dx = (x + 0.5f) / w - 0.5f, dy = ((y + 0.5f) / h - 0.5f) * 0.9f;
                    float d = Mathf.Sqrt(dx * dx + dy * dy) / 0.62f;
                    float a = Mathf.Clamp01(Mathf.SmoothStep(0.25f, 1f, d)) * 0.92f;
                    tex.SetPixel(x, y, new Color(0.035f, 0.037f, 0.045f, a));
                }
            System.IO.File.WriteAllBytes(path, tex.EncodeToPNG());
            AssetDatabase.ImportAsset(path);
            var imp = (TextureImporter)AssetImporter.GetAtPath(path);
            imp.textureType = TextureImporterType.Sprite;
            imp.textureShape = TextureImporterShape.Texture2D;
            imp.spriteImportMode = SpriteImportMode.Single;
            imp.wrapMode = TextureWrapMode.Clamp;
            imp.SaveAndReimport();
        }
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
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

        // Background art is 4:3; cover the 16:9 frame instead of pillarboxing.
        var panel = GameObject.Find("Panel");
        if (panel != null)
        {
            var img = panel.GetComponent<Image>();
            img.preserveAspect = false;
            var fit = panel.GetComponent<AspectRatioFitter>();
            if (fit == null) fit = panel.AddComponent<AspectRatioFitter>();
            fit.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
            fit.aspectRatio = img.sprite.rect.width / img.sprite.rect.height;
        }
        var scaler = canvas.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;
        // Buttons were anchored top-left in 1920x1080 pixels; anchor them to the centre like the logo so they
        // keep their place relative to it at any aspect ratio.
        foreach (var name in new[] { "PlayButton", "ExitButton" })
        {
            var go = GameObject.Find(name);
            if (go == null) continue;
            var rt = (RectTransform)go.transform;
            if (rt.anchorMin == new Vector2(0.5f, 0.5f)) continue;
            var p = rt.anchoredPosition + new Vector2(rt.anchorMin.x * 1920f - 960f, rt.anchorMin.y * 1080f - 540f);
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = p;
        }

        var es = Object.FindAnyObjectByType<EventSystem>();
        var play = GameObject.Find("PlayButton");
        if (es != null && play != null) es.firstSelectedGameObject = play;

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
    }
}
