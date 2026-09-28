using TMPro;
using UnityEditor;
using UnityEngine;

// Builds the TextMesh Pro font asset for the game's display lettering (Luckiest Guy).
// Run with: Tools/jam.sh run FontAssetBuilder.Build
public static class FontAssetBuilder
{
    const string FontPath = "Assets/Fonts/LuckiestGuy-Regular.ttf";
    // Lives in Resources so runtime UI can load it by name: Resources.Load<TMP_FontAsset>("ComicFont").
    const string AssetPath = "Assets/Resources/ComicFont.asset";

    public static void Build()
    {
        AssetDatabase.ImportAsset(FontPath);
        var font = AssetDatabase.LoadAssetAtPath<Font>(FontPath);
        var asset = TMP_FontAsset.CreateFontAsset(font, 90, 9, UnityEngine.TextCore.LowLevel.GlyphRenderMode.SDFAA, 1024, 1024, AtlasPopulationMode.Dynamic);
        asset.name = "LuckiestGuy SDF";
        AssetDatabase.DeleteAsset(AssetPath);
        AssetDatabase.CreateAsset(asset, AssetPath);
        AssetDatabase.AddObjectToAsset(asset.material, asset);
        AssetDatabase.AddObjectToAsset(asset.atlasTexture, asset);
        asset.TryAddCharacters("ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789 !?.,:;'\"-—+x×/()#%&");
        EditorUtility.SetDirty(asset);
        AssetDatabase.SaveAssets();
        Debug.Log("JAM: font asset -> " + AssetPath);
        EditorApplication.Exit(0);
    }
}
