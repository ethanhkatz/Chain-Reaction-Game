using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

// Level 1 "Breaking objects" — the tutorial. Builds Assets/Scenes/Levels/Level1.unity from SampleScene.
// Beats: run-up -> rock wall (break into stairs, climb over) -> rock under a ledge (break into stairs, climb up)
//        -> small lava gap -> three thin cyan walls to plow through (smash-smash-smash) -> glowing exit.
// Secret: a cracked wall left of the spawn hides a cell with the training note and a key hung out of reach.
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
    const float LevelEnd = 90f;          // inner face of the right wall
    const float ExitX = 84.5f;
    static readonly float[] SmashWallX = { 55f, 62f, 69f };
    const float SmashWallH = 2.8f;       // jumpable, so the walls can never trap you
    // Secret cell behind the cracked wall left of the spawn.
    const float CellX0 = -17f, CellX1 = -9.5f, CellTop = 8.5f, CrackTop = 4.2f, CrackX1 = -8f;
    const string KeyPath = "Assets/Art/Level1/L1_Key.png";
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
        BuildSmashWalls();
        BuildCollectibles();
        BuildSecret();
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
        // Main floor: secret cell, spawn run-up, rock wall, up to the ledge.
        Block("Floor_Main", CellX0 - 1f, 28.2f, 0f, -14f);
        // Ledge the player climbs onto via the second rock's stairs.
        Block("Ledge_A", 28.2f, 36f, LedgeTop, -14f);
        // Landing on the far side of the first rock, flush with its top step.
        Block("Landing_A", 15f + 1.785f, 19.5f, LedgeTop, -0.5f);
        // Lava pit floor (notch between the ledges).
        Block("Pit_Floor", 36f, 38.5f, PitFloor, -14f);
        Block("Ledge_B", 38.5f, LevelEnd + 2f, LedgeTop, -14f);
        // Boundary walls and ceiling. The left wall is hollowed out into the secret cell.
        Block("Wall_Left", -34f, CellX0, 30f, -14f);
        Block("Wall_Left_CellRoof", CellX0 - 0.5f, CrackX1, 30f, CellTop);
        Block("Wall_Left_Lintel", CellX1, CrackX1, CellTop, CrackTop);
        Block("Wall_Right", LevelEnd, LevelEnd + 24f, 30f, LedgeTop);
        Block("Ceiling", -34f, LevelEnd + 24f, 30f, CeilingY);
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
        exit.transform.position = new Vector3(ExitX, LedgeTop + doorSprite.bounds.size.y * ds * 0.5f - 0.1f, 0f);
        var ec = exit.AddComponent<BoxCollider2D>();
        ec.isTrigger = true;
        ec.size = new Vector2(doorSprite.bounds.size.x * 0.6f, doorSprite.bounds.size.y * 0.8f);
    }

    // Beat 5: three thin cyan walls in a row. Run the ball through them; each shatters in one hit.
    static void BuildSmashWalls()
    {
        var pillar = Sprite("obj_cyan_pillar");
        for (int i = 0; i < SmashWallX.Length; i++)
        {
            var w = SpriteObj("Smash_Wall_" + (i + 1), pillar, geoRoot, SortGeo, Color.white);
            w.layer = LayerMask.NameToLayer("Ground");
            float sx = 1.0f / pillar.bounds.size.x, sy = SmashWallH / pillar.bounds.size.y;
            w.transform.localScale = new Vector3(sx, sy, 1f);
            w.transform.position = new Vector3(SmashWallX[i], LedgeTop + SmashWallH * 0.5f, 0f);
            var col = w.AddComponent<BoxCollider2D>();
            col.size = pillar.bounds.size;
            col.sharedMaterial = AssetDatabase.LoadAssetAtPath<PhysicsMaterial2D>(WallMat);
            ConfigureSmash(w.AddComponent<SmashWall>(), false, new Color(0.55f, 1f, 0.9f, 1f), "smash_wall");
        }
    }

    static void ConfigureSmash(SmashWall sw, bool ignorePlayer, Color chunkColor, string kind)
    {
        var so = new SerializedObject(sw);
        so.FindProperty("ignorePlayer").boolValue = ignorePlayer;
        so.FindProperty("breakSpeed").floatValue = 1.5f;
        var arr = so.FindProperty("chunkSprites");
        arr.arraySize = 3;
        arr.GetArrayElementAtIndex(0).objectReferenceValue = Sprite("obj_rock_cracked_1");
        arr.GetArrayElementAtIndex(1).objectReferenceValue = Sprite("obj_rock_cracked_2");
        arr.GetArrayElementAtIndex(2).objectReferenceValue = Sprite("obj_cyan_block_small");
        so.FindProperty("chunkCount").intValue = 7;
        so.FindProperty("chunkScale").floatValue = 0.22f;
        so.FindProperty("chunkColor").colorValue = chunkColor;
        so.FindProperty("crashSprite").objectReferenceValue = Sprite("fx_crash");
        so.FindProperty("crashScale").floatValue = 0.22f;
        so.FindProperty("reportKind").stringValue = kind;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    // Collectibles: one easy on the run-up, one floating over the middle smash wall (jump onto it instead of
    // plowing through), one hidden in the secret cell.
    static void BuildCollectibles()
    {
        Collectible("Collectible_RunUp", new Vector2(6f, 1.1f), geoRoot);
        Collectible("Collectible_OverWall", new Vector2(SmashWallX[1], LedgeTop + SmashWallH + 1.3f), geoRoot);
    }

    static GameObject Collectible(string name, Vector2 pos, Transform parent)
    {
        var s = Sprite("obj_cyan_block_small");
        var go = SpriteObj(name, s, parent, 3, Color.white);
        go.transform.localScale = new Vector3(0.35f, 0.35f, 1f);
        go.transform.position = pos;
        var c = go.AddComponent<BoxCollider2D>();
        c.isTrigger = true;
        c.size = s.bounds.size;
        return go;
    }

    // Easter egg: a cracked, slightly paler wall left of the spawn. Walk into it and the trailing ball smashes it,
    // revealing a cell with the training note and the cell key dangling far out of reach.
    static void BuildSecret()
    {
        var root = new GameObject("Secret_KeyCell").transform;

        var crack = Block("Secret_CrackedWall", CellX1, CrackX1, CrackTop, 0f);
        crack.transform.SetParent(root);
        crack.layer = 0; // not Ground: the player walks through it and must not "stand" inside it
        var csr = crack.GetComponent<SpriteRenderer>();
        csr.color = new Color(0.9f, 0.9f, 0.86f, 1f);
        ConfigureSmash(crack.AddComponent<SmashWall>(), true, new Color(0.75f, 0.77f, 0.8f, 1f), "secret");
        // Crack overlay so the wall reads as weak.
        var rockCrack = Sprite("obj_rock_cracked_1");
        var ov = SpriteObj("Crack_Overlay", rockCrack, crack.transform, SortGeo + 1, new Color(0.2f, 0.21f, 0.23f, 0.55f));
        ov.transform.position = new Vector3((CellX1 + CrackX1) * 0.5f, CrackTop * 0.5f, 0f);
        float ok = 1f / crack.transform.localScale.x;
        ov.transform.localScale = new Vector3(0.28f * ok, 0.55f * ok, 1f);

        // Dim cell backdrop.
        var bsprite = AssetDatabase.LoadAssetAtPath<Sprite>(BlockPath);
        var back = SpriteObj("Cell_Back", bsprite, root, SortBgProps + 2, new Color(0.22f, 0.23f, 0.26f, 1f));
        var bsr = back.GetComponent<SpriteRenderer>();
        bsr.drawMode = SpriteDrawMode.Tiled;
        back.transform.localScale = new Vector3(1.6f, 1.6f, 1f);
        bsr.size = new Vector2(CellX1 - CellX0, CellTop) / 1.6f;
        back.transform.position = new Vector3((CellX0 + CellX1) * 0.5f, CellTop * 0.5f, 0f);

        // The training note pinned to the back wall.
        var note = SpriteObj("Cell_TrainingNote", Sprite("story_note_training"), root, SortProps + 5, new Color(0.9f, 0.91f, 0.93f, 1f));
        note.transform.localScale = new Vector3(0.26f, 0.26f, 1f);
        note.transform.rotation = Quaternion.Euler(0, 0, -4f);
        note.transform.position = new Vector3(-14.8f, 3.3f, 0f);

        // The key, hanging from the roof on a short chain, way above jump height.
        var link = Sprite("obj_chain_link_1");
        for (int k = 0; k < 2; k++)
        {
            var l = SpriteObj("Cell_KeyChain", link, root, SortProps + 5, new Color(0.6f, 0.62f, 0.65f, 1f));
            l.transform.localScale = new Vector3(0.22f, 0.22f, 1f);
            l.transform.rotation = Quaternion.Euler(0, 0, 90f + 20f * k);
            l.transform.position = new Vector3(-11.6f, CellTop - 0.25f - 0.4f * k, 0f);
        }
        var key = SpriteObj("Cell_Key", EnsureKeySprite(), root, SortProps + 6, new Color(1f, 0.93f, 0.7f, 1f));
        key.transform.localScale = new Vector3(0.55f, 0.55f, 1f);
        key.transform.rotation = Quaternion.Euler(0, 0, -80f);
        key.transform.position = new Vector3(-11.6f, CellTop - 1.45f, 0f);

        var label = new GameObject("Cell_Label");
        label.transform.SetParent(root);
        label.transform.position = new Vector3(-11.6f, CellTop - 2.6f, 0f);
        var tm = label.AddComponent<TextMesh>();
        tm.text = "where's the key?";
        tm.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        tm.fontSize = 48;
        tm.characterSize = 0.06f;
        tm.anchor = TextAnchor.MiddleCenter;
        tm.alignment = TextAlignment.Center;
        tm.color = new Color(1f, 0.93f, 0.7f, 0.9f);
        var mr = label.GetComponent<MeshRenderer>();
        mr.sharedMaterial = tm.font.material;
        mr.sortingOrder = SortProps + 6;

        // Hidden collectible on the cell floor.
        Collectible("Collectible_SecretCell", new Vector2(-15.8f, 1.0f), root);
    }

    // A tiny pixel-art key, generated once and saved as a sprite asset.
    static Sprite EnsureKeySprite()
    {
        if (!System.IO.File.Exists(KeyPath))
        {
            string[] rows =
            {
                "..XXX...................",
                ".X...X..................",
                "X.....X.................",
                "X.....XXXXXXXXXXXXXXXXX.",
                "X.....XXXXXXXXXXXXXXXXXX",
                "X.....X..........X.X.X..",
                ".X...X...........X.X.X..",
                "..XXX............X...X..",
            };
            int w = rows[0].Length, h = rows.Length;
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                    tex.SetPixel(x, h - 1 - y, rows[y][x] == 'X' ? Color.white : new Color(0, 0, 0, 0));
            System.IO.File.WriteAllBytes(KeyPath, tex.EncodeToPNG());
            AssetDatabase.ImportAsset(KeyPath, ImportAssetOptions.ForceUpdate);
            var ti = (TextureImporter)AssetImporter.GetAtPath(KeyPath);
            ti.textureType = TextureImporterType.Sprite;
            ti.spriteImportMode = SpriteImportMode.Single;
            ti.spritePixelsPerUnit = 12;
            ti.filterMode = FilterMode.Point;
            ti.textureCompression = TextureImporterCompression.Uncompressed;
            ti.SaveAndReimport();
        }
        var s = AssetDatabase.LoadAssetAtPath<Sprite>(KeyPath);
        if (s == null) throw new Exception("key sprite missing");
        return s;
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
        so.FindProperty("isStairs").boolValue = true;
        so.FindProperty("matchSizeOnStairs").boolValue = true;
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
        for (int i = 0; i < 6; i++)
        {
            var b = SpriteObj("BG_" + i, bg, decoRoot, SortBg, new Color(0.30f, 0.32f, 0.36f, 1f));
            b.GetComponent<SpriteRenderer>().flipX = (i % 2) == 1;
            b.transform.localScale = new Vector3(bs, bs, 1f);
            b.transform.position = new Vector3(-26f + bw * 0.5f + i * bw, 6.5f, 0f);
        }

        // Background pillars (dimmed to prison gray so they read as scenery, not interactables).
        var pillar = Sprite("obj_cyan_pillar");
        var pillarTint = new Color(0.26f, 0.28f, 0.30f, 1f);
        foreach (var px in new[] { -3f, 9f, 22f, 33f, 44f, 58.5f, 72.5f, 80f })
        {
            var p = SpriteObj("BG_Pillar", pillar, decoRoot, SortBgProps, pillarTint);
            float ps = CeilingY / pillar.bounds.size.y;
            p.transform.localScale = new Vector3(ps * 0.8f, ps, 1f);
            p.transform.position = new Vector3(px, CeilingY * 0.5f, 0f);
        }

        // Hanging chains from the ceiling (gray, decorative).
        var link = Sprite("obj_chain_link_1");
        var chainTint = new Color(0.45f, 0.48f, 0.5f, 1f);
        foreach (var (cx, n) in new[] { (3.5f, 5), (11f, 3), (25f, 4), (41.5f, 3), (52f, 4), (65.5f, 3), (77f, 5) })
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
        foreach (var (sx, s) in new[] { (7f, 0.6f), (8.2f, 0.4f), (19f, 0.5f), (31f, 0.65f), (32.1f, 0.42f), (45f, 0.5f), (59f, 0.55f), (66f, 0.45f), (74f, 0.6f), (75.1f, 0.4f) })
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
