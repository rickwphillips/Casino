using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class GameLogger : MonoBehaviour
{
    public static GameLogger Instance { get; private set; }

    [SerializeField] private int logCacheSize = 10;
    private List<string> logCache = new List<string>();

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// Add a log message to the cache. When cache size is reached, flush all logs to console.
    /// </summary>
    private void AddToCache(string message)
    {
        logCache.Add(message);
        if (logCache.Count >= logCacheSize)
        {
            FlushCache();
        }
    }

    /// <summary>
    /// Flush all cached logs to the console immediately as a single concatenated message.
    /// </summary>
    public void FlushCache()
    {
        if (logCache.Count > 0)
        {
            string concatenatedLogs = string.Join("\n", logCache);
            Debug.Log(concatenatedLogs);
            logCache.Clear();
        }
    }

    public void LogGameStart() => new[] {
        "\n" + new string('=', 80),
        "CASINO GAME STARTED",
        ScoringManager.Instance != null ? $"Variant: {ScoringManager.Instance.CurrentVariant}" : null,
        ScoringManager.Instance != null ? $"Win Score: {ScoringManager.Instance.WinScore}" : null,
        new string('=', 80) + "\n"
    }.Where(msg => msg != null)
     .ToList()
     .ForEach(AddToCache);
    
    public void LogInitialDeal(GamePlayer dealer, GamePlayer nonDealer, List<PlayingCard> tableCards) => new[] {
        "\n--- INITIAL DEAL ---",
        $"Dealer: {dealer.Name}",
        $"Non-Dealer: {nonDealer.Name}",
        "\nCards dealt to each player: 4",
        $"Cards on table: {string.Join(", ", tableCards.Select(card => card.ToString()))}"
    }.ToList()
     .ForEach(AddToCache);
    
    public void LogPlayerTurn(GamePlayer player, PlayingCard playedCard) => new[] {
        $"\n>>> {player.Name.ToUpper()}'S TURN",
        $"    Card played: {playedCard}"
    }.ToList()
     .ForEach(AddToCache);
    
    public void LogCapture(GamePlayer player, PlayingCard playedCard, List<PlayingCard> capturedCards) => new[] {
        $"    ✓ CAPTURED {capturedCards.Count + 1} card(s) with {playedCard}: {playedCard}, {string.Join(", ", capturedCards.Select(card => card.ToString()))}",
        $"    Total captured this round: {player.CapturedCards.Count}"
    }.ToList()
     .ForEach(AddToCache);
    
    public void LogSweep(GamePlayer player) => new[] {
        "    ★ SWEEP! Captured ALL cards on the table!",
        $"    Sweep count: {player.SweepCount}"
    }.ToList()
     .ForEach(AddToCache);
    
    public void LogTrail(GamePlayer player, PlayingCard trailedCard, List<PlayingCard> tableCards) => new[] {
        $"    → Trailed (no capture): {trailedCard}",
        $"    Table now has: {string.Join(", ", tableCards.Select(card => card.ToString()))}"
    }.ToList()
     .ForEach(AddToCache);
    
    public void LogRoundEnd(int roundNumber, int cardsPlayedThisRound) => new[] {
        $"\n{new string('-', 80)}",
        $"ROUND {roundNumber} COMPLETE - Both players played all 4 cards",
        new string('-', 80)
    }.ToList()
     .ForEach(AddToCache);
    
    public void LogRemainingTableCards(GamePlayer player, List<PlayingCard> remainingCards) =>
        AddToCache($"{player.Name} gets remaining table cards: {string.Join(", ", remainingCards.Select(card => card.ToString()))}");
    
    public void LogDeckStatus(int cardsRemaining) =>
        AddToCache($"\nCards remaining in deck: {cardsRemaining}");
    
    public void LogNewDeal(int handNumber) => new[] {
        $"\n--- HAND {handNumber} DEALT ---",
        "Each player gets 4 more cards"
    }.ToList()
     .ForEach(AddToCache);
    
    public void LogScoringStart() => new[] {
        $"\n{new string('=', 80)}",
        "SCORING THIS HAND",
        new string('=', 80)
    }.ToList()
     .ForEach(AddToCache);
    
    public void LogScoreAward(string player, string reason, int points) =>
        AddToCache($"  {player} gets {points} point(s) for: {reason}");
    
    public void LogHandTotals(GamePlayer player1, GamePlayer player2, int player1Cards, int player2Cards, int player1Spades, int player2Spades) => new[] {
        "\n  Stats:",
        $"    {player1.Name}: {player1Cards} cards, {player1Spades} spades",
        $"    {player2.Name}: {player2Cards} cards, {player2Spades} spades"
    }.ToList()
     .ForEach(AddToCache);
    
    public void LogCumulativeScores(GamePlayer player1, GamePlayer player2) => new[] {
        $"\n{new string('-', 80)}",
        "CUMULATIVE SCORES:",
        $"  {player1.Name}: {player1.Score} points",
        $"  {player2.Name}: {player2.Score} points",
        $"{new string('-', 80)}\n"
    }.ToList()
     .ForEach(AddToCache);
    
    public void LogBuildCreated(GamePlayer player, Build build) => new[] {
        $"    ⚒ BUILD CREATED: {player.Name} builds for {build.DeclaredValue}",
        $"    Cards in build: {string.Join(" + ", build.Cards.Select(c => c.ToString()))}"
    }.ToList()
     .ForEach(AddToCache);

    public void LogBuildCaptured(GamePlayer player, Build build) =>
        AddToCache($"    ✓ CAPTURED BUILD of {build.DeclaredValue}: {string.Join(" + ", build.Cards.Select(c => c.ToString()))}");

    public void LogBuildModified(GamePlayer player, Build build, PlayingCard addedCard, int newValue) => new[] {
        $"    ⚒ BUILD MODIFIED: {player.Name} adds {addedCard} to build",
        $"    New build value: {newValue} (was {build.DeclaredValue - CaptureChecker.GetCardValue(addedCard)})",
        $"    Ownership transferred to {player.Name}"
    }.ToList()
     .ForEach(AddToCache);

    public void LogRemainingBuild(Build build) =>
        AddToCache($"Remaining build of {build.DeclaredValue} goes to {build.Owner.Name}: {string.Join(" + ", build.Cards.Select(c => c.ToString()))}");

    public void LogDealerSwap(GamePlayer newDealer) =>
        AddToCache($"\nDEALER SWAP: {newDealer.Name} is now the dealer");

    public void LogGameOver(GamePlayer winner, int winScore) => new[] {
        $"\n{new string('=', 80)}",
        "GAME OVER!",
        new string('=', 80),
        $"\nWINNER: {winner.Name}",
        $"Final Score: {winner.Score} points (needed {winScore} to win)",
        $"{new string('=', 80)}\n"
    }.ToList()
     .ForEach(AddToCache);

    public void LogGameOverWithBreakdown(GamePlayer winner, GamePlayer loser, int winScore,
        Dictionary<string, int> winnerBreakdown, Dictionary<string, int> loserBreakdown)
    {
        var logs = new List<string>
        {
            $"\n{new string('=', 80)}",
            "GAME OVER!",
            new string('=', 80),
            $"\nWINNER: {winner.Name}",
            $"Final Score: {winner.Score} points (needed {winScore} to win)",
            "",
            "DETAILED SCORE BREAKDOWN:",
            new string('-', 80),
            ""
        };

        // Winner breakdown
        logs.Add($"{winner.Name}'s Scoring:");
        if (winnerBreakdown.Count > 0)
        {
            foreach (var kvp in winnerBreakdown.OrderByDescending(x => x.Value))
            {
                logs.Add($"  {kvp.Key,-20} {kvp.Value,3} pts");
            }
            logs.Add($"  {new string('-', 28)}");
            logs.Add($"  {"TOTAL",-20} {winner.Score,3} pts");
        }
        else
        {
            logs.Add($"  No points scored");
        }

        logs.Add("");

        // Loser breakdown
        logs.Add($"{loser.Name}'s Scoring:");
        if (loserBreakdown.Count > 0)
        {
            foreach (var kvp in loserBreakdown.OrderByDescending(x => x.Value))
            {
                logs.Add($"  {kvp.Key,-20} {kvp.Value,3} pts");
            }
            logs.Add($"  {new string('-', 28)}");
            logs.Add($"  {"TOTAL",-20} {loser.Score,3} pts");
        }
        else
        {
            logs.Add($"  No points scored");
        }

        logs.Add("");
        logs.Add(new string('=', 80));
        logs.Add("");

        logs.ForEach(AddToCache);
    }
    
    private List<string> ConvertCardsToStrings(List<PlayingCard> cards) =>
        cards.Select(card => card.ToString()).ToList();
}