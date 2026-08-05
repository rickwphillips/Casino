using System.Collections;
using System.IO;
using UnityEngine;

// Plays a whole game by itself, so the real flow gets exercised.
//
// Everything up to now was verified from an opening deal or a staged board. That
// never touches captures, sweeps, round ends, deck exhaustion, the table-card
// award, or game over: the parts most likely to be broken and least likely to be
// noticed. This drives the human seat with the same Hard evaluator that powers
// the Suggest hint, so a full game runs unattended.
//
// Drop <project>/autoplay.flag. Writes a move transcript to autoplay-log.txt and
// screenshots the interesting moments. Never active without the flag.
public class CasinoAutoPlay : MonoBehaviour
{
    private const float MoveDelay = 0.28f;
    private const int MoveCap = 400;        // backstop against a stuck turn loop

    private static string FlagPath =>
        Path.GetFullPath(Path.Combine(Application.dataPath, "..", "autoplay.flag"));
    private static string LogPath =>
        Path.GetFullPath(Path.Combine(Application.dataPath, "..", "autoplay-log.txt"));

    private readonly System.Text.StringBuilder log = new();
    private int moves, rounds;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Install()
    {
        if (!File.Exists(FlagPath)) return;
        var host = new GameObject("CasinoAutoPlay");
        host.AddComponent<CasinoAutoPlay>();
        DontDestroyOnLoad(host);
    }

    private void Start() => StartCoroutine(Run());

    private IEnumerator Run()
    {
        try { File.Delete(FlagPath); } catch { }
        Note("autoplay armed, waiting for the deal");
        yield return new WaitForSeconds(3.2f);   // let the opening deal finish

        var gm = GameManager.Instance;
        var ui = UIManager.Instance;
        if (gm == null || ui == null) { Note("no GameManager/UIManager"); Flush(); yield break; }

        var wait = new WaitForSeconds(MoveDelay);

        while (moves < MoveCap)
        {
            if (gm.GetCurrentPhase() == GameManager.GamePhase.GameOver)
            {
                Note("GAME OVER");
                ScreenshotCapture.Capture("autoplay-gameover");
                break;
            }

            // A round summary blocks play until acknowledged.
            if (ui.IsSummaryOpen)
            {
                rounds++;
                Note($"--- round {rounds} summary ---");
                ScreenshotCapture.Capture($"autoplay-round{rounds}");
                yield return new WaitForSeconds(0.6f);
                ui.ContinueSummary();
                yield return wait;
                continue;
            }

            if (!gm.IsWaitingForHumanInput()) { yield return wait; continue; }

            if (!PlayBestMove(gm)) { Note("no suggestion available; stopping"); break; }
            moves++;
            yield return wait;
        }

        if (moves >= MoveCap) Note($"hit move cap of {MoveCap} without finishing");
        Note($"moves={moves} rounds={rounds}");
        Flush();
    }

    // The human seat plays whatever the Hard evaluator recommends, using the same
    // entry points the buttons use, so this exercises the real move validation
    // rather than a shortcut around it.
    private bool PlayBestMove(GameManager gm)
    {
        var me = gm.GetCurrentPlayer();
        var action = gm.GetSuggestionForCurrentPlayer();
        if (action == null) return false;

        switch (action.Type)
        {
            case AIPlayer.AIAction.ActionType.CreateBuild:
                Note($"{me.Name}: build {action.DeclaredValue}");
                if (gm.TryCreateBuild(me, action.CardIndex, action.BuildCards)) return true;
                Note("  build refused, playing card instead");
                break;

            case AIPlayer.AIAction.ActionType.ModifyBuild:
                Note($"{me.Name}: raise build to {action.DeclaredValue}");
                if (gm.TryRaiseBuild(me, action.CardIndex, action.TargetBuild)) return true;
                Note("  raise refused, playing card instead");
                break;

            case AIPlayer.AIAction.ActionType.AddToBuild:
                Note($"{me.Name}: add to build {action.TargetBuild?.DeclaredValue}");
                if (gm.TryAddToBuild(me, action.CardIndex, action.TargetBuild)) return true;
                Note("  add refused, playing card instead");
                break;

            default:
                Note($"{me.Name}: play card {action.CardIndex}");
                break;
        }

        gm.PlayCard(me, action.CardIndex);
        return true;
    }

    private void Note(string line)
    {
        log.AppendLine(line);
        Debug.Log("[autoplay] " + line);
        Flush();
    }

    private void Flush()
    {
        try { File.WriteAllText(LogPath, log.ToString()); }
        catch (System.Exception e) { Debug.LogWarning($"autoplay log failed: {e.Message}"); }
    }
}
