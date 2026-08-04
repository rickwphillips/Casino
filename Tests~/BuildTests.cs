using System;
using System.Collections.Generic;
using System.Linq;
public static class BuildTests {
    static PlayingCard C(string s) {
        var suit = PlayingCard.Suit.Hearts;
        var rank = s switch { "A" => PlayingCard.Rank.Ace, "J" => PlayingCard.Rank.Jack,
            "Q" => PlayingCard.Rank.Queen, "K" => PlayingCard.Rank.King,
            var n => (PlayingCard.Rank)(int.Parse(n) - 1) };
        return new PlayingCard(suit, rank);
    }
    public static int Main() {
        int fails = 0;
        void T(string name, string hand, string[] table, int[] want) {
            var got = CaptureChecker.PossibleBuildValues(C(hand), table.Select(C).ToList());
            bool ok = got.OrderBy(x=>x).SequenceEqual(want.OrderBy(x=>x));
            if (!ok) fails++;
            Console.WriteLine($"{(ok?"PASS":"FAIL")}  {name}: got [{string.Join(",",got)}] want [{string.Join(",",want)}]");
        }
        T("6 + [4,10,8,2] = three 10s", "6", new[]{"4","10","8","2"}, new[]{10});
        T("6 + [4] = build 10", "6", new[]{"4"}, new[]{10});
        T("3 + [3] = build 6 (pair also 3? no: both used)", "3", new[]{"3"}, new[]{3,6});
        T("Q + [Q] = Queens", "Q", new[]{"Q"}, new[]{12});
        T("Q + [5] = nothing", "Q", new[]{"5"}, new int[0]);
        T("6 + [5] = nothing (11)", "6", new[]{"5"}, new int[0]);
        Console.WriteLine(fails==0?"ALL PASS":$"{fails} FAILURES");
        return fails;
    }
}
