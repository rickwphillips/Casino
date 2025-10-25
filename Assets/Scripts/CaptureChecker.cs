using System.Collections.Generic;
using UnityEngine;

public class CaptureChecker
{
    public static List<PlayingCard> GetValidCaptures(PlayingCard playedCard, List<PlayingCard> tableCards)
    {
        List<PlayingCard> captures = new();
        
        // Aces are worth 1, face cards are worth 10, numbered cards are worth their number
        int playedValue = GetCardValue(playedCard);
        
        // Face cards can only capture matching face cards
        if (IsFaceCard(playedCard))
        {
            foreach (PlayingCard tableCard in tableCards)
            {
                if (IsFaceCard(tableCard) && tableCard.rank == playedCard.rank)
                {
                    captures.Add(tableCard);
                }
            }
            return captures;
        }
        
        // Check for direct pair
        foreach (PlayingCard tableCard in tableCards)
        {
            if (!IsFaceCard(tableCard) && tableCard.rank == playedCard.rank)
            {
                captures.Add(tableCard);
            }
        }
        
        // Check for combinations that sum to played card value
        List<PlayingCard> combination = FindCombination(playedValue, tableCards, new List<PlayingCard>(), 0);
        foreach (PlayingCard card in combination)
        {
            if (!captures.Contains(card))
            {
                captures.Add(card);
            }
        }
        
        return captures;
    }
    
    private static int GetCardValue(PlayingCard card)
    {
        if (card.rank == PlayingCard.Rank.Ace) return 1;
        if (card.rank == PlayingCard.Rank.Two) return 2;
        if (card.rank == PlayingCard.Rank.Three) return 3;
        if (card.rank == PlayingCard.Rank.Four) return 4;
        if (card.rank == PlayingCard.Rank.Five) return 5;
        if (card.rank == PlayingCard.Rank.Six) return 6;
        if (card.rank == PlayingCard.Rank.Seven) return 7;
        if (card.rank == PlayingCard.Rank.Eight) return 8;
        if (card.rank == PlayingCard.Rank.Nine) return 9;
        if (card.rank == PlayingCard.Rank.Ten) return 10;
        return 10; // Jack, Queen, King
    }
    
    private static bool IsFaceCard(PlayingCard card)
    {
        return card.rank == PlayingCard.Rank.Jack || 
               card.rank == PlayingCard.Rank.Queen || 
               card.rank == PlayingCard.Rank.King;
    }
    
    private static List<PlayingCard> FindCombination(int targetValue, List<PlayingCard> availableCards, List<PlayingCard> currentCombination, int currentSum)
    {
        if (currentSum == targetValue && currentCombination.Count > 0)
        {
            return new List<PlayingCard>(currentCombination);
        }
        
        if (currentSum >= targetValue)
        {
            return new List<PlayingCard>();
        }
        
        for (int i = 0; i < availableCards.Count; i++)
        {
            PlayingCard card = availableCards[i];
            int cardValue = GetCardValue(card);
            
            currentCombination.Add(card);
            List<PlayingCard> remaining = availableCards.GetRange(i + 1, availableCards.Count - i - 1);
            
            List<PlayingCard> result = FindCombination(targetValue, remaining, currentCombination, currentSum + cardValue);
            if (result.Count > 0)
            {
                return result;
            }
            
            currentCombination.RemoveAt(currentCombination.Count - 1);
        }
        
        return new List<PlayingCard>();
    }
}