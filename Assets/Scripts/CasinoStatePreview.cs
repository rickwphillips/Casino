using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

// Design-verification harness for the card state vocabulary.
//
// The four card states and the build tags are wired to the right call sites, but
// an opening deal shows none of them: there is no build on the table and nothing
// is selected. Reaching a board that exercises them by playing takes a human.
//
// Dropping <project>/state-preview.flag stages one instead: an opponent's single
// (raisable) build, a player's multi (locked) build, and all four card states on
// the hand at once. Purely presentational; it mutates the board only so the UI
// has something to draw, and it is never active without the flag.
public class CasinoStatePreview : MonoBehaviour
{
    private const float Delay = 2.5f;   // after the deal, before the settled screenshot

    private static string FlagPath =>
        Path.GetFullPath(Path.Combine(Application.dataPath, "..", "state-preview.flag"));

    // Second mode: stage one card of every awkward rank so typography can be
    // judged. The deal is random, so waiting for a J to turn up is not a plan.
    private static string RankFlagPath =>
        Path.GetFullPath(Path.Combine(Application.dataPath, "..", "rank-preview.flag"));

    // Third mode: a board rigged so the three rules that have never been driven
    // by hand are all reachable from one position, without waiting for a deal to
    // produce them (a partial capture with disjoint options, a steal of an
    // opponent's build, and a raise that transfers ownership).
    private static string ScenarioFlagPath =>
        Path.GetFullPath(Path.Combine(Application.dataPath, "..", "scenario-preview.flag"));

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Install()
    {
        if (!File.Exists(FlagPath) && !File.Exists(RankFlagPath) && !File.Exists(ScenarioFlagPath)) return;
        UIManager.SkipTitle = true;
        // The staged boards and transcripts assume the human is non-dealer
        // and moves first; pin the seat so runs stay reproducible.
        GameManager.ForceHumanNonDealer = true;   // these stage a board to photograph
        var host = new GameObject("CasinoStatePreview");
        host.AddComponent<CasinoStatePreview>();
        DontDestroyOnLoad(host);
    }

    private void Start() => StartCoroutine(Stage());

    private IEnumerator Stage()
    {
        yield return new WaitForSeconds(Delay);
        bool ranksOnly = File.Exists(RankFlagPath) && !File.Exists(FlagPath);
        bool scenario = File.Exists(ScenarioFlagPath) && !File.Exists(FlagPath) && !File.Exists(RankFlagPath);
        try { File.Delete(FlagPath); } catch { }
        try { File.Delete(RankFlagPath); } catch { }
        try { File.Delete(ScenarioFlagPath); } catch { }

        var gm = GameManager.Instance;
        var ui = UIManager.Instance;
        if (gm == null || ui == null) { Debug.LogWarning("StatePreview: no GameManager/UIManager"); yield break; }

        if (scenario) { StageScenario(gm, ui); yield break; }

        if (ranksOnly)
        {
            // Every rank that behaves differently in this face: a descending J,
            // the tailed Q, plus digits that ascend (6, 9) and sit low (10, 4).
            Restack(gm.GetNonDealer(), "JS", "QH", "KC", "10D");
            var sampler = gm.GetTableCards();
            sampler.Clear();
            sampler.AddRange(new[] { Card("AS"), Card("6D"), Card("9C"), Card("4H") });
            ui.RefreshUI();
            Debug.Log("StatePreview: staged a rank sampler");
            yield break;
        }

        var table = gm.GetTableCards();
        var builds = gm.GetActiveBuilds();
        if (table == null || builds == null || table.Count < 4) { Debug.LogWarning("StatePreview: table too small"); yield break; }

        // Two builds from the loose table cards: one raisable, one locked.
        var theirs = new List<PlayingCard> { table[0], table[1] };
        var mine = new List<PlayingCard> { table[2], table[3] };
        table.RemoveRange(0, 4);

        builds.Add(new Build(theirs, Value(theirs), gm.GetDealer(), false));
        builds.Add(new Build(mine, Value(mine), gm.GetNonDealer(), true));

        ui.RefreshUI();
        yield return null;              // let the build UI exist before tagging states
        ui.ApplyStatePreview();

        Debug.Log("StatePreview: staged two builds and all four card states");
    }

    // One board, three untested rules, all reachable from the human's first turn.
    //
    //   Hand   9♠  6♦  2♠  8♣
    //   Table  9♣  5♦  4♠  6♥  3♦   plus a DEALER-owned single build of 6 (2♣+4♦)
    //
    // 9♠ is the README's own partial-capture example: it can take the lone 9♣,
    // or 5♦+4♠, or 6♥+3♦, or any combination. The point of the rule is that the
    // player chooses, so the interesting test is taking only one of those sets
    // and confirming the rest stay on the table.
    //
    // 6♦ takes the opponent's build of 6, and may sweep the loose 6♥ along with
    // it in the same play. Combining an opponent's build with table cards is the
    // steal, and it is legal precisely because the build is not yours.
    //
    // 2♠ raises that build to 8, which is legal only while it is a single build
    // and only because 8♣ is in hand to capture the new total. Ownership moves to
    // the raiser. Playing 6♦ onto it instead adds at value, which locks it as a
    // multi-build: the same board reaches RAISABLE and LOCKED by two different
    // plays, which is what makes the tag worth showing.
    //
    // Cards are dealt from a shuffled deck first, so a rank staged here can also
    // sit in the AI's hand. Harmless for driving the UI, wrong for judging play.
    private static void StageScenario(GameManager gm, UIManager ui)
    {
        var table = gm.GetTableCards();
        var builds = gm.GetActiveBuilds();
        if (table == null || builds == null)
        {
            Debug.LogWarning("StatePreview: no table or builds to stage into");
            return;
        }

        // Two 6s on purpose. Adding at value requires still holding a card that
        // captures the build after the played one leaves your hand, so with a
        // single 6 the game correctly refuses with "Need another to take it" and
        // the locked multi-build can never be reached from this board. Swap 6♠
        // for 2♠ to test raising instead, which needs the 8♣ as the capture card.
        Restack(gm.GetNonDealer(), "9S", "6D", "6S", "8C");

        table.Clear();
        table.AddRange(new[] { Card("9C"), Card("5D"), Card("4S"), Card("6H"), Card("3D") });

        builds.Clear();
        builds.Add(new Build(new List<PlayingCard> { Card("2C"), Card("4D") },
                             6, gm.GetDealer(), false));   // single, so raisable and stealable

        ui.RefreshUI();
        Debug.Log("StatePreview: staged the scenario board. " +
                  "9♠ = partial capture (9♣ / 5♦+4♠ / 6♥+3♦), " +
                  "6♦ = steal the 6-build (optionally with 6♥), " +
                  "2♠ = raise it to 8, 6♦ onto it = add at value and lock as multi.");
    }

    private static void Restack(GamePlayer p, params string[] codes)
    {
        p.ClearHand();
        foreach (var c in codes) p.AddCard(Card(c));
    }

    // "10D" / "JS" / "AH": rank then suit letter.
    private static PlayingCard Card(string code)
    {
        string r = code.Substring(0, code.Length - 1);
        char su = code[code.Length - 1];
        var suit = su switch
        {
            'H' => PlayingCard.Suit.Hearts,
            'D' => PlayingCard.Suit.Diamonds,
            'C' => PlayingCard.Suit.Clubs,
            _ => PlayingCard.Suit.Spades
        };
        var rank = r switch
        {
            "A" => PlayingCard.Rank.Ace, "J" => PlayingCard.Rank.Jack,
            "Q" => PlayingCard.Rank.Queen, "K" => PlayingCard.Rank.King,
            _ => (PlayingCard.Rank)(int.Parse(r) - 1)
        };
        return new PlayingCard(suit, rank);
    }

    private static int Value(List<PlayingCard> cards)
    {
        // Rank is zero-based (Ace = 0), so face value is the ordinal plus one.
        int sum = 0;
        foreach (var c in cards) sum += (int)c.rank + 1;
        return Mathf.Clamp(sum, 1, 10);   // builds are 1-10 unless they are face builds
    }
}
