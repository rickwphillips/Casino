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

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Install()
    {
        if (!File.Exists(FlagPath) && !File.Exists(RankFlagPath)) return;
        var host = new GameObject("CasinoStatePreview");
        host.AddComponent<CasinoStatePreview>();
        DontDestroyOnLoad(host);
    }

    private void Start() => StartCoroutine(Stage());

    private IEnumerator Stage()
    {
        yield return new WaitForSeconds(Delay);
        bool ranksOnly = File.Exists(RankFlagPath) && !File.Exists(FlagPath);
        try { File.Delete(FlagPath); } catch { }
        try { File.Delete(RankFlagPath); } catch { }

        var gm = GameManager.Instance;
        var ui = UIManager.Instance;
        if (gm == null || ui == null) { Debug.LogWarning("StatePreview: no GameManager/UIManager"); yield break; }

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
