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
        art.credits = Gen("credits_screen");
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
        fade.duration = 0.9f;
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

        // Everything but the backdrop sits in Content so the stamp can shake it.
        var content = Rect("Content", canvasGo.transform);
        content.anchorMin = Vector2.zero; content.anchorMax = Vector2.one; content.offsetMin = content.offsetMax = Vector2.zero;
        ctrl.content = content;

        var poster = Place(Rect("Poster", content), new Vector2(-540, 190), new Vector2(620, 587), -4f);
        var pimg = poster.gameObject.AddComponent<Image>();
        pimg.sprite = S("story_ending_adieu_poster");
        pimg.preserveAspect = true;
        pimg.raycastTarget = false;
        var sh = poster.gameObject.AddComponent<Shadow>();
        sh.effectColor = new Color(0, 0, 0, 0.6f); sh.effectDistance = new Vector2(12, -14);
        ctrl.poster = poster;

        // Headline lettering is made at runtime (comic outline material); this is where it goes.
        ctrl.headlineAnchor = Place(Rect("Headline", content), new Vector2(-530, -150), new Vector2(760, 120), 4f);

        // Release form: 700x940, rows measured off the art (header ~19%, rows at 31/41/51/62/72%, notes ~83%).
        const float FW = 700f, FH = 940f;
        var form = Place(Rect("ReleaseForm", content), new Vector2(290, 25), new Vector2(FW, FH), 1.5f);
        var fimg = form.gameObject.AddComponent<Image>();
        fimg.sprite = Gen("ui_release_form");
        fimg.raycastTarget = false;
        var fsh = form.gameObject.AddComponent<Shadow>();
        fsh.effectColor = new Color(0, 0, 0, 0.55f); fsh.effectDistance = new Vector2(12, -14);
        float Y(float p) => FH * 0.5f - p * FH;
        var comic = Resources.Load<TMP_FontAsset>("ComicFont");
        var ink = new Color(0.1f, 0.1f, 0.13f);
        var hdr = Text(form, "INMATE RELEASE FORM", 46, new Vector2(0, Y(0.19f)), new Vector2(FW * 0.78f, 70), new Color(0.93f, 0.96f, 0.95f));
        hdr.font = comic;
        hdr.characterSpacing = 4f;
        string[] labels = { "TIME SERVED", "PROPERTY DAMAGE", "CONTRABAND", "BEST CHAIN", "PAROLE RATING" };
        float[] rows = { 0.31f, 0.41f, 0.51f, 0.62f, 0.72f };
        ctrl.rowValues = new TextMeshProUGUI[labels.Length];
        for (int i = 0; i < labels.Length; i++)
        {
            var lab = Text(form, labels[i], 21, new Vector2(-FW * 0.335f, Y(rows[i])), new Vector2(FW * 0.17f, 62), ink);
            lab.font = comic;
            lab.enableAutoSizing = true; lab.fontSizeMin = 12; lab.fontSizeMax = 21;
            lab.lineSpacing = -18f;
            var val = Text(form, "\u2014", 40, new Vector2(FW * 0.13f, Y(rows[i])), new Vector2(FW * 0.6f, 62), ink);
            val.alignment = TextAlignmentOptions.MidlineLeft;
            val.characterSpacing = 3f;
            val.name = "Value " + labels[i];
            ctrl.rowValues[i] = val;
        }
        var notes = Text(form, "NOTES: <i>kept the ball. it seems to like you.</i>", 27, new Vector2(-FW * 0.04f, Y(0.83f)), new Vector2(FW * 0.8f, 60),
            new Color(0.16f, 0.24f, 0.55f));
        notes.font = comic;
        notes.alignment = TextAlignmentOptions.MidlineLeft;
        notes.rectTransform.localRotation = Quaternion.Euler(0, 0, 2.5f);

        var stamp = Place(Rect("Stamp", content), new Vector2(470, -150), new Vector2(450, 203), -12f);
        var simg = stamp.gameObject.AddComponent<Image>();
        simg.sprite = Gen("ui_stamp_released");
        simg.preserveAspect = true;
        simg.raycastTarget = false;
        stamp.gameObject.AddComponent<CanvasGroup>();
        ctrl.stamp = stamp;

        var es = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));

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

    static RectTransform Place(RectTransform rt, Vector2 pos, Vector2 size, float rot)
    {
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
        rt.localRotation = Quaternion.Euler(0, 0, rot);
        return rt;
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
