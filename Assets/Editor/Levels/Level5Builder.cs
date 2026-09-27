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
    static readonly Color DarkGray = new Color(0.28f, 0.29f, 0.33f);
    static readonly Color Cyan = new Color(0.36f, 0.95f, 0.84f);
    static readonly Color Orange = new Color(1f, 0.55f, 0.18f);

    static Transform root;

    public static void Build()
    {
        try
        {
            Directory("Assets/Scenes/Levels");
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) != null) AssetDatabase.DeleteAsset(ScenePath);
            if (!AssetDatabase.CopyAsset("Assets/Scenes/SampleScene.unity", ScenePath)) throw new Exception("copy failed");
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
        Solid("Wall_Left", -11, -4, -8, 22);
        Solid("Floor_A", -11, -4, 6, 0);
        Solid("Trench_Bed", 6, -4, 9f, -0.35f);
        Lava("Lava_Trench", 6, -0.35f, 9f, -0.05f);
        Solid("Floor_B", 9f, -4, 29, 0);
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
        Solid("Gallery_Floor", 29, -4, 60, 3.5f);
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
        Solid("Floor_E", 60, -4, 106, 3.5f);
        Solid("Ceiling_E", 58, 20, 106, 22, DarkGray);
        Solid("Wall_Right", 103, -4, 106, 22);
        CyanBlock("Step_1", 69f, 6.0f);
        CyanBlock("Step_2", 73f, 8.5f);
        Solid("Catwalk_1", 75.5f, 10.2f, 86f, 11f);
        Solid("Catwalk_2", 88.2f, 10.2f, 103f, 11f);

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
        if (ssr != null) ssr.color = Orange;
        Lava("Lava_Pit_E", 76, 3.5f, 86, 4.1f);
        Solid("Pit_Bed", 76, 3.0f, 86, 3.5f);

        var door = new GameObject("ExitDoor");
        door.transform.SetParent(root);
        door.tag = "Finish";
        door.transform.position = new Vector3(99.5f, 11f + 2.55f, 0);
        door.transform.localScale = Vector3.one * 0.5f;
        var dsr = door.AddComponent<SpriteRenderer>();
        dsr.sprite = S("obj_exit_door_glow");
        dsr.sortingOrder = 2;
        var dcol = door.AddComponent<BoxCollider2D>();
        dcol.isTrigger = true;
        dcol.size = new Vector2(dsr.sprite.bounds.size.x * 0.6f, dsr.sprite.bounds.size.y * 0.9f);
        Deco("Exit_Glow", S("fx_crash"), new Vector2(99.5f, 13.5f), 0.35f, new Color(1, 1, 1, 0.12f), 1);

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
        var go = Block(name, x0, y0, x1, y1, c);
        go.layer = LayerMask.NameToLayer("Ground");
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
