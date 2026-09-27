using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Builds Assets/Scenes/YardBreak.unity (60-second bonus round) from SampleScene. Re-run freely:
//   Tools/jam.sh run YardBreakBuilder.Build
// A single-screen exercise yard at night: fixed camera, riveted floor, three low platforms at two heights.
// YardBreakGame spawns the cardboard guards at runtime.
public static class YardBreakBuilder
{
    const string ScenePath = "Assets/Scenes/YardBreak.unity";
    const float FloorTop = -5f;
    const float Ortho = 7.5f;
    const float HalfWidth = 13.3f;
    const float LowPlat = FloorTop + 1.9f, HighPlat = FloorTop + 3.6f;

    static readonly Color Stone = new Color(0.22f, 0.23f, 0.26f);
    static readonly Color Mass = new Color(0.1f, 0.105f, 0.12f);

    public static void Build()
    {
        try { BuildScene(); EditorApplication.Exit(0); }
        catch (Exception e) { Debug.LogException(e); EditorApplication.Exit(1); }
    }

    static void BuildScene()
    {
        if (System.IO.File.Exists(ScenePath))
        {
            System.IO.File.Copy("Assets/Scenes/SampleScene.unity", ScenePath, true);
            AssetDatabase.ImportAsset(ScenePath, ImportAssetOptions.ForceUpdate);
        }
        else if (!AssetDatabase.CopyAsset("Assets/Scenes/SampleScene.unity", ScenePath)) throw new Exception("copy failed");
        var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

        string[] keep = { "Main Camera", "CinemachineCamera", "Global Light 2D", "GameManager", "Canvas", "EventSystem", "Player", "Ball" };
        foreach (var root in scene.GetRootGameObjects())
            if (Array.IndexOf(keep, root.name) < 0) UnityEngine.Object.DestroyImmediate(root);

        int ground = LayerMask.NameToLayer("Ground");
        var yard = new GameObject("Yard").transform;

        // --- backdrop: full-bleed night yard, slightly oversized for the parallax drift ---
        var bgSprite = Load("Assets/Art/Gen/minigame_bg_yard.png");
        var bg = new GameObject("Background");
        bg.transform.SetParent(yard);
        var bgsr = bg.AddComponent<SpriteRenderer>();
        bgsr.sprite = bgSprite;
        bgsr.sortingOrder = -100;
        float bgScale = Mathf.Max(2f * HalfWidth * 1.08f / bgSprite.bounds.size.x, 2f * Ortho * 1.02f / bgSprite.bounds.size.y);
        bg.transform.localScale = Vector3.one * bgScale;
        bg.transform.position = new Vector3(0, 0.4f, 5f);

        // --- floor: riveted metal strip over a dark mass ---
        var floorPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Floor.prefab");
        Slab(floorPrefab, scene, yard, "Floor", 0f, FloorTop, 2f * HalfWidth + 4f);
        Tiled("Floor Mass", new Vector2(0, FloorTop - 1f - 6f), new Vector2(2f * HalfWidth + 6f, 12f), Mass, yard, -10, false, 0);

        // --- low platforms at two heights ---
        Slab(floorPrefab, scene, yard, "Platform Left", -7.5f, LowPlat, 3.6f);
        Slab(floorPrefab, scene, yard, "Platform Right", 7.5f, LowPlat, 3.6f);
        Slab(floorPrefab, scene, yard, "Platform Center", 0f, HighPlat, 4.2f);
        foreach (var x in new[] { -7.5f, 7.5f }) Tiled("Post", new Vector2(x, (LowPlat - 1f + FloorTop) / 2f), new Vector2(0.5f, LowPlat - 1f - FloorTop), Stone, yard, -11, false, 0);
        Tiled("Post", new Vector2(0, (HighPlat - 1f + FloorTop) / 2f), new Vector2(0.5f, HighPlat - 1f - FloorTop), Stone, yard, -11, false, 0);

        // --- walls just past the screen edge ---
        Tiled("Wall Left", new Vector2(-HalfWidth - 0.8f, 2f), new Vector2(2f, 24f), Stone, yard, -9, true, ground);
        Tiled("Wall Right", new Vector2(HalfWidth + 0.8f, 2f), new Vector2(2f, 24f), Stone, yard, -9, true, ground);

        // --- spawn ---
        var player = GameObject.Find("Player");
        var ball = GameObject.Find("Ball");
        player.transform.position = new Vector3(0f, FloorTop + 1.6f, 0f);
        ball.transform.position = new Vector3(-2f, FloorTop + 1.33f, 0f);

        // --- fixed camera framing the whole yard ---
        foreach (var go in scene.GetRootGameObjects())
        {
            if (go.name == "Main Camera")
            {
                var cam = go.GetComponent<Camera>();
                cam.orthographicSize = Ortho;
                cam.clearFlags = CameraClearFlags.SolidColor;
                cam.backgroundColor = new Color(0.06f, 0.065f, 0.08f);
                go.transform.position = new Vector3(0, 0, -10f);
            }
            if (go.name == "CinemachineCamera")
            {
                go.transform.position = new Vector3(0, 0, -10f);
                foreach (var c in go.GetComponents<Component>())
                {
                    if (c == null) continue;
                    var so = new SerializedObject(c);
                    var lens = so.FindProperty("Lens.OrthographicSize");
                    if (lens != null) lens.floatValue = Ortho;
                    var tgt = so.FindProperty("Target.TrackingTarget");
                    if (tgt != null) tgt.objectReferenceValue = null;
                    so.ApplyModifiedPropertiesWithoutUndo();
                }
            }
        }

        // --- game controller ---
        var gameGo = new GameObject("YardBreakGame");
        var game = gameGo.AddComponent<YardBreakGame>();
        var gso = new SerializedObject(game);
        gso.FindProperty("targetSprite").objectReferenceValue = Load("Assets/Art/Gen/minigame_guard_target.png");
        gso.FindProperty("crashSprite").objectReferenceValue = Load("Assets/Art/Catalog/fx_crash.png");
        gso.FindProperty("background").objectReferenceValue = bg.transform;
        gso.FindProperty("arenaHalfWidth").floatValue = HalfWidth;
        Vector2[] spots =
        {
            new Vector2(-11.2f, FloorTop), new Vector2(-4f, FloorTop), new Vector2(-1.2f, FloorTop), new Vector2(3.5f, FloorTop),
            new Vector2(11.2f, FloorTop), new Vector2(-10f, FloorTop), new Vector2(10f, FloorTop),
            new Vector2(-7.5f, LowPlat), new Vector2(7.5f, LowPlat), new Vector2(0f, HighPlat),
        };
        var sp = gso.FindProperty("spawnPoints");
        sp.arraySize = spots.Length;
        for (int i = 0; i < spots.Length; i++) sp.GetArrayElementAtIndex(i).vector2Value = spots[i];
        gso.ApplyModifiedPropertiesWithoutUndo();

        EditorSceneManager.MarkSceneDirty(scene);
        if (!EditorSceneManager.SaveScene(scene)) throw new Exception("save failed");
        Debug.Log("JAM: built " + ScenePath);
    }

    static Sprite Load(string path)
    {
        var s = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        if (s == null)
        {
            // generated art can arrive with a stub .meta; make sure it imports as a single sprite
            var ti = AssetImporter.GetAtPath(path) as TextureImporter;
            if (ti == null) throw new Exception("missing texture " + path);
            Debug.Log($"JAM: reimporting {path} as sprite (was {ti.textureType}/{ti.spriteImportMode}/{ti.textureShape})");
            ti.textureType = TextureImporterType.Sprite;
            ti.spriteImportMode = SpriteImportMode.Single;
            ti.textureShape = TextureImporterShape.Texture2D;
            ti.SaveAndReimport();
            s = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }
        if (s == null) throw new Exception("missing sprite " + path);
        return s;
    }

    // A riveted-metal slab (Floor prefab) whose top surface sits at yTop.
    static void Slab(GameObject prefab, UnityEngine.SceneManagement.Scene scene, Transform parent, string name, float x, float yTop, float width)
    {
        var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
        go.name = name;
        go.transform.SetParent(parent);
        go.transform.position = new Vector3(x, yTop - 0.5f, 0f);
        go.GetComponent<SpriteRenderer>().size = new Vector2(width, 1f);
        go.GetComponent<BoxCollider2D>().size = new Vector2(width, 1f);
    }

    static GameObject Tiled(string name, Vector2 pos, Vector2 size, Color color, Transform parent, int order, bool solid, int layer)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent);
        go.transform.position = pos;
        go.layer = layer;
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Images/Ground.png");
        sr.drawMode = SpriteDrawMode.Tiled;
        sr.size = size;
        sr.color = color;
        sr.sortingOrder = order;
        if (solid) go.AddComponent<BoxCollider2D>().size = size;
        return go;
    }
}
