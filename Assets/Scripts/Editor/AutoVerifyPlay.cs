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

    // Poll rather than only checking at load. The flag used to be read once per
    // domain reload, so `touch auto-verify.flag` on its own did nothing: Unity
    // skips the reimport when a file's contents have not changed, so no reload
    // happened and the flag sat there. That made unattended runs fail silently
    // and intermittently, which is worse than failing outright.
    private const double PollSeconds = 0.5;
    private static double nextPoll;

    static AutoVerifyPlay()
    {
        EditorApplication.update += Poll;
        if (File.Exists(Marker)) EditorApplication.delayCall += Kick;
    }

    static void Poll()
    {
        if (EditorApplication.timeSinceStartup < nextPoll) return;
        nextPoll = EditorApplication.timeSinceStartup + PollSeconds;
        if (EditorApplication.isCompiling || EditorApplication.isUpdating) return;
        if (!File.Exists(Marker)) return;
        Kick();
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
        // Pin the Game view first: entering Play with the wrong size makes the
        // whole run useless, and both used to race on delayCall.
        GameViewSizePin.ApplyRequested();
        Debug.Log("AutoVerifyPlay: entering Play mode fresh");
        EditorApplication.EnterPlaymode();
    }
}
