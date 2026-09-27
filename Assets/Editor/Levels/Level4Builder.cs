using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Builds Assets/Scenes/Levels/Level4.unity ("Stalactites") from SampleScene. Re-run freely:
//   Tools/jam.sh run Level4Builder.Build
// Beats: (1) stalactites drop as you pass - keep moving; (2) swing the ball up to knock a stalactite onto the
// cracked rock blocking the way; (3) lure a stalactite onto a pressure button to open the laser gate; (4) exit.
public static class Level4Builder
{
    const string ScenePath = "Assets/Scenes/Levels/Level4.unity";
    const float FloorTop = -4.77f;

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
        var bg = Sprite("Background", "env_background_prison_blur", new Vector2(22f, 0f), 3.2f, level, -100);
        bg.GetComponent<SpriteRenderer>().color = new Color(0.36f, 0.36f, 0.4f);

        // --- floor, bedrock, ceiling, walls ---
        var floorPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Floor.prefab");
        var floor = (GameObject)PrefabUtility.InstantiatePrefab(floorPrefab, scene);
        floor.name = "Floor";
        floor.transform.SetParent(level);
        floor.transform.position = new Vector3(22f, FloorTop - 0.5f, 0f);
        floor.GetComponent<SpriteRenderer>().size = new Vector2(60f, 1f);
        floor.GetComponent<BoxCollider2D>().size = new Vector2(60f, 1f);

        Tiled("Bedrock", "Assets/Images/Ground.png", new Vector2(22f, FloorTop - 13f), new Vector2(130f, 24f), DarkStone, level, -10, false, 0);
        Tiled("Ceiling", "Assets/Images/Ground.png", new Vector2(22f, 9.5f), new Vector2(130f, 6f), DarkStone, level, -10, false, 0);
        Tiled("Wall Left", "Assets/Images/Ground.png", new Vector2(-6.5f, 2f), new Vector2(3f, 16f), Stone, level, -10, true, ground);
        Tiled("Wall Right", "Assets/Images/Ground.png", new Vector2(51.5f, 2f), new Vector2(3f, 16f), Stone, level, -10, true, ground);

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

        // --- beat 1: stalactites drop as you pass; keep moving ---
        Tint(Stalactite("Stalactite Drop A", "haz_stalactite_gray", new Vector2(9f, 5.45f), true, null, level), Hazard);
        Tint(Stalactite("Stalactite Drop B", "haz_stalactite_gray", new Vector2(14f, 5.45f), true, null, level), Hazard);
        Tint(Stalactite("Stalactite Drop C", "haz_stalactite_gray", new Vector2(17.5f, 5.45f), true, null, level), Hazard);

        // --- beat 2: knock a stalactite loose with the ball onto the cracked rock ---
        var rock = Sprite("Cracked Rock", "obj_rock_cracked_1", Vector2.zero, 0.55f, level, 0);
        var rockSr = rock.GetComponent<SpriteRenderer>();
        rock.transform.position = new Vector3(25f, FloorTop + rockSr.bounds.extents.y, 0f);
        rock.tag = "Rock";
        rock.layer = ground;
        rock.AddComponent<BoxCollider2D>().sharedMaterial = Slick();
        float rockTop = rockSr.bounds.max.y, rockLeft = rockSr.bounds.min.x;
        // a rock outcrop hangs from the ceiling just before the rock; the cyan (interactive) stalactite under it
        // is too high to bump with your head but sits in the arc of the ball on a running jump. Knocked loose, it
        // keeps some of the ball's momentum and lands on the cracked rock, smashing it.
        const float tipY = 1.3f;
        var knock = Stalactite("Stalactite Knock", "haz_stalactite_cyan", new Vector2(rockLeft - 2.2f, 0f), false, rock.transform, level);
        var kso = new SerializedObject(knock.GetComponent<StalactiteController>());
        kso.FindProperty("aimAtTargetWhenKnocked").boolValue = true;
        kso.ApplyModifiedPropertiesWithoutUndo();
        var ksr = knock.GetComponent<SpriteRenderer>();
        knock.transform.position = new Vector3(knock.transform.position.x, tipY + ksr.bounds.extents.y, 0f);
        var outcropBottom = ksr.bounds.max.y;
        Tiled("Outcrop", "Assets/Images/Ground.png", new Vector2(knock.transform.position.x, (outcropBottom + 6.5f) / 2f), new Vector2(2.4f, 6.5f - outcropBottom), Stone, level, -10, false, 0);

        // --- beat 3: lure a stalactite onto the button to open the gate ---
        var pedestal = Sprite("Button Pedestal", "obj_cyan_block_small", Vector2.zero, 0.5f, level, 0);
        var psr = pedestal.GetComponent<SpriteRenderer>();
        pedestal.transform.position = new Vector3(35f, FloorTop + psr.bounds.extents.y, 0f);
        pedestal.layer = ground;
        pedestal.AddComponent<BoxCollider2D>().sharedMaterial = Slick();
        var button = Sprite("Press Button", "obj_button_cyan", Vector2.zero, 0.32f, level, 1);
        var bsr = button.GetComponent<SpriteRenderer>();
        button.transform.position = new Vector3(35f, psr.bounds.max.y + bsr.bounds.extents.y - 0.05f, 0f);
        var bcol = button.AddComponent<BoxCollider2D>();
        bcol.isTrigger = true;
        var press = button.AddComponent<PressButton>();

        var gate = Sprite("Laser Gate", "haz_laser_gate_1", Vector2.zero, 0.85f, level, 0);
        var gsr = gate.GetComponent<SpriteRenderer>();
        gate.transform.position = new Vector3(41f, FloorTop + gsr.bounds.extents.y, 0f);
        gate.AddComponent<BoxCollider2D>().sharedMaterial = Slick();
        var gateComp = gate.AddComponent<Gate>();
        Tiled("Gate Housing", "Assets/Images/Ground.png", new Vector2(41f, (gsr.bounds.max.y + 6.5f) / 2f), new Vector2(2f, 6.5f - gsr.bounds.max.y), Stone, level, -10, true, ground);

        var so = new SerializedObject(press);
        var targets = so.FindProperty("targets");
        targets.arraySize = 1;
        targets.GetArrayElementAtIndex(0).objectReferenceValue = gateComp;
        so.FindProperty("pressedSprite").objectReferenceValue = LoadSprite("obj_button_red");
        so.ApplyModifiedPropertiesWithoutUndo();

        Tint(Stalactite("Stalactite Button", "haz_stalactite_gray", new Vector2(35f, 5.45f), true, button.transform, level), Hazard);

        // --- beat 4: exit ---
        var exit = Sprite("Exit Door", "obj_exit_door_glow", Vector2.zero, 0.5f, level, -2);
        var esr = exit.GetComponent<SpriteRenderer>();
        exit.transform.position = new Vector3(46.5f, FloorTop + esr.bounds.extents.y, 0f);
        exit.tag = "Finish";
        var ecol = exit.AddComponent<BoxCollider2D>();
        ecol.isTrigger = true;

        // --- dressing ---
        foreach (var x in new[] { -3f, 20f, 31f })
        {
            var p = Sprite("Pillar", "obj_cyan_pillar", Vector2.zero, 0.6f, level, -10);
            var sr = p.GetComponent<SpriteRenderer>();
            sr.color = new Color(0.55f, 0.6f, 0.62f);
            p.transform.position = new Vector3(x, FloorTop + sr.bounds.extents.y, 0f);
        }
        int li = 0;
        foreach (var x in new[] { 4f, 11.5f, 28.5f, 38f })
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
