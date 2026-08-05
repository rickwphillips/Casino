using System.Collections;
using System.IO;
using UnityEngine;

// Drives the UI the way a player does, and writes down what it said.
//
// CasinoAutoPlay proves the rules survive a whole game, but it calls GameManager
// directly, so it never touches the layer between a player and those rules:
// selecting a card, seeing what that selection can take, asking for a
// suggestion, or being told why a move was refused. That layer is all in
// UIManager, and until now the only way to test it was to sit and click.
//
// This probe walks the four paths a player actually uses and captures both a
// screenshot and the hint line at each step, because the hint line is where the
// game explains itself. A screenshot shows it did not crash; the hint text shows
// it said something true.
//
// Drop <project>/interaction-probe.flag. Writes interaction-probe.txt. Never
// active without the flag.
public class CasinoInteractionProbe : MonoBehaviour
{
    private static string Root => Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
    private static string FlagPath => Path.Combine(Root, "interaction-probe.flag");
    private static string LogPath => Path.Combine(Root, "interaction-probe.txt");

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Install()
    {
        if (!File.Exists(FlagPath)) return;
        try { File.WriteAllText(LogPath, ""); } catch { }
        Mark("install");
        try { File.Delete(FlagPath); } catch { }

        var host = new GameObject("CasinoInteractionProbe");
        host.AddComponent<CasinoInteractionProbe>();
        DontDestroyOnLoad(host);
    }

    private void Start() => StartCoroutine(Run());

    private IEnumerator Run()
    {
        yield return new WaitForSecondsRealtime(3.4f);   // deal settles

        var gm = GameManager.Instance;
        var ui = UIManager.Instance;
        if (gm == null || ui == null) { Mark("no GameManager/UIManager"); yield break; }

        // The human is non-dealer and moves first, so the board is already
        // waiting on input; if it is not, this run tells us nothing.
        if (!gm.IsWaitingForHumanInput()) { Mark("not the human's turn; aborting"); yield break; }

        var hand = ui.HumanHandCardUIs;
        var table = ui.TableCardUIs;
        if (hand == null || hand.Count == 0 || table == null || table.Count == 0)
        {
            Mark($"nothing to drive (hand={hand?.Count ?? -1} table={table?.Count ?? -1})");
            yield break;
        }

        Mark($"hand={hand.Count} table={table.Count}");

        // 1. Select a hand card. This should light up whatever it can capture.
        var handCard = hand[0];
        Mark($"click hand card {Describe(handCard)} -> {handCard.SimulateClick()}");
        yield return Settle();
        Shot("probe-1-hand-selected", ui);

        // 2. Add a table card to the selection. Table-first selection is the
        //    primary flow, so this is the path most players spend time in.
        var tableCard = table[0];
        Mark($"click table card {Describe(tableCard)} -> {tableCard.SimulateClick()}");
        yield return Settle();
        Shot("probe-2-table-selected", ui);

        // 3. Ask for advice. Same evaluator the Hard AI uses.
        Mark("press Suggest");
        ui.PressSuggest();
        yield return Settle();
        Shot("probe-3-suggested", ui);

        // 4. Try to build from whatever is selected. Most selections are not a
        //    legal build, and the refusal arrives as a disabled button with a
        //    label saying why ("Not a build", "No 9 in hand"), not as a hint.
        //    PressBuild reports false when the button was not pressable, which is
        //    the normal outcome and not a failure.
        Mark($"press Build -> pressed={ui.PressBuild()}");
        yield return Settle();
        Shot("probe-4-build-attempt", ui);

        Mark("done");
    }

    private static WaitForSecondsRealtime Settle() => new(0.9f);

    private static void Shot(string label, UIManager ui)
    {
        ScreenshotCapture.Capture(label);
        Mark($"  hint:   \"{ui.CurrentHint}\"");
        Mark($"  sweep:  {ui.SweepButtonState}");
        Mark($"  build:  {ui.BuildButtonState}");
        Mark($"  trail:  {ui.TrailButtonState}");
    }

    private static string Describe(CardUI c) =>
        c == null || c.Card == null ? "(null)" : $"{c.Card.rank} of {c.Card.suit}";

    private static void Mark(string line)
    {
        try { File.AppendAllText(LogPath, $"[{Time.realtimeSinceStartup,7:F2}] {line}\n"); }
        catch { }
    }
}
