using System.Collections.Generic;
using UnityEngine;

public class AIPlayer
{
    public enum Difficulty { Easy, Medium, Hard }
    
    private Difficulty difficulty;
    private GamePlayer player;
    
    public AIPlayer(GamePlayer gamePlayer, Difficulty diff)
    {
        player = gamePlayer;
        difficulty = diff;
    }
    
    public void SetDifficulty(Difficulty newDifficulty)
    {
        difficulty = newDifficulty;
        Debug.Log(player.playerName + " AI difficulty set to: " + difficulty);
    }
    
    public int GetBestMove(List<PlayingCard> tableCards)
    {
        switch (difficulty)
        {
            case Difficulty.Easy:
                return GetEasyMove();
            case Difficulty.Medium:
                return GetMediumMove(tableCards);
            case Difficulty.Hard:
                return GetHardMove(tableCards);
            default:
                return 0;
        }
    }
    
    // Easy: Random valid move
    private int GetEasyMove()
    {
        int randomIndex = Random.Range(0, player.hand.Count);
        Debug.Log(player.playerName + " (Easy AI) plays random card at index " + randomIndex);
        return randomIndex;
    }
    
    // Medium: Prioritize captures, prefer high-value cards
    private int GetMediumMove(List<PlayingCard> tableCards)
    {
        List<int> captureIndices = new();
        List<int> highValueCaptureIndices = new();
        
        // Find all cards that can capture
        for (int i = 0; i < player.hand.Count; i++)
        {
            List<PlayingCard> captures = CaptureChecker.GetValidCaptures(player.hand[i], tableCards);
            
            if (captures.Count > 0)
            {
                captureIndices.Add(i);
                
                // Check if capturing high-value cards
                bool hasHighValue = false;
                foreach (PlayingCard card in captures)
                {
                    if (IsHighValueCard(card))
                    {
                        hasHighValue = true;
                        break;
                    }
                }
                
                if (hasHighValue)
                {
                    highValueCaptureIndices.Add(i);
                }
            }
        }
        
        // Prefer high-value captures
        if (highValueCaptureIndices.Count > 0)
        {
            int chosen = highValueCaptureIndices[Random.Range(0, highValueCaptureIndices.Count)];
            Debug.Log(player.playerName + " (Medium AI) plays high-value capture at index " + chosen);
            return chosen;
        }
        
        // Then prefer any capture
        if (captureIndices.Count > 0)
        {
            int chosen = captureIndices[Random.Range(0, captureIndices.Count)];
            Debug.Log(player.playerName + " (Medium AI) plays capture at index " + chosen);
            return chosen;
        }
        
        // Otherwise play randomly
        int randomIndex = Random.Range(0, player.hand.Count);
        Debug.Log(player.playerName + " (Medium AI) trails at index " + randomIndex);
        return randomIndex;
    }
    
    // Hard: Strategic play with lookahead
    private int GetHardMove(List<PlayingCard> tableCards)
    {
        int bestIndex = -1;
        int bestScore = -1;
        
        // Evaluate each card
        for (int i = 0; i < player.hand.Count; i++)
        {
            int cardScore = EvaluateMove(player.hand[i], tableCards, i);
            
            if (cardScore > bestScore)
            {
                bestScore = cardScore;
                bestIndex = i;
            }
        }
        
        Debug.Log(player.playerName + " (Hard AI) plays strategic move at index " + bestIndex + " (score: " + bestScore + ")");
        return bestIndex;
    }
    
    private int EvaluateMove(PlayingCard card, List<PlayingCard> tableCards, int cardIndex)
    {
        int score = 0;
        
        List<PlayingCard> captures = CaptureChecker.GetValidCaptures(card, tableCards);
        
        // Bonus for capturing
        score += captures.Count * 10;
        
        // Big bonus for sweep
        if (captures.Count > 0 && captures.Count == tableCards.Count)
        {
            score += 50;
        }
        
        // Bonus for Big Casino (10 of Diamonds)
        if (HasSpecificCard(captures, PlayingCard.Suit.Diamonds, PlayingCard.Rank.Ten))
        {
            score += 30;
        }
        
        // Bonus for Little Casino (2 of Spades)
        if (HasSpecificCard(captures, PlayingCard.Suit.Spades, PlayingCard.Rank.Two))
        {
            score += 20;
        }
        
        // Bonus for Aces
        int aceCount = 0;
        foreach (PlayingCard c in captures)
        {
            if (c.rank == PlayingCard.Rank.Ace)
                aceCount++;
        }
        score += aceCount * 15;
        
        // Bonus for Spades (for most spades scoring)
        int spadeCount = 0;
        foreach (PlayingCard c in captures)
        {
            if (c.suit == PlayingCard.Suit.Spades)
                spadeCount++;
        }
        score += spadeCount * 5;
        
        // If no capture, prefer trailing high-value cards to avoid opponent capturing them
        if (captures.Count == 0)
        {
            if (IsHighValueCard(card))
            {
                score -= 5; // Slight penalty for trailing high cards
            }
            else
            {
                score += 1; // Slight preference for trailing low cards
            }
        }
        
        return score;
    }
    
    private bool IsHighValueCard(PlayingCard card)
    {
        return (card.rank == PlayingCard.Rank.Ace ||
                card.rank == PlayingCard.Rank.Ten ||
                card.suit == PlayingCard.Suit.Spades ||
                (card.suit == PlayingCard.Suit.Diamonds && card.rank == PlayingCard.Rank.Ten) ||
                (card.suit == PlayingCard.Suit.Spades && card.rank == PlayingCard.Rank.Two));
    }
    
    private bool HasSpecificCard(List<PlayingCard> cards, PlayingCard.Suit suit, PlayingCard.Rank rank)
    {
        foreach (PlayingCard card in cards)
        {
            if (card.suit == suit && card.rank == rank)
                return true;
        }
        return false;
    }
}