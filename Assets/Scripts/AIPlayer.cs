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
        Debug.Log(player.playerName + " AI difficulty set to: " + difficulty);
    }
    
    public int GetBestMove(List<PlayingCard> tableCards) {
        return difficulty switch {
            Difficulty.Easy => GetEasyMove(),
            Difficulty.Medium => GetMediumMove(tableCards),
            Difficulty.Hard => GetHardMove(tableCards),
            _ => 0
        };
    }
    
    // Easy: Random valid move
    private int GetEasyMove() {
        int randomIndex = Random.Range(0, player.hand.Count);
        Debug.Log(player.playerName + " (Easy AI) plays random card at index " + randomIndex);
        return randomIndex;
    }

  // Medium: Prioritize captures, prefer high-value cards
  private int GetMediumMove(List<PlayingCard> tableCards) {
    var moveAnalysis = Enumerable.Range(0, player.hand.Count)
        .Select(i => new {
          Index = i,
          Captures = CaptureChecker.GetValidCaptures(player.hand[i], tableCards),
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
      Debug.Log($"{player.playerName} (Medium AI) plays high-value capture at index {chosen}");
      return chosen;
    }

    if (captureMoves.Any()) {
      var chosen = captureMoves[Random.Range(0, captureMoves.Count)];
      Debug.Log($"{player.playerName} (Medium AI) plays capture at index {chosen}");
      return chosen;
    }

    var randomIndex = Random.Range(0, player.hand.Count);
    Debug.Log($"{player.playerName} (Medium AI) trails at index {randomIndex}");
    return randomIndex;
  }
    
    // Hard: Strategic play with lookahead
    private int GetHardMove(List<PlayingCard> tableCards) {
        var bestMove = Enumerable.Range(0, player.hand.Count)
            .Select(i => new {
                Index = i,
                Score = EvaluateMove(player.hand[i], tableCards, i)
            })
            .OrderByDescending(x => x.Score)
            .First();
            
        Debug.Log($"{player.playerName} (Hard AI) plays strategic move at index {bestMove.Index} (score: {bestMove.Score})");
        return bestMove.Index;
    }
    
    private int EvaluateMove(PlayingCard card, List<PlayingCard> tableCards, int cardIndex) {
        var captures = CaptureChecker.GetValidCaptures(card, tableCards);
        
        return new[] {
            // Base capture score
            captures.Count * 10,
            
            // Sweep bonus
            captures.Count > 0 && captures.Count == tableCards.Count ? 50 : 0,
            
            // Big Casino bonus (10 of Diamonds)
            HasSpecificCard(captures, PlayingCard.Suit.Diamonds, PlayingCard.Rank.Ten) ? 30 : 0,
            
            // Little Casino bonus (2 of Spades)
            HasSpecificCard(captures, PlayingCard.Suit.Spades, PlayingCard.Rank.Two) ? 20 : 0,
            
            // Ace bonus
            captures.Count(c => c.rank == PlayingCard.Rank.Ace) * 15,
            
            // Spade bonus (for most spades scoring)
            captures.Count(c => c.suit == PlayingCard.Suit.Spades) * 5,
            
            // No capture strategy - trailing cards
            captures.Count == 0 ? (IsHighValueCard(card) ? -5 : 1) : 0
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
