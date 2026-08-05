using System.Collections;
using System.IO;
using UnityEngine;

// Plays a whole game by itself, so the real flow gets exercised.
//
// Everything verified so far came from an opening deal or a staged board. That
// never touches captures, sweeps, round ends, deck exhaustion, the table-card
// award, or game over: the parts most likely to be broken and least likely to be
// noticed. This drives the human seat with the same Hard evaluator that powers
// the Suggest hint, so a full game runs unattended.
//
// Drop <project>/autoplay.flag. Writes a transcript to autoplay-log.txt and
// screenshots the interesting moments. Never active without the flag.
//
// Put the word "probe" in the flag file to install the harness and stop after the
// first breadcrumb, driving nothing. That separates "the harness cannot even
// start" from "driving the game hangs" in one run.
//
// ---------------------------------------------------------------------------
// Why this file is written so defensively
//
// Two earlier attempts pinned a Unity thread at 100% with nothing on screen and
// no transcript, and the instrumentation could not say where. The flag was
// consumed, so Run() had begun; the transcript was absent, though its first
// statement wrote one. That left two readings that could not be told apart,
// because the old Note() called Debug.Log BEFORE it wrote the file: either the
// write failed, or Unity's own logging never returned.
//
// Hence the rules below, all of which exist to keep a breadcrumb honest:
//
//   - Mark() writes straight to disk with File.AppendAllText and does not touch
//     Debug.Log. A breadcrumb must not depend on the engine it is diagnosing.
//   - The transcript is truncated and the first breadcrumb written in Install(),
//     BEFORE the flag is consumed. A run that dies earlier than that leaves the
//     flag in place, which is itself the answer.
//   - Every breadcrumb carries realtimeSinceStartup, so a stalled clock and a
//     stalled coroutine look different.
//   - Waits are WaitForSecondsRealtime. WaitForSeconds is scaled by timeScale,
//     so a zero timeScale would stall the loop forever and read as a hang.
//   - The turn breadcrumb is written BEFORE asking the evaluator, not after. If
//     the Hard evaluator is what spins, the last line names the turn it spun on.
// ---------------------------------------------------------------------------
public class CasinoAutoPlay : MonoBehaviour
{
    private const float MoveDelay = 0.28f;
    private const float DealWait = 3.2f;
    private const int MoveCap = 400;        // backstop against a stuck turn loop

    private static string Root => Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
    private static string FlagPath => Path.Combine(Root, "autoplay.flag");
    private static string LogPath => Path.Combine(Root, "autoplay-log.txt");

    private static bool probeOnly;
    private int moves, rounds;
    private bool shotBuild;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Install()
    {
        if (!File.Exists(FlagPath)) return;

        // Read the mode before consuming the flag, and get a breadcrumb down
        // before anything else can hang. If the transcript is missing after a
        // run, nothing in this class ran at all.
        probeOnly = ReadFlagMode().Contains("probe");
        try { File.WriteAllText(LogPath, ""); } catch { }
        Mark($"install (probeOnly={probeOnly})");

        try { File.Delete(FlagPath); } catch { }
        Mark("flag consumed");

        var host = new GameObject("CasinoAutoPlay");
        host.AddComponent<CasinoAutoPlay>();
        DontDestroyOnLoad(host);
        Mark("host created");
    }

    private static string ReadFlagMode()
    {
        try { return File.ReadAllText(FlagPath).Trim().ToLowerInvariant(); }
        catch { return ""; }
    }

    private void Start()
    {
        Mark("Start");
        StartCoroutine(Run());
        Mark("coroutine returned to Start");
    }

    private IEnumerator Run()
    {
        Mark("Run entered");

        // The bisect: prove the harness can install, start, and survive a frame
        // without driving anything. If even this hangs, the game is not at fault.
        if (probeOnly)
        {
            yield return null;
            Mark("probe survived one frame");
            yield return new WaitForSecondsRealtime(2f);
            Mark("probe survived two seconds; stopping");
            yield break;
        }

        yield return new WaitForSecondsRealtime(DealWait);   // let the opening deal finish
        Mark("deal wait over");

        var gm = GameManager.Instance;
        var ui = UIManager.Instance;
        if (gm == null || ui == null) { Mark("no GameManager/UIManager"); yield break; }

        while (moves < MoveCap)
        {
            if (gm.GetCurrentPhase() == GameManager.GamePhase.GameOver)
            {
                Mark("GAME OVER");
                ScreenshotCapture.Capture("autoplay-gameover");
                break;
            }

            // A round summary blocks play until acknowledged.
            if (ui.IsSummaryOpen)
            {
                rounds++;
                Mark($"--- round {rounds} summary ---");
                ScreenshotCapture.Capture($"autoplay-round{rounds}");
                yield return new WaitForSecondsRealtime(0.6f);
                ui.ContinueSummary();
                yield return new WaitForSecondsRealtime(MoveDelay);
                continue;
            }

            // The build tags (RAISABLE / LOCKED) and the card-state colours have
            // only ever been photographed on a board staged by CasinoStatePreview.
            // A real build, made by real play, is the thing that proves they are
            // wired to the live state and not just to the preview harness.
            if (!shotBuild)
            {
                var builds = gm.GetActiveBuilds();
                if (builds != null && builds.Count > 0)
                {
                    shotBuild = true;
                    Mark($"first build on the table ({builds.Count}); capturing");
                    // Let the tweens land. Firing the instant the build exists
                    // caught table cards mid-scale, which is honest but useless
                    // as a reference shot of the build tags.
                    yield return new WaitForSecondsRealtime(1.2f);
                    ScreenshotCapture.Capture("autoplay-build");
                }
            }

            if (!gm.IsWaitingForHumanInput())
            {
                yield return new WaitForSecondsRealtime(MoveDelay);
                continue;
            }

            if (!PlayBestMove(gm)) { Mark("no suggestion available; stopping"); break; }
            moves++;
            yield return new WaitForSecondsRealtime(MoveDelay);
        }

        if (moves >= MoveCap) Mark($"hit move cap of {MoveCap} without finishing");
        Mark($"done: moves={moves} rounds={rounds}");
    }

    // The human seat plays whatever the Hard evaluator recommends, using the same
    // entry points the buttons use, so this exercises the real move validation
    // rather than a shortcut around it.
    private bool PlayBestMove(GameManager gm)
    {
        var me = gm.GetCurrentPlayer();

        // Before, not after: if the evaluator is the thing that spins, this line
        // is the last one on disk and it names the turn.
        Mark($"move {moves + 1}: asking the evaluator");
        var action = gm.GetSuggestionForCurrentPlayer();
        if (action == null) return false;

        switch (action.Type)
        {
            case AIPlayer.AIAction.ActionType.CreateBuild:
                Mark($"{me.Name}: build {action.DeclaredValue}");
                if (gm.TryCreateBuild(me, action.CardIndex, action.BuildCards)) return true;
                Mark("  build refused, playing card instead");
                break;

            case AIPlayer.AIAction.ActionType.ModifyBuild:
                Mark($"{me.Name}: raise build to {action.DeclaredValue}");
                if (gm.TryRaiseBuild(me, action.CardIndex, action.TargetBuild)) return true;
                Mark("  raise refused, playing card instead");
                break;

            case AIPlayer.AIAction.ActionType.AddToBuild:
                Mark($"{me.Name}: add to build {action.TargetBuild?.DeclaredValue}");
                if (gm.TryAddToBuild(me, action.CardIndex, action.TargetBuild)) return true;
                Mark("  add refused, playing card instead");
                break;

            default:
                Mark($"{me.Name}: play card {action.CardIndex}");
                break;
        }

        gm.PlayCard(me, action.CardIndex);
        return true;
    }

    // Straight to disk, appended, no Debug.Log. This is the one thing in the file
    // that must keep working when everything else does not.
    private static void Mark(string line)
    {
        try { File.AppendAllText(LogPath, $"[{Time.realtimeSinceStartup,7:F2}] {line}\n"); }
        catch { }
    }
}
