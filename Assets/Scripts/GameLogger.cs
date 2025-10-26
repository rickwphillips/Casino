using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class GameLogger : MonoBehaviour
{
    public static GameLogger Instance { get; private set; }
    
    private void Awake() {
        if (Instance == null) { Instance = this; }
        else { Destroy(gameObject); }
    }

    public void LogGameStart() => new[] {
        "\n" + new string('=', 80),
        "CASINO GAME STARTED",
        ScoringManager.Instance != null ? $"Variant: {ScoringManager.Instance.GetCurrentVariant()}" : null,
        ScoringManager.Instance != null ? $"Win Score: {ScoringManager.Instance.GetWinScore()}" : null,
        new string('=', 80) + "\n"
    }.Where(msg => msg != null)
     .ToList()
     .ForEach(Debug.Log);
    
    public void LogInitialDeal(GamePlayer dealer, GamePlayer nonDealer, List<PlayingCard> tableCards) => new[] {
        "\n--- INITIAL DEAL ---",
        $"Dealer: {dealer.playerName}",
        $"Non-Dealer: {nonDealer.playerName}",
        "\nCards dealt to each player: 4",
        $"Cards on table: {string.Join(", ", tableCards.Select(card => card.ToString()))}"
    }.ToList()
     .ForEach(Debug.Log);
    
    public void LogPlayerTurn(GamePlayer player, PlayingCard playedCard) => new[] {
        $"\n>>> {player.playerName.ToUpper()}'S TURN",
        $"    Card played: {playedCard}"
    }.ToList()
     .ForEach(Debug.Log);
    
    public void LogCapture(GamePlayer player, List<PlayingCard> capturedCards) => new[] {
        $"    ✓ CAPTURED {capturedCards.Count} card(s): {string.Join(", ", capturedCards.Select(card => card.ToString()))}",
        $"    Total captured this round: {player.capturedCards.Count}"
    }.ToList()
     .ForEach(Debug.Log);
    
    public void LogSweep(GamePlayer player) => new[] {
        "    ★ SWEEP! Captured ALL cards on the table!",
        $"    Sweep count: {player.sweepCount}"
    }.ToList()
     .ForEach(Debug.Log);
    
    public void LogTrail(GamePlayer player, PlayingCard trailedCard, List<PlayingCard> tableCards) => new[] {
        $"    → Trailed (no capture): {trailedCard}",
        $"    Table now has: {string.Join(", ", tableCards.Select(card => card.ToString()))}"
    }.ToList()
     .ForEach(Debug.Log);
    
    public void LogRoundEnd(int roundNumber, int cardsPlayedThisRound) => new[] {
        $"\n{new string('-', 80)}",
        $"ROUND {roundNumber} COMPLETE - Both players played all 4 cards",
        new string('-', 80)
    }.ToList()
     .ForEach(Debug.Log);
    
    public void LogRemainingTableCards(GamePlayer player, List<PlayingCard> remainingCards) =>
        Debug.Log($"{player.playerName} gets remaining table cards: {string.Join(", ", remainingCards.Select(card => card.ToString()))}");
    
    public void LogDeckStatus(int cardsRemaining) =>
        Debug.Log($"\nCards remaining in deck: {cardsRemaining}");
    
    public void LogNewDeal(int handNumber) => new[] {
        $"\n--- HAND {handNumber} DEALT ---",
        "Each player gets 4 more cards"
    }.ToList()
     .ForEach(Debug.Log);
    
    public void LogScoringStart() => new[] {
        $"\n{new string('=', 80)}",
        "SCORING THIS HAND",
        new string('=', 80)
    }.ToList()
     .ForEach(Debug.Log);
    
    public void LogScoreAward(string player, string reason, int points) =>
        Debug.Log($"  {player} gets {points} point(s) for: {reason}");
    
    public void LogHandTotals(GamePlayer player1, GamePlayer player2, int player1Cards, int player2Cards, int player1Spades, int player2Spades) => new[] {
        "\n  Stats:",
        $"    {player1.playerName}: {player1Cards} cards, {player1Spades} spades",
        $"    {player2.playerName}: {player2Cards} cards, {player2Spades} spades"
    }.ToList()
     .ForEach(Debug.Log);
    
    public void LogCumulativeScores(GamePlayer player1, GamePlayer player2) => new[] {
        $"\n{new string('-', 80)}",
        "CUMULATIVE SCORES:",
        $"  {player1.playerName}: {player1.score} points",
        $"  {player2.playerName}: {player2.score} points",
        $"{new string('-', 80)}\n"
    }.ToList()
     .ForEach(Debug.Log);
    
    public void LogDealerSwap(GamePlayer newDealer) =>
        Debug.Log($"\nDEALER SWAP: {newDealer.playerName} is now the dealer");
    
    public void LogGameOver(GamePlayer winner, int winScore) => new[] {
        $"\n{new string('=', 80)}",
        "GAME OVER!",
        new string('=', 80),
        $"\nWINNER: {winner.playerName}",
        $"Final Score: {winner.score} points (needed {winScore} to win)",
        $"{new string('=', 80)}\n"
    }.ToList()
     .ForEach(Debug.Log);
    
    private List<string> ConvertCardsToStrings(List<PlayingCard> cards) =>
        cards.Select(card => card.ToString()).ToList();
}