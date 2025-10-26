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
    
    private AIPlayer dealerAI;
    private AIPlayer nonDealerAI;
    
    private GamePhase currentPhase;
    private GamePlayer currentPlayer;
    private GamePlayer lastPlayerToCaptureThisRound;
    private int cardsPlayedThisRound = 0;
    private const int HAND_SIZE = 4;
    private const int TABLE_SIZE = 4;
    
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
            
            // Check for valid captures
            List<PlayingCard> captures = CaptureChecker.GetValidCaptures(playedCard, tableCards);
            
            if (captures.Count > 0)
            {
                // Capture cards
                foreach (PlayingCard capturedCard in captures)
                {
                    tableCards.Remove(capturedCard);
                    ((List<PlayingCard>)player.CapturedCards).Add(capturedCard);
                }
                
                GameLogger.Instance.LogCapture(player, captures);
                
                // Check for sweep
                if (tableCards.Count == 0)
                {
                    player.IncrementSweepCount();
                    GameLogger.Instance.LogSweep(player);
                }
            }
            else
            {
                // Trail - add to table
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
        
        // Any remaining table cards go to the last player who captured (dealer in this case)
        // For now, just leave them on table - they'll stay until next round
        
        cardsPlayedThisRound = 0;
        
        if (deck.CardsRemaining() == 0)
        {
            GameLogger.Instance.LogDeckStatus(0);
            ScoreRound();
            SwapDealer();
            
            if (dealer.Score >= ScoringManager.Instance.GetWinScore() || 
                nonDealer.Score >= ScoringManager.Instance.GetWinScore())
            {
                EndGame();
                return;
            }
            
            deck = new GameDeck();
            deck.Shuffle();
        }
        else
        {
            GameLogger.Instance.LogDeckStatus(deck.CardsRemaining());
        }
        
        GameLogger.Instance.LogNewDeal(1);
        nonDealer.AddCards(deck.DrawCards(HAND_SIZE));
        dealer.AddCards(deck.DrawCards(HAND_SIZE));
        
        currentPlayer = nonDealer;
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
            nonDealer.AddScore(sm.GetPointsForMostCards());
            GameLogger.Instance.LogScoreAward(nonDealer.Name, "Most cards (" + nonDealerCardCount + ")", sm.GetPointsForMostCards());
        }
        else if (dealerCardCount > nonDealerCardCount)
        {
            dealer.AddScore(sm.GetPointsForMostCards());
            GameLogger.Instance.LogScoreAward(dealer.Name, "Most cards (" + dealerCardCount + ")", sm.GetPointsForMostCards());
        }
        
        // Most spades
        if (nonDealerSpades > dealerSpades && sm.GetPointsForMostSpades() > 0)
        {
            nonDealer.AddScore(sm.GetPointsForMostSpades());
            GameLogger.Instance.LogScoreAward(nonDealer.Name, "Most spades (" + nonDealerSpades + ")", sm.GetPointsForMostSpades());
        }
        else if (dealerSpades > nonDealerSpades && sm.GetPointsForMostSpades() > 0)
        {
            dealer.AddScore(sm.GetPointsForMostSpades());
            GameLogger.Instance.LogScoreAward(dealer.Name, "Most spades (" + dealerSpades + ")", sm.GetPointsForMostSpades());
        }
        
        // Big Casino (10 of Diamonds)
        if (HasCard(dealer.CapturedCards.ToList(), PlayingCard.Suit.Diamonds, PlayingCard.Rank.Ten))
        {
            dealer.AddScore(sm.GetPointsForBigCasino());
            GameLogger.Instance.LogScoreAward(dealer.Name, "Big Casino (10 of Diamonds)", sm.GetPointsForBigCasino());
        }
        else if (HasCard(nonDealer.CapturedCards.ToList(), PlayingCard.Suit.Diamonds, PlayingCard.Rank.Ten))
        {
            nonDealer.AddScore(sm.GetPointsForBigCasino());
            GameLogger.Instance.LogScoreAward(nonDealer.Name, "Big Casino (10 of Diamonds)", sm.GetPointsForBigCasino());
        }
        
        // Little Casino (2 of Spades)
        if (HasCard(dealer.CapturedCards.ToList(), PlayingCard.Suit.Spades, PlayingCard.Rank.Two))
        {
            dealer.AddScore(sm.GetPointsForLittleCasino());
            GameLogger.Instance.LogScoreAward(dealer.Name, "Little Casino (2 of Spades)", sm.GetPointsForLittleCasino());
        }
        else if (HasCard(nonDealer.CapturedCards.ToList(), PlayingCard.Suit.Spades, PlayingCard.Rank.Two))
        {
            nonDealer.AddScore(sm.GetPointsForLittleCasino());
            GameLogger.Instance.LogScoreAward(nonDealer.Name, "Little Casino (2 of Spades)", sm.GetPointsForLittleCasino());
        }
        
        // Aces
        int dealerAces = CountAces(dealer.CapturedCards.ToList());
        int nonDealerAces = CountAces(nonDealer.CapturedCards.ToList());
        dealer.AddScore(dealerAces * sm.GetPointsPerAce());
        nonDealer.AddScore(nonDealerAces * sm.GetPointsPerAce());
        
        if (dealerAces > 0)
            GameLogger.Instance.LogScoreAward(dealer.Name, dealerAces + " Ace(s)", dealerAces * sm.GetPointsPerAce());
        if (nonDealerAces > 0)
            GameLogger.Instance.LogScoreAward(nonDealer.Name, nonDealerAces + " Ace(s)", nonDealerAces * sm.GetPointsPerAce());
        
        // Sweeps
        dealer.AddScore(dealer.SweepCount * sm.GetPointsPerSweep());
        nonDealer.AddScore(nonDealer.SweepCount * sm.GetPointsPerSweep());
        
        if (dealer.SweepCount > 0)
            GameLogger.Instance.LogScoreAward(dealer.Name, dealer.SweepCount + " Sweep(s)", dealer.SweepCount * sm.GetPointsPerSweep());
        if (nonDealer.SweepCount > 0)
            GameLogger.Instance.LogScoreAward(nonDealer.Name, nonDealer.SweepCount + " Sweep(s)", nonDealer.SweepCount * sm.GetPointsPerSweep());
        
        GameLogger.Instance.LogCumulativeScores(dealer, nonDealer);
        
        // Reset for next round
        ((List<PlayingCard>)dealer.CapturedCards).Clear();
        ((List<PlayingCard>)nonDealer.CapturedCards).Clear();
    }
    
    private int CountSpades(List<PlayingCard> cards) =>
        cards.Count(card => card.suit == PlayingCard.Suit.Spades);
    
    private int CountAces(List<PlayingCard> cards) =>
        cards.Count(card => card.rank == PlayingCard.Rank.Ace);
    
    private bool HasCard(List<PlayingCard> cards, PlayingCard.Suit suit, PlayingCard.Rank rank) =>
        cards.Any(card => card.suit == suit && card.rank == rank);
    
    private void SwapDealer() {
        (dealer, nonDealer) = (nonDealer, dealer);
        GameLogger.Instance.LogDealerSwap(dealer);
    }
    
    private void EndGame() {
        currentPhase = GamePhase.GameOver;
        var winner = dealer.Score >= ScoringManager.Instance.GetWinScore() ? dealer : nonDealer;
        GameLogger.Instance.LogGameOver(winner, ScoringManager.Instance.GetWinScore());
    }
    
    public GamePlayer GetCurrentPlayer() => currentPlayer;
    public GamePlayer GetDealer() => dealer;
    public GamePlayer GetNonDealer() => nonDealer;
    public List<PlayingCard> GetTableCards() => tableCards;
    public GameDeck GetDeck() => deck;
    public GamePhase GetCurrentPhase() => currentPhase;
    
    public void AIPlayTurn() {
        if (!useAI || currentPhase == GamePhase.GameOver) return;
        
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