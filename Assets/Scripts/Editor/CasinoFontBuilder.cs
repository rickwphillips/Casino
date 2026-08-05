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

    // At most one automatic build per editor session, tracked in SessionState so
    // it survives the domain reloads that a build causes.
    private const string BuiltKey = "CasinoFontBuilder.builtThisSession";

    // Writing assets from a load-time hook is how you hang an editor. Creating an
    // asset triggers an import, an import can trigger a domain reload, and a
    // domain reload runs every InitializeOnLoad hook again, including this one.
    // If the build does not durably satisfy NeedsBuild() the cycle never closes,
    // and the editor sits pinned at ~200% CPU with the log stopping mid-write.
    // That exact loop was reproduced here on 2026-08-05 with a different
    // load-time hook that called SaveAssets, so this is a real failure mode and
    // not a hypothetical.
    //
    // The latch is the fix: even if NeedsBuild() is wrong or the build genuinely
    // fails, it can burn one attempt per session instead of spinning forever, and
    // it says so rather than failing silently.
    [InitializeOnLoadMethod]
    private static void BuildIfMissing()
    {
        if (!NeedsBuild()) return;

        if (SessionState.GetBool(BuiltKey, false))
        {
            Debug.LogWarning(
                "CasinoFontBuilder: the font asset still looks unbuilt after an attempt " +
                "this session. Not retrying, because rebuilding from a load-time hook in " +
                "a loop wedges the editor. Run Casino > Fonts > Rebuild TMP font asset by " +
                "hand and watch what it reports.");
            return;
        }

        SessionState.SetBool(BuiltKey, true);
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

    // The manual path clears the latch: if you ask for it explicitly, you get it,
    // and a later automatic pass is allowed to try again.
    [MenuItem("Casino/Fonts/Rebuild TMP font asset")]
    private static void RebuildFromMenu()
    {
        SessionState.EraseBool(BuiltKey);
        Build(true);
    }

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

        // Deliberately no AssetDatabase.Refresh(). Everything above went through
        // the AssetDatabase already, so the asset is registered without it, and
        // Refresh kicks off an import pass that can trigger a domain reload,
        // which re-enters this class. It was the loop's accelerator, not a
        // requirement.
        Debug.Log($"CasinoFontBuilder: built {OutPath} from {Path.GetFileName(SourceTtf)}");
    }
}
