using System.IO;
using UnityEditor;
using UnityEngine;

// Automation hook: drop <project>/auto-verify.flag and the editor restarts Play
// mode fresh. Lets tooling drive the editor without a human pressing anything.
//
// Three bugs shaped this file, all of which failed silently, which is the worst
// way for a verification tool to fail: reading a stale screenshot as a fresh one
// is how you end up believing a broken thing works.
//
//   1. The flag was read once per domain reload, so `touch` alone did nothing.
//      Unity skips the reimport when a file's contents have not changed, so no
//      reload happened and the flag just sat there. Hence polling.
//   2. The flag was deleted when EnterPlaymode was *called*, not when Play
//      actually began. EnterPlaymode is a request Unity can ignore (mid-compile,
//      mid-import, an unsaved-scene prompt), which left the flag consumed with
//      nothing running. Hence: retire the flag only once isPlaying is observed,
//      and keep retrying until then.
//   3. "Did we start this session?" cannot live in a static. Entering Play
//      triggers a domain reload, which resets statics, so the tool would decide
//      its own session was stale and stop it, forever. Hence SessionState, which
//      survives reloads within one editor session.
[InitializeOnLoad]
public static class AutoVerifyPlay
{
    private const string PendingKey = "AutoVerifyPlay.pending";
    private const double PollSeconds = 0.5;
    private const double RetrySeconds = 2.0;

    static string Marker => Path.Combine(Application.dataPath, "..", "auto-verify.flag");

    private static double nextPoll, nextAttempt;

    static AutoVerifyPlay() => EditorApplication.update += Poll;

    static void Poll()
    {
        if (EditorApplication.timeSinceStartup < nextPoll) return;
        nextPoll = EditorApplication.timeSinceStartup + PollSeconds;

        if (EditorApplication.isCompiling || EditorApplication.isUpdating) return;

        if (!File.Exists(Marker))
        {
            SessionState.EraseBool(PendingKey);
            return;
        }

        if (EditorApplication.isPlaying)
        {
            if (SessionState.GetBool(PendingKey, false))
            {
                // Our request took. Retire the flag so the run happens once.
                try { File.Delete(Marker); } catch { }
                SessionState.EraseBool(PendingKey);
            }
            else
            {
                // Someone else's session is running; restart it so the run is fresh.
                Debug.Log("AutoVerifyPlay: stopping stale Play session first");
                EditorApplication.ExitPlaymode();
            }
            return;
        }

        if (EditorApplication.timeSinceStartup < nextAttempt) return;
        nextAttempt = EditorApplication.timeSinceStartup + RetrySeconds;

        // Pin the Game view first: entering Play at the wrong size wastes the run.
        GameViewSizePin.ApplyRequested();
        SessionState.SetBool(PendingKey, true);
        Debug.Log("AutoVerifyPlay: requesting Play mode");
        EditorApplication.EnterPlaymode();
    }
}
