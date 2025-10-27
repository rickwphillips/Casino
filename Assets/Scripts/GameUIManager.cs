using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class GameUIManager : MonoBehaviour
{
    public static GameUIManager Instance { get; private set; }

    [Header("Player 1 (Dealer) UI")]
    [SerializeField] private Text player1NameText;
    [SerializeField] private Text player1ScoreText;
    [SerializeField] private Text player1HandText;
    [SerializeField] private Text player1CapturedText;

    [Header("Player 2 (Non-Dealer) UI")]
    [SerializeField] private Text player2NameText;
    [SerializeField] private Text player2ScoreText;
    [SerializeField] private Text player2HandText;
    [SerializeField] private Text player2CapturedText;

    [Header("Table UI")]
    [SerializeField] private Text tableCardsText;
    [SerializeField] private Text buildsText;

    [Header("Game State UI")]
    [SerializeField] private Text currentTurnText;
    [SerializeField] private Text gamePhaseText;
    [SerializeField] private Text variantText;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        InvokeRepeating(nameof(UpdateUI), 0.5f, 0.5f);
    }

    public void UpdateUI()
    {
        if (GameManager.Instance == null) return;

        UpdatePlayerUI();
        UpdateTableUI();
        UpdateGameStateUI();
    }

    private void UpdatePlayerUI()
    {
        var dealer = GameManager.Instance.GetDealer();
        var nonDealer = GameManager.Instance.GetNonDealer();

        if (dealer != null)
        {
            if (player1NameText != null)
                player1NameText.text = $"{dealer.Name} (Dealer)";

            if (player1ScoreText != null)
                player1ScoreText.text = $"Score: {dealer.Score} | Sweeps: {dealer.SweepCount}";

            if (player1HandText != null)
                player1HandText.text = $"Hand ({dealer.HandSize()}): {FormatCards(dealer.Hand.ToList())}";

            if (player1CapturedText != null)
                player1CapturedText.text = $"Captured ({dealer.CapturedCards.Count}): {FormatCards(dealer.CapturedCards.ToList())}";
        }

        if (nonDealer != null)
        {
            if (player2NameText != null)
                player2NameText.text = nonDealer.Name;

            if (player2ScoreText != null)
                player2ScoreText.text = $"Score: {nonDealer.Score} | Sweeps: {nonDealer.SweepCount}";

            if (player2HandText != null)
                player2HandText.text = $"Hand ({nonDealer.HandSize()}): {FormatCards(nonDealer.Hand.ToList())}";

            if (player2CapturedText != null)
                player2CapturedText.text = $"Captured ({nonDealer.CapturedCards.Count}): {FormatCards(nonDealer.CapturedCards.ToList())}";
        }
    }

    private void UpdateTableUI()
    {
        var tableCards = GameManager.Instance.GetTableCards();
        var builds = GameManager.Instance.GetActiveBuilds();

        if (tableCardsText != null)
        {
            tableCardsText.text = $"Table Cards ({tableCards.Count}): {FormatCards(tableCards)}";
        }

        if (buildsText != null)
        {
            if (builds.Count > 0)
            {
                var buildStrings = builds.Select(b =>
                    $"[{b.DeclaredValue}] {FormatCards(b.Cards.ToList())} (Owner: {b.Owner.Name})");
                buildsText.text = $"Builds ({builds.Count}):\n" + string.Join("\n", buildStrings);
            }
            else
            {
                buildsText.text = "Builds: None";
            }
        }
    }

    private void UpdateGameStateUI()
    {
        var currentPlayer = GameManager.Instance.GetCurrentPlayer();

        if (currentTurnText != null && currentPlayer != null)
        {
            currentTurnText.text = $"Current Turn: {currentPlayer.Name}";
        }

        if (gamePhaseText != null && ScoringManager.Instance != null)
        {
            gamePhaseText.text = $"Win Score: {ScoringManager.Instance.WinScore}";
        }

        if (variantText != null && ScoringManager.Instance != null)
        {
            variantText.text = $"Variant: {ScoringManager.Instance.CurrentVariant}";
        }
    }

    private string FormatCards(List<PlayingCard> cards)
    {
        if (cards == null || cards.Count == 0)
            return "None";

        return string.Join(", ", cards.Select(c => FormatCard(c)));
    }

    private string FormatCard(PlayingCard card)
    {
        // Use Unicode suit symbols for compact display
        string suit = card.suit switch
        {
            PlayingCard.Suit.Hearts => "♥",
            PlayingCard.Suit.Diamonds => "♦",
            PlayingCard.Suit.Clubs => "♣",
            PlayingCard.Suit.Spades => "♠",
            _ => "?"
        };

        string rank = card.rank switch
        {
            PlayingCard.Rank.Ace => "A",
            PlayingCard.Rank.Jack => "J",
            PlayingCard.Rank.Queen => "Q",
            PlayingCard.Rank.King => "K",
            _ => ((int)card.rank).ToString()
        };

        return $"{rank}{suit}";
    }
}
