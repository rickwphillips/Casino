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

        // Find all possible capture combinations
        var allCombinations = FindAllCombinations(playedValue, tableCards);

        // Return the best combination (prioritize: most cards, then direct pairs)
        if (allCombinations.Count == 0) {
            return new List<PlayingCard>();
        }

        // Find combinations with direct rank matches
        var directPairs = allCombinations
            .Where(combo => combo.Any(card => card.rank == playedCard.rank))
            .ToList();

        // Prefer direct pairs, then combinations with most cards
        var bestCombination = (directPairs.Count > 0 ? directPairs : allCombinations)
            .OrderByDescending(combo => combo.Count)
            .ThenByDescending(combo => combo.Count(c => IsHighValueCard(c)))
            .First();

        return bestCombination;
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
}