using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class AIPlayer {
    public enum Difficulty { Easy, Medium, Hard }
    public GameLogger Logger = new ();


    public class AIAction {
        public enum ActionType { PlayCard, CreateBuild, ModifyBuild }
        public ActionType Type { get; set; }
        public int CardIndex { get; set; }
        public List<PlayingCard> BuildCards { get; set; }
        public int DeclaredValue { get; set; }
        public Build TargetBuild { get; set; }

    }

    private Difficulty difficulty;
    private GamePlayer player;

    public AIPlayer(GamePlayer gamePlayer, Difficulty diff) {
        player = gamePlayer;
        difficulty = diff;
    }

    public void SetDifficulty(Difficulty newDifficulty) {
        difficulty = newDifficulty;
        Logger.LogMessage(player.Name + " AI difficulty set to: " + difficulty);
    }
    
    public AIAction GetBestAction(List<PlayingCard> tableCards, List<Build> activeBuilds) {
        return difficulty switch {
            Difficulty.Easy => GetEasyAction(),
            Difficulty.Medium => GetMediumAction(tableCards, activeBuilds),
            Difficulty.Hard => GetHardAction(tableCards, activeBuilds),
            _ => new AIAction { Type = AIAction.ActionType.PlayCard, CardIndex = 0 }
        };
    }

    // Legacy method for backwards compatibility
    public int GetBestMove(List<PlayingCard> tableCards, List<Build> activeBuilds) {
        var action = GetBestAction(tableCards, activeBuilds);
        return action.CardIndex;
    }
    
    // Easy: Random valid move
    private AIAction GetEasyAction() {
        int randomIndex = Random.Range(0, player.HandSize());
        Logger.LogMessage(player.Name + " (Easy AI) plays random card at index " + randomIndex);
        return new AIAction { Type = AIAction.ActionType.PlayCard, CardIndex = randomIndex };
    }

  // Medium: Prioritize build captures, regular captures, consider build creation, prefer high-value cards
  private AIAction GetMediumAction(List<PlayingCard> tableCards, List<Build> activeBuilds) {
    // Check for build captures first (highest priority)
    var buildCaptureMoves = Enumerable.Range(0, player.HandSize())
        .Where(i => {
          int cardValue = CaptureChecker.GetCardValue(player.Hand[i]);
          return activeBuilds.Any(b => b.DeclaredValue == cardValue);
        })
        .ToList();

    if (buildCaptureMoves.Any()) {
      var chosen = buildCaptureMoves[Random.Range(0, buildCaptureMoves.Count)];
      Logger.LogMessage($"{player.Name} (Medium AI) plays build capture at index {chosen}");
      return new AIAction { Type = AIAction.ActionType.PlayCard, CardIndex = chosen };
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
      Logger.LogMessage($"{player.Name} (Medium AI) plays high-value capture at index {chosen}");
      return new AIAction { Type = AIAction.ActionType.PlayCard, CardIndex = chosen };
    }

    if (captureMoves.Any()) {
      var chosen = captureMoves[Random.Range(0, captureMoves.Count)];
      Logger.LogMessage($"{player.Name} (Medium AI) plays capture at index {chosen}");
      return new AIAction { Type = AIAction.ActionType.PlayCard, CardIndex = chosen };
    }

    // Consider creating a build (simple approach for medium AI)
    if (tableCards.Count > 0 && Random.value > 0.5f) { // 50% chance to try building
      var allPossibleBuilds = new List<AIAction>();
      foreach (var handCard in player.Hand) {
        allPossibleBuilds.AddRange(FindPossibleBuilds(handCard, tableCards));
      }

      if (allPossibleBuilds.Any()) {
        var chosenBuild = allPossibleBuilds[Random.Range(0, allPossibleBuilds.Count)];
        Logger.LogMessage($"{player.Name} (Medium AI) creates build for value {chosenBuild.DeclaredValue}");
        return chosenBuild;
      }
    }

    // No captures or builds available - trail
    var randomIndex = Random.Range(0, player.HandSize());
    Logger.LogMessage($"{player.Name} (Medium AI) trails at index {randomIndex}");
    return new AIAction { Type = AIAction.ActionType.PlayCard, CardIndex = randomIndex };
  }
    
    // Hard: Strategic play with lookahead and build evaluation
    private AIAction GetHardAction(List<PlayingCard> tableCards, List<Build> activeBuilds) {
        var allActions = new List<(AIAction action, int score)>();

        // Evaluate all regular play actions
        for (int i = 0; i < player.HandSize(); i++) {
            var playAction = new AIAction { Type = AIAction.ActionType.PlayCard, CardIndex = i };
            int score = EvaluateMove(player.Hand[i], tableCards, activeBuilds, i);
            allActions.Add((playAction, score));
        }

        // Evaluate all possible build creation actions
        foreach (var handCard in player.Hand) {
            var buildActions = FindPossibleBuilds(handCard, tableCards);
            foreach (var buildAction in buildActions) {
                int score = EvaluateBuildCreation(buildAction) + 30; // Base bonus for strategic builds
                allActions.Add((buildAction, score));
            }
        }

        // Select the best action
        var bestAction = allActions.OrderByDescending(x => x.score).First();

        if (bestAction.action.Type == AIAction.ActionType.CreateBuild) {
            Logger.LogMessage($"{player.Name} (Hard AI) creates strategic build for value {bestAction.action.DeclaredValue} (score: {bestAction.score})");
        } else {
            Logger.LogMessage($"{player.Name} (Hard AI) plays strategic move at index {bestAction.action.CardIndex} (score: {bestAction.score})");
        }

        return bestAction.action;
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

    // Find all possible build combinations for a given hand card
    private List<AIAction> FindPossibleBuilds(PlayingCard handCard, List<PlayingCard> tableCards) {
        var possibleBuilds = new List<AIAction>();
        int handCardValue = CaptureChecker.GetCardValue(handCard);
        int handCardIndex = player.Hand.ToList().IndexOf(handCard);

        // Try combinations of 1 to 3 table cards
        for (int size = 1; size <= Mathf.Min(3, tableCards.Count); size++) {
            var combinations = GetCombinations(tableCards, size);
            foreach (var combo in combinations) {
                int comboSum = combo.Sum(c => CaptureChecker.GetCardValue(c));
                int totalValue = comboSum + handCardValue;

                // Check if we have another card in hand that can capture this build
                bool canCapture = player.Hand.Any(c => c != handCard && CaptureChecker.GetCardValue(c) == totalValue);

                if (canCapture) {
                    possibleBuilds.Add(new AIAction {
                        Type = AIAction.ActionType.CreateBuild,
                        CardIndex = handCardIndex,
                        BuildCards = new List<PlayingCard>(combo),
                        DeclaredValue = totalValue
                    });
                }
            }
        }

        return possibleBuilds;
    }

    // Get all combinations of specified size from a list
    private List<List<PlayingCard>> GetCombinations(List<PlayingCard> cards, int size) {
        var result = new List<List<PlayingCard>>();
        if (size == 0) {
            result.Add(new List<PlayingCard>());
            return result;
        }
        if (cards.Count < size) return result;

        for (int i = 0; i <= cards.Count - size; i++) {
            var card = cards[i];
            var remainingCards = cards.Skip(i + 1).ToList();
            var smallerCombos = GetCombinations(remainingCards, size - 1);
            foreach (var combo in smallerCombos) {
                var newCombo = new List<PlayingCard> { card };
                newCombo.AddRange(combo);
                result.Add(newCombo);
            }
        }

        return result;
    }

    // Evaluate the value of creating a build
    private int EvaluateBuildCreation(AIAction buildAction) {
        int score = 0;

        // Base value for creating a build
        score += 20;

        // Bonus for protecting high-value cards
        int highValueCardsInBuild = buildAction.BuildCards.Count(IsHighValueCard);
        score += highValueCardsInBuild * 10;

        // Bonus for build value
        score += buildAction.DeclaredValue;

        // Penalty if too risky (opponent might have the card)
        if (buildAction.DeclaredValue <= 10) {
            score -= 5; // Common values are more risky
        }

        return score;
    }

  }
