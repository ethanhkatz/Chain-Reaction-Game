using System.Linq;
using UnityEditor;
using UnityEngine;

// Assigns the 8-frame long-idle animation (the cowboy-hat drop) to the Player prefab.
// Run with: Tools/jam.sh run IdleFramesWiring.Wire
public static class IdleFramesWiring
{
    static readonly string[] Frames =
    {
        "Assets/Images/Idle-Animation(1).png", "Assets/Images/Idle_Animation(2).png", "Assets/Images/Idle-Animation(3).png",
        "Assets/Images/Idle-Animation(4).png", "Assets/Images/Idle-Animation(5).png", "Assets/Images/Idle-Animation(6).png",
        "Assets/Images/Idle-Animation(7).png", "Assets/Images/Idle-Animation(8).png",
    };

    public static void Wire()
    {
        var prefab = PrefabUtility.LoadPrefabContents("Assets/Prefabs/Player.prefab");
        var so = new SerializedObject(prefab.GetComponent<PlayerController>());
        var prop = so.FindProperty("longIdleFrames");
        prop.arraySize = Frames.Length;
        for (int i = 0; i < Frames.Length; i++)
        {
            var sprite = AssetDatabase.LoadAllAssetsAtPath(Frames[i]).OfType<Sprite>().FirstOrDefault();
            if (sprite == null) { Debug.LogError("JAM: no sprite in " + Frames[i]); EditorApplication.Exit(1); return; }
            prop.GetArrayElementAtIndex(i).objectReferenceValue = sprite;
        }
        so.ApplyModifiedPropertiesWithoutUndo();
        PrefabUtility.SaveAsPrefabAsset(prefab, "Assets/Prefabs/Player.prefab");
        PrefabUtility.UnloadPrefabContents(prefab);
        Debug.Log("JAM: wired " + Frames.Length + " long-idle frames");
        EditorApplication.Exit(0);
    }
}
