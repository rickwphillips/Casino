using System.Collections;
using System.IO;
using System.Linq;
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
        UIManager.SkipTitle = true;
        // The staged boards and transcripts assume the human is non-dealer
        // and moves first; pin the seat so runs stay reproducible.
        GameManager.ForceHumanNonDealer = true;   // the probe starts at the dealt board
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

        // 5. Deselecting the hand card must clear the table selection
        //    (ClearTableSelection out of OnCardSelected). A table card left
        //    highlighted with nothing in hand to spend it on is a lie the
        //    player acts on. Step 4 may have played a move, so wait for the
        //    turn to come back before driving cards again.
        for (float waited = 0f; !gm.IsWaitingForHumanInput() && waited < 12f; waited += 0.5f)
            yield return new WaitForSecondsRealtime(0.5f);
        if (!gm.IsWaitingForHumanInput())
        {
            Mark("5: turn never came back; skipping deselect check");
        }
        else
        {
            // An arbitrary pairing will not do: a face hand card refuses any
            // table card of another rank up front, and the check would pass
            // with nothing selected. A numeric hand card and a numeric table
            // card always stick (an invalid sweep is still a valid selection),
            // so pick those; only a hand or table of all face cards defeats it.
            hand = ui.HumanHandCardUIs;
            table = ui.TableCardUIs;
            var numericHand = hand?.FirstOrDefault(c => c?.Card != null && c.Card.rank < PlayingCard.Rank.Jack);
            var numericTable = table?.FirstOrDefault(c => c?.Card != null && c.Card.rank < PlayingCard.Rank.Jack);
            if (numericHand == null || numericTable == null)
            {
                Mark("5: no numeric hand/table pair this deal; deselect check inconclusive");
            }
            else
            {
                if (!numericHand.IsSelected)
                    Mark($"click hand card {Describe(numericHand)} -> {numericHand.SimulateClick()}");
                yield return Settle();
                if (!numericTable.IsSelected)
                    Mark($"click table card {Describe(numericTable)} -> {numericTable.SimulateClick()}");
                yield return Settle();
                int before = table.Count(c => c != null && c.IsSelected);
                Mark($"table selected before deselect: {before}");
                Mark($"click hand card again (deselect) -> {numericHand.SimulateClick()}");
                yield return Settle();
                int still = table.Count(c => c != null && c.IsSelected);
                Mark(before > 0 && still == 0
                    ? $"PASS: deselecting the hand card cleared all {before} selected table card(s)"
                    : before == 0
                        ? "INCONCLUSIVE: table selection never took"
                        : $"FAIL: {still} of {before} table card(s) still selected after hand deselect");
                Shot("probe-5-deselect-cleared", ui);
            }
        }

        // 6. A build owner must sweep or build, so while the build stands the
        //    Trail button should not exist at all, not sit disabled. Stage it
        //    through the UI when the deal allows: find a numeric hand+table
        //    pair whose sum another hand card can capture, build it, then
        //    check the action row once the turn comes back.
        for (float waited = 0f; !gm.IsWaitingForHumanInput() && waited < 12f; waited += 0.5f)
            yield return new WaitForSecondsRealtime(0.5f);
        hand = ui.HumanHandCardUIs;
        table = ui.TableCardUIs;
        var buildPair = (
            from h in hand
            where h?.Card != null && h.Card.rank < PlayingCard.Rank.Jack
            from t in table
            where t?.Card != null && t.Card.rank < PlayingCard.Rank.Jack
            let v = CaptureChecker.GetCardValue(h.Card) + CaptureChecker.GetCardValue(t.Card)
            where v <= 10 && hand.Any(o => o != h && o?.Card != null
                && CaptureChecker.BuildCaptureValue(o.Card) == v)
            select (h, t)).FirstOrDefault();
        if (!gm.IsWaitingForHumanInput() || buildPair.h == null)
        {
            Mark("6: no legal build this deal; trail-hiding check inconclusive");
        }
        else
        {
            if (!buildPair.h.IsSelected)
                Mark($"click hand card {Describe(buildPair.h)} -> {buildPair.h.SimulateClick()}");
            yield return Settle();
            if (!buildPair.t.IsSelected)
                Mark($"click table card {Describe(buildPair.t)} -> {buildPair.t.SimulateClick()}");
            yield return Settle();
            Mark($"press Build -> pressed={ui.PressBuild()}");

            // The AI answers; wait for the turn to come back with the build
            // still standing.
            for (float waited = 0f; !gm.IsWaitingForHumanInput() && waited < 12f; waited += 0.5f)
                yield return new WaitForSecondsRealtime(0.5f);
            if (!gm.IsWaitingForHumanInput() || !gm.PlayerOwnsBuild(gm.GetCurrentPlayer()))
            {
                Mark("6: build gone or turn never returned; trail-hiding check inconclusive");
            }
            else
            {
                var anyCard = ui.HumanHandCardUIs.FirstOrDefault(c => c != null);
                Mark($"click hand card {Describe(anyCard)} -> {anyCard.SimulateClick()}");
                yield return Settle();
                Mark(ui.TrailButtonState == "hidden"
                    ? "PASS: Trail hidden while owning a build"
                    : $"FAIL: Trail visible while owning a build ({ui.TrailButtonState})");
                Shot("probe-6-owner-no-trail", ui);
            }
        }

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
