using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

// Builds Assets/Scenes/Levels/Level3.unity ("Falling platforms"). Re-run freely; this script is the source of truth.
//   Tools/jam.sh run Level3Builder.Build
// Beats: (1) smash a pillar so a drawbridge slab falls across the lava, (2) smash a pillar so a platform drops onto
// a high button that opens the laser gate, (3) smash the latch so a trapdoor drops you down the shaft, (4) exit.
public static class Level3Builder
{
    const string ScenePath = "Assets/Scenes/Levels/Level3.unity";
    const string Catalog = "Assets/Art/Catalog/";
    const string GrayPath = "Assets/Art/Level3/l3_prison_block.png";

    static readonly HashSet<string> Keep = new HashSet<string>
        { "Main Camera", "Global Light 2D", "CinemachineCamera", "GameManager", "Canvas", "EventSystem", "Player", "Ball" };

    static int groundLayer, lavaLayer;
    static Sprite gray;
    static Transform root;
    static PhysicsMaterial2D slick;
    const string SlickPath = "Assets/Art/Level3/l3_slick.physicsMaterial2D";

    public static void Build()
    {
        try
        {
            Directory.CreateDirectory("Assets/Scenes/Levels");
            gray = EnsureGraySprite();
            slick = AssetDatabase.LoadAssetAtPath<PhysicsMaterial2D>(SlickPath);
            if (slick == null)
            {
                // Frictionless walls so the player can't stick to a wall by holding into it mid-fall.
                slick = new PhysicsMaterial2D("l3_slick") { friction = 0f, bounciness = 0f };
                AssetDatabase.CreateAsset(slick, SlickPath);
            }
            // Overwrite in place when the scene exists so its GUID (build settings reference) stays stable.
            if (File.Exists(ScenePath))
            {
                File.Copy("Assets/Scenes/SampleScene.unity", ScenePath, true);
                AssetDatabase.ImportAsset(ScenePath, ImportAssetOptions.ForceUpdate);
            }
            else if (!AssetDatabase.CopyAsset("Assets/Scenes/SampleScene.unity", ScenePath)) throw new Exception("copy failed");
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            groundLayer = LayerMask.NameToLayer("Ground");
            lavaLayer = LayerMask.NameToLayer("Lava");

            GameObject player = null, ball = null;
            foreach (var go in scene.GetRootGameObjects())
            {
                if (!Keep.Contains(go.name)) { UnityEngine.Object.DestroyImmediate(go); continue; }
                if (go.name == "Player") player = go;
                if (go.name == "Ball") ball = go;
            }
            if (player == null || ball == null) throw new Exception("Player/Ball missing");

            root = new GameObject("Level3").transform;
            BuildLevel();

            var spawn = new Vector3(-8f, 1.6f, 0f);
            player.transform.position = spawn;
            ball.transform.position = spawn + new Vector3(-2.5f, -0.2f, 0f);
            foreach (var go in scene.GetRootGameObjects())
                if (go.name == "Main Camera" || go.name == "CinemachineCamera")
                    go.transform.position = new Vector3(spawn.x, spawn.y, go.transform.position.z);

            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene)) throw new Exception("save failed");
            AssetDatabase.SaveAssets();
            Debug.Log("JAM: Level3 built -> " + ScenePath);
            EditorApplication.Exit(0);
        }
        catch (Exception e)
        {
            Debug.LogError("JAM: Level3 build failed: " + e);
            EditorApplication.Exit(1);
        }
    }

    static void BuildLevel()
    {
        // Backdrop: the blurred prison, far behind and darkened.
        var bg = Sprite("Background", S("env_background_prison_blur"), new Vector2(28f, -2f), 4.2f, -100);
        bg.color = new Color(0.32f, 0.34f, 0.38f);
        bg.transform.position += Vector3.forward * 20f;

        // ---- Static prison shell ----
        Block("LeftWall", -18, -14, -4, 17);
        Block("Ceiling", -18, 49, 14, 17);
        Block("StartFloor", -18, 9.5f, -4, 0);
        Block("PitFloor", 9.5f, 20.5f, -4, -3);
        Block("RoomFloor", 20.5f, 40, -4, 0);
        Block("ShaftLeftWall", 36, 40, -20, -4);
        Block("ShaftRightWall", 46, 49, -10, 14);
        Block("ShaftLedge1", 40, 41.8f, -7.6f, -7);
        Block("ShaftLedge2", 40, 42.5f, -12.6f, -12);
        Block("CorridorFloor", 36, 74, -24, -16);
        Block("CorridorCeiling", 46, 74, -10, -8);
        Block("EndWall", 72, 74, -16, -8);
        Block("GateHeader", 33.5f, 35.5f, 7.3f, 14);
        Block("ButtonLedge", 30.5f, 33.5f, 7.3f, 8f);

        // Lava pit under the drawbridge.
        Lava("LavaPit", 9.5f, 20.5f, -3f, -1.2f);

        var crash = S("fx_crash");

        // ---- Beat 1: drawbridge over the lava ----
        // Upright slab hinged at the pit's left edge; a braced pillar holds it. Smash the pillar and it falls across.
        const float L = 11.4f, T = 1.35f;
        var bridge = Platform("Drawbridge", new Vector2(9.5f + T / 2f, L / 2f), 90f, L, T);
        SetPlatform(bridge, FallingPlatform.Mode.Hinge, new Vector2(9.5f, 0f), 0f, crash);
        var p1 = Pillar("Support_Drawbridge", 6f, 0f, 5.2f, bridge);
        var brace = Tiled("Brace", S("obj_cyan_block_small"), root, new Vector2(7.75f, 4.9f), new Vector2(3.4f, 0.55f), 0.3f, -1);
        brace.transform.SetParent(p1.transform, true);

        // ---- Beat 2: chain reaction -> platform drops on the high button -> laser gate opens ----
        var gateGo = new GameObject("LaserGate");
        gateGo.transform.SetParent(root);
        gateGo.transform.position = new Vector3(34.5f, 3.65f, 0f);
        gateGo.transform.localScale = new Vector3(1f, 7.3f / 6.07f, 1f);
        var gsr = gateGo.AddComponent<SpriteRenderer>();
        gsr.sprite = S("haz_laser_gate_1");
        gsr.sortingOrder = 1;
        gateGo.AddComponent<BoxCollider2D>();
        var gate = gateGo.AddComponent<Gate>();
        SetField(gate, "openOffset", new Vector2(0f, 3f));

        var btn = new GameObject("PressButton");
        btn.transform.SetParent(root);
        btn.transform.position = new Vector3(32.4f, 8f + 0.68f, 0f);
        btn.transform.localScale = Vector3.one * 0.4f;
        var bsr = btn.AddComponent<SpriteRenderer>();
        bsr.sprite = S("obj_button_cyan");
        bsr.sortingOrder = 1;
        btn.AddComponent<BoxCollider2D>().isTrigger = true;
        var press = btn.AddComponent<PressButton>();
        var so = new SerializedObject(press);
        var targets = so.FindProperty("targets");
        targets.arraySize = 1;
        targets.GetArrayElementAtIndex(0).objectReferenceValue = gate;
        so.FindProperty("pressedSprite").objectReferenceValue = S("obj_button_red");
        so.ApplyModifiedPropertiesWithoutUndo();

        var dropper = Platform("DropPlatform", new Vector2(31.15f, 10.4f + T / 2f), 0f, 4.3f, T);
        SetPlatform(dropper, FallingPlatform.Mode.Drop, Vector2.zero, 0f, crash);
        Pillar("Support_DropPlatform", 29.8f, 0f, 10.4f, dropper);

        // ---- Beat 3: trapdoor over the shaft, latched by a pillar; smash it and drop down the prison ----
        var trap = Platform("Trapdoor", new Vector2(43f, -T / 2f), 0f, 6f, T);
        SetPlatform(trap, FallingPlatform.Mode.Hinge, new Vector2(46f, -T / 2f), 90f, crash);
        Pillar("Support_Trapdoor", 38.2f, 0f, 3.2f, trap);

        // ---- Beat 4: exit at the bottom corridor ----
        var exit = new GameObject("ExitDoor");
        exit.tag = "Finish";
        exit.transform.SetParent(root);
        exit.transform.position = new Vector3(68f, -16f + 2.58f, 0f);
        exit.transform.localScale = Vector3.one * 0.5f;
        var esr = exit.AddComponent<SpriteRenderer>();
        esr.sprite = S("obj_exit_door_glow");
        esr.sortingOrder = 0;
        var ec = exit.AddComponent<BoxCollider2D>();
        ec.isTrigger = true;
        ec.size = new Vector2(3f, 9f);
    }

    // ---------- helpers ----------
    static Sprite S(string name)
    {
        var s = AssetDatabase.LoadAssetAtPath<Sprite>(Catalog + name + ".png");
        if (s == null) throw new Exception("missing sprite " + name);
        return s;
    }

    static SpriteRenderer Sprite(string name, Sprite sprite, Vector2 pos, float scale, int order)
    {
        var go = new GameObject(name);
        go.transform.SetParent(root);
        go.transform.position = pos;
        go.transform.localScale = Vector3.one * scale;
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.sortingOrder = order;
        return sr;
    }

    static SpriteRenderer Tiled(string name, Sprite sprite, Transform parent, Vector2 pos, Vector2 worldSize, float tileScale, int order)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = pos;
        go.transform.localScale = Vector3.one * tileScale;
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.drawMode = SpriteDrawMode.Tiled;
        sr.tileMode = SpriteTileMode.Continuous;
        sr.size = worldSize / tileScale;
        sr.sortingOrder = order;
        return sr;
    }

    static void Block(string name, float x0, float x1, float y0, float y1)
    {
        var go = new GameObject(name);
        go.layer = groundLayer;
        go.transform.SetParent(root);
        go.transform.position = new Vector3((x0 + x1) / 2f, (y0 + y1) / 2f, 0f);
        var size = new Vector2(x1 - x0, y1 - y0);
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = gray;
        sr.drawMode = SpriteDrawMode.Sliced;
        sr.size = size;
        sr.sortingOrder = 0;
        var bc = go.AddComponent<BoxCollider2D>();
        bc.size = size;
        bc.sharedMaterial = slick;
    }

    static void Lava(string name, float x0, float x1, float y0, float y1)
    {
        var sr = Tiled(name, S("haz_lava_tile"), root, new Vector2((x0 + x1) / 2f, (y0 + y1) / 2f), new Vector2(x1 - x0, y1 - y0), 0.5f, 1);
        sr.gameObject.layer = lavaLayer;
        var c = sr.gameObject.AddComponent<BoxCollider2D>();
        c.isTrigger = true;
        c.size = sr.size;
    }

    static FallingPlatform Platform(string name, Vector2 center, float rotation, float length, float thickness)
    {
        float s = thickness / 5.6f; // obj_cyan_platform is 9.44 x 5.6 at 100 PPU
        var sr = Tiled(name, S("obj_cyan_platform"), root, center, new Vector2(length, thickness), s, 1);
        var go = sr.gameObject;
        go.layer = groundLayer;
        go.transform.rotation = Quaternion.Euler(0, 0, rotation);
        var pc = go.AddComponent<BoxCollider2D>();
        pc.size = sr.size;
        pc.sharedMaterial = slick;
        var rb = go.AddComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;
        return go.AddComponent<FallingPlatform>();
    }

    static void SetPlatform(FallingPlatform p, FallingPlatform.Mode mode, Vector2 pivot, float targetAngle, Sprite fx)
    {
        var so = new SerializedObject(p);
        so.FindProperty("mode").enumValueIndex = (int)mode;
        so.FindProperty("hingePivot").vector2Value = pivot;
        so.FindProperty("targetAngle").floatValue = targetAngle;
        so.FindProperty("impactFx").objectReferenceValue = fx;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    static BreakableSupport Pillar(string name, float x, float yBottom, float height, FallingPlatform platform)
    {
        var sr = Tiled(name, S("obj_cyan_pillar"), root, new Vector2(x, yBottom + height / 2f), new Vector2(0.935f, height), 0.5f, -1);
        var c = sr.gameObject.AddComponent<BoxCollider2D>();
        c.size = new Vector2(sr.size.x * 0.85f, sr.size.y);
        var sup = sr.gameObject.AddComponent<BreakableSupport>();
        SetField(sup, "platform", platform);
        return sup;
    }

    static void SetField(UnityEngine.Object o, string field, object value)
    {
        var so = new SerializedObject(o);
        var p = so.FindProperty(field);
        if (value is Vector2 v) p.vector2Value = v;
        else p.objectReferenceValue = (UnityEngine.Object)value;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    // A sliced gray prison block (charcoal-outlined, a few cracks), generated once and committed.
    static Sprite EnsureGraySprite()
    {
        if (!File.Exists(GrayPath))
        {
            Directory.CreateDirectory(Path.GetDirectoryName(GrayPath));
            const int N = 128, B = 6;
            var tex = new Texture2D(N, N, TextureFormat.RGBA32, false);
            var fill = new Color(0.42f, 0.44f, 0.46f);
            var edge = new Color(0.07f, 0.07f, 0.08f);
            var hi = new Color(0.52f, 0.54f, 0.56f);
            for (int y = 0; y < N; y++)
                for (int x = 0; x < N; x++)
                {
                    bool border = x < B || y < B || x >= N - B || y >= N - B;
                    bool bevel = !border && (y >= N - B - 4);
                    tex.SetPixel(x, y, border ? edge : bevel ? hi : fill);
                }
            tex.Apply();
            File.WriteAllBytes(GrayPath, tex.EncodeToPNG());
            AssetDatabase.ImportAsset(GrayPath);
            var ti = (TextureImporter)AssetImporter.GetAtPath(GrayPath);
            ti.spriteBorder = new Vector4(12, 12, 12, 12);
            var settings = new TextureImporterSettings();
            ti.ReadTextureSettings(settings);
            settings.spriteMeshType = SpriteMeshType.FullRect;
            ti.SetTextureSettings(settings);
            ti.SaveAndReimport();
        }
        var s = AssetDatabase.LoadAssetAtPath<Sprite>(GrayPath);
        if (s == null) throw new Exception("gray sprite missing");
        return s;
    }
}
