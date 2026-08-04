using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }
    
    public enum GamePhase { Playing, RoundEnd, GameOver }
    
    [Header("Player Configuration")]
    [SerializeField] private GamePlayer.PlayerType dealerPlayerType = GamePlayer.PlayerType.AI;
    [SerializeField] private AIPlayer.Difficulty dealerAIDifficulty = AIPlayer.Difficulty.Medium;
    [SerializeField] private GamePlayer.PlayerType nonDealerPlayerType = GamePlayer.PlayerType.Human;
    [SerializeField] private AIPlayer.Difficulty nonDealerAIDifficulty = AIPlayer.Difficulty.Medium;
    
    [Header("Game Settings")]
    [SerializeField] private float aiMoveDelay = 1.5f;
    
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
    private bool waitingForHumanInput = false;

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
        // 1-player guarantee: this is a human-vs-AI game. If the scene still
        // carries an old AI-vs-AI configuration, seat the human as non-dealer.
        if (dealerPlayerType == GamePlayer.PlayerType.AI &&
            nonDealerPlayerType == GamePlayer.PlayerType.AI)
        {
            nonDealerPlayerType = GamePlayer.PlayerType.Human;
        }

        deck = new GameDeck();
        deck.Shuffle();

        dealer = new GamePlayer("Dealer", dealerPlayerType);
        nonDealer = new GamePlayer("Non-Dealer", nonDealerPlayerType);

        if (dealer.IsAI())
            dealerAI = new AIPlayer(dealer, dealerAIDifficulty);
        if (nonDealer.IsAI())
            nonDealerAI = new AIPlayer(nonDealer, nonDealerAIDifficulty);

        tableCards.Clear();
        activeBuilds.Clear();
        cardsPlayedThisRound = 0;
        waitingForHumanInput = false;

        // Clear score breakdowns for new game
        dealerScoreBreakdown.Clear();
        nonDealerScoreBreakdown.Clear();
        
        DealInitialRound();
        
        currentPlayer = nonDealer;
        currentPhase = GamePhase.Playing;
        
        GameLogger.Instance.LogGameStart();
        GameLogger.Instance.LogInitialDeal(dealer, nonDealer, tableCards);
        
        // Notify UI to refresh
        if (UIManager.Instance != null)
            UIManager.Instance.RefreshUI();
        
        // Start turn sequence
        ProcessNextTurn();
    }

    private void ProcessNextTurn()
    {
        if (currentPhase != GamePhase.Playing) return;

        if (currentPlayer.IsHuman())
        {
            waitingForHumanInput = true;
        }
        else
        {
            waitingForHumanInput = false;
            StartCoroutine(AIPlayTurnCoroutine());
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

    public bool PlayerOwnsBuild(GamePlayer player) => PlayerHasPendingBuild(player);

    // forceTrail: play the card to the table without capturing. Legal whenever
    // the player does not own a build, even if captures are available.
    public void PlayCard(GamePlayer player, int cardIndex, bool forceTrail = false) {
        if (player != currentPlayer) {
            Debug.LogWarning($"It's not {player.Name}'s turn!");
            return;
        }

        // If it's a human player's turn, we should be waiting for input
        if (player.IsHuman() && !waitingForHumanInput) {
            Debug.LogWarning($"Not currently accepting human input!");
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

            // Builds are captured by matching value (1-10) or face rank (11-13)
            int buildValue = CaptureChecker.BuildCaptureValue(playedCard);

            // Check if player can capture their own builds
            var playerBuilds = activeBuilds.Where(b => b.Owner == player && b.DeclaredValue == buildValue).ToList();

            // Check for valid table card captures
            List<PlayingCard> captures = CaptureChecker.GetValidCaptures(playedCard, tableCards);

            // Check if player can capture opponent builds
            var opponentBuildsToCapture = activeBuilds.Where(b => b.Owner != player && b.DeclaredValue == buildValue).ToList();

            bool hasPendingBuild = PlayerHasPendingBuild(player);
            bool canCapture = captures.Count > 0 || playerBuilds.Count > 0 || opponentBuildsToCapture.Count > 0;

            // Build owners cannot trail: they must capture or keep building
            if (hasPendingBuild && (forceTrail || !canCapture))
            {
                Debug.LogWarning($"{player.Name} owns a build and must capture or build - cannot trail.");
                player.AddCard(playedCard); // Return card to hand
                return;
            }

            if (canCapture && !forceTrail)
            {
                if (UIManager.Instance != null)
                {
                    UIManager.Instance.AnimateCapture(playedCard, captures, player.IsHuman());
                    string taken = string.Join(" ", captures.Select(CaptureChecker.Describe));
                    UIManager.Instance.ShowMove(
                        $"{Who(player)} play{(player.IsHuman() ? "" : "s")} {CaptureChecker.Describe(playedCard)}" +
                        (taken.Length > 0 ? $" - takes {taken}" : " - takes a build"));
                }

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
                if (UIManager.Instance != null)
                {
                    UIManager.Instance.AnimateTrail(playedCard);
                    UIManager.Instance.ShowMove(
                        $"{Who(player)} trail{(player.IsHuman() ? "" : "s")} {CaptureChecker.Describe(playedCard)}");
                }
                tableCards.Add(playedCard);
                GameLogger.Instance.LogTrail(player, playedCard, tableCards);
            }
            
            AdvanceAfterPlay(player);
        }
    }

    [SerializeField] private float moveShowDelay = 1.3f;

    // Shared post-action bookkeeping: counts the play, clears the human-input
    // flag, refreshes UI, then pauses so the move is readable before the next
    // turn begins.
    private void AdvanceAfterPlay(GamePlayer player)
    {
        cardsPlayedThisRound++;

        if (player.IsHuman())
            waitingForHumanInput = false;

        if (UIManager.Instance != null)
            UIManager.Instance.RefreshUI();

        StartCoroutine(ContinueAfterPause());
    }

    private System.Collections.IEnumerator ContinueAfterPause()
    {
        yield return new WaitForSeconds(moveShowDelay);

        if (cardsPlayedThisRound == HAND_SIZE * 2)
        {
            EndRound();
        }
        else
        {
            currentPlayer = (currentPlayer == dealer) ? nonDealer : dealer;
            ProcessNextTurn();
        }
    }

    private string Who(GamePlayer p) => p.IsHuman() ? "You" : "AI";

    // Full build action for the current player (human UI or AI): plays the hand
    // card, validates and creates the build, and advances the turn. On failure
    // the hand card is returned and the turn does not advance.
    public bool TryCreateBuild(GamePlayer player, int handCardIndex, List<PlayingCard> tableCardsForBuild)
    {
        if (player != currentPlayer)
        {
            Debug.LogWarning($"It's not {player.Name}'s turn!");
            return false;
        }

        PlayingCard handCard = player.PlayCard(handCardIndex);
        if (handCard == null)
            return false;

        // Declared value: face builds use their rank value; numeric builds use
        // the highest value the cards partition into that the player can capture
        int declaredValue = CaptureChecker.PossibleBuildValues(handCard, tableCardsForBuild)
            .Where(v => PlayerCanCaptureValue(player, v))
            .DefaultIfEmpty(0)
            .Max();

        if (!CreateBuild(player, handCard, tableCardsForBuild, declaredValue))
        {
            player.AddCard(handCard); // invalid build: return card, turn continues
            return false;
        }

        AdvanceAfterPlay(player);
        return true;
    }

    // Capture exactly the chosen table cards with the chosen hand card.
    // The player picks what to sweep; partial captures are legal.
    public bool TryCaptureSelected(GamePlayer player, int handCardIndex, List<PlayingCard> chosenCards)
    {
        if (player != currentPlayer)
        {
            Debug.LogWarning($"It's not {player.Name}'s turn!");
            return false;
        }
        if (handCardIndex < 0 || handCardIndex >= player.HandSize())
            return false;

        PlayingCard handCard = player.Hand[handCardIndex];
        if (!CaptureChecker.IsExactCaptureSet(handCard, chosenCards))
            return false;

        handCard = player.PlayCard(handCardIndex);

        if (UIManager.Instance != null)
        {
            UIManager.Instance.AnimateCapture(handCard, chosenCards, player.IsHuman());
            UIManager.Instance.ShowMove(
                $"{Who(player)} play{(player.IsHuman() ? "" : "s")} {CaptureChecker.Describe(handCard)}" +
                $" - takes {string.Join(" ", chosenCards.Select(CaptureChecker.Describe))}");
        }

        foreach (var c in chosenCards)
            tableCards.Remove(c);

        player.AddCapturedCard(handCard);
        player.AddCapturedCards(chosenCards);
        lastPlayerToCaptureThisRound = player;
        GameLogger.Instance.LogCapture(player, handCard, chosenCards);

        if (tableCards.Count == 0 && activeBuilds.Count == 0)
        {
            player.IncrementSweepCount();
            GameLogger.Instance.LogSweep(player);
        }

        AdvanceAfterPlay(player);
        return true;
    }

    // Hard-AI evaluation of the current player's hand, exposed as a hint for
    // the human player. Read-only: nothing is played.
    public AIPlayer.AIAction GetSuggestionForCurrentPlayer()
    {
        if (currentPlayer == null || currentPlayer.HandSize() == 0)
            return null;

        var advisor = new AIPlayer(currentPlayer, AIPlayer.Difficulty.Hard);
        return advisor.GetBestAction(tableCards, activeBuilds);
    }
    
    private void EndRound()
    {
        GameLogger.Instance.LogRoundEnd(1, cardsPlayedThisRound);

        bool isDeckEmpty = deck.CardsRemaining() == 0;

        // The table persists for the whole deck. Trailed cards and builds are
        // only awarded once the deck is exhausted, to the last player to capture.
        bool awardNow = isDeckEmpty;

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

            // A player wins by reaching the win score. If both cross in the same
            // hand the higher score takes it; if they are tied, play continues.
            int winScore = ScoringManager.Instance.WinScore;
            bool someoneCrossed = dealer.Score >= winScore || nonDealer.Score >= winScore;

            if (someoneCrossed && dealer.Score != nonDealer.Score)
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

        // Refresh UI after round end
        if (UIManager.Instance != null)
            UIManager.Instance.RefreshUI();

        // Continue game flow
        ProcessNextTurn();
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

        // Reset for next round. Sweeps are scored once, in the round they occur.
        dealer.ClearCapturedCards();
        nonDealer.ClearCapturedCards();
        dealer.ResetSweepCount();
        nonDealer.ResetSweepCount();
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
        
        // Swap AI instances too
        (dealerAI, nonDealerAI) = (nonDealerAI, dealerAI);
        
        GameLogger.Instance.LogDealerSwap(dealer);
    }
    
    private void EndGame() {
        currentPhase = GamePhase.GameOver;
        var winner = dealer.Score > nonDealer.Score ? dealer : nonDealer;
        var loser = winner == dealer ? nonDealer : dealer;

        GameLogger.Instance.LogGameOverWithBreakdown(
            winner, loser,
            ScoringManager.Instance.WinScore,
            winner == dealer ? dealerScoreBreakdown : nonDealerScoreBreakdown,
            loser == dealer ? dealerScoreBreakdown : nonDealerScoreBreakdown
        );

        // Refresh UI to show game over state
        if (UIManager.Instance != null)
            UIManager.Instance.RefreshUI();
    }
    
    public GamePlayer GetCurrentPlayer() => currentPlayer;
    public GamePlayer GetDealer() => dealer;
    public GamePlayer GetNonDealer() => nonDealer;
    public List<PlayingCard> GetTableCards() => tableCards;
    public List<Build> GetActiveBuilds() => activeBuilds;
    public GameDeck GetDeck() => deck;
    public GamePhase GetCurrentPhase() => currentPhase;
    public bool IsWaitingForHumanInput() => waitingForHumanInput;

    // Build management methods
    private bool PlayerHasPendingBuild(GamePlayer player)
    {
        return activeBuilds.Any(b => b.Owner == player);
    }

    private bool PlayerCanCaptureValue(GamePlayer player, int value)
    {
        return player.Hand.Any(card => CaptureChecker.BuildCaptureValue(card) == value);
    }

    // Legal builds: numeric builds of value 1-10, or same-rank face builds
    // (Jacks / Queens / Kings). Nothing else.
    private bool CanCreateBuild(GamePlayer player, List<PlayingCard> cards, int declaredValue)
    {
        if (cards.Any(card => IsFaceCard(card)))
        {
            // Face build: every card must share one face rank
            var rank = cards[0].rank;
            if (!IsFaceCard(cards[0]) || cards.Any(c => c.rank != rank))
            {
                Debug.LogWarning("Face builds must be a single rank: all Jacks, all Queens, or all Kings.");
                return false;
            }

            if (declaredValue != CaptureChecker.BuildCaptureValue(cards[0]))
            {
                Debug.LogWarning($"Face build declared value {declaredValue} does not match rank {rank}.");
                return false;
            }

            // Must hold another card of that rank to capture the build
            if (!player.Hand.Any(c => c.rank == rank))
            {
                Debug.LogWarning($"{player.Name} holds no {rank} to capture this build!");
                return false;
            }

            return true;
        }

        // Numeric build: value must be 1-10
        if (declaredValue < 1 || declaredValue > 10)
        {
            Debug.LogWarning($"Build value {declaredValue} is not capturable - builds are 1-10 or face ranks.");
            return false;
        }

        // Must have the capture card in hand
        if (!PlayerCanCaptureValue(player, declaredValue))
        {
            Debug.LogWarning($"{player.Name} does not have a card with value {declaredValue} to capture this build!");
            return false;
        }

        // The cards must partition into one or more sets each summing to the
        // declared value (multiple sets = a multi-build, e.g. three 10s)
        if (!CaptureChecker.CanPartitionExact(new List<PlayingCard>(cards), declaredValue))
        {
            Debug.LogWarning($"Cards do not form set(s) of {declaredValue}!");
            return false;
        }

        return true;
    }

    private bool IsFaceCard(PlayingCard card)
    {
        return card.rank == PlayingCard.Rank.Jack ||
               card.rank == PlayingCard.Rank.Queen ||
               card.rank == PlayingCard.Rank.King;
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

        if (UIManager.Instance != null)
            UIManager.Instance.ShowMove($"{Who(player)} build{(player.IsHuman() ? "" : "s")} {(declaredValue > 10 ? ((PlayingCard.Rank)(declaredValue - 1)).ToString() + "s" : declaredValue.ToString())}");

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
    
    private System.Collections.IEnumerator AIPlayTurnCoroutine()
    {
        // Wait for the AI move delay
        yield return new WaitForSeconds(aiMoveDelay);

        if (currentPhase == GamePhase.GameOver) yield break;
        if (currentPlayer.IsHuman()) yield break; // Safety check

        // Check if current player has cards to play
        if (currentPlayer.HandSize() == 0)
        {
            Debug.LogWarning($"AIPlayTurn called but {currentPlayer.Name} has no cards!");
            yield break;
        }

        var ai = currentPlayer == dealer ? dealerAI : nonDealerAI;
        var action = ai.GetBestAction(tableCards, activeBuilds);

        // If it's a PlayCard action, check if there will be captures and highlight them
        if (action.Type == AIPlayer.AIAction.ActionType.PlayCard && UIManager.Instance != null)
        {
            var handCard = currentPlayer.Hand[action.CardIndex];
            List<PlayingCard> captures = CaptureChecker.GetValidCaptures(handCard, tableCards);

            if (captures.Count > 0)
            {
                // Highlight the cards that will be captured
                yield return StartCoroutine(UIManager.Instance.HighlightTableCardsForCapture(captures, 0.8f));
            }
        }

        // Execute the AI's chosen action
        switch (action.Type)
        {
            case AIPlayer.AIAction.ActionType.PlayCard:
                PlayCard(currentPlayer, action.CardIndex);
                break;

            case AIPlayer.AIAction.ActionType.CreateBuild:
                if (!TryCreateBuild(currentPlayer, action.CardIndex, action.BuildCards))
                {
                    // Build was invalid: play the card normally (capture or trail)
                    Debug.LogWarning("AI build creation failed, playing card normally");
                    PlayCard(currentPlayer, action.CardIndex);
                }
                break;

            case AIPlayer.AIAction.ActionType.ModifyBuild:
                // TODO: Implement build modification
                Debug.LogWarning("AI build modification not yet implemented");
                PlayCard(currentPlayer, action.CardIndex);
                break;
        }
    }
    
    public void SetDealerAIDifficulty(AIPlayer.Difficulty diff) {
        if (dealerAI != null)
            dealerAI.SetDifficulty(diff);
        Debug.Log("Dealer AI difficulty set to: " + diff);
    }

    public void SetNonDealerAIDifficulty(AIPlayer.Difficulty diff)
    {
        if (nonDealerAI != null)
            nonDealerAI.SetDifficulty(diff);
        Debug.Log("Non-Dealer AI difficulty set to: " + diff);
    }
    
    
}