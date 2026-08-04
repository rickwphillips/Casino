using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class CaptureChecker {
    public static List<PlayingCard> GetValidCaptures(PlayingCard playedCard, List<PlayingCard> tableCards) {
        // Aces are worth 1, face cards are worth 10, numbered cards are worth their number
        int playedValue = GetCardValue(playedCard);

        // Face cards can only capture matching face cards
        if (IsFaceCard(playedCard)) {
            return tableCards
                .Where(tableCard => IsFaceCard(tableCard) && tableCard.rank == playedCard.rank)
                .ToList();
        }

        // Casino rule: the played card captures ALL matching cards and ALL
        // sets summing to its value, simultaneously. Find the union of
        // disjoint combinations that captures the most cards.
        var eligibleCards = tableCards.Where(card => !IsFaceCard(card)).ToList();
        return MaxDisjointCapture(eligibleCards, playedValue);
    }

    // Largest union of disjoint card sets, each summing to the target value.
    private static List<PlayingCard> MaxDisjointCapture(List<PlayingCard> available, int targetValue) {
        var combinations = new List<List<PlayingCard>>();
        FindCombinationsRecursive(targetValue, available, new List<PlayingCard>(), 0, 0, combinations);

        var best = new List<PlayingCard>();
        foreach (var combo in combinations) {
            var remaining = new List<PlayingCard>(available);
            foreach (var card in combo) remaining.Remove(card);

            var rest = MaxDisjointCapture(remaining, targetValue);
            int total = combo.Count + rest.Count;

            if (total > best.Count ||
                (total == best.Count && total > 0 &&
                 combo.Concat(rest).Count(IsHighValueCard) > best.Count(IsHighValueCard))) {
                best = combo.Concat(rest).ToList();
            }

            // Cannot beat capturing every available card
            if (best.Count == available.Count) break;
        }

        return best;
    }

    private static List<List<PlayingCard>> FindAllCombinations(int targetValue, List<PlayingCard> tableCards) {
        var allCombinations = new List<List<PlayingCard>>();
        var eligibleCards = tableCards.Where(card => !IsFaceCard(card)).ToList();

        // Find all valid combinations recursively
        FindCombinationsRecursive(targetValue, eligibleCards, new List<PlayingCard>(), 0, 0, allCombinations);

        return allCombinations;
    }

    private static void FindCombinationsRecursive(
        int targetValue,
        List<PlayingCard> availableCards,
        List<PlayingCard> currentCombination,
        int currentSum,
        int startIndex,
        List<List<PlayingCard>> allCombinations)
    {
        if (currentSum == targetValue && currentCombination.Count > 0) {
            allCombinations.Add(new List<PlayingCard>(currentCombination));
            return;
        }

        if (currentSum > targetValue || startIndex >= availableCards.Count) {
            return;
        }

        for (int i = startIndex; i < availableCards.Count; i++) {
            var card = availableCards[i];
            int cardValue = GetCardValue(card);

            if (currentSum + cardValue <= targetValue) {
                currentCombination.Add(card);
                FindCombinationsRecursive(targetValue, availableCards, currentCombination, currentSum + cardValue, i + 1, allCombinations);
                currentCombination.RemoveAt(currentCombination.Count - 1);
            }
        }
    }

    private static bool IsHighValueCard(PlayingCard card) {
        return card.rank == PlayingCard.Rank.Ace ||
               card.rank == PlayingCard.Rank.Ten ||
               (card.suit == PlayingCard.Suit.Diamonds && card.rank == PlayingCard.Rank.Ten) ||
               (card.suit == PlayingCard.Suit.Spades && card.rank == PlayingCard.Rank.Two);
    }
    
    // True if the chosen cards are exactly capturable by the played card:
    // for a face card, all chosen share its rank; for a number card, the
    // chosen cards partition into sets each summing to its value. The player
    // chooses what to sweep - they never have to take everything available.
    public static bool IsExactCaptureSet(PlayingCard playedCard, List<PlayingCard> chosen) {
        if (chosen == null || chosen.Count == 0) return false;

        if (IsFaceCard(playedCard))
            return chosen.All(c => c.rank == playedCard.rank);

        if (chosen.Any(IsFaceCard)) return false;
        return CanPartitionExact(new List<PlayingCard>(chosen), GetCardValue(playedCard));
    }

    // Chosen-sweep validation including builds. An OPPONENT'S single-group
    // build counts as one atomic card of its declared value and may combine
    // with table cards (their build-of-6 + a table 2 falls to an 8) - a
    // steal. The build's own owner, multi-builds, and face builds are only
    // takeable as standalone exact matches.
    public static bool IsExactCaptureSetWithBuilds(PlayingCard playedCard, GamePlayer capturer,
        List<PlayingCard> chosenCards, List<Build> chosenBuilds) {
        chosenCards ??= new List<PlayingCard>();
        chosenBuilds ??= new List<Build>();
        if (chosenCards.Count == 0 && chosenBuilds.Count == 0) return false;

        int target = BuildCaptureValue(playedCard);

        if (IsFaceCard(playedCard))
            return chosenCards.All(c => c.rank == playedCard.rank) &&
                   chosenBuilds.All(b => b.DeclaredValue == target);

        if (chosenCards.Any(IsFaceCard)) return false;

        // Standalone-only builds: multis, face builds, and your OWN builds
        // (the owner takes their build at its declared value, never combined)
        bool Combinable(Build b) => !b.IsMultiBuild && b.DeclaredValue <= 10 && b.Owner != capturer;
        if (chosenBuilds.Any(b => !Combinable(b) && b.DeclaredValue != target)) return false;

        var values = chosenCards.Select(GetCardValue).ToList();
        values.AddRange(chosenBuilds.Where(Combinable).Select(b => b.DeclaredValue));
        // Standalone builds are their own complete groups; nothing to solve for them
        return values.Count == 0 || CanPartitionValues(values, target);
    }

    // Partition a multiset of values into groups each summing exactly to target
    private static bool CanPartitionValues(List<int> values, int target) {
        if (values.Count == 0) return true;
        if (values.Any(v => v <= 0 || v > target)) return false;
        if (values.Sum() % target != 0) return false;
        return BuildGroup(values, target, target - values[0], new List<int> { 0 });
    }

    private static bool BuildGroup(List<int> values, int target, int need, List<int> usedIdx) {
        if (need == 0) {
            var remaining = values.Where((v, i) => !usedIdx.Contains(i)).ToList();
            return CanPartitionValues(remaining, target);
        }
        for (int i = usedIdx[usedIdx.Count - 1] + 1; i < values.Count; i++) {
            if (usedIdx.Contains(i) || values[i] > need) continue;
            usedIdx.Add(i);
            if (BuildGroup(values, target, need - values[i], usedIdx)) return true;
            usedIdx.RemoveAt(usedIdx.Count - 1);
        }
        return false;
    }

    // All values 1-10 this hand card + table cards can declare as a build:
    // the combined cards must partition into sets each summing to the value.
    // Multiple sets = a multi-build (e.g. three 10s built at once).
    public static List<int> PossibleBuildValues(PlayingCard handCard, List<PlayingCard> tableCards) {
        var values = new List<int>();
        var all = new List<PlayingCard>(tableCards) { handCard };
        if (all.Any(IsFaceCard)) {
            if (all.All(c => c.rank == handCard.rank) && IsFaceCard(handCard))
                values.Add(BuildCaptureValue(handCard));
            return values;
        }
        for (int v = 1; v <= 10; v++)
            if (CanPartitionExact(new List<PlayingCard>(all), v))
                values.Add(v);
        return values;
    }

    public static bool CanPartitionExact(List<PlayingCard> cards, int target) {
        if (cards.Count == 0) return true;

        var combos = new List<List<PlayingCard>>();
        FindCombinationsRecursive(target, cards, new List<PlayingCard>(), 0, 0, combos);

        // Every card must be used: try each combo containing the first card
        foreach (var combo in combos.Where(c => c.Contains(cards[0]))) {
            var remaining = new List<PlayingCard>(cards);
            foreach (var c in combo) remaining.Remove(c);
            if (CanPartitionExact(remaining, target)) return true;
        }
        return false;
    }

    // Value a card counts for when capturing or declaring a BUILD:
    // numeric cards 1-10, face builds J=11 Q=12 K=13.
    public static int BuildCaptureValue(PlayingCard card) => card.rank switch {
        PlayingCard.Rank.Jack => 11,
        PlayingCard.Rank.Queen => 12,
        PlayingCard.Rank.King => 13,
        _ => GetCardValue(card)
    };

    public static int GetCardValue(PlayingCard card) => card.rank switch {
        PlayingCard.Rank.Ace => 1,
        PlayingCard.Rank.Two => 2,
        PlayingCard.Rank.Three => 3,
        PlayingCard.Rank.Four => 4,
        PlayingCard.Rank.Five => 5,
        PlayingCard.Rank.Six => 6,
        PlayingCard.Rank.Seven => 7,
        PlayingCard.Rank.Eight => 8,
        PlayingCard.Rank.Nine => 9,
        PlayingCard.Rank.Ten => 10,
        _ => 0  // Face cards (Jack, Queen, King) have no numeric value for captures
    };
    
    private static bool IsFaceCard(PlayingCard card) {
        return card.rank == PlayingCard.Rank.Jack ||
               card.rank == PlayingCard.Rank.Queen ||
               card.rank == PlayingCard.Rank.King;
    }

    // Short display form, e.g. "9♥" / "K♠"
    public static string Describe(PlayingCard card) {
        string rank = card.rank switch {
            PlayingCard.Rank.Ace => "A",
            PlayingCard.Rank.Jack => "J",
            PlayingCard.Rank.Queen => "Q",
            PlayingCard.Rank.King => "K",
            _ => ((int)card.rank + 1).ToString()
        };
        string suit = card.suit switch {
            PlayingCard.Suit.Hearts => "♥",
            PlayingCard.Suit.Diamonds => "♦",
            PlayingCard.Suit.Clubs => "♣",
            _ => "♠"
        };
        return rank + suit;
    }
}