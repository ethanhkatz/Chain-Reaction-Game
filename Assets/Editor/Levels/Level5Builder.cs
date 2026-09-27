using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Level 5 - "The Great Escape". Combined-mechanics finale, built entirely from code.
// Run: Tools/jam.sh run Level5Builder.Build
//
// Route (left to right, climbing):
//   A  cell block: spawn, shallow lava trench to hop
//   B  checkpoint: drag the ball over a button to drop the first laser gate
//   C  cracked boulder: smash it three times into stairs to climb onto the upper gallery
//   D  the Great Chain: one hit on the first pillar -> 5 dominoes -> button -> trapdoor drops a boulder
//      -> boulder lands on a second button -> final laser gate opens
//   E  the climb: cyan blocks up to a catwalk, leap the gap (the ceiling gives way behind you) to the exit
public static class Level5Builder
{
    const string ScenePath = "Assets/Scenes/Levels/Level5.unity";
    const string Cat = "Assets/Art/Catalog/";

    static readonly Color Gray = new Color(0.46f, 0.47f, 0.52f);
    static readonly Color DarkGray = new Color(0.22f, 0.23f, 0.26f); // non-walkable mass (terrain standard)
    static readonly Color Cyan = new Color(0.36f, 0.95f, 0.84f);
    static readonly Color Orange = new Color(1f, 0.55f, 0.18f);

    static Transform root;

    public static void Build()
    {
        try
        {
            Directory("Assets/Scenes/Levels");
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) != null)
            {
                // overwrite contents in place so the scene keeps its GUID (build settings reference it)
                System.IO.File.Copy("Assets/Scenes/SampleScene.unity", ScenePath, true);
                AssetDatabase.ImportAsset(ScenePath, ImportAssetOptions.ForceUpdate);
            }
            else if (!AssetDatabase.CopyAsset("Assets/Scenes/SampleScene.unity", ScenePath)) throw new Exception("copy failed");
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

            var keep = new HashSet<string> { "Main Camera", "Global Light 2D", "CinemachineCamera", "GameManager", "Canvas", "EventSystem", "Player", "Ball" };
            foreach (var go in scene.GetRootGameObjects())
                if (!keep.Contains(go.name)) UnityEngine.Object.DestroyImmediate(go);

            root = new GameObject("Level5").transform;
            BuildLevel();

            var cam = GameObject.Find("Main Camera").GetComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.07f, 0.07f, 0.08f);

            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene)) throw new Exception("save failed");
            Debug.Log("JAM: Level5 built");
            EditorApplication.Exit(0);
        }
        catch (Exception e)
        {
            Debug.LogError("JAM: Level5Builder failed: " + e);
            EditorApplication.Exit(1);
        }
    }

    static void BuildLevel()
    {
        // ---------- backdrop ----------
        var bg = S("env_background_prison_blur");
        for (int i = 0; i < 5; i++)
            Deco("Backdrop", bg, new Vector2(-6 + i * 27.3f, 8f), 1f, new Color(0.24f, 0.24f, 0.27f), -100);

        // ---------- A: cell block (floor top y=0) ----------
        Solid("Wall_Left", -11, -20, -8, 22, DarkGray);
        Solid("Floor_A", -11, -20, 6, 0);
        Solid("Trench_Bed", 6, -20, 9f, -0.35f);
        Lava("Lava_Trench", 6, -0.35f, 9f, -0.05f);
        Solid("Floor_B", 9f, -20, 29, 0);
        Solid("Ceiling_AB", -11, 8, 27, 10, DarkGray);
        Deco("Note", S("story_note_training"), new Vector2(-4.5f, 4.2f), 0.3f, new Color(0.8f, 0.8f, 0.8f), -5);
        Chain(new Vector2(3, 8), 3);
        Chain(new Vector2(12, 8), 2);

        // ---------- B: tutorial button + laser gate ----------
        var gate0 = LaserGate("Gate_Tutorial", 19.5f, 0f, 8f);
        Button("Button_Tutorial", 15.5f, 0f, gate0);

        // ---------- C: cracked boulder -> stairs up to the gallery (y=3.5) ----------
        var rock = Prefab("Assets/Prefabs/BreakableRockStair.prefab", new Vector2(27.75f, 1.81f));
        rock.name = "CrackedBoulder";
        rock.transform.localScale = new Vector3(-1, 1, 1); // stairs sprite climbs to the left; mirror it to climb right
        var rockShape = new SerializedObject(rock.GetComponent<RockShape>());
        rockShape.FindProperty("isStairs").boolValue = true;
        rockShape.FindProperty("matchSizeOnStairs").boolValue = true;
        rockShape.ApplyModifiedPropertiesWithoutUndo();
        Solid("Gallery_Floor", 29, -20, 60, 3.5f);
        Solid("Ceiling_Gallery", 27, 16, 58, 18, DarkGray);
        Solid("Pillar_CeilingStep", 27, 10, 29, 16, DarkGray);

        // ---------- D: the Great Chain ----------
        float[] dx = { 34f, 38.4f, 42.8f, 47.2f, 51.6f };
        for (int i = 0; i < dx.Length; i++)
        {
            var d = Prefab("Assets/Prefabs/TippyRock.prefab", new Vector2(dx[i], 3.5f + 2.6f));
            d.name = "Domino_" + (i + 1);
            d.layer = LayerMask.NameToLayer("Ground"); // so the player can walk and jump on the fallen pillars
            foreach (var sr in d.GetComponentsInChildren<SpriteRenderer>()) sr.color = Cyan;
        }
        // trapdoor shelf with the boulder, high above the second button
        Solid("Shelf", 58, 11.6f, 62f, 12.2f, DarkGray);
        var trap = Block("Trapdoor", 62f, 11.6f, 65f, 12.2f, Cyan);
        trap.layer = LayerMask.NameToLayer("Ground");
        var trapGate = trap.AddComponent<Gate>();
        SetGate(trapGate, new Vector2(3.2f, 0f), 0.35f, false);
        Solid("Shelf_Wall", 65f, 11.6f, 66f, 16f, DarkGray);
        var boulder = new GameObject("Boulder");
        boulder.transform.SetParent(root);
        boulder.transform.position = new Vector3(63.5f, 13.4f, 0);
        boulder.transform.localScale = Vector3.one * 0.35f;
        var bsr = boulder.AddComponent<SpriteRenderer>();
        bsr.sprite = S("obj_rock_cracked_1");
        bsr.color = Cyan;
        var brb = boulder.AddComponent<Rigidbody2D>();
        brb.mass = 3f;
        var bcol = boulder.AddComponent<CircleCollider2D>();
        bcol.radius = 2.1f;

        var finalGate = LaserGate("Gate_Final", 60.5f, 3.5f, 11.6f);
        Button("Button_Boulder", 63.5f, 3.5f, finalGate);
        Button("Button_Chain", 56f, 3.5f, trapGate);
        Chain(new Vector2(40, 16), 2);
        Chain(new Vector2(49, 16), 3);

        // ---------- E: the climb to the exit ----------
        Solid("Floor_E", 60, -20, 106, 3.5f);
        Solid("Ceiling_E", 58, 20, 103, 22, DarkGray);
        CyanBlock("Step_1", 68.5f, 5.3f);
        CyanBlock("Step_2", 72.3f, 7.2f);
        Solid("Catwalk_1", 75.5f, 8.2f, 86f, 9f);
        Solid("Catwalk_2", 88.2f, 8.2f, 103f, 9f);

        // the ceiling gives way as you leap the gap: stalactite falls through it and smashes the rock below
        var target = new GameObject("Rubble_Target");
        target.transform.SetParent(root);
        target.tag = "Rock";
        target.transform.position = new Vector3(87.1f, 3.5f + 1.6f, 0);
        target.transform.localScale = Vector3.one * 0.5f;
        var tsr = target.AddComponent<SpriteRenderer>();
        tsr.sprite = S("obj_rock_cracked_2");
        tsr.color = Gray;
        var tcol = target.AddComponent<BoxCollider2D>();
        tcol.size = tsr.sprite.bounds.size * 0.9f;
        var stal = Prefab("Assets/Prefabs/Stlactite.prefab", new Vector2(87.1f, 18.8f));
        stal.name = "Stalactite_Gap";
        stal.transform.localScale = Vector3.one * 1.3f;
        var ssr = stal.GetComponent<SpriteRenderer>();
        if (ssr != null) ssr.color = new Color(1f, 0.3f, 0.12f); // orange-red hazard
        Lava("Lava_Pit_E", 76, 3.5f, 86, 4.1f);
        Solid("Pit_Bed", 76, 3.0f, 86, 3.5f);

        var door = new GameObject("ExitDoor");
        door.transform.SetParent(root);
        door.tag = "Finish";
        door.transform.position = new Vector3(146.5f, 9f + 2.55f, 0);
        door.transform.localScale = Vector3.one * 0.5f;
        var dsr = door.AddComponent<SpriteRenderer>();
        dsr.sprite = S("obj_exit_door_glow");
        dsr.sortingOrder = -1; // below player/ball, above terrain
        var dcol = door.AddComponent<BoxCollider2D>();
        dcol.isTrigger = true;
        dcol.size = new Vector2(dsr.sprite.bounds.size.x * 0.6f, dsr.sprite.bounds.size.y * 0.9f);
        Deco("Exit_Glow", S("fx_crash"), new Vector2(146.5f, 11.5f), 0.35f, new Color(1, 1, 1, 0.12f), -3);

        BuildWardenArena();

        // ---------- spawn ----------
        var player = GameObject.Find("Player");
        var ball = GameObject.Find("Ball");
        // spawn by the left wall, ball to the right: holding Left for a moment parks you against the wall
        // (L5_SPAWN_X/L5_SPAWN_Y env vars move the spawn for testing a later section)
        float sx = -6f, sy = 1.7f;
        var ex = Environment.GetEnvironmentVariable("L5_SPAWN_X");
        var ey = Environment.GetEnvironmentVariable("L5_SPAWN_Y");
        if (!string.IsNullOrEmpty(ex)) sx = float.Parse(ex, System.Globalization.CultureInfo.InvariantCulture);
        if (!string.IsNullOrEmpty(ey)) sy = float.Parse(ey, System.Globalization.CultureInfo.InvariantCulture);
        player.transform.position = new Vector3(sx, sy, 0);
        ball.transform.position = new Vector3(sx + 2.5f, sy - 0.3f, 0);
        var cmFollow = GameObject.Find("CinemachineCamera");
        cmFollow.transform.position = new Vector3(sx, sy, cmFollow.transform.position.z);
        var mc = GameObject.Find("Main Camera");
        mc.transform.position = new Vector3(sx, sy, mc.transform.position.z);
    }

    // ---------- F: the Warden's arena (floor y=9, ceiling y=22.5) ----------
    // Three cyan cores, each cracked by a chain reaction, never by the ball directly:
    //   head     - ball onto the ledge button -> the ceiling stalactite drops on it
    //   shoulder - ball smashes the anchor post -> the cable snaps and the crusher drops on it
    //   knee     - ball topples the domino into it
    static void BuildWardenArena()
    {
        Solid("Arena_Floor", 103, -20, 150, 9);
        Solid("Arena_Lintel", 102, 20, 104, 22.5f, DarkGray);
        Solid("Arena_Ceiling", 102, 22.5f, 153, 25, DarkGray);
        Solid("Wall_Right", 150, -20, 153, 25, DarkGray);
        Solid("Boss_Ledge", 107, 9, 113, 10.5f);

        var cp = new GameObject("BossCheckpoint");
        cp.transform.SetParent(root);
        cp.transform.position = new Vector3(102.5f, 14f, 0);
        var cpc = cp.AddComponent<BoxCollider2D>();
        cpc.isTrigger = true;
        cpc.size = new Vector2(1f, 10f);
        cp.AddComponent<BossCheckpoint>().spawn = new Vector2(100f, 10.7f);

        var bossGo = new GameObject("Warden");
        bossGo.transform.SetParent(root);
        var boss = bossGo.AddComponent<WardenBoss>();
        // All Warden art is loaded by path from Assets/Art/Boss (catalog stand-ins until it lands) and sized by a
        // target world height, so the images can be swapped without touching code. Everything else - cores,
        // colliders, crusher, stalactite, domino - is placed from fractions of the body sprite's rect.
        var bodySprite = BossArt("warden_body", "obj_rock_cracked_1");
        var bodyRect = new Rect(130f, 9f, 0, 10f); // left, floor, width from aspect, target height
        bodyRect.width = bodyRect.height * bodySprite.bounds.size.x / bodySprite.bounds.size.y;
        var body = new GameObject("Warden_Body").transform;
        body.SetParent(bossGo.transform);
        body.position = new Vector3(bodyRect.center.x, bodyRect.y, 0);
        var bodyArt = Deco("Warden_Body_Art", bodySprite, bodyRect.center, bodyRect.height / bodySprite.bounds.size.y,
            bodySprite == S("obj_rock_cracked_1") ? new Color(0.55f, 0.55f, 0.58f) : Color.white, -4);
        bodyArt.transform.SetParent(body, true);
        // collision silhouette: full-width base (lower 60%), tower on the right 2/3 above it
        BodyBox(body, "Base", bodyRect, 0f, 0f, 1f, 0.6f);
        BodyBox(body, "Tower", bodyRect, 0.33f, 0.6f, 1f, 0.95f);

        var armSprite = BossArt("warden_arm", "haz_stalactite_gray");
        var arm = Deco("Warden_Arm", armSprite, BodyPoint(bodyRect, 0.08f, 0.85f), 3.2f / armSprite.bounds.size.y,
            armSprite == S("haz_stalactite_gray") ? Orange : Color.white, -3);
        arm.transform.SetParent(body, true);
        var tower = arm.transform;
        var eye = Deco("Warden_Eye", S("ball_small_a"), BodyPoint(bodyRect, 0.5f, 0.8f), 0.25f, Orange, -2);
        eye.transform.SetParent(body, true);
        Chain(new Vector2(bodyRect.x + bodyRect.width * 0.8f, 22.5f), 2);

        // cores (anchors as fractions of the body sprite: x from its left edge, y from its bottom)
        var coreSprite = BossArt("warden_core", "obj_cyan_block_small");
        var coreBroken = BossArt("warden_core_broken", "obj_cyan_block_small");
        Vector2 headAt = BodyPoint(bodyRect, 0.66f, 0.95f), shoulderAt = BodyPoint(bodyRect, 0.16f, 0.6f), kneeAt = BodyPoint(bodyRect, 0.0f, 0.22f);
        var coreHead = Core(body, "Core_Head", headAt + Vector2.up * 0.5f, coreSprite, coreBroken, boss);
        var coreShoulder = Core(body, "Core_Shoulder", shoulderAt + Vector2.up * 0.5f, coreSprite, coreBroken, boss);
        var coreKnee = Core(body, "Core_Knee", kneeAt, coreSprite, coreBroken, boss);

        // head: button on the ledge drops the stalactite hanging over the head core
        var stal = Deco("Warden_Stalactite", S("haz_stalactite_cyan"), new Vector2(headAt.x, 21.6f), 0.6f, Color.white, 2);
        var srb = stal.AddComponent<Rigidbody2D>();
        srb.mass = 2f;
        var scol = stal.AddComponent<BoxCollider2D>();
        scol.size = new Vector2(1.0f, 2.0f);
        var dropper = stal.AddComponent<WardenDropper>();
        stal.AddComponent<WardenStriker>().consumeOnHit = true;
        var btn = Button("Button_Warden", 110f, 10.5f, dropper);
        var bbc = btn.GetComponent<BoxCollider2D>(); // tall catch zone: the ball dragged or swung across the ledge presses it
        bbc.size = new Vector2(bbc.size.x * 1.6f, bbc.size.y * 2.5f);
        bbc.offset = new Vector2(0, bbc.size.y * 0.3f);

        // shoulder: anchor post holds the crusher by a cable over a ceiling pulley
        var crusher = Deco("Warden_Crusher", S("obj_cyan_platform"), new Vector2(shoulderAt.x, 20.4f), 0.22f, Color.white, 1);
        crusher.layer = LayerMask.NameToLayer("Ground");
        var crb = crusher.AddComponent<Rigidbody2D>();
        crb.bodyType = RigidbodyType2D.Kinematic;
        var ccol = crusher.AddComponent<BoxCollider2D>();
        ccol.size = new Vector2(S("obj_cyan_platform").bounds.size.x * 0.92f, S("obj_cyan_platform").bounds.size.y * 0.8f);
        var fp = crusher.AddComponent<FallingPlatform>();
        var fso = new SerializedObject(fp);
        fso.FindProperty("dropMass").floatValue = 6f;
        fso.FindProperty("impactFx").objectReferenceValue = S("fx_crash");
        fso.ApplyModifiedPropertiesWithoutUndo();
        crusher.AddComponent<WardenStriker>();

        var post = Deco("Anchor_Post", S("obj_cyan_pillar"), new Vector2(118f, 9f + 1.5f), 0.5f, Color.white, 1);
        post.layer = LayerMask.NameToLayer("Ground");
        var pcol = post.AddComponent<BoxCollider2D>();
        pcol.size = S("obj_cyan_pillar").bounds.size * 0.9f;
        var sup = post.AddComponent<BreakableSupport>();
        var sso = new SerializedObject(sup);
        sso.FindProperty("platform").objectReferenceValue = fp;
        sso.ApplyModifiedPropertiesWithoutUndo();
        var rope = new GameObject("Crusher_Cable");
        rope.transform.SetParent(root);
        rope.AddComponent<WardenRope>().support = sup;
        Cable(rope.transform, new Vector2(118f, 12f), new Vector2(118f, 22.3f));
        Cable(rope.transform, new Vector2(118f, 22.3f), new Vector2(shoulderAt.x, 22.3f));
        Cable(rope.transform, new Vector2(shoulderAt.x, 22.3f), new Vector2(shoulderAt.x, 21.1f));
        Deco("Pulley", S("ball_small_b"), new Vector2(118f, 22.2f), 0.12f, Gray, 0);
        Deco("Pulley", S("ball_small_b"), new Vector2(shoulderAt.x, 22.2f), 0.12f, Gray, 0);

        // knee: a domino standing in front of the machine
        var dom = Prefab("Assets/Prefabs/TippyRock.prefab", new Vector2(kneeAt.x - 5f, 9f + 2.63f));
        dom.name = "Warden_Domino";
        dom.layer = LayerMask.NameToLayer("Ground");
        foreach (var sr in dom.GetComponentsInChildren<SpriteRenderer>()) sr.color = Cyan;
        var ds = dom.AddComponent<WardenStriker>();
        ds.respawnIfWasted = true;
        ds.ignorePlayer = true; // walk past it and the trailing ball knocks it into the machine

        // HP pips
        var pips = new SpriteRenderer[3];
        for (int i = 0; i < 3; i++)
        {
            var pip = Deco("Warden_Pip_" + (i + 1), S("obj_cyan_block_small"), new Vector2(bodyRect.center.x - 0.9f + i * 0.9f, bodyRect.yMax + 1.3f), 0.28f, Color.white, 4);
            pip.transform.SetParent(body, true);
            pips[i] = pip.GetComponent<SpriteRenderer>();
        }

        var exitGate = LaserGate("Gate_Exit", Mathf.Max(144f, bodyRect.xMax + 1.5f), 9f, 22.5f);

        var so = new SerializedObject(boss);
        var cores = so.FindProperty("cores");
        cores.arraySize = 3;
        cores.GetArrayElementAtIndex(0).objectReferenceValue = coreHead;
        cores.GetArrayElementAtIndex(1).objectReferenceValue = coreShoulder;
        cores.GetArrayElementAtIndex(2).objectReferenceValue = coreKnee;
        var pp = so.FindProperty("pips");
        pp.arraySize = 3;
        for (int i = 0; i < 3; i++) pp.GetArrayElementAtIndex(i).objectReferenceValue = pips[i];
        so.FindProperty("body").objectReferenceValue = body;
        so.FindProperty("tower").objectReferenceValue = tower;
        so.FindProperty("bodyArt").objectReferenceValue = bodyArt.GetComponent<SpriteRenderer>();
        so.FindProperty("defeatedSprite").objectReferenceValue = BossArt("warden_defeated", "obj_gray_stairs_cracked");
        so.FindProperty("laserStartX").floatValue = kneeAt.x - 0.6f;
        so.FindProperty("arenaMaxX").floatValue = kneeAt.x - 1f;
        so.FindProperty("eye").objectReferenceValue = eye.GetComponent<SpriteRenderer>();
        so.FindProperty("exitGate").objectReferenceValue = exitGate;
        so.FindProperty("ledgeX").vector2Value = new Vector2(107, 113);
        so.FindProperty("ledgeTop").floatValue = 10.5f;
        so.FindProperty("shardSprite").objectReferenceValue = S("haz_stalactite_gray");
        so.FindProperty("warnSprite").objectReferenceValue = FirstSprite("Assets/Images/Ground.png");
        so.FindProperty("laserSprite").objectReferenceValue = S("haz_laser_gate_2");
        so.FindProperty("crashSprite").objectReferenceValue = S("fx_crash");
        so.ApplyModifiedPropertiesWithoutUndo();
    }


    static Sprite BossArt(string file, string fallback)
    {
        var sp = AssetDatabase.LoadAllAssetsAtPath("Assets/Art/Boss/" + file + ".png").OfType<Sprite>().FirstOrDefault();
        return sp != null ? sp : S(fallback);
    }

    static Vector2 BodyPoint(Rect r, float fx, float fy) => new Vector2(r.x + r.width * fx, r.y + r.height * fy);

    static void BodyBox(Transform parent, string name, Rect r, float fx0, float fy0, float fx1, float fy1)
    {
        var go = new GameObject("Warden_" + name);
        go.transform.SetParent(parent, true);
        Vector2 a = BodyPoint(r, fx0, fy0), b = BodyPoint(r, fx1, fy1);
        go.transform.position = (a + b) / 2;
        go.AddComponent<BoxCollider2D>().size = b - a;
    }

    static WardenCore Core(Transform parent, string name, Vector2 pos, Sprite sprite, Sprite broken, WardenBoss boss)
    {
        var go = Deco(name, sprite, pos, 1.1f / sprite.bounds.size.y, Color.white, -1);
        go.transform.SetParent(parent, true);
        var bc = go.AddComponent<BoxCollider2D>();
        bc.size = sprite.bounds.size * 0.9f;
        var core = go.AddComponent<WardenCore>();
        core.boss = boss;
        core.crackFx = S("fx_crash");
        core.brokenSprite = broken;
        return core;
    }

    static void Cable(Transform parent, Vector2 a, Vector2 b)
    {
        var go = new GameObject("Cable");
        go.transform.SetParent(parent);
        go.transform.position = (a + b) / 2;
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = FirstSprite("Assets/Images/Ground.png");
        sr.drawMode = SpriteDrawMode.Tiled;
        var d = b - a;
        sr.size = new Vector2(Mathf.Max(Mathf.Abs(d.x), 0.14f), Mathf.Max(Mathf.Abs(d.y), 0.14f));
        sr.color = new Color(0.08f, 0.08f, 0.09f);
        sr.sortingOrder = -2;
    }

    // ---------------- helpers ----------------
    static void Directory(string path)
    {
        if (!AssetDatabase.IsValidFolder(path)) AssetDatabase.CreateFolder("Assets/Scenes", "Levels");
    }

    static Sprite S(string name)
    {
        var s = AssetDatabase.LoadAssetAtPath<Sprite>(Cat + name + ".png");
        if (s == null) throw new Exception("missing sprite " + name);
        return s;
    }

    static Sprite FirstSprite(string path) =>
        AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().First();

    static GameObject Prefab(string path, Vector2 pos)
    {
        var p = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (p == null) throw new Exception("missing prefab " + path);
        var go = (GameObject)PrefabUtility.InstantiatePrefab(p);
        go.transform.SetParent(root);
        go.transform.position = pos;
        return go;
    }

    static GameObject Deco(string name, Sprite s, Vector2 pos, float scale, Color c, int order)
    {
        var go = new GameObject(name);
        go.transform.SetParent(root);
        go.transform.position = new Vector3(pos.x, pos.y, order <= -50 ? 5f : 0f);
        go.transform.localScale = Vector3.one * scale;
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = s;
        sr.color = c;
        sr.sortingOrder = order;
        return go;
    }

    // Tiled gray block with a sprite that tiles (x0,y0)-(x1,y1).
    static GameObject Block(string name, float x0, float y0, float x1, float y1, Color c)
    {
        var go = new GameObject(name);
        go.transform.SetParent(root);
        go.transform.position = new Vector3((x0 + x1) / 2, (y0 + y1) / 2, 0);
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = FirstSprite("Assets/Images/Ground.png");
        sr.drawMode = SpriteDrawMode.Tiled;
        sr.size = new Vector2(x1 - x0, y1 - y0);
        sr.color = c;
        var bc = go.AddComponent<BoxCollider2D>();
        bc.size = sr.size;
        return go;
    }

    static GameObject Solid(string name, float x0, float y0, float x1, float y1) => Solid(name, x0, y0, x1, y1, Gray);

    static GameObject Solid(string name, float x0, float y0, float x1, float y1, Color c)
    {
        // Terrain standard: the whole block is dark mass at order -10; walkable blocks (default Gray) get a
        // 1-unit strip of untinted riveted floor art along their top edge so the playable surface pops.
        bool walkable = c == Gray;
        var go = Block(name, x0, y0, x1, y1, DarkGray);
        go.GetComponent<SpriteRenderer>().sortingOrder = -10;
        go.layer = LayerMask.NameToLayer("Ground");
        if (walkable)
        {
            float h = Mathf.Min(1f, y1 - y0);
            var top = new GameObject("Surface");
            top.transform.SetParent(go.transform);
            top.transform.position = new Vector3((x0 + x1) / 2, y1 - h / 2, 0);
            var tsr = top.AddComponent<SpriteRenderer>();
            tsr.sprite = FirstSprite("Assets/Images/Ground.png");
            tsr.drawMode = SpriteDrawMode.Tiled;
            tsr.size = new Vector2(x1 - x0, h);
            tsr.sortingOrder = -9;
        }
        var rb = go.AddComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Static;
        return go;
    }

    static void Lava(string name, float x0, float y0, float x1, float y1)
    {
        var go = new GameObject(name);
        go.transform.SetParent(root);
        go.layer = LayerMask.NameToLayer("Lava");
        go.transform.position = new Vector3((x0 + x1) / 2, (y0 + y1) / 2, 0);
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = FirstSprite("Assets/Images/Lava.png");
        sr.drawMode = SpriteDrawMode.Tiled;
        sr.size = new Vector2(x1 - x0, y1 - y0);
        sr.color = Orange;
        sr.sortingOrder = 3;
        var bc = go.AddComponent<BoxCollider2D>();
        bc.size = sr.size;
        bc.isTrigger = true;
    }

    static void CyanBlock(string name, float cx, float top)
    {
        var s = S("obj_cyan_platform");
        var go = new GameObject(name);
        go.transform.SetParent(root);
        go.layer = LayerMask.NameToLayer("Ground");
        go.transform.localScale = Vector3.one * 0.5f;
        var size = s.bounds.size;
        go.transform.position = new Vector3(cx, top - size.y * 0.25f, 0);
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = s;
        var bc = go.AddComponent<BoxCollider2D>();
        bc.size = new Vector2(size.x * 0.92f, size.y);
        var rb = go.AddComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Static;
    }

    static void Chain(Vector2 top, int links)
    {
        for (int i = 0; i < links; i++)
            Deco("ChainLink", S(i % 2 == 0 ? "obj_chain_link_1" : "obj_chain_link_2"),
                top + new Vector2(0, -0.6f - i * 0.65f), 0.4f, new Color(0.55f, 0.56f, 0.6f), -10)
                .transform.rotation = Quaternion.Euler(0, 0, i % 2 == 0 ? 90 : 0);
    }

    // Laser gate spanning floorY..ceilY at x. Blocks until activated.
    static Gate LaserGate(string name, float x, float floorY, float ceilY)
    {
        var s = S("haz_laser_gate_1");
        var go = new GameObject(name);
        go.transform.SetParent(root);
        float h = ceilY - floorY;
        go.transform.localScale = new Vector3(0.5f, h / s.bounds.size.y, 1);
        go.transform.position = new Vector3(x, floorY + h / 2, 0);
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = s;
        sr.sortingOrder = 2;
        var bc = go.AddComponent<BoxCollider2D>();
        bc.size = s.bounds.size;
        var g = go.AddComponent<Gate>();
        SetGate(g, new Vector2(0, 1.5f), 0.6f, true);
        return g;
    }

    static void SetGate(Gate g, Vector2 offset, float time, bool fade)
    {
        var so = new SerializedObject(g);
        so.FindProperty("openOffset").vector2Value = offset;
        so.FindProperty("openTime").floatValue = time;
        so.FindProperty("fadeOut").boolValue = fade;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    static PressButton Button(string name, float x, float floorY, MonoBehaviour target)
    {
        var s = S("obj_button_cyan");
        var go = new GameObject(name);
        go.transform.SetParent(root);
        go.transform.localScale = Vector3.one * 0.5f;
        go.transform.position = new Vector3(x, floorY + s.bounds.size.y * 0.25f, 0);
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = s;
        sr.sortingOrder = 1;
        var bc = go.AddComponent<BoxCollider2D>();
        bc.isTrigger = true;
        bc.size = new Vector2(s.bounds.size.x * 1.4f, s.bounds.size.y);
        var pb = go.AddComponent<PressButton>();
        var so = new SerializedObject(pb);
        var t = so.FindProperty("targets");
        t.arraySize = 1;
        t.GetArrayElementAtIndex(0).objectReferenceValue = target;
        so.FindProperty("pressedSprite").objectReferenceValue = S("obj_button_red");
        so.ApplyModifiedPropertiesWithoutUndo();
        return pb;
    }
}
