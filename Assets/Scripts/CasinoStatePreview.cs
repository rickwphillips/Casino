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

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Install()
    {
        if (!File.Exists(FlagPath)) return;
        var host = new GameObject("CasinoStatePreview");
        host.AddComponent<CasinoStatePreview>();
        DontDestroyOnLoad(host);
    }

    private void Start() => StartCoroutine(Stage());

    private IEnumerator Stage()
    {
        yield return new WaitForSeconds(Delay);
        try { File.Delete(FlagPath); } catch { }

        var gm = GameManager.Instance;
        var ui = UIManager.Instance;
        if (gm == null || ui == null) { Debug.LogWarning("StatePreview: no GameManager/UIManager"); yield break; }

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

    private static int Value(List<PlayingCard> cards)
    {
        // Rank is zero-based (Ace = 0), so face value is the ordinal plus one.
        int sum = 0;
        foreach (var c in cards) sum += (int)c.rank + 1;
        return Mathf.Clamp(sum, 1, 10);   // builds are 1-10 unless they are face builds
    }
}
