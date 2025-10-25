using System.Collections.Generic;
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
    
    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }
    
    private void Start()
    {
        Invoke(nameof(InitializeGame), 0.1f);
    }
    
    public void InitializeGame()
    {
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
        if (useAI)
        {
            Invoke(nameof(AIPlayTurn), 1f);
        }
    }
    
    private void DealInitialRound()
    {
        nonDealer.AddCards(deck.DrawCards(HAND_SIZE));
        dealer.AddCards(deck.DrawCards(HAND_SIZE));
        tableCards.AddRange(deck.DrawCards(TABLE_SIZE));
    }
    
    public void PlayCard(GamePlayer player, int cardIndex)
    {
        if (player != currentPlayer)
        {
            Debug.LogWarning($"It's not {player.playerName}'s turn!");
            return;
        }
        
        if (player.HandSize() == 0)
        {
            Debug.LogWarning($"{player.playerName} has no cards to play!");
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
                    player.capturedCards.Add(capturedCard);
                }
                
                GameLogger.Instance.LogCapture(player, captures);
                
                // Check for sweep
                if (tableCards.Count == 0)
                {
                    player.sweepCount += 1;
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
            
            if (dealer.score >= ScoringManager.Instance.GetWinScore() || 
                nonDealer.score >= ScoringManager.Instance.GetWinScore())
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
        
        int dealerCardCount = dealer.capturedCards.Count;
        int nonDealerCardCount = nonDealer.capturedCards.Count;
        int dealerSpades = CountSpades(dealer.capturedCards);
        int nonDealerSpades = CountSpades(nonDealer.capturedCards);
        
        GameLogger.Instance.LogHandTotals(dealer, nonDealer, dealerCardCount, nonDealerCardCount, dealerSpades, nonDealerSpades);
        
        // Most cards
        if (nonDealerCardCount > dealerCardCount)
        {
            nonDealer.AddScore(sm.GetPointsForMostCards());
            GameLogger.Instance.LogScoreAward(nonDealer.playerName, "Most cards (" + nonDealerCardCount + ")", sm.GetPointsForMostCards());
        }
        else if (dealerCardCount > nonDealerCardCount)
        {
            dealer.AddScore(sm.GetPointsForMostCards());
            GameLogger.Instance.LogScoreAward(dealer.playerName, "Most cards (" + dealerCardCount + ")", sm.GetPointsForMostCards());
        }
        
        // Most spades
        if (nonDealerSpades > dealerSpades && sm.GetPointsForMostSpades() > 0)
        {
            nonDealer.AddScore(sm.GetPointsForMostSpades());
            GameLogger.Instance.LogScoreAward(nonDealer.playerName, "Most spades (" + nonDealerSpades + ")", sm.GetPointsForMostSpades());
        }
        else if (dealerSpades > nonDealerSpades && sm.GetPointsForMostSpades() > 0)
        {
            dealer.AddScore(sm.GetPointsForMostSpades());
            GameLogger.Instance.LogScoreAward(dealer.playerName, "Most spades (" + dealerSpades + ")", sm.GetPointsForMostSpades());
        }
        
        // Big Casino (10 of Diamonds)
        if (HasCard(dealer.capturedCards, PlayingCard.Suit.Diamonds, PlayingCard.Rank.Ten))
        {
            dealer.AddScore(sm.GetPointsForBigCasino());
            GameLogger.Instance.LogScoreAward(dealer.playerName, "Big Casino (10 of Diamonds)", sm.GetPointsForBigCasino());
        }
        else if (HasCard(nonDealer.capturedCards, PlayingCard.Suit.Diamonds, PlayingCard.Rank.Ten))
        {
            nonDealer.AddScore(sm.GetPointsForBigCasino());
            GameLogger.Instance.LogScoreAward(nonDealer.playerName, "Big Casino (10 of Diamonds)", sm.GetPointsForBigCasino());
        }
        
        // Little Casino (2 of Spades)
        if (HasCard(dealer.capturedCards, PlayingCard.Suit.Spades, PlayingCard.Rank.Two))
        {
            dealer.AddScore(sm.GetPointsForLittleCasino());
            GameLogger.Instance.LogScoreAward(dealer.playerName, "Little Casino (2 of Spades)", sm.GetPointsForLittleCasino());
        }
        else if (HasCard(nonDealer.capturedCards, PlayingCard.Suit.Spades, PlayingCard.Rank.Two))
        {
            nonDealer.AddScore(sm.GetPointsForLittleCasino());
            GameLogger.Instance.LogScoreAward(nonDealer.playerName, "Little Casino (2 of Spades)", sm.GetPointsForLittleCasino());
        }
        
        // Aces
        int dealerAces = CountAces(dealer.capturedCards);
        int nonDealerAces = CountAces(nonDealer.capturedCards);
        dealer.AddScore(dealerAces * sm.GetPointsPerAce());
        nonDealer.AddScore(nonDealerAces * sm.GetPointsPerAce());
        
        if (dealerAces > 0)
            GameLogger.Instance.LogScoreAward(dealer.playerName, dealerAces + " Ace(s)", dealerAces * sm.GetPointsPerAce());
        if (nonDealerAces > 0)
            GameLogger.Instance.LogScoreAward(nonDealer.playerName, nonDealerAces + " Ace(s)", nonDealerAces * sm.GetPointsPerAce());
        
        // Sweeps
        dealer.AddScore(dealer.sweepCount * sm.GetPointsPerSweep());
        nonDealer.AddScore(nonDealer.sweepCount * sm.GetPointsPerSweep());
        
        if (dealer.sweepCount > 0)
            GameLogger.Instance.LogScoreAward(dealer.playerName, dealer.sweepCount + " Sweep(s)", dealer.sweepCount * sm.GetPointsPerSweep());
        if (nonDealer.sweepCount > 0)
            GameLogger.Instance.LogScoreAward(nonDealer.playerName, nonDealer.sweepCount + " Sweep(s)", nonDealer.sweepCount * sm.GetPointsPerSweep());
        
        GameLogger.Instance.LogCumulativeScores(dealer, nonDealer);
        
        // Reset for next round
        dealer.capturedCards.Clear();
        dealer.sweepCount = 0;
        nonDealer.capturedCards.Clear();
        nonDealer.sweepCount = 0;
    }
    
    private int CountSpades(List<PlayingCard> cards)
    {
        int count = 0;
        foreach (PlayingCard card in cards)
        {
            if (card.suit == PlayingCard.Suit.Spades)
                count++;
        }
        return count;
    }
    
    private int CountAces(List<PlayingCard> cards)
    {
        int count = 0;
        foreach (PlayingCard card in cards)
        {
            if (card.rank == PlayingCard.Rank.Ace)
                count++;
        }
        return count;
    }
    
    private bool HasCard(List<PlayingCard> cards, PlayingCard.Suit suit, PlayingCard.Rank rank)
    {
        foreach (PlayingCard card in cards)
        {
            if (card.suit == suit && card.rank == rank)
                return true;
        }
        return false;
    }
    
    private void SwapDealer()
    {
        GamePlayer temp = dealer;
        dealer = nonDealer;
        nonDealer = temp;
        GameLogger.Instance.LogDealerSwap(dealer);
    }
    
    private void EndGame()
    {
        currentPhase = GamePhase.GameOver;
        GamePlayer winner = (dealer.score >= ScoringManager.Instance.GetWinScore()) ? dealer : nonDealer;
        GameLogger.Instance.LogGameOver(winner, ScoringManager.Instance.GetWinScore());
    }
    
    public GamePlayer GetCurrentPlayer() => currentPlayer;
    public GamePlayer GetDealer() => dealer;
    public GamePlayer GetNonDealer() => nonDealer;
    public List<PlayingCard> GetTableCards() => tableCards;
    public GameDeck GetDeck() => deck;
    public GamePhase GetCurrentPhase() => currentPhase;
    
    public void AIPlayTurn()
    {
        if (!useAI || currentPhase == GamePhase.GameOver)
            return;
        
        AIPlayer currentAI = (currentPlayer == dealer) ? dealerAI : nonDealerAI;
        int bestMove = currentAI.GetBestMove(tableCards);
        PlayCard(currentPlayer, bestMove);
        
        // Schedule next AI turn if game is still playing
        if (useAI && currentPhase == GamePhase.Playing)
        {
            Invoke(nameof(AIPlayTurn), 1f);
        }
    }
    
    public void SetAIDifficulty(AIPlayer.Difficulty diff)
    {
        dealerAI.SetDifficulty(diff);
        nonDealerAI.SetDifficulty(diff);
        Debug.Log("AI difficulty set to: " + diff);
    }
    
    public void SetDealerAIDifficulty(AIPlayer.Difficulty diff)
    {
        dealerAI.SetDifficulty(diff);
        Debug.Log("Dealer AI difficulty set to: " + diff);
    }
    
    public void SetNonDealerAIDifficulty(AIPlayer.Difficulty diff)
    {
        nonDealerAI.SetDifficulty(diff);
        Debug.Log("Non-Dealer AI difficulty set to: " + diff);
    }
    
    public void SetUseAI(bool enabled)
    {
        useAI = enabled;
        Debug.Log("AI enabled: " + useAI);
    }
}