using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Builds Assets/Scenes/Levels/Level2.unity ("Rocks") from SampleScene. Re-run freely; the builder is the source of truth.
// Beats: (1) smash the cracked rock with the ball, push a piece into the pit onto the button -> laser gate opens;
//        (2) knock the first tippy rock -> dominoes fall across the lava trench as a bridge; (3) exit door.
public static class Level2Builder
{
    const string ScenePath = "Assets/Scenes/Levels/Level2.unity";
    const string Cat = "Assets/Art/Catalog/";
    const float Ceil = 7f;          // corridor ceiling (floor top is y = 0); player is ~3.2 tall, jumps ~8
    static readonly Color Cyan = new Color(0.36f, 0.95f, 0.84f);
    static readonly Color RockCyan = new Color(0.55f, 1f, 0.9f);
    static readonly Color Wall = new Color(0.55f, 0.36f, 0.4f);
    static readonly Color Orange = new Color(1f, 0.55f, 0.15f);

    static Sprite groundSprite, lavaSprite;
    static Transform root;

    public static void Build()
    {
        try
        {
            System.IO.Directory.CreateDirectory("Assets/Scenes/Levels");
            AssetDatabase.DeleteAsset(ScenePath);
            if (!AssetDatabase.CopyAsset("Assets/Scenes/SampleScene.unity", ScenePath)) throw new Exception("copy failed");
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

            var lavaGo = GameObject.Find("Lava");
            lavaSprite = lavaGo.GetComponent<SpriteRenderer>().sprite;
            var floorPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Floor.prefab");
            groundSprite = floorPrefab.GetComponent<SpriteRenderer>().sprite;

            foreach (var go in scene.GetRootGameObjects())
            {
                var n = go.name;
                if (n.StartsWith("Floor") || n.StartsWith("TippyRock") || n == "Lava" || n == "Door")
                    UnityEngine.Object.DestroyImmediate(go);
            }

            root = new GameObject("Level2").transform;
            var cam = Camera.main;
            cam.backgroundColor = new Color(0.11f, 0.11f, 0.13f);
            cam.clearFlags = CameraClearFlags.SolidColor;

            // ---- backdrop ----
            var bg = Sprite(Cat + "env_background_prison_blur.png", "Background", new Vector2(32, 6), 2.6f, new Color(0.3f, 0.3f, 0.34f), -100);
            bg.transform.position = new Vector3(32, 6, 10);
            var bg2 = Sprite(Cat + "env_background_prison_blur.png", "Background2", new Vector2(-30, 6), 2.6f, new Color(0.3f, 0.3f, 0.34f), -100);
            bg2.transform.position = new Vector3(-36, 6, 10);
            bg2.GetComponent<SpriteRenderer>().flipX = true;
            var bg3 = Sprite(Cat + "env_background_prison_blur.png", "Background3", new Vector2(100, 6), 2.6f, new Color(0.3f, 0.3f, 0.34f), -100);
            bg3.transform.position = new Vector3(100, 6, 10);
            bg3.GetComponent<SpriteRenderer>().flipX = true;

            // ---- shell: floors, ceiling, walls ----
            Block("Floor_Start", -12, 19f, -8, 0);             // spawn + cracked rock
            Block("Floor_PitToLava", 24f, 36, -8, 0);          // pit x 19..24
            Block("PitBed", 19f, 24f, -8, -6f);
            Block("TrenchBed", 36, 54, -8, -0.6f);             // lava trench x 36..54
            Block("Floor_End", 54, 76, -8, 0);
            Block("Ceiling", -12, 76, Ceil, Ceil + 12);
            Block("Wall_Left", -14, -10, -8, Ceil + 12);
            Block("Wall_Right", 72, 76, -8, Ceil + 12);

            // ---- beat 1: cracked rock -> piece into pit -> button -> gate ----
            var big = Sprite(Cat + "obj_rock_cracked_1.png", "CrackedRock", new Vector2(10, 0), 0.8f, RockCyan, 1);
            var bigSr = big.GetComponent<SpriteRenderer>();
            big.transform.position = new Vector3(10, bigSr.bounds.extents.y, 0);
            big.layer = LayerMask.NameToLayer("Rock");
            big.tag = "Rock";
            big.AddComponent<BoxCollider2D>().size = bigSr.sprite.bounds.size;
            big.AddComponent<Rigidbody2D>().bodyType = RigidbodyType2D.Kinematic;
            var split = big.AddComponent<SplittingRock>();
            var so = new SerializedObject(split);
            so.FindProperty("breakSpeed").floatValue = 4f;
            var ps = so.FindProperty("pieceSprites");
            ps.arraySize = 3;
            ps.GetArrayElementAtIndex(0).objectReferenceValue = Load(Cat + "obj_rock_cracked_3.png");
            ps.GetArrayElementAtIndex(1).objectReferenceValue = Load(Cat + "obj_rock_cracked_2.png");
            ps.GetArrayElementAtIndex(2).objectReferenceValue = Load(Cat + "obj_rock_cracked_3.png");
            so.FindProperty("pieceCount").intValue = 3;
            so.FindProperty("pieceScale").floatValue = 0.34f;
            so.FindProperty("pieceMass").floatValue = 1f;
            so.FindProperty("crashSprite").objectReferenceValue = Load(Cat + "fx_crash.png");
            so.FindProperty("crashScale").floatValue = 0.35f;
            so.ApplyModifiedPropertiesWithoutUndo();

            // pit: lava at the bottom, button trigger just above it (out of the ball's reach from the ledge)
            Lava("PitLava", 19f, 24f, -6f, -4.8f);
            var button = Sprite(Cat + "obj_button_cyan.png", "PitButton", new Vector2(21.5f, -6f), 0.6f, Color.white, 3);
            var bsr = button.GetComponent<SpriteRenderer>();
            button.transform.position = new Vector3(21.5f, -6f + bsr.bounds.extents.y, 0);
            var btrig = button.AddComponent<BoxCollider2D>();
            btrig.isTrigger = true;
            btrig.size = new Vector2(5f / 0.6f, bsr.sprite.bounds.size.y);

            // laser gate: floor to ceiling
            var gate = Sprite(Cat + "haz_laser_gate_1.png", "LaserGate", new Vector2(30, Ceil * 0.5f), 1f, Color.white, 2);
            var gsr = gate.GetComponent<SpriteRenderer>();
            gate.transform.localScale = new Vector3(1f, Ceil / gsr.sprite.bounds.size.y, 1f);
            gate.AddComponent<BoxCollider2D>().size = gsr.sprite.bounds.size;
            var gateComp = gate.AddComponent<Gate>();
            var gso = new SerializedObject(gateComp);
            gso.FindProperty("openOffset").vector2Value = new Vector2(0, 2f);
            gso.ApplyModifiedPropertiesWithoutUndo();

            var pb = button.AddComponent<PressButton>();
            var pso = new SerializedObject(pb);
            var tg = pso.FindProperty("targets");
            tg.arraySize = 1;
            tg.GetArrayElementAtIndex(0).objectReferenceValue = gateComp;
            pso.FindProperty("pressedSprite").objectReferenceValue = Load(Cat + "obj_button_red.png");
            pso.FindProperty("minMass").floatValue = 0.8f;
            pso.ApplyModifiedPropertiesWithoutUndo();

            // cyan "wire" from pit up to the gate so the link reads
            var wire = Sprite("", "GateWire", new Vector2(26, Ceil - 0.25f), 1f, Cyan * new Color(1, 1, 1, 0.6f), -5, groundSprite);
            var wsr = wire.GetComponent<SpriteRenderer>();
            wsr.drawMode = SpriteDrawMode.Tiled; wsr.size = new Vector2(9f, 0.15f);
            wire.transform.position = new Vector3(25.5f, Ceil - 0.2f, 0);
            var wire2 = Sprite("", "GateWireDown", new Vector2(21, 0), 1f, Cyan * new Color(1, 1, 1, 0.6f), -5, groundSprite);
            var w2 = wire2.GetComponent<SpriteRenderer>();
            w2.drawMode = SpriteDrawMode.Tiled; w2.size = new Vector2(0.15f, Ceil);
            wire2.transform.position = new Vector3(21.5f, Ceil * 0.5f, 0);

            // ---- beat 2: domino tippy rocks across the lava trench ----
            Lava("TrenchLava", 36, 54, -0.6f, -0.15f);
            var tippyPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/TippyRock.prefab");
            float[] xs = { 33.8f, 37.3f, 40.8f, 44.3f, 47.8f, 51.3f };
            for (int i = 0; i < xs.Length; i++)
            {
                float surface = xs[i] < 36 ? 0f : -0.6f;
                var t = (GameObject)PrefabUtility.InstantiatePrefab(tippyPrefab, root);
                t.name = "TippyRock_" + i;
                t.transform.position = new Vector3(xs[i], surface + 2.62f, 0);
                var hinge = t.GetComponent<HingeJoint2D>();
                hinge.autoConfigureConnectedAnchor = false;
                hinge.connectedAnchor = new Vector2(xs[i], surface + 2.62f - 0.42f * t.transform.localScale.y);
                // dress the plain block as a cyan pillar; hide the duplicate child sprite
                foreach (var csr in t.GetComponentsInChildren<SpriteRenderer>()) csr.enabled = false;
                var art = new GameObject("PillarArt");
                art.transform.SetParent(t.transform, false);
                var asr = art.AddComponent<SpriteRenderer>();
                asr.sprite = Load(Cat + "obj_cyan_pillar.png");
                asr.sortingOrder = 1;
                var sz = asr.sprite.bounds.size;
                art.transform.localScale = new Vector3(1.15f / sz.x, 1f / sz.y, 1f);
            }

            // ---- beat 3: exit ----
            var door = Sprite(Cat + "obj_exit_door_glow.png", "ExitDoor", new Vector2(66, 0), 0.55f, Color.white, 1);
            var dsr = door.GetComponent<SpriteRenderer>();
            door.transform.position = new Vector3(66, dsr.bounds.extents.y, 0);
            door.tag = "Finish";
            var dcol = door.AddComponent<BoxCollider2D>();
            dcol.isTrigger = true;
            dcol.size = dsr.sprite.bounds.size * 0.6f;

            // ---- dressing ----
            Sprite(Cat + "haz_stalactite_gray.png", "Deco_Stal1", new Vector2(4, Ceil - 0.6f), 0.5f, new Color(0.6f, 0.6f, 0.65f), -2);
            Sprite(Cat + "haz_stalactite_gray.png", "Deco_Stal2", new Vector2(15, Ceil - 0.6f), 0.4f, new Color(0.6f, 0.6f, 0.65f), -2);
            Sprite(Cat + "haz_stalactite_gray.png", "Deco_Stal3", new Vector2(58, Ceil - 0.6f), 0.5f, new Color(0.6f, 0.6f, 0.65f), -2);
            Sprite(Cat + "obj_chain_link_1.png", "Deco_Chain", new Vector2(-6, Ceil - 0.7f), 0.5f, new Color(0.55f, 0.55f, 0.6f), -3);
            Sprite(Cat + "obj_chain_link_2.png", "Deco_Chain2", new Vector2(-6, Ceil - 1.4f), 0.5f, new Color(0.55f, 0.55f, 0.6f), -3);
            Sprite(Cat + "story_note_training.png", "Deco_Note", new Vector2(-3, 4.3f), 0.18f, new Color(0.8f, 0.8f, 0.8f), -4);
            Sprite(Cat + "obj_gray_stairs_cracked.png", "Deco_Rubble", new Vector2(62, 1.1f), 0.45f, new Color(0.5f, 0.5f, 0.55f), -3);

            // ---- spawn ----
            var player = GameObject.FindWithTag("Player");
            var ball = GameObject.Find("Ball");
            player.transform.position = new Vector3(-2, 1.7f, 0);
            ball.transform.position = new Vector3(0.6f, 1f, 0);
            var cmc = GameObject.Find("CinemachineCamera");
            if (cmc) cmc.transform.position = new Vector3(-2, 1.7f, cmc.transform.position.z);
            cam.transform.position = new Vector3(-2, 1.7f, cam.transform.position.z);

            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene)) throw new Exception("save failed");
            Debug.Log("JAM: built " + ScenePath);
            EditorApplication.Exit(0);
        }
        catch (Exception e)
        {
            Debug.LogError("JAM: Level2Builder failed: " + e);
            EditorApplication.Exit(1);
        }
    }

    static Sprite Load(string path)
    {
        var s = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        if (s == null) s = AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().FirstOrDefault();
        if (s == null) throw new Exception("missing sprite " + path);
        return s;
    }

    static GameObject Sprite(string path, string name, Vector2 pos, float scale, Color color, int order, Sprite sprite = null)
    {
        var go = new GameObject(name);
        go.transform.SetParent(root, false);
        go.transform.position = pos;
        go.transform.localScale = Vector3.one * scale;
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite != null ? sprite : Load(path);
        sr.color = color;
        sr.sortingOrder = order;
        return go;
    }

    // Solid ground block (layer Ground) spanning world rect [x0,x1] x [y0,y1], tiled with the floor texture.
    static GameObject Block(string name, float x0, float x1, float y0, float y1)
    {
        const float k = 0.5f; // tile the floor texture at half size
        var go = Sprite("", name, new Vector2((x0 + x1) * 0.5f, (y0 + y1) * 0.5f), k, Wall, 0, groundSprite);
        go.layer = LayerMask.NameToLayer("Ground");
        var sr = go.GetComponent<SpriteRenderer>();
        sr.drawMode = SpriteDrawMode.Tiled;
        sr.size = new Vector2(x1 - x0, y1 - y0) / k;
        go.AddComponent<BoxCollider2D>().size = sr.size;
        return go;
    }

    static GameObject Lava(string name, float x0, float x1, float y0, float yTrigTop)
    {
        var go = Sprite("", name, new Vector2((x0 + x1) * 0.5f, (y0 + yTrigTop) * 0.5f), 1f, Orange, 4, lavaSprite);
        go.layer = LayerMask.NameToLayer("Lava");
        var sr = go.GetComponent<SpriteRenderer>();
        sr.drawMode = SpriteDrawMode.Tiled;
        sr.size = new Vector2(x1 - x0, yTrigTop - y0 + 0.15f);
        var c = go.AddComponent<BoxCollider2D>();
        c.isTrigger = true;
        c.size = new Vector2(x1 - x0, yTrigTop - y0);
        return go;
    }
}
