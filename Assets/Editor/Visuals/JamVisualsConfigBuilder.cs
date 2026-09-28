using System.Linq;
using UnityEditor;
using UnityEngine;

// Builds Assets/Resources/JamVisualsConfig.asset. Run: Tools/jam.sh run JamVisualsConfigBuilder.Build
public static class JamVisualsConfigBuilder
{
    const string Path = "Assets/Resources/JamVisualsConfig.asset";

    public static void Build()
    {
        try
        {
            var cfg = AssetDatabase.LoadAssetAtPath<JamVisualsConfig>(Path);
            if (cfg == null)
            {
                cfg = ScriptableObject.CreateInstance<JamVisualsConfig>();
                AssetDatabase.CreateAsset(cfg, Path);
            }
            cfg.background = FirstSprite("Assets/Art/Catalog/env_background_prison_blur.png");
            cfg.chainLinkA = FirstSprite("Assets/Art/Catalog/obj_chain_link_1.png");
            cfg.chainLinkB = FirstSprite("Assets/Art/Catalog/obj_chain_link_2.png");
            cfg.crash = LargestSprite("Assets/Art/Catalog/fx_crash.png");
            const string mats = "Packages/com.unity.render-pipelines.universal/Runtime/Materials/";
            cfg.litSprite = AssetDatabase.LoadAssetAtPath<Material>(mats + "Sprite-Lit-Default.mat");
            cfg.unlitSprite = AssetDatabase.LoadAssetAtPath<Material>(mats + "Sprite-Unlit-Default.mat");
            EditorUtility.SetDirty(cfg);
            AssetDatabase.SaveAssets();
            Debug.Log($"JAM: config bg={cfg.background} links={cfg.chainLinkA},{cfg.chainLinkB} crash={cfg.crash} lit={cfg.litSprite} unlit={cfg.unlitSprite}");
            if (Application.isBatchMode) EditorApplication.Exit(0);
        }
        catch (System.Exception e)
        {
            Debug.LogError("JAM: config build failed " + e);
            if (Application.isBatchMode) EditorApplication.Exit(1);
        }
    }

    static Sprite FirstSprite(string p) => AssetDatabase.LoadAllAssetsAtPath(p).OfType<Sprite>().FirstOrDefault();

    static Sprite LargestSprite(string p) => AssetDatabase.LoadAllAssetsAtPath(p).OfType<Sprite>()
        .OrderByDescending(s => s.rect.width * s.rect.height).FirstOrDefault();
}
