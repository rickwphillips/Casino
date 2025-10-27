using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }
    
    public enum GamePhase { Playing, RoundEnd, GameOver }
    
    [SerializeField] private AIPlayer.Difficulty dealerAIDifficulty = AIPlayer.Difficulty.Medium;
    [SerializeField] private AIPlayer.Difficulty nonDealerAIDifficulty = AIPlayer.Difficulty.Medium;
    [SerializeField] private bool useAI = true;
    
    private GameDeck deck;
    private GamePlayer dealer;
    private GamePlayer nonDealer;
    private List<PlayingCard> tableCards = new();
    private List<Build> activeBuilds = new();

    private AIPlayer dealerAI;
    private AIPlayer nonDealerAI;

    private GamePhase currentPhase;
    private GamePlayer currentPlayer;
    private GamePlayer lastPlayerToCaptureThisRound;
    private int cardsPlayedThisRound = 0;
    private const int HAND_SIZE = 4;
    private const int TABLE_SIZE = 4;

    // Game statistics tracking
    private readonly Dictionary<string, int> dealerScoreBreakdown = new();
    private readonly Dictionary<string, int> nonDealerScoreBreakdown = new();
    
    private void Awake() {
        if (Instance == null) { Instance = this; }
        else { Destroy(gameObject); }
    }
    
    private void Start() {
        Invoke(nameof(InitializeGame), 0.1f);
    }
    
    public void InitializeGame() {
        deck = new GameDeck();
        deck.Shuffle();
        
        dealer = new GamePlayer("GamePlayer1");
        nonDealer = new GamePlayer("GamePlayer2");
        
        dealerAI = new AIPlayer(dealer, dealerAIDifficulty);
        nonDealerAI = new AIPlayer(nonDealer, nonDealerAIDifficulty);
        
        tableCards.Clear();
        cardsPlayedThisRound = 0;
        
        DealInitialRound();
        
        currentPlayer = nonDealer;
        currentPhase = GamePhase.Playing;
        
        GameLogger.Instance.LogGameStart();
        GameLogger.Instance.LogInitialDeal(dealer, nonDealer, tableCards);
        
        // Start AI turns automatically
        if (useAI) {
            Invoke(nameof(AIPlayTurn), 1f);
        }
    }
    
    private void DealInitialRound() => new[] {
        (nonDealer, HAND_SIZE),
        (dealer, HAND_SIZE),
        (null, TABLE_SIZE)
    }.ToList().ForEach(deal => {
        var cards = deck.DrawCards(deal.Item2);
        if (deal.Item1 != null) { deal.Item1.AddCards(cards); }
        else { tableCards.AddRange(cards); }
    });

    public void PlayCard(GamePlayer player, int cardIndex) {
        if (player != currentPlayer) {
            Debug.LogWarning($"It's not {player.Name}'s turn!");
            return;
        }

        if (player.HandSize() == 0)
        {
            Debug.LogWarning($"{player.Name} has no cards to play!");
            return;
        }

        PlayingCard playedCard = player.PlayCard(cardIndex);
        if (playedCard != null)
        {
            GameLogger.Instance.LogPlayerTurn(player, playedCard);

            int playedValue = CaptureChecker.GetCardValue(playedCard);

            // Check if player can capture their own builds
            var playerBuilds = activeBuilds.Where(b => b.Owner == player && b.DeclaredValue == playedValue).ToList();

            // Check for valid table card captures
            List<PlayingCard> captures = CaptureChecker.GetValidCaptures(playedCard, tableCards);

            // Check if player can capture opponent builds
            var opponentBuildsToCapture = activeBuilds.Where(b => b.Owner != player && b.DeclaredValue == playedValue).ToList();

            bool hasPendingBuild = PlayerHasPendingBuild(player);
            bool canCapture = captures.Count > 0 || playerBuilds.Count > 0 || opponentBuildsToCapture.Count > 0;

            // If player has a pending build, they MUST capture if possible
            if (hasPendingBuild && !canCapture)
            {
                Debug.LogWarning($"{player.Name} has a pending build and must capture it! Cannot trail.");
                player.AddCard(playedCard); // Return card to hand
                return;
            }

            if (canCapture)
            {
                // Capture table cards
                foreach (PlayingCard capturedCard in captures)
                {
                    tableCards.Remove(capturedCard);
                }

                // Capture all matching builds
                var allBuildsToCapture = playerBuilds.Concat(opponentBuildsToCapture).ToList();
                foreach (var build in allBuildsToCapture)
                {
                    CaptureBuild(player, build);
                }

                // Add captured cards AND the played card to player's captured pile
                player.AddCapturedCard(playedCard);
                player.AddCapturedCards(captures);

                // Track last player to capture for end-of-round logic
                lastPlayerToCaptureThisRound = player;

                GameLogger.Instance.LogCapture(player, playedCard, captures);

                // Log build captures
                foreach (var build in allBuildsToCapture)
                {
                    GameLogger.Instance.LogBuildCaptured(player, build);
                }

                // Check for sweep (table cards empty AND no active builds)
                if (tableCards.Count == 0 && activeBuilds.Count == 0)
                {
                    player.IncrementSweepCount();
                    GameLogger.Instance.LogSweep(player);
                }
            }
            else
            {
                // Trail - add to table (only if no pending builds)
                tableCards.Add(playedCard);
                GameLogger.Instance.LogTrail(player, playedCard, tableCards);
            }
            
            cardsPlayedThisRound++;
            
            if (cardsPlayedThisRound == HAND_SIZE * 2)
            {
                EndRound();
            }
            else
            {
                currentPlayer = (currentPlayer == dealer) ? nonDealer : dealer;
            }
        }
    }
    
    private void EndRound()
    {
        GameLogger.Instance.LogRoundEnd(1, cardsPlayedThisRound);

        bool isDeckEmpty = deck.CardsRemaining() == 0;
        bool awardNow = ScoringManager.Instance.TableCardTiming == ScoreVariables.TableCardAwardTiming.AfterEachHand ||
                        (ScoringManager.Instance.TableCardTiming == ScoreVariables.TableCardAwardTiming.OnlyAtGameEnd && isDeckEmpty);

        // Award remaining table cards based on configuration
        if (awardNow && tableCards.Count > 0 && lastPlayerToCaptureThisRound != null)
        {
            GameLogger.Instance.LogRemainingTableCards(lastPlayerToCaptureThisRound, tableCards);
            lastPlayerToCaptureThisRound.AddCapturedCards(new List<PlayingCard>(tableCards));
            tableCards.Clear();
        }

        // Award remaining builds based on configuration
        if (awardNow)
        {
            foreach (var build in activeBuilds.ToList())
            {
                build.Owner.AddCapturedCards(build.Cards.ToList());
                GameLogger.Instance.LogRemainingBuild(build);
            }
            activeBuilds.Clear();
        }

        cardsPlayedThisRound = 0;

        if (isDeckEmpty)
        {
            GameLogger.Instance.LogDeckStatus(0);
            ScoreRound();
            SwapDealer();

            if (dealer.Score >= ScoringManager.Instance.WinScore ||
                nonDealer.Score >= ScoringManager.Instance.WinScore)
            {
                EndGame();
                return;
            }

            deck = new GameDeck();
            deck.Shuffle();

            // Deal initial table cards for new game
            tableCards.Clear();
            tableCards.AddRange(deck.DrawCards(TABLE_SIZE));
            GameLogger.Instance.LogNewDeal(1);
            nonDealer.AddCards(deck.DrawCards(HAND_SIZE));
            dealer.AddCards(deck.DrawCards(HAND_SIZE));
        }
        else
        {
            GameLogger.Instance.LogDeckStatus(deck.CardsRemaining());

            // Only deal new cards if there are cards remaining
            if (deck.CardsRemaining() >= HAND_SIZE * 2)
            {
                GameLogger.Instance.LogNewDeal(1);
                nonDealer.AddCards(deck.DrawCards(HAND_SIZE));
                dealer.AddCards(deck.DrawCards(HAND_SIZE));
            }
            else
            {
                Debug.LogWarning($"Not enough cards in deck to deal. Remaining: {deck.CardsRemaining()}");
                return;
            }
        }

        currentPlayer = nonDealer;

        // Restart AI turn loop if using AI
        if (useAI && currentPhase == GamePhase.Playing)
        {
            Invoke(nameof(AIPlayTurn), 1f);
        }
    }
    
    private void ScoreRound()
    {
        GameLogger.Instance.LogScoringStart();
        ScoringManager sm = ScoringManager.Instance;
        
        int dealerCardCount = dealer.CapturedCards.Count;
        int nonDealerCardCount = nonDealer.CapturedCards.Count;
        int dealerSpades = CountSpades(dealer.CapturedCards.ToList());
        int nonDealerSpades = CountSpades(nonDealer.CapturedCards.ToList());
        
        GameLogger.Instance.LogHandTotals(dealer, nonDealer, dealerCardCount, nonDealerCardCount, dealerSpades, nonDealerSpades);
        
        // Most cards
        if (nonDealerCardCount > dealerCardCount)
        {
            nonDealer.AddScore(sm.PointsForMostCards);
            AddToScoreBreakdown(nonDealer, "Most Cards", sm.PointsForMostCards);
            GameLogger.Instance.LogScoreAward(nonDealer.Name, "Most cards (" + nonDealerCardCount + ")", sm.PointsForMostCards);
        }
        else if (dealerCardCount > nonDealerCardCount)
        {
            dealer.AddScore(sm.PointsForMostCards);
            AddToScoreBreakdown(dealer, "Most Cards", sm.PointsForMostCards);
            GameLogger.Instance.LogScoreAward(dealer.Name, "Most cards (" + dealerCardCount + ")", sm.PointsForMostCards);
        }

        // Most spades
        if (nonDealerSpades > dealerSpades && sm.PointsForMostSpades > 0)
        {
            nonDealer.AddScore(sm.PointsForMostSpades);
            AddToScoreBreakdown(nonDealer, "Most Spades", sm.PointsForMostSpades);
            GameLogger.Instance.LogScoreAward(nonDealer.Name, "Most spades (" + nonDealerSpades + ")", sm.PointsForMostSpades);
        }
        else if (dealerSpades > nonDealerSpades && sm.PointsForMostSpades > 0)
        {
            dealer.AddScore(sm.PointsForMostSpades);
            AddToScoreBreakdown(dealer, "Most Spades", sm.PointsForMostSpades);
            GameLogger.Instance.LogScoreAward(dealer.Name, "Most spades (" + dealerSpades + ")", sm.PointsForMostSpades);
        }

        // Big Casino - use configured card
        if (HasCard(dealer.CapturedCards.ToList(), sm.BigCasinoSuit, sm.BigCasinoRank))
        {
            dealer.AddScore(sm.PointsForBigCasino);
            AddToScoreBreakdown(dealer, "Big Casino", sm.PointsForBigCasino);
            GameLogger.Instance.LogScoreAward(dealer.Name, $"Big Casino ({sm.BigCasinoRank} of {sm.BigCasinoSuit})", sm.PointsForBigCasino);
        }
        else if (HasCard(nonDealer.CapturedCards.ToList(), sm.BigCasinoSuit, sm.BigCasinoRank))
        {
            nonDealer.AddScore(sm.PointsForBigCasino);
            AddToScoreBreakdown(nonDealer, "Big Casino", sm.PointsForBigCasino);
            GameLogger.Instance.LogScoreAward(nonDealer.Name, $"Big Casino ({sm.BigCasinoRank} of {sm.BigCasinoSuit})", sm.PointsForBigCasino);
        }

        // Little Casino - use configured card
        if (HasCard(dealer.CapturedCards.ToList(), sm.LittleCasinoSuit, sm.LittleCasinoRank))
        {
            dealer.AddScore(sm.PointsForLittleCasino);
            AddToScoreBreakdown(dealer, "Little Casino", sm.PointsForLittleCasino);
            GameLogger.Instance.LogScoreAward(dealer.Name, $"Little Casino ({sm.LittleCasinoRank} of {sm.LittleCasinoSuit})", sm.PointsForLittleCasino);
        }
        else if (HasCard(nonDealer.CapturedCards.ToList(), sm.LittleCasinoSuit, sm.LittleCasinoRank))
        {
            nonDealer.AddScore(sm.PointsForLittleCasino);
            AddToScoreBreakdown(nonDealer, "Little Casino", sm.PointsForLittleCasino);
            GameLogger.Instance.LogScoreAward(nonDealer.Name, $"Little Casino ({sm.LittleCasinoRank} of {sm.LittleCasinoSuit})", sm.PointsForLittleCasino);
        }

        // Individual card rank scoring - check all ranks for configured points
        foreach (PlayingCard.Rank rank in System.Enum.GetValues(typeof(PlayingCard.Rank)))
        {
            int pointsPerCard = GetPointsForRank(sm, rank);
            if (pointsPerCard > 0)
            {
                int dealerCount = CountCardsOfRank(dealer.CapturedCards.ToList(), rank);
                int nonDealerCount = CountCardsOfRank(nonDealer.CapturedCards.ToList(), rank);

                if (dealerCount > 0)
                {
                    int points = dealerCount * pointsPerCard;
                    dealer.AddScore(points);
                    AddToScoreBreakdown(dealer, GetRankName(rank), points);
                    GameLogger.Instance.LogScoreAward(dealer.Name, $"{dealerCount} {GetRankName(rank)}", points);
                }

                if (nonDealerCount > 0)
                {
                    int points = nonDealerCount * pointsPerCard;
                    nonDealer.AddScore(points);
                    AddToScoreBreakdown(nonDealer, GetRankName(rank), points);
                    GameLogger.Instance.LogScoreAward(nonDealer.Name, $"{nonDealerCount} {GetRankName(rank)}", points);
                }
            }
        }

        // Sweeps - award based on configured value
        dealer.AddScore(dealer.SweepCount * sm.PointsPerSweep);
        nonDealer.AddScore(nonDealer.SweepCount * sm.PointsPerSweep);

        if (dealer.SweepCount > 0)
        {
            AddToScoreBreakdown(dealer, "Sweeps", dealer.SweepCount * sm.PointsPerSweep);
            GameLogger.Instance.LogScoreAward(dealer.Name, dealer.SweepCount + " Sweep(s)", dealer.SweepCount * sm.PointsPerSweep);
        }
        if (nonDealer.SweepCount > 0)
        {
            AddToScoreBreakdown(nonDealer, "Sweeps", nonDealer.SweepCount * sm.PointsPerSweep);
            GameLogger.Instance.LogScoreAward(nonDealer.Name, nonDealer.SweepCount + " Sweep(s)", nonDealer.SweepCount * sm.PointsPerSweep);
        }
        
        GameLogger.Instance.LogCumulativeScores(dealer, nonDealer);

        // Reset for next round
        dealer.ClearCapturedCards();
        nonDealer.ClearCapturedCards();
    }
    
    private void AddToScoreBreakdown(GamePlayer player, string category, int points)
    {
        var breakdown = player == dealer ? dealerScoreBreakdown : nonDealerScoreBreakdown;
        if (breakdown.ContainsKey(category))
            breakdown[category] += points;
        else
            breakdown[category] = points;
    }

    private int CountSpades(List<PlayingCard> cards) =>
        cards.Count(card => card.suit == PlayingCard.Suit.Spades);

    private int CountCardsOfRank(List<PlayingCard> cards, PlayingCard.Rank rank) =>
        cards.Count(card => card.rank == rank);

    private bool HasCard(List<PlayingCard> cards, PlayingCard.Suit suit, PlayingCard.Rank rank) =>
        cards.Any(card => card.suit == suit && card.rank == rank);

    private int GetPointsForRank(ScoringManager sm, PlayingCard.Rank rank) => rank switch
    {
        PlayingCard.Rank.Ace => sm.PointsPerAce,
        PlayingCard.Rank.Two => sm.PointsPerTwo,
        PlayingCard.Rank.Three => sm.PointsPerThree,
        PlayingCard.Rank.Four => sm.PointsPerFour,
        PlayingCard.Rank.Five => sm.PointsPerFive,
        PlayingCard.Rank.Six => sm.PointsPerSix,
        PlayingCard.Rank.Seven => sm.PointsPerSeven,
        PlayingCard.Rank.Eight => sm.PointsPerEight,
        PlayingCard.Rank.Nine => sm.PointsPerNine,
        PlayingCard.Rank.Ten => sm.PointsPerTen,
        PlayingCard.Rank.Jack => sm.PointsPerJack,
        PlayingCard.Rank.Queen => sm.PointsPerQueen,
        PlayingCard.Rank.King => sm.PointsPerKing,
        _ => 0
    };

    private string GetRankName(PlayingCard.Rank rank) => rank switch
    {
        PlayingCard.Rank.Ace => "Aces",
        PlayingCard.Rank.Two => "Twos",
        PlayingCard.Rank.Three => "Threes",
        PlayingCard.Rank.Four => "Fours",
        PlayingCard.Rank.Five => "Fives",
        PlayingCard.Rank.Six => "Sixes",
        PlayingCard.Rank.Seven => "Sevens",
        PlayingCard.Rank.Eight => "Eights",
        PlayingCard.Rank.Nine => "Nines",
        PlayingCard.Rank.Ten => "Tens",
        PlayingCard.Rank.Jack => "Jacks",
        PlayingCard.Rank.Queen => "Queens",
        PlayingCard.Rank.King => "Kings",
        _ => rank.ToString()
    };
    
    private void SwapDealer() {
        (dealer, nonDealer) = (nonDealer, dealer);
        GameLogger.Instance.LogDealerSwap(dealer);
    }
    
    private void EndGame() {
        currentPhase = GamePhase.GameOver;
        var winner = dealer.Score >= ScoringManager.Instance.WinScore ? dealer : nonDealer;
        var loser = winner == dealer ? nonDealer : dealer;

        GameLogger.Instance.LogGameOverWithBreakdown(
            winner, loser,
            ScoringManager.Instance.WinScore,
            winner == dealer ? dealerScoreBreakdown : nonDealerScoreBreakdown,
            loser == dealer ? dealerScoreBreakdown : nonDealerScoreBreakdown
        );
    }
    
    public GamePlayer GetCurrentPlayer() => currentPlayer;
    public GamePlayer GetDealer() => dealer;
    public GamePlayer GetNonDealer() => nonDealer;
    public List<PlayingCard> GetTableCards() => tableCards;
    public List<Build> GetActiveBuilds() => activeBuilds;
    public GameDeck GetDeck() => deck;
    public GamePhase GetCurrentPhase() => currentPhase;

    // Build management methods
    private bool PlayerHasPendingBuild(GamePlayer player)
    {
        return activeBuilds.Any(b => b.Owner == player);
    }

    private bool PlayerCanCaptureValue(GamePlayer player, int value)
    {
        return player.Hand.Any(card => CaptureChecker.GetCardValue(card) == value);
    }

    private bool CanCreateBuild(GamePlayer player, List<PlayingCard> cards, int declaredValue)
    {
        // Must have the capture card in hand
        if (!PlayerCanCaptureValue(player, declaredValue))
        {
            Debug.LogWarning($"{player.Name} does not have a card with value {declaredValue} to capture this build!");
            return false;
        }

        // Calculate actual sum of cards
        int actualSum = cards.Sum(card => CaptureChecker.GetCardValue(card));
        if (actualSum != declaredValue)
        {
            Debug.LogWarning($"Build sum {actualSum} does not match declared value {declaredValue}!");
            return false;
        }

        return true;
    }

    public bool CreateBuild(GamePlayer player, PlayingCard handCard, List<PlayingCard> tableCardsForBuild, int declaredValue)
    {
        var buildCards = new List<PlayingCard>(tableCardsForBuild) { handCard };

        if (!CanCreateBuild(player, buildCards, declaredValue))
        {
            return false;
        }

        // Remove cards from table
        foreach (var card in tableCardsForBuild)
        {
            tableCards.Remove(card);
        }

        // Create the build
        var build = new Build(buildCards, declaredValue, player);
        activeBuilds.Add(build);

        GameLogger.Instance.LogBuildCreated(player, build);
        return true;
    }

    public bool ModifyBuild(GamePlayer player, Build build, PlayingCard handCard, int newDeclaredValue)
    {
        // Cannot modify multi-builds
        if (build.IsMultiBuild)
        {
            Debug.LogWarning($"Cannot modify a multi-build!");
            return false;
        }

        // Must have the new capture card in hand
        if (!PlayerCanCaptureValue(player, newDeclaredValue))
        {
            Debug.LogWarning($"{player.Name} does not have a card with value {newDeclaredValue} to capture this build!");
            return false;
        }

        // Calculate new total: existing build + hand card
        int currentBuildValue = build.Cards.Sum(card => CaptureChecker.GetCardValue(card));
        int handCardValue = CaptureChecker.GetCardValue(handCard);
        int actualNewValue = currentBuildValue + handCardValue;

        if (actualNewValue != newDeclaredValue)
        {
            Debug.LogWarning($"Build sum {actualNewValue} does not match declared value {newDeclaredValue}!");
            return false;
        }

        // New value must be greater than old value
        if (newDeclaredValue <= build.DeclaredValue)
        {
            Debug.LogWarning($"New build value {newDeclaredValue} must be greater than current value {build.DeclaredValue}!");
            return false;
        }

        // Modify the build (adds card, changes value, transfers ownership)
        build.ModifyBuild(handCard, newDeclaredValue, player);

        GameLogger.Instance.LogBuildModified(player, build, handCard, newDeclaredValue);
        return true;
    }

    private void CaptureBuild(GamePlayer player, Build build)
    {
        // Add all build cards to captured pile
        player.AddCapturedCards(build.Cards.ToList());

        // Remove build from active builds
        activeBuilds.Remove(build);
    }
    
    public void AIPlayTurn() {
        if (!useAI || currentPhase == GamePhase.GameOver) return;

        // Check if current player has cards to play
        if (currentPlayer.HandSize() == 0)
        {
            Debug.LogWarning($"AIPlayTurn called but {currentPlayer.Name} has no cards!");
            return;
        }

        var ai = currentPlayer == dealer ? dealerAI : nonDealerAI;
        var bestMove = ai.GetBestMove(tableCards);
        PlayCard(currentPlayer, bestMove);

        if (useAI && currentPhase == GamePhase.Playing) {
            Invoke(nameof(AIPlayTurn), 1f);
        }
    }
    
    public void SetAIDifficulty(AIPlayer.Difficulty diff) {
        dealerAI.SetDifficulty(diff);
        nonDealerAI.SetDifficulty(diff);
        Debug.Log("AI difficulty set to: " + diff);
    }
    
    public void SetDealerAIDifficulty(AIPlayer.Difficulty diff) {
        dealerAI.SetDifficulty(diff);
        Debug.Log("Dealer AI difficulty set to: " + diff);
    }
    
    public void SetNonDealerAIDifficulty(AIPlayer.Difficulty diff) {
        nonDealerAI.SetDifficulty(diff);
        Debug.Log("Non-Dealer AI difficulty set to: " + diff);
    }
    
    public void SetUseAI(bool enabled) {
        useAI = enabled;
        Debug.Log("AI enabled: " + useAI);
    }
}