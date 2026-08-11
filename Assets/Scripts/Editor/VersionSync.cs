using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

// Editing bundleVersion in ProjectSettings.asset from outside the editor does
// nothing while the editor runs: PlayerSettings live in memory and clobber the
// file on save, the same trap CLAUDE.md documents for Scene.unity. Version
// bumps are done on disk (they belong in the release commit), so this menu
// item re-applies whatever the file says through the API the editor respects.
public static class VersionSync
{
    [MenuItem("Casino/Reload bundleVersion from disk")]
    private static void Reload()
    {
        var line = File.ReadLines("ProjectSettings/ProjectSettings.asset")
            .FirstOrDefault(l => l.TrimStart().StartsWith("bundleVersion:"));
        if (line == null)
        {
            Debug.LogWarning("VersionSync: no bundleVersion line found");
            return;
        }
        var version = line.Split(':')[1].Trim();
        PlayerSettings.bundleVersion = version;
        AssetDatabase.SaveAssets();
        Debug.Log($"VersionSync: bundleVersion now {PlayerSettings.bundleVersion}");
    }
}
