using System.IO;
using TMPro;
using UnityEditor;
using UnityEngine;

// Builds the TMP font asset for the Parlor direction from the OFL source TTF.
//
// A .ttf is not usable by TextMeshPro directly: it needs a TMP_FontAsset with an
// atlas and material. That can only be created in the editor, so it is generated
// here and committed, and the runtime just loads it from Resources.
//
// Runs automatically when the asset is missing (a fresh clone, or after deleting
// it), so nobody has to know this step exists. Casino > Fonts > Rebuild forces it.
public static class CasinoFontBuilder
{
    private const string SourceTtf = "Assets/Fonts/SourceSerif4-Variable.ttf";
    private const string OutDir = "Assets/Resources/Fonts";
    private const string OutPath = OutDir + "/CasinoSerif.asset";

    [InitializeOnLoadMethod]
    private static void BuildIfMissing()
    {
        if (!NeedsBuild()) return;
        EditorApplication.delayCall += () => Build(true);
    }

    // Existence is not enough. Deleting the .asset and swapping the source TTF
    // did not take: Unity restored the asset from its cache, still pointing at
    // the old face, and because a dynamic font asset keeps its baked atlas the
    // game kept rendering the old glyphs while every file on disk said otherwise.
    // Check what the asset is actually made of.
    private static bool NeedsBuild()
    {
        var existing = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(OutPath);
        if (existing == null) return true;
        string source = existing.sourceFontFile != null
            ? AssetDatabase.GetAssetPath(existing.sourceFontFile) : null;
        if (source == SourceTtf) return false;
        Debug.Log($"CasinoFontBuilder: rebuilding, asset was built from '{source ?? "unknown"}' " +
                  $"but the source is now {SourceTtf}");
        return true;
    }

    [MenuItem("Casino/Fonts/Rebuild TMP font asset")]
    private static void RebuildFromMenu() => Build(true);

    private static void Build(bool force)
    {
        var source = AssetDatabase.LoadAssetAtPath<Font>(SourceTtf);
        if (source == null)
        {
            Debug.LogWarning($"CasinoFontBuilder: no source font at {SourceTtf}. " +
                             "The UI falls back to the TMP default, which is a sans.");
            return;
        }
        if (!force && !NeedsBuild()) return;

        var asset = TMP_FontAsset.CreateFontAsset(source);
        if (asset == null)
        {
            Debug.LogWarning("CasinoFontBuilder: TMP could not create a font asset from " + SourceTtf);
            return;
        }
        asset.name = "CasinoSerif";

        Directory.CreateDirectory(OutDir);
        AssetDatabase.DeleteAsset(OutPath);
        AssetDatabase.CreateAsset(asset, OutPath);

        // Atlas and material must live inside the asset or they are lost on reload.
        if (asset.atlasTextures != null && asset.atlasTextures.Length > 0)
        {
            asset.atlasTextures[0].name = "CasinoSerif Atlas";
            AssetDatabase.AddObjectToAsset(asset.atlasTextures[0], asset);
        }
        if (asset.material != null)
        {
            asset.material.name = "CasinoSerif Material";
            AssetDatabase.AddObjectToAsset(asset.material, asset);
        }

        EditorUtility.SetDirty(asset);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"CasinoFontBuilder: built {OutPath} from {Path.GetFileName(SourceTtf)}");
    }
}
