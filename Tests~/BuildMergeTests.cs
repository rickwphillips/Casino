using System;
using System.Collections.Generic;
using System.Linq;

// Rules: two builds of the same declared value never coexist on the table. A
// second group of that value joins the existing stack, which locks the build as
// a multi-build (unraisable) and passes ownership to the builder.
public static class BuildMergeTests
{
    static PlayingCard C(string s)
    {
        var rank = s switch { "A" => PlayingCard.Rank.Ace, "J" => PlayingCard.Rank.Jack,
            "Q" => PlayingCard.Rank.Queen, "K" => PlayingCard.Rank.King,
            var n => (PlayingCard.Rank)(int.Parse(n) - 1) };
        return new PlayingCard(PlayingCard.Suit.Hearts, rank);
    }

    static int failures = 0;

    static void T(string name, object got, object want)
    {
        bool ok = Equals(got, want);
        if (!ok) failures++;
        Console.WriteLine($"{(ok ? "PASS" : "FAIL")}  {name}  (got {got}, want {want})");
    }

    public static int Main()
    {
        var me = new GamePlayer("Me");
        var foe = new GamePlayer("Foe");

        // The reported case: 7+2 built as 9, then 6+3 built as 9 on a later turn.
        var table = new List<Build>
        {
            new Build(new List<PlayingCard> { C("7"), C("2") }, 9, me, false)
        };
        var second = new List<PlayingCard> { C("6"), C("3") };

        var target = Build.FindMergeTarget(table, 9);
        T("second 9 finds the existing 9-build", target != null, true);

        target.MergeGroup(second, me);
        T("one build remains, not two", table.Count, 1);
        T("stack holds all four cards", target.Cards.Count, 4);
        T("merged build is multi (unraisable)", target.IsMultiBuild, true);
        T("declared value unchanged", target.DeclaredValue, 9);

        // Raising a merged build is refused at the source.
        bool threw = false;
        try { target.ModifyBuild(C("A"), 10, me); } catch (InvalidOperationException) { threw = true; }
        T("merged build cannot be raised", threw, true);

        // A different value starts its own stack.
        T("no merge target for a value not on the table",
            Build.FindMergeTarget(table, 8) == null, true);

        // An opponent's build of the same value is a merge target too, and the
        // builder takes it over.
        var foeTable = new List<Build>
        {
            new Build(new List<PlayingCard> { C("5"), C("2") }, 7, foe, false)
        };
        var foeTarget = Build.FindMergeTarget(foeTable, 7);
        foeTarget.MergeGroup(new List<PlayingCard> { C("4"), C("3") }, me);
        T("merging an opponent's build transfers ownership", foeTarget.Owner, me);
        T("opponent's merged build is multi", foeTarget.IsMultiBuild, true);

        T("empty table has no merge target", Build.FindMergeTarget(new List<Build>(), 9) == null, true);

        Console.WriteLine(failures == 0 ? "ALL PASS" : $"{failures} FAILURES");
        return failures;
    }
}
