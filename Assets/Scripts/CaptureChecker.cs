using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class CaptureChecker {
    public static List<PlayingCard> GetValidCaptures(PlayingCard playedCard, List<PlayingCard> tableCards) {
        List<PlayingCard> captures = new();
        
        // Aces are worth 1, face cards are worth 10, numbered cards are worth their number
        int playedValue = GetCardValue(playedCard);
        
        // Face cards can only capture matching face cards
        if (IsFaceCard(playedCard)) {
            return tableCards
                .Where(tableCard => IsFaceCard(tableCard) && tableCard.rank == playedCard.rank)
                .ToList();
        }
        
        // Check for direct pair
        captures.AddRange(
            tableCards.Where(tableCard => !IsFaceCard(tableCard) && tableCard.rank == playedCard.rank)
        );
        
        // Check for combinations that sum to played card value
        // Add any valid combinations that sum to played card value
        captures.AddRange(
            FindCombination(playedValue, tableCards, new(), 0)
                .Where(card => !captures.Contains(card))
        );
        
        return captures;
    }
    
    private static int GetCardValue(PlayingCard card) => card.rank switch {
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
    
    private static List<PlayingCard> FindCombination(int targetValue, List<PlayingCard> availableCards, List<PlayingCard> currentCombination, int currentSum) {
        if (currentSum == targetValue && currentCombination.Count > 0) {
            return new List<PlayingCard>(currentCombination);
        }
        
        if (currentSum >= targetValue) {
            return new List<PlayingCard>();
        }
        
        return availableCards
            .Select((card, index) => new {
                Card = card,
                Value = GetCardValue(card),
                Remaining = availableCards.Skip(index + 1).ToList()
            })
            .Select(x => {
                currentCombination.Add(x.Card);
                var result = FindCombination(targetValue, x.Remaining, currentCombination, currentSum + x.Value);
                currentCombination.RemoveAt(currentCombination.Count - 1);
                return result;
            })
            .FirstOrDefault(result => result.Count > 0) ?? new List<PlayingCard>();
    }
}