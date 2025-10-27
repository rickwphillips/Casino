using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class AIPlayer {
    public enum Difficulty { Easy, Medium, Hard }
    
    private Difficulty difficulty;
    private GamePlayer player;
    
    public AIPlayer(GamePlayer gamePlayer, Difficulty diff) {
        player = gamePlayer;
        difficulty = diff;
    }
    
    public void SetDifficulty(Difficulty newDifficulty) {
        difficulty = newDifficulty;
        Debug.Log(player.Name + " AI difficulty set to: " + difficulty);
    }
    
    public int GetBestMove(List<PlayingCard> tableCards, List<Build> activeBuilds) {
        return difficulty switch {
            Difficulty.Easy => GetEasyMove(),
            Difficulty.Medium => GetMediumMove(tableCards, activeBuilds),
            Difficulty.Hard => GetHardMove(tableCards, activeBuilds),
            _ => 0
        };
    }
    
    // Easy: Random valid move
    private int GetEasyMove() {
        int randomIndex = Random.Range(0, player.HandSize());
        Debug.Log(player.Name + " (Easy AI) plays random card at index " + randomIndex);
        return randomIndex;
    }

  // Medium: Prioritize build captures, regular captures, prefer high-value cards
  private int GetMediumMove(List<PlayingCard> tableCards, List<Build> activeBuilds) {
    // Check for build captures first (highest priority)
    var buildCaptureMoves = Enumerable.Range(0, player.HandSize())
        .Where(i => {
          int cardValue = CaptureChecker.GetCardValue(player.Hand[i]);
          return activeBuilds.Any(b => b.DeclaredValue == cardValue);
        })
        .ToList();

    if (buildCaptureMoves.Any()) {
      var chosen = buildCaptureMoves[Random.Range(0, buildCaptureMoves.Count)];
      Debug.Log($"{player.Name} (Medium AI) plays build capture at index {chosen}");
      return chosen;
    }

    // Check for regular captures
    var moveAnalysis = Enumerable.Range(0, player.HandSize())
        .Select(i => new {
          Index = i,
          Captures = CaptureChecker.GetValidCaptures(player.Hand[i], tableCards),
          HasHighValue = false
        })
        .Select(x => new {
          x.Index,
          x.Captures,
          HasHighValue = x.Captures.Any(IsHighValueCard)
        })
        .ToList();

    var highValueMoves = moveAnalysis
        .Where(x => x.Captures.Any() && x.HasHighValue)
        .Select(x => x.Index)
        .ToList();

    var captureMoves = moveAnalysis
        .Where(x => x.Captures.Any())
        .Select(x => x.Index)
        .ToList();

    if (highValueMoves.Any()) {
      var chosen = highValueMoves[Random.Range(0, highValueMoves.Count)];
      Debug.Log($"{player.Name} (Medium AI) plays high-value capture at index {chosen}");
      return chosen;
    }

    if (captureMoves.Any()) {
      var chosen = captureMoves[Random.Range(0, captureMoves.Count)];
      Debug.Log($"{player.Name} (Medium AI) plays capture at index {chosen}");
      return chosen;
    }

    // No captures available - trail
    var randomIndex = Random.Range(0, player.HandSize());
    Debug.Log($"{player.Name} (Medium AI) trails at index {randomIndex}");
    return randomIndex;
  }
    
    // Hard: Strategic play with lookahead and build evaluation
    private int GetHardMove(List<PlayingCard> tableCards, List<Build> activeBuilds) {
        var bestMove = Enumerable.Range(0, player.HandSize())
            .Select(i => new {
                Index = i,
                Score = EvaluateMove(player.Hand[i], tableCards, activeBuilds, i)
            })
            .OrderByDescending(x => x.Score)
            .First();

        Debug.Log($"{player.Name} (Hard AI) plays strategic move at index {bestMove.Index} (score: {bestMove.Score})");
        return bestMove.Index;
    }

    private int EvaluateMove(PlayingCard card, List<PlayingCard> tableCards, List<Build> activeBuilds, int cardIndex) {
        var captures = CaptureChecker.GetValidCaptures(card, tableCards);
        int cardValue = CaptureChecker.GetCardValue(card);

        // Check if this card can capture any builds
        var buildsToCaptureCount = activeBuilds.Count(b => b.DeclaredValue == cardValue);
        var myBuilds = activeBuilds.Count(b => b.Owner == player);
        var opponentBuilds = activeBuilds.Count(b => b.Owner != player);

        return new[] {
            // Build capture bonus (VERY high priority)
            buildsToCaptureCount * 100,

            // Penalty if we have a build and don't protect it
            myBuilds > 0 && buildsToCaptureCount == 0 ? -80 : 0,

            // Base capture score
            captures.Count * 10,

            // Sweep bonus
            captures.Count > 0 && captures.Count == tableCards.Count && activeBuilds.Count == 0 ? 50 : 0,

            // Big Casino bonus (10 of Diamonds)
            HasSpecificCard(captures, PlayingCard.Suit.Diamonds, PlayingCard.Rank.Ten) ? 30 : 0,

            // Little Casino bonus (2 of Spades)
            HasSpecificCard(captures, PlayingCard.Suit.Spades, PlayingCard.Rank.Two) ? 20 : 0,

            // Ace bonus
            captures.Count(c => c.rank == PlayingCard.Rank.Ace) * 15,

            // Spade bonus (for most spades scoring)
            captures.Count(c => c.suit == PlayingCard.Suit.Spades) * 5,

            // Bonus for capturing opponent's build
            buildsToCaptureCount > 0 && opponentBuilds > 0 ? 40 : 0,

            // No capture strategy - trailing cards
            captures.Count == 0 && buildsToCaptureCount == 0 ? (IsHighValueCard(card) ? -5 : 1) : 0
        }.Sum();
    }
    
    private bool IsHighValueCard(PlayingCard card) {
        return card.rank == PlayingCard.Rank.Ace ||
                card.rank == PlayingCard.Rank.Ten ||
                card.suit == PlayingCard.Suit.Spades ||
                (card.suit == PlayingCard.Suit.Diamonds && card.rank == PlayingCard.Rank.Ten) ||
                (card.suit == PlayingCard.Suit.Spades && card.rank == PlayingCard.Rank.Two);
    }

  private bool HasSpecificCard(List<PlayingCard> cards, PlayingCard.Suit suit, PlayingCard.Rank rank) =>
      cards.Any(card => card.suit == suit && card.rank == rank);

  }
