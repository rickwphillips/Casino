using System;
using System.Collections.Generic;
using System.Linq;

// Rules: an OPPONENT'S single-group build acts as a card of its value in a
// chosen sweep (a steal). The owner takes their own build only at its
// declared value. Multi-builds are locked to exact value for everyone.
public static class BuildSweepTests
{
    static PlayingCard C(string s)
    {
        var rank = s switch { "A" => PlayingCard.Rank.Ace, "J" => PlayingCard.Rank.Jack,
            "Q" => PlayingCard.Rank.Queen, "K" => PlayingCard.Rank.King,
            var n => (PlayingCard.Rank)(int.Parse(n) - 1) };
        return new PlayingCard(PlayingCard.Suit.Hearts, rank);
    }

    static int failures = 0;

    static void T(string name, bool got, bool want)
    {
        if (got != want) failures++;
        Console.WriteLine($"{(got == want ? "PASS" : "FAIL")}  {name}  (got {got}, want {want})");
    }

    public static int Main()
    {
        var me = new GamePlayer("Me");
        var foe = new GamePlayer("Foe");

        Build Single(int v, GamePlayer owner) =>
            new Build(new List<PlayingCard> { C(v.ToString()) }, v, owner, false);
        Build Multi(int v, GamePlayer owner) =>
            new Build(new List<PlayingCard> { C(v.ToString()), C(v.ToString()) }, v, owner, true);

        bool Check(string played, List<PlayingCard> cards, List<Build> builds) =>
            CaptureChecker.IsExactCaptureSetWithBuilds(C(played), me, cards, builds);

        T("steal: foe's 6-build + table 2 falls to my 8",
            Check("8", new List<PlayingCard> { C("2") }, new List<Build> { Single(6, foe) }), true);

        T("no self-combine: MY 6-build + table 2 vs my 8",
            Check("8", new List<PlayingCard> { C("2") }, new List<Build> { Single(6, me) }), false);

        T("owner takes own build at value",
            Check("6", new List<PlayingCard>(), new List<Build> { Single(6, me) }), true);

        T("owner takes own build + matching table group",
            Check("6", new List<PlayingCard> { C("4"), C("2") }, new List<Build> { Single(6, me) }), true);

        T("multi locked: foe's multi-6 + 2 vs my 8",
            Check("8", new List<PlayingCard> { C("2") }, new List<Build> { Multi(6, foe) }), false);

        T("multi taken at exact value",
            Check("6", new List<PlayingCard>(), new List<Build> { Multi(6, foe) }), true);

        T("two foe singles combine: 3-build + 6-build vs my 9",
            Check("9", new List<PlayingCard>(), new List<Build> { Single(3, foe), Single(6, foe) }), true);

        T("build alone below value is not takeable",
            Check("8", new List<PlayingCard>(), new List<Build> { Single(6, foe) }), false);

        Console.WriteLine(failures == 0 ? "ALL PASS" : $"{failures} FAILURES");
        return failures;
    }
}
