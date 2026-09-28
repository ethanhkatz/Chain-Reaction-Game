using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Builds Assets/Scenes/Levels/Level4.unity ("Stalactites") from SampleScene. Re-run freely:
//   Tools/jam.sh run Level4Builder.Build
// Beats: (1) stalactites drop as you pass - keep moving; (2) swing the ball up to knock a stalactite onto the
// cracked rock blocking the way; (3) sprint the gauntlet under a ceiling of stalactites that drop in sequence;
// (4) two knocks, two rocks, in order; (5) lure a stalactite onto a pressure button to open the laser gate; exit.
// Secret: walk left from the spawn through the fake wall.
public static class Level4Builder
{
    const string ScenePath = "Assets/Scenes/Levels/Level4.unity";
    const float FloorTop = -4.77f;
    const float SecretLeft = -19f;   // inner face of the far-left wall (the secret room is between it and the fake wall)
    const float ExitWallX = 81.5f;
    const float LevelMidX = 31f, LevelWidth = 104f;

    static readonly Color Cyan = new Color(0.36f, 0.95f, 0.84f);
    static readonly Color Stone = new Color(0.22f, 0.23f, 0.26f); // terrain standard: non-walkable mass
    static readonly Color DarkStone = Stone;
    static readonly Color Hazard = new Color(1f, 0.478f, 0.165f); // #FF7A2A: orange = kills you

    public static void Build()
    {
        try
        {
            BuildScene();
            EditorApplication.Exit(0);
        }
        catch (Exception e)
        {
            Debug.LogException(e);
            EditorApplication.Exit(1);
        }
    }

    static void BuildScene()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Scenes/Levels")) AssetDatabase.CreateFolder("Assets/Scenes", "Levels");
        if (System.IO.File.Exists(ScenePath))
        {
            // overwrite contents only, so the scene keeps its GUID (build settings reference it)
            System.IO.File.Copy("Assets/Scenes/SampleScene.unity", ScenePath, true);
            AssetDatabase.ImportAsset(ScenePath, ImportAssetOptions.ForceUpdate);
        }
        else if (!AssetDatabase.CopyAsset("Assets/Scenes/SampleScene.unity", ScenePath)) throw new Exception("copy failed");
        var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

        string[] keep = { "Main Camera", "CinemachineCamera", "Global Light 2D", "GameManager", "Canvas", "EventSystem", "Player", "Ball" };
        foreach (var root in scene.GetRootGameObjects())
            if (Array.IndexOf(keep, root.name) < 0) UnityEngine.Object.DestroyImmediate(root);

        int ground = LayerMask.NameToLayer("Ground");
        var level = new GameObject("Level4").transform;

        // --- backdrop ---
        var bg = Sprite("Background", "env_background_prison_blur", new Vector2(31f, 0f), 4.6f, level, -100);
        bg.GetComponent<SpriteRenderer>().color = new Color(0.36f, 0.36f, 0.4f);

        // --- floor, bedrock, ceiling, walls ---
        var floorPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Floor.prefab");
        var floor = (GameObject)PrefabUtility.InstantiatePrefab(floorPrefab, scene);
        floor.name = "Floor";
        floor.transform.SetParent(level);
        floor.transform.position = new Vector3(LevelMidX, FloorTop - 0.5f, 0f);
        floor.GetComponent<SpriteRenderer>().size = new Vector2(LevelWidth, 1f);
        floor.GetComponent<BoxCollider2D>().size = new Vector2(LevelWidth, 1f);

        Tiled("Bedrock", "Assets/Images/Ground.png", new Vector2(LevelMidX, FloorTop - 13f), new Vector2(160f, 24f), DarkStone, level, -10, false, 0);
        Tiled("Ceiling", "Assets/Images/Ground.png", new Vector2(LevelMidX, 9.5f), new Vector2(160f, 6f), DarkStone, level, -10, false, 0);
        Tiled("Wall Left", "Assets/Images/Ground.png", new Vector2(SecretLeft - 1.5f, 2f), new Vector2(3f, 16f), Stone, level, -10, true, ground);
        Tiled("Wall Right", "Assets/Images/Ground.png", new Vector2(ExitWallX, 2f), new Vector2(3f, 16f), Stone, level, -10, true, ground);

        // --- spawn ---
        var player = GameObject.Find("Player");
        var ball = GameObject.Find("Ball");
        player.transform.position = new Vector3(0f, -3.2f, 0f);
        ball.transform.position = new Vector3(-2.2f, FloorTop + 1.33f, 0f);
        foreach (var camName in new[] { "Main Camera", "CinemachineCamera" })
        {
            var cam = GameObject.Find(camName);
            if (cam != null) cam.transform.position = new Vector3(0f, -3.2f, cam.transform.position.z);
            if (cam != null && cam.TryGetComponent(out Camera c)) { c.clearFlags = CameraClearFlags.SolidColor; c.backgroundColor = new Color(0.08f, 0.08f, 0.09f); }
        }
        var note = Sprite("Training Note", "story_note_training", new Vector2(2.5f, 1.2f), 0.28f, level, -5);
        note.GetComponent<SpriteRenderer>().color = new Color(0.85f, 0.85f, 0.85f);

        // collectible 1 (easy): right on the path
        Collectible("Collectible_Easy", new Vector2(5f, FloorTop + 1.3f), level);

        // --- beat 1: stalactites drop as you pass; keep moving ---
        Tint(Stalactite("Stalactite Drop A", "haz_stalactite_gray", new Vector2(9f, 5.45f), true, null, level), Hazard);
        Tint(Stalactite("Stalactite Drop B", "haz_stalactite_gray", new Vector2(14f, 5.45f), true, null, level), Hazard);
        Tint(Stalactite("Stalactite Drop C", "haz_stalactite_gray", new Vector2(17.5f, 5.45f), true, null, level), Hazard);

        // --- beat 2: knock a stalactite loose with the ball onto the cracked rock ---
        // a rock outcrop hangs from the ceiling just before the rock; the cyan (interactive) stalactite under it
        // is too high to bump with your head but sits in the arc of the ball on a running jump. Knocked loose, it
        // arcs onto the cracked rock and smashes it.
        var rock = CrackedRock("Cracked Rock", "obj_rock_cracked_1", 25f, 0.55f, level, ground);
        KnockStalactite("Stalactite Knock", rock, 1.3f, level);

        // --- beat 3 (new): the gauntlet. A low ceiling packed with stalactites that let go one after another as
        // you pass under them. Stop and you're flattened; the middle one guards a collectible you jump for. ---
        const float gStart = 30f, gEnd = 45f, gCeil = 3.4f;
        Tiled("Gauntlet Ceiling", "Assets/Images/Ground.png", new Vector2((gStart + gEnd) / 2f, (gCeil + 7f) / 2f), new Vector2(gEnd - gStart + 1.2f, 7f - gCeil), Stone, level, -10, false, 0);
        int gi = 0;
        for (float x = gStart + 0.8f; x < gEnd - 0.4f; x += 1.35f, gi++)
            Tint(Hang("Stalactite Gauntlet " + gi, "haz_stalactite_gray", x, gCeil, true, null, level), Hazard);
        // collectible 2 (detour): hangs under a stalactite in the gauntlet; jump for it without breaking stride
        Collectible("Collectible_Gauntlet", new Vector2(gStart + 0.8f + 1.35f * 5f, FloorTop + 3.1f), level);

        // --- beat 4 (new): two rocks, two stalactites, one order. The first knock clears the near rock; only then
        // can you reach the second stalactite, which arcs onto the tall rock behind it. ---
        var rockA = CrackedRock("Cracked Rock A", "obj_rock_cracked_2", 52f, 0.55f, level, ground);
        var knockA = KnockStalactite("Stalactite Knock A", rockA, 1.3f, level);
        var rockB = CrackedRock("Cracked Rock B", "obj_rock_cracked_3", 58.5f, 0.65f, level, ground);
        var knockB = KnockStalactite("Stalactite Knock B", rockB, 1.6f, level);
        // order marks on the outcrops: one notch, then two
        Notches(knockA.transform.position.x, 1, level);
        Notches(knockB.transform.position.x, 2, level);

        // --- beat 5: lure a stalactite onto the button to open the gate ---
        const float buttonX = 66f, gateX = 72f;
        var pedestal = Sprite("Button Pedestal", "obj_cyan_block_small", Vector2.zero, 0.5f, level, 0);
        var psr = pedestal.GetComponent<SpriteRenderer>();
        pedestal.transform.position = new Vector3(buttonX, FloorTop + psr.bounds.extents.y, 0f);
        pedestal.layer = ground;
        pedestal.AddComponent<BoxCollider2D>().sharedMaterial = Slick();
        var button = Sprite("Press Button", "obj_button_cyan", Vector2.zero, 0.32f, level, 1);
        var bsr = button.GetComponent<SpriteRenderer>();
        button.transform.position = new Vector3(buttonX, psr.bounds.max.y + bsr.bounds.extents.y - 0.05f, 0f);
        var bcol = button.AddComponent<BoxCollider2D>();
        bcol.isTrigger = true;
        var press = button.AddComponent<PressButton>();

        var gate = Sprite("Laser Gate", "haz_laser_gate_1", Vector2.zero, 0.85f, level, 0);
        var gsr = gate.GetComponent<SpriteRenderer>();
        gate.transform.position = new Vector3(gateX, FloorTop + gsr.bounds.extents.y, 0f);
        gate.AddComponent<BoxCollider2D>().sharedMaterial = Slick();
        var gateComp = gate.AddComponent<Gate>();
        Tiled("Gate Housing", "Assets/Images/Ground.png", new Vector2(gateX, (gsr.bounds.max.y + 6.5f) / 2f), new Vector2(2f, 6.5f - gsr.bounds.max.y), Stone, level, -10, true, ground);

        var so = new SerializedObject(press);
        var targets = so.FindProperty("targets");
        targets.arraySize = 1;
        targets.GetArrayElementAtIndex(0).objectReferenceValue = gateComp;
        so.FindProperty("pressedSprite").objectReferenceValue = LoadSprite("obj_button_red");
        so.ApplyModifiedPropertiesWithoutUndo();

        Tint(Stalactite("Stalactite Button", "haz_stalactite_gray", new Vector2(buttonX, 5.45f), true, button.transform, level), Hazard);

        // --- exit ---
        var exit = Sprite("Exit Door", "obj_exit_door_glow", Vector2.zero, 0.5f, level, -2);
        var esr = exit.GetComponent<SpriteRenderer>();
        exit.transform.position = new Vector3(ExitWallX - 5f, FloorTop + esr.bounds.extents.y, 0f);
        exit.tag = "Finish";
        var ecol = exit.AddComponent<BoxCollider2D>();
        ecol.isTrigger = true;

        // --- secret: a fake wall left of the spawn hides a room with a cardboard box ---
        BuildSecretRoom(level, ground);

        // --- dressing ---
        foreach (var x in new[] { -3f, 20f, 47.5f, 62f })
        {
            var p = Sprite("Pillar", "obj_cyan_pillar", Vector2.zero, 0.6f, level, -10);
            var sr = p.GetComponent<SpriteRenderer>();
            sr.color = new Color(0.55f, 0.6f, 0.62f);
            p.transform.position = new Vector3(x, FloorTop + sr.bounds.extents.y, 0f);
        }
        int li = 0;
        foreach (var x in new[] { 4f, 11.5f, 27.5f, 64f, 69f })
            for (int k = 0; k < 4; k++)
            {
                var c = Sprite("Chain", "obj_chain_link_" + (1 + (k + li) % 2), new Vector2(x, 6.2f - k * 0.62f), 0.35f, level, -8);
                c.transform.rotation = Quaternion.Euler(0, 0, 90f);
                c.GetComponent<SpriteRenderer>().color = new Color(0.6f, 0.6f, 0.65f);
                li++;
            }
        var stairs = Sprite("Broken Stairs", "obj_gray_stairs_cracked", Vector2.zero, 0.5f, level, -9);
        var ssr = stairs.GetComponent<SpriteRenderer>();
        ssr.color = new Color(0.6f, 0.6f, 0.62f);
        stairs.transform.position = new Vector3(6f, FloorTop + ssr.bounds.extents.y, 0f);

        EditorSceneManager.MarkSceneDirty(scene);
        if (!EditorSceneManager.SaveScene(scene)) throw new Exception("save failed");
        Debug.Log("JAM: built " + ScenePath);
    }

    static GameObject CrackedRock(string name, string sprite, float x, float scale, Transform level, int ground)
    {
        var rock = Sprite(name, sprite, Vector2.zero, scale, level, 0);
        var sr = rock.GetComponent<SpriteRenderer>();
        rock.transform.position = new Vector3(x, FloorTop + sr.bounds.extents.y, 0f);
        rock.tag = "Rock";
        rock.layer = ground;
        rock.AddComponent<BoxCollider2D>().sharedMaterial = Slick();
        return rock;
    }

    // cyan stalactite under a ceiling outcrop, 2.2 left of the rock: in the arc of the ball when you jump against the rock
    static GameObject KnockStalactite(string name, GameObject rock, float tipY, Transform level)
    {
        float rockLeft = rock.GetComponent<SpriteRenderer>().bounds.min.x;
        var knock = Stalactite(name, "haz_stalactite_cyan", new Vector2(rockLeft - 2.2f, 0f), false, rock.transform, level);
        var kso = new SerializedObject(knock.GetComponent<StalactiteController>());
        kso.FindProperty("aimAtTargetWhenKnocked").boolValue = true;
        kso.FindProperty("launchUpSpeed").floatValue = 1.5f; // near-flat throw: clears your head but stays on screen (a high lob leaves the view and is culled)
        kso.ApplyModifiedPropertiesWithoutUndo();
        var ksr = knock.GetComponent<SpriteRenderer>();
        knock.transform.position = new Vector3(knock.transform.position.x, tipY + ksr.bounds.extents.y, 0f);
        var top = ksr.bounds.max.y;
        Tiled(name + " Outcrop", "Assets/Images/Ground.png", new Vector2(knock.transform.position.x, (top + 6.5f) / 2f), new Vector2(2.4f, 6.5f - top), Stone, level, -10, false, 0);
        return knock;
    }

    static void Notches(float x, int n, Transform level)
    {
        for (int i = 0; i < n; i++)
            Rect("Order Notch", new Vector2(x + (i - (n - 1) / 2f) * 0.4f, 4.4f), new Vector2(0.16f, 0.7f), Cyan, -9, level);
    }

    static GameObject Hang(string name, string sprite, float x, float ceilY, bool byPlayer, Transform target, Transform level)
    {
        var go = Stalactite(name, sprite, new Vector2(x, 0f), byPlayer, target, level);
        var sr = go.GetComponent<SpriteRenderer>();
        go.transform.position = new Vector3(x, ceilY + 0.1f - sr.bounds.max.y, 0f);
        return go;
    }

    static GameObject Collectible(string name, Vector2 pos, Transform level)
    {
        var go = Sprite(name, "obj_cyan_block_small", pos, 0.35f, level, 3);
        go.AddComponent<BoxCollider2D>().isTrigger = true;
        return go;
    }

    static void BuildSecretRoom(Transform level, int ground)
    {
        const float roomRight = -5.5f, roomCeil = 1.2f;
        var root = new GameObject("Secret_CardboardBox").transform;
        root.SetParent(level);
        var zone = root.gameObject.AddComponent<BoxCollider2D>();
        zone.isTrigger = true;
        zone.offset = new Vector2((SecretLeft + roomRight) / 2f, (FloorTop + roomCeil) / 2f);
        zone.size = new Vector2(roomRight - SecretLeft, roomCeil - FloorTop);

        // the room: a low pocket in the rock
        Tiled("Secret Ceiling", "Assets/Images/Ground.png", new Vector2((SecretLeft + roomRight) / 2f, (roomCeil + 7f) / 2f), new Vector2(roomRight - SecretLeft + 1f, 7f - roomCeil), Stone, root, -10, true, ground);
        // the fake wall: the same rock as everything else, drawn in front until you walk into it
        var cover = Tiled("Secret Cover", "Assets/Images/Ground.png", new Vector2((SecretLeft - 1f + roomRight + 1f) / 2f, 1f), new Vector2(roomRight - SecretLeft + 2f, 12f), Stone, root, 60, false, 0); // above the box (45-47) and alert

        // cardboard box (in front of the player, so standing at it hides you)
        const float bx = -14.5f, bw = 3.4f, bh = 3.1f; // taller than the player so hiding reads
        var box = new GameObject("Cardboard Box").transform;
        box.SetParent(root);
        float by = FloorTop + bh / 2f;
        var ink = new Color(0.05f, 0.05f, 0.06f);
        var card = new Color(0.55f, 0.46f, 0.33f);
        Rect("Box Outline", new Vector2(bx, by), new Vector2(bw + 0.14f, bh + 0.14f), ink, 45, box);
        Rect("Box Body", new Vector2(bx, by), new Vector2(bw, bh), card, 46, box);
        Rect("Box Flap Line", new Vector2(bx, by + bh / 2f - 0.35f), new Vector2(bw, 0.07f), ink, 47, box);
        Rect("Box Tape", new Vector2(bx, by + bh / 2f - 0.17f), new Vector2(0.4f, 0.34f), new Color(0.72f, 0.64f, 0.5f), 47, box);
        Rect("Box Handle", new Vector2(bx, by + 0.15f), new Vector2(0.55f, 0.14f), ink, 47, box);
        Rect("Box Fold L", new Vector2(bx - bw / 2f + 0.25f, by - 0.3f), new Vector2(0.06f, 0.9f), new Color(0.45f, 0.37f, 0.26f), 47, box);
        var hide = box.gameObject.AddComponent<BoxCollider2D>();
        hide.isTrigger = true;
        hide.offset = new Vector2(bx, by);
        hide.size = new Vector2(bw - 0.4f, bh + 1.5f);

        // a security camera watching the room
        Rect("Camera Mount", new Vector2(-8.2f, roomCeil - 0.25f), new Vector2(0.12f, 0.5f), ink, 4, root);
        Rect("Camera Body", new Vector2(-8.5f, roomCeil - 0.6f), new Vector2(0.9f, 0.4f), new Color(0.35f, 0.36f, 0.4f), 4, root);
        Rect("Camera Lens", new Vector2(-8.98f, roomCeil - 0.6f), new Vector2(0.14f, 0.24f), Cyan, 5, root);

        // "!" alert that pops over your head when you're spotted
        var alert = new GameObject("Alert").transform;
        alert.SetParent(root);
        var red = new Color(1f, 0.35f, 0.4f);
        Rect("Bang Outline", new Vector2(0f, 0.25f), new Vector2(0.36f, 0.9f), ink, 40, alert);
        Rect("Bang", new Vector2(0f, 0.25f), new Vector2(0.22f, 0.76f), red, 41, alert);
        Rect("Dot Outline", new Vector2(0f, -0.52f), new Vector2(0.36f, 0.36f), ink, 40, alert);
        Rect("Dot", new Vector2(0f, -0.52f), new Vector2(0.22f, 0.22f), red, 41, alert);

        // collectible 3 (hidden): in the corner past the box
        Collectible("Collectible_Secret", new Vector2(SecretLeft + 1.4f, FloorTop + 1.3f), root);

        var secret = root.gameObject.AddComponent<Level4SecretRoom>();
        var so = new SerializedObject(secret);
        var c = so.FindProperty("cover");
        c.arraySize = 1;
        c.GetArrayElementAtIndex(0).objectReferenceValue = cover.GetComponent<SpriteRenderer>();
        so.FindProperty("alert").objectReferenceValue = alert;
        so.FindProperty("hideZone").objectReferenceValue = hide;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    const string PixelPath = "Assets/Scenes/Levels/Level4Pixel.png";
    static Sprite Pixel()
    {
        var s = AssetDatabase.LoadAssetAtPath<Sprite>(PixelPath);
        if (s != null) return s;
        var tex = new Texture2D(4, 4);
        var px = new Color[16];
        for (int i = 0; i < 16; i++) px[i] = Color.white;
        tex.SetPixels(px);
        System.IO.File.WriteAllBytes(PixelPath, tex.EncodeToPNG());
        AssetDatabase.ImportAsset(PixelPath);
        var imp = (TextureImporter)AssetImporter.GetAtPath(PixelPath);
        imp.textureType = TextureImporterType.Sprite;
        imp.spriteImportMode = SpriteImportMode.Single;
        imp.spritePixelsPerUnit = 4;
        imp.filterMode = FilterMode.Point;
        imp.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Sprite>(PixelPath);
    }

    static GameObject Rect(string name, Vector2 pos, Vector2 size, Color color, int order, Transform parent)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent);
        go.transform.position = pos;
        go.transform.localScale = new Vector3(size.x, size.y, 1f);
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = Pixel();
        sr.color = color;
        sr.sortingOrder = order;
        return go;
    }

    const string SlickPath = "Assets/Scenes/Levels/Level4Slick.physicsMaterial2D";
    static PhysicsMaterial2D Slick()
    {
        var m = AssetDatabase.LoadAssetAtPath<PhysicsMaterial2D>(SlickPath);
        if (m == null)
        {
            m = new PhysicsMaterial2D("Level4Slick") { friction = 0f, bounciness = 0f };
            AssetDatabase.CreateAsset(m, SlickPath);
        }
        return m;
    }

    static GameObject Tint(GameObject go, Color c)
    {
        go.GetComponent<SpriteRenderer>().color = c;
        return go;
    }

    static void SetField(UnityEngine.Object o, string field, float v)
    {
        var so = new SerializedObject(o);
        so.FindProperty(field).floatValue = v;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    static Sprite LoadSprite(string name)
    {
        var s = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Catalog/" + name + ".png");
        if (s == null) throw new Exception("missing sprite " + name);
        return s;
    }

    static GameObject Sprite(string name, string sprite, Vector2 pos, float scale, Transform parent, int order)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent);
        go.transform.position = pos;
        go.transform.localScale = Vector3.one * scale;
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = LoadSprite(sprite);
        sr.sortingOrder = order;
        return go;
    }

    static GameObject Tiled(string name, string spritePath, Vector2 pos, Vector2 size, Color color, Transform parent, int order, bool solid, int layer)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent);
        go.transform.position = pos;
        go.layer = layer;
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);
        sr.drawMode = SpriteDrawMode.Tiled;
        sr.size = size;
        sr.color = color;
        sr.sortingOrder = order;
        if (solid) { var col = go.AddComponent<BoxCollider2D>(); col.size = size; col.sharedMaterial = Slick(); }
        return go;
    }

    static GameObject Stalactite(string name, string sprite, Vector2 pos, bool byPlayer, Transform target, Transform parent)
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Stlactite.prefab");
        var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
        go.name = name;
        go.transform.position = pos;
        go.transform.localScale = Vector3.one * 0.9f;
        var sr = go.GetComponent<SpriteRenderer>();
        sr.sprite = LoadSprite(sprite);
        sr.sortingOrder = 1;
        // refit the collider to the catalog art
        var poly = go.GetComponent<PolygonCollider2D>();
        var shape = new System.Collections.Generic.List<Vector2>();
        int n = sr.sprite.GetPhysicsShapeCount();
        if (n > 0)
        {
            poly.pathCount = n;
            for (int i = 0; i < n; i++) { shape.Clear(); sr.sprite.GetPhysicsShape(i, shape); poly.SetPath(i, shape); }
        }
        var so = new SerializedObject(go.GetComponent<StalactiteController>());
        so.FindProperty("triggeredByPlayer").boolValue = byPlayer;
        so.FindProperty("target").objectReferenceValue = target;
        so.FindProperty("findTargetByTag").boolValue = false;
        so.ApplyModifiedPropertiesWithoutUndo();
        return go;
    }
}
