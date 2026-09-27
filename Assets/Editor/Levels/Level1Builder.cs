using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

// Level 1 "Breaking objects" — the tutorial. Builds Assets/Scenes/Levels/Level1.unity from SampleScene.
// Beats: run-up -> rock wall (break into stairs, climb over) -> rock under a ledge (break into stairs, climb up)
//        -> small lava gap -> glowing exit.
// Run: Tools/jam.sh run Level1Builder.Build
public static class Level1Builder
{
    const string ScenePath = "Assets/Scenes/Levels/Level1.unity";
    const string Cat = "Assets/Art/Catalog/";
    const string BlockPath = "Assets/Art/Level1/prison_block.png";

    // Sorting orders: background far behind, props behind gameplay, gameplay at 0.
    const int SortBg = -100, SortBgProps = -60, SortProps = -20, SortGeo = 5;

    static readonly string[] Keep =
        { "Main Camera", "CinemachineCamera", "Global Light 2D", "GameManager", "Canvas", "EventSystem", "Player", "Ball" };

    // Layout (world units). Floor top is y = 0.
    // Flush with the rock top: the stairs' top step leads straight onto solid ground, so the ball never
    // hangs over a drop and leaves the player dangling on the chain mid-climb.
    const float LedgeTop = 5.176f;
    const float PitFloor = 2.6f;
    const float CeilingY = 14f;
    const float RockScale = 0.8f, RockWidthScale = 0.8f;
    const string WallMat = "Assets/Art/Level1/L1_NoFriction.physicsMaterial2D";

    static Transform geoRoot, decoRoot;

    public static void Build()
    {
        try
        {
            BuildInternal();
            EditorApplication.Exit(0);
        }
        catch (Exception e)
        {
            Debug.LogException(e);
            EditorApplication.Exit(1);
        }
    }

    static void BuildInternal()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Scenes/Levels"))
            AssetDatabase.CreateFolder("Assets/Scenes", "Levels");
        // Keep the scene GUID stable (build settings reference it): overwrite the file contents in place.
        if (System.IO.File.Exists(ScenePath))
        {
            System.IO.File.Copy("Assets/Scenes/SampleScene.unity", ScenePath, true);
            AssetDatabase.ImportAsset(ScenePath, ImportAssetOptions.ForceUpdate);
        }
        else if (!AssetDatabase.CopyAsset("Assets/Scenes/SampleScene.unity", ScenePath))
            throw new Exception("copy failed");

        ConfigureBlockSprite();
        EnsureNoFrictionMaterial();

        var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        foreach (var go in scene.GetRootGameObjects())
            if (Array.IndexOf(Keep, go.name) < 0)
                UnityEngine.Object.DestroyImmediate(go);

        geoRoot = new GameObject("Level1_Geometry").transform;
        decoRoot = new GameObject("Level1_Dressing").transform;

        BuildGeometry();
        BuildGameplay();
        BuildDressing();
        PlaceActors(scene);
        ConfigureCamera(scene);

        if (!EditorSceneManager.SaveScene(scene, ScenePath))
            throw new Exception("save failed");
        Debug.Log("JAM: Level1 built");
    }

    // ---------------------------------------------------------------- geometry

    static void BuildGeometry()
    {
        // Main floor: spawn run-up, rock wall, up to the ledge.
        Block("Floor_Main", -12f, 28.2f, 0f, -14f);
        // Ledge the player climbs onto via the second rock's stairs.
        Block("Ledge_A", 28.2f, 36f, LedgeTop, -14f);
        // Landing on the far side of the first rock, flush with its top step.
        Block("Landing_A", 15f + 1.785f, 19.5f, LedgeTop, -0.5f);
        // Lava pit floor (notch between the ledges).
        Block("Pit_Floor", 36f, 38.5f, PitFloor, -14f);
        Block("Ledge_B", 38.5f, 60f, LedgeTop, -14f);
        // Boundary walls and ceiling.
        Block("Wall_Left", -30f, -8f, 30f, -14f);
        Block("Wall_Right", 52f, 76f, 30f, LedgeTop);
        Block("Ceiling", -30f, 76f, 30f, CeilingY);
    }

    static void BuildGameplay()
    {
        // Beat 2: rock wall blocking the run (too tall to jump; 3 ball hits turn it into stairs).
        Rock("Rock_Wall", 15f);
        // Beat 3: rock under the ledge — its stairs are the only way up.
        var rockW = RockSize().x;
        Rock("Rock_Stairs", 28.2f - rockW * 0.5f);

        // Beat 4: lava gap.
        var lavaSprite = Sprite("haz_lava_pool_framed");
        var lava = SpriteObj("Lava_Pool", lavaSprite, geoRoot, SortGeo - 1, Color.white);
        float lw = 2.5f / lavaSprite.bounds.size.x;
        lava.transform.localScale = new Vector3(lw, lw * 1.05f, 1f);
        float lh = lavaSprite.bounds.size.y * lw * 1.05f;
        lava.transform.position = new Vector3(37.25f, PitFloor + lh * 0.5f - 0.05f, 0f);
        var hazard = new GameObject("Lava_Trigger");
        hazard.transform.SetParent(geoRoot);
        hazard.layer = LayerMask.NameToLayer("Lava");
        hazard.transform.position = new Vector3(37.25f, PitFloor + lh * 0.45f, 0f);
        var hc = hazard.AddComponent<BoxCollider2D>();
        hc.isTrigger = true;
        hc.size = new Vector2(2.4f, lh * 0.8f);

        // Beat 5: glowing exit.
        var doorSprite = Sprite("obj_exit_door_glow");
        var exit = SpriteObj("Exit_Door", doorSprite, geoRoot, -2, Color.white);
        exit.tag = "Finish";
        float ds = 0.55f;
        exit.transform.localScale = new Vector3(ds, ds, 1f);
        exit.transform.position = new Vector3(47.5f, LedgeTop + doorSprite.bounds.size.y * ds * 0.5f - 0.1f, 0f);
        var ec = exit.AddComponent<BoxCollider2D>();
        ec.isTrigger = true;
        ec.size = new Vector2(doorSprite.bounds.size.x * 0.6f, doorSprite.bounds.size.y * 0.8f);
    }

    static Vector2 RockSize()
    {
        var s = Sprite("obj_rock_cracked_3").bounds.size;
        return new Vector2(s.x * RockWidthScale, s.y * RockScale);
    }

    static void Rock(string name, float x)
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/BreakableRockStair.prefab");
        var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        go.name = name;
        go.transform.SetParent(geoRoot);
        var intact = Sprite("obj_rock_cracked_3");
        var sr = go.GetComponent<SpriteRenderer>();
        sr.sprite = intact;
        sr.sortingOrder = SortGeo;
        // Negative X scale: RockShape preserves the sign, so the stairs rise toward the right (climbable from the left).
        go.transform.localScale = new Vector3(-RockWidthScale, RockScale, 1f);
        go.transform.position = new Vector3(x, RockSize().y * 0.5f, 0f);
        var box = go.GetComponent<BoxCollider2D>();
        box.size = intact.bounds.size * 0.98f;
        box.offset = Vector2.zero;

        var so = new SerializedObject(go.GetComponent<RockShape>());
        so.FindProperty("stage1Sprite").objectReferenceValue = Sprite("obj_rock_cracked_2");
        so.FindProperty("stage2Sprite").objectReferenceValue = Sprite("obj_rock_cracked_1");
        so.FindProperty("stairsSprite").objectReferenceValue = Sprite("obj_gray_stairs_cracked");
        so.ApplyModifiedPropertiesWithoutUndo();

    }

    static void EnsureNoFrictionMaterial()
    {
        if (AssetDatabase.LoadAssetAtPath<PhysicsMaterial2D>(WallMat) != null) return;
        var m = new PhysicsMaterial2D("L1_NoFriction") { friction = 0f, bounciness = 0f };
        m.frictionCombine = PhysicsMaterialCombine2D.Minimum;
        AssetDatabase.CreateAsset(m, WallMat);
        AssetDatabase.SaveAssets();
    }

    // ---------------------------------------------------------------- dressing

    static void BuildDressing()
    {
        // Far background: blurred prison, darkened, mirrored copies so seams match.
        var bg = Sprite("env_background_prison_blur");
        float bs = 1.25f;
        float bw = bg.bounds.size.x * bs;
        for (int i = 0; i < 4; i++)
        {
            var b = SpriteObj("BG_" + i, bg, decoRoot, SortBg, new Color(0.30f, 0.32f, 0.36f, 1f));
            b.GetComponent<SpriteRenderer>().flipX = (i % 2) == 1;
            b.transform.localScale = new Vector3(bs, bs, 1f);
            b.transform.position = new Vector3(-20f + bw * 0.5f + i * bw, 6.5f, 0f);
        }

        // Background pillars (dimmed to prison gray so they read as scenery, not interactables).
        var pillar = Sprite("obj_cyan_pillar");
        var pillarTint = new Color(0.26f, 0.28f, 0.30f, 1f);
        foreach (var px in new[] { -3f, 9f, 22f, 33f, 44f })
        {
            var p = SpriteObj("BG_Pillar", pillar, decoRoot, SortBgProps, pillarTint);
            float ps = CeilingY / pillar.bounds.size.y;
            p.transform.localScale = new Vector3(ps * 0.8f, ps, 1f);
            p.transform.position = new Vector3(px, CeilingY * 0.5f, 0f);
        }

        // Hanging chains from the ceiling (gray, decorative).
        var link = Sprite("obj_chain_link_1");
        var chainTint = new Color(0.45f, 0.48f, 0.5f, 1f);
        foreach (var (cx, n) in new[] { (3.5f, 5), (11f, 3), (25f, 4), (41.5f, 3) })
        {
            for (int k = 0; k < n; k++)
            {
                var l = SpriteObj("Deco_Chain", link, decoRoot, SortProps, chainTint);
                l.transform.localScale = new Vector3(0.35f, 0.35f, 1f);
                l.transform.rotation = Quaternion.Euler(0, 0, 55f);
                l.transform.position = new Vector3(cx, CeilingY - 0.3f - 0.85f * k, 0f);
            }
        }

        // Gray stalactites on the ceiling (no colliders — just atmosphere).
        var stal = Sprite("haz_stalactite_gray");
        foreach (var (sx, s) in new[] { (7f, 0.6f), (8.2f, 0.4f), (19f, 0.5f), (31f, 0.65f), (32.1f, 0.42f), (45f, 0.5f) })
        {
            var st = SpriteObj("Deco_Stalactite", stal, decoRoot, SortProps, new Color(0.75f, 0.77f, 0.8f, 1f));
            st.transform.localScale = new Vector3(s, s, 1f);
            st.transform.position = new Vector3(sx, CeilingY - stal.bounds.size.y * s * 0.5f + 0.1f, 0f);
        }

        // Diegetic hint near the spawn: the training note (ball breaks rocks).
        var note = Sprite("story_note_training");
        var n1 = SpriteObj("Deco_TrainingNote", note, decoRoot, SortProps, new Color(0.85f, 0.87f, 0.9f, 1f));
        n1.transform.localScale = new Vector3(0.36f, 0.36f, 1f);
        n1.transform.rotation = Quaternion.Euler(0, 0, 2.5f);
        n1.transform.position = new Vector3(5.5f, 6.2f, 0f);

        // Rubble near the second rock, gray stairs silhouette in the back.
        var gstairs = Sprite("obj_gray_stairs_cracked");
        var gs = SpriteObj("BG_Stairs", gstairs, decoRoot, SortBgProps + 1, new Color(0.35f, 0.37f, 0.4f, 1f));
        gs.transform.localScale = new Vector3(-0.5f, 0.5f, 1f);
        gs.transform.position = new Vector3(-4.5f, gstairs.bounds.size.y * 0.25f, 0f);
    }

    // ---------------------------------------------------------------- actors / camera

    static void PlaceActors(Scene scene)
    {
        GameObject player = null, ball = null;
        foreach (var go in scene.GetRootGameObjects())
        {
            if (go.name == "Player") player = go;
            if (go.name == "Ball") ball = go;
        }
        if (player == null || ball == null) throw new Exception("Player/Ball missing");
        player.transform.position = new Vector3(-1f, 1.65f, 0f);
        ball.transform.position = new Vector3(1.4f, 1.4f, 0f);
    }

    static void ConfigureCamera(Scene scene)
    {
        foreach (var go in scene.GetRootGameObjects())
        {
            if (go.name == "Main Camera")
            {
                var cam = go.GetComponent<Camera>();
                cam.backgroundColor = new Color(0.09f, 0.095f, 0.105f, 1f);
                cam.orthographicSize = 9f;
                go.transform.position = new Vector3(-1f, 1.65f, -10f);
            }
            if (go.name == "CinemachineCamera")
            {
                go.transform.position = new Vector3(-1f, 1.65f, -10f);
                foreach (var c in go.GetComponents<Component>())
                {
                    if (c == null || c.GetType().Name != "CinemachineCamera") continue;
                    var so = new SerializedObject(c);
                    var p = so.FindProperty("Lens.OrthographicSize");
                    if (p != null) { p.floatValue = 9f; so.ApplyModifiedPropertiesWithoutUndo(); }
                }
            }
        }
    }

    // ---------------------------------------------------------------- helpers

    static void ConfigureBlockSprite()
    {
        var ti = (TextureImporter)AssetImporter.GetAtPath(BlockPath);
        if (ti == null) throw new Exception("missing " + BlockPath);
        var settings = new TextureImporterSettings();
        ti.ReadTextureSettings(settings);
        bool dirty = settings.spriteMeshType != SpriteMeshType.FullRect || ti.spriteBorder != new Vector4(22, 22, 22, 22);
        if (dirty)
        {
            settings.spriteMeshType = SpriteMeshType.FullRect;
            ti.SetTextureSettings(settings);
            ti.spriteBorder = new Vector4(22, 22, 22, 22);
            ti.SaveAndReimport();
        }
    }

    static GameObject Block(string name, float x0, float x1, float yTop, float yBottom)
    {
        var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(BlockPath);
        var go = SpriteObj(name, sprite, geoRoot, SortGeo, Color.white);
        go.layer = LayerMask.NameToLayer("Ground");
        var sr = go.GetComponent<SpriteRenderer>();
        // Tiled riveted prison blocks; the transform scale sets the tile size (~2 units).
        const float k = 1.6f;
        sr.drawMode = SpriteDrawMode.Tiled;
        sr.tileMode = SpriteTileMode.Continuous;
        var size = new Vector2(x1 - x0, yTop - yBottom) / k;
        sr.size = size;
        sr.color = new Color(0.82f, 0.84f, 0.88f, 1f);
        go.transform.localScale = new Vector3(k, k, 1f);
        go.transform.position = new Vector3((x0 + x1) * 0.5f, (yTop + yBottom) * 0.5f, 0f);
        var col = go.AddComponent<BoxCollider2D>();
        col.size = size;
        return go;
    }

    static Sprite Sprite(string name)
    {
        var s = AssetDatabase.LoadAssetAtPath<Sprite>(Cat + name + ".png");
        if (s == null) throw new Exception("missing sprite " + name);
        return s;
    }

    static GameObject SpriteObj(string name, Sprite sprite, Transform parent, int order, Color color)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent);
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.sortingOrder = order;
        sr.color = color;
        return go;
    }
}
