using System;
using System.Collections.Generic;
using System.Linq;

public static class CaptureTests
{
    static PlayingCard C(string s)
    {
        var suit = s[^1] switch { 'H' => PlayingCard.Suit.Hearts, 'D' => PlayingCard.Suit.Diamonds,
                                  'C' => PlayingCard.Suit.Clubs, _ => PlayingCard.Suit.Spades };
        var rank = s[..^1] switch { "A" => PlayingCard.Rank.Ace, "J" => PlayingCard.Rank.Jack,
                                    "Q" => PlayingCard.Rank.Queen, "K" => PlayingCard.Rank.King,
                                    var n => (PlayingCard.Rank)(int.Parse(n) - 1) };
        return new PlayingCard(suit, rank);
    }

    static string Show(IEnumerable<PlayingCard> cards) =>
        cards.Any() ? string.Join(" ", cards.Select(c => c.ToString().Split(' ')[0])) : "(none)";

    static int failures = 0;

    static void Case(string name, string played, string[] table, string[] expect)
    {
        var captures = CaptureChecker.GetValidCaptures(C(played), table.Select(C).ToList());
        var got = captures.Select(c => c.rank).OrderBy(r => r).ToList();
        var want = expect.Select(e => C(e + "H").rank).OrderBy(r => r).ToList();
        bool ok = got.SequenceEqual(want);
        if (!ok) failures++;
        Console.WriteLine($"{(ok ? "PASS" : "FAIL")}  {name}");
        Console.WriteLine($"      play {played} into [{string.Join(" ", table)}]  ->  got: {Show(captures)}   want: {string.Join(" ", expect)}");
    }

    public static int Main()
    {
        Console.WriteLine("== combination capture ==");
        Case("6 takes 5+A", "6H", new[] { "5D", "AC" }, new[] { "5", "A" });
        Case("6 takes 6 AND 5+A together", "6H", new[] { "6S", "5D", "AC" }, new[] { "6", "5", "A" });
        Case("6 takes both 6s", "6H", new[] { "6S", "6D" }, new[] { "6", "6" });
        Case("10 takes 10 AND 6+4 AND 8+2", "10H", new[] { "10S", "6D", "4C", "8H", "2S" },
             new[] { "10", "6", "4", "8", "2" });
        Case("7 takes 3+4 twice", "7H", new[] { "3D", "4C", "3S", "4H" }, new[] { "3", "4", "3", "4" });

        Console.WriteLine("== face cards ==");
        Case("K takes both Ks", "KH", new[] { "KS", "KD", "5C" }, new[] { "K", "K" });
        Case("K ignores numbers", "KH", new[] { "6S", "7D" }, new string[0]);

        Console.WriteLine("== no capture ==");
        Case("9 vs nothing summing", "9H", new[] { "10S", "KD", "8C" }, new string[0]);

        Console.WriteLine("== sweep reachability ==");
        Case("full clear = sweep possible", "8H", new[] { "8S", "5D", "3C", "6H", "2S" },
             new[] { "8", "5", "3", "6", "2" });


        Console.WriteLine("== chosen-sweep validation ==");
        void Sel(string name, string played, string[] chosen, bool want)
        {
            bool got = CaptureChecker.IsExactCaptureSet(C(played), chosen.Select(C).ToList());
            if (got != want) failures++;
            Console.WriteLine($"{(got == want ? "PASS" : "FAIL")}  {name}  (got {got}, want {want})");
        }
        Sel("9 takes chosen 5+4", "9H", new[] { "5D", "4C" }, true);
        Sel("9 takes just one 9", "9H", new[] { "9S" }, true);
        Sel("partial is fine: 9 leaves other sets", "9H", new[] { "4C", "5D" }, true);
        Sel("9 can't take 5+K", "9H", new[] { "5D", "KC" }, false);
        Sel("9 can't take lone 5", "9H", new[] { "5D" }, false);
        Sel("7 takes 3+4 and 7 together", "7H", new[] { "3D", "4C", "7S" }, true);
        Sel("K takes chosen K", "KH", new[] { "KS" }, true);
        Sel("K can't take 6", "KH", new[] { "6S" }, false);

        Console.WriteLine(failures == 0 ? "\nALL PASS" : $"\n{failures} FAILURES");
        return failures;
    }
}
