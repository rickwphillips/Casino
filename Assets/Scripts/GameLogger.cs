using System.Collections.Generic;
using UnityEngine;

public class GameLogger : MonoBehaviour
{
    public static GameLogger Instance { get; private set; }
    
    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }
    
    public void LogGameStart()
    {
        Debug.Log("\n" + new string('=', 80));
        Debug.Log("CASINO GAME STARTED");
        if (ScoringManager.Instance != null)
        {
            Debug.Log("Variant: " + ScoringManager.Instance.GetCurrentVariant());
            Debug.Log("Win Score: " + ScoringManager.Instance.GetWinScore());
        }
        Debug.Log(new string('=', 80) + "\n");
    }
    
    public void LogInitialDeal(GamePlayer dealer, GamePlayer nonDealer, List<PlayingCard> tableCards)
    {
        Debug.Log("\n--- INITIAL DEAL ---");
        Debug.Log("Dealer: " + dealer.playerName);
        Debug.Log("Non-Dealer: " + nonDealer.playerName);
        Debug.Log("\nCards dealt to each player: 4");
        Debug.Log("Cards on table: " + string.Join(", ", ConvertCardsToStrings(tableCards)));
    }
    
    public void LogPlayerTurn(GamePlayer player, PlayingCard playedCard)
    {
        Debug.Log("\n>>> " + player.playerName.ToUpper() + "'S TURN");
        Debug.Log("    Card played: " + playedCard);
    }
    
    public void LogCapture(GamePlayer player, List<PlayingCard> capturedCards)
    {
        Debug.Log("    ✓ CAPTURED " + capturedCards.Count + " card(s): " + string.Join(", ", ConvertCardsToStrings(capturedCards)));
        Debug.Log("    Total captured this round: " + player.capturedCards.Count);
    }
    
    public void LogSweep(GamePlayer player)
    {
        Debug.Log("    ★ SWEEP! Captured ALL cards on the table!");
        Debug.Log("    Sweep count: " + player.sweepCount);
    }
    
    public void LogTrail(GamePlayer player, PlayingCard trailedCard, List<PlayingCard> tableCards)
    {
        Debug.Log("    → Trailed (no capture): " + trailedCard);
        Debug.Log("    Table now has: " + string.Join(", ", ConvertCardsToStrings(tableCards)));
    }
    
    public void LogRoundEnd(int roundNumber, int cardsPlayedThisRound)
    {
        Debug.Log("\n" + new string('-', 80));
        Debug.Log("ROUND " + roundNumber + " COMPLETE - Both players played all 4 cards");
        Debug.Log(new string('-', 80));
    }
    
    public void LogRemainingTableCards(GamePlayer player, List<PlayingCard> remainingCards)
    {
        Debug.Log(player.playerName + " gets remaining table cards: " + string.Join(", ", ConvertCardsToStrings(remainingCards)));
    }
    
    public void LogDeckStatus(int cardsRemaining)
    {
        Debug.Log("\nCards remaining in deck: " + cardsRemaining);
    }
    
    public void LogNewDeal(int handNumber)
    {
        Debug.Log("\n--- HAND " + handNumber + " DEALT ---");
        Debug.Log("Each player gets 4 more cards");
    }
    
    public void LogScoringStart()
    {
        Debug.Log("\n" + new string('=', 80));
        Debug.Log("SCORING THIS HAND");
        Debug.Log(new string('=', 80));
    }
    
    public void LogScoreAward(string player, string reason, int points)
    {
        Debug.Log("  " + player + " gets " + points + " point(s) for: " + reason);
    }
    
    public void LogHandTotals(GamePlayer player1, GamePlayer player2, int player1Cards, int player2Cards, int player1Spades, int player2Spades)
    {
        Debug.Log("\n  Stats:");
        Debug.Log("    " + player1.playerName + ": " + player1Cards + " cards, " + player1Spades + " spades");
        Debug.Log("    " + player2.playerName + ": " + player2Cards + " cards, " + player2Spades + " spades");
    }
    
    public void LogCumulativeScores(GamePlayer player1, GamePlayer player2)
    {
        Debug.Log("\n" + new string('-', 80));
        Debug.Log("CUMULATIVE SCORES:");
        Debug.Log("  " + player1.playerName + ": " + player1.score + " points");
        Debug.Log("  " + player2.playerName + ": " + player2.score + " points");
        Debug.Log(new string('-', 80) + "\n");
    }
    
    public void LogDealerSwap(GamePlayer newDealer)
    {
        Debug.Log("\nDEALER SWAP: " + newDealer.playerName + " is now the dealer");
    }
    
    public void LogGameOver(GamePlayer winner, int winScore)
    {
        Debug.Log("\n" + new string('=', 80));
        Debug.Log("GAME OVER!");
        Debug.Log(new string('=', 80));
        Debug.Log("\nWINNER: " + winner.playerName);
        Debug.Log("Final Score: " + winner.score + " points (needed " + winScore + " to win)");
        Debug.Log(new string('=', 80) + "\n");
    }
    
    private List<string> ConvertCardsToStrings(List<PlayingCard> cards)
    {
        List<string> cardStrings = new List<string>();
        foreach (PlayingCard card in cards)
        {
            cardStrings.Add(card.ToString());
        }
        return cardStrings;
    }
}