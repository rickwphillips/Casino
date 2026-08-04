using System.IO;
using UnityEditor;
using UnityEngine;

// Automation hook: if <project>/auto-verify.flag exists when the editor
// (re)loads scripts, restart Play mode fresh: exit a stale session if one is
// running, then enter Play. Lets tooling drive the editor by focusing it.
[InitializeOnLoad]
public static class AutoVerifyPlay
{
    static string Marker => Path.Combine(Application.dataPath, "..", "auto-verify.flag");

    static AutoVerifyPlay()
    {
        if (!File.Exists(Marker)) return;
        EditorApplication.delayCall += Kick;
    }

    static void Kick()
    {
        if (EditorApplication.isPlaying)
        {
            Debug.Log("AutoVerifyPlay: stopping stale Play session first");
            EditorApplication.playModeStateChanged += ResumeAfterExit;
            EditorApplication.ExitPlaymode();
            return;
        }
        StartFresh();
    }

    static void ResumeAfterExit(PlayModeStateChange state)
    {
        if (state != PlayModeStateChange.EnteredEditMode) return;
        EditorApplication.playModeStateChanged -= ResumeAfterExit;
        EditorApplication.delayCall += StartFresh;
    }

    static void StartFresh()
    {
        try { File.Delete(Marker); } catch { }
        Debug.Log("AutoVerifyPlay: entering Play mode fresh");
        EditorApplication.EnterPlaymode();
    }
}
