using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CardUI : MonoBehaviour
{
    private PlayingCard card;
    private Button button;
    private TextMeshProUGUI rankSuitText;
    private Image cardImage;
    private bool isSelectable = false;

    public PlayingCard Card => card;
    private bool isSelected = false;
    
    private Vector3 originalScale;
    private float animationSpeed = 0.15f;
    
    private void Start()
    {
        button = GetComponent<Button>();
        cardImage = GetComponent<Image>();
        rankSuitText = GetComponentInChildren<TextMeshProUGUI>();
        
        originalScale = transform.localScale;
        
        if (button != null)
            button.onClick.AddListener(OnCardClicked);
    }
    
    public void Initialize(PlayingCard c, bool selectable = false)
    {
        card = c;
        isSelectable = selectable;
        isSelected = false;
        UpdateDisplay();
    }
    
    private void UpdateDisplay()
    {
        if (rankSuitText != null && card != null)
        {
            rankSuitText.text = $"{card.rank}\n{card.suit}";
        }
        
        if (button != null)
            button.interactable = isSelectable;
        
        UpdateVisuals();
    }
    
    private void UpdateVisuals()
    {
        if (cardImage != null)
        {
            if (isSelected)
                cardImage.color = new(0.7f, 0.9f, 1f);
            else if (isSelectable)
                cardImage.color = Color.white;
            else
                cardImage.color = new Color(0.7f, 0.7f, 0.7f); // Gray out non-selectable cards
        }
    }
    
    private void OnCardClicked()
    {
        if (!isSelectable) return;
        
        isSelected = !isSelected;
        UpdateVisuals();
        
        StopAllCoroutines();
        StartCoroutine(AnimateCardScale(isSelected ? 1.15f : 1f));
        
        UIManager.Instance.OnCardSelected(this, card);
    }
    
    private System.Collections.IEnumerator AnimateCardScale(float targetScale)
    {
        Vector3 startScale = transform.localScale;
        Vector3 endScale = originalScale * targetScale;
        float elapsedTime = 0f;
        
        while (elapsedTime < animationSpeed)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / animationSpeed;
            transform.localScale = Vector3.Lerp(startScale, endScale, t);
            yield return null;
        }
        
        transform.localScale = endScale;
    }
    
    public void SetSelected(bool selected)
    {
        isSelected = selected;
        UpdateVisuals();
    }
    
    public PlayingCard GetCard()
    {
        return card;
    }
}

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }
    
    [SerializeField] private Transform dealerHandContainer;
    [SerializeField] private Transform nonDealerHandContainer;
    [SerializeField] private Transform tableCardsContainer;
    [SerializeField] private TextMeshProUGUI currentPlayerText;
    [SerializeField] private TextMeshProUGUI deckCountText;
    [SerializeField] private TextMeshProUGUI gameStatusText;
    [SerializeField] private TextMeshProUGUI dealerScoreText;
    [SerializeField] private TextMeshProUGUI nonDealerScoreText;
    [SerializeField] private GameObject cardPrefab;
    [SerializeField] private Button playCardButton;
    [SerializeField] private Button restartButton;
    
    private CardUI selectedCard = null;
    private List<CardUI> dealerCardUIs = new();
    private List<CardUI> nonDealerCardUIs = new();
    private List<CardUI> tableCardUIs = new();
    
    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }
    
    private void Start()
    {
        if (playCardButton != null)
            playCardButton.onClick.AddListener(OnPlayCardClicked);
        
        if (restartButton != null)
            restartButton.onClick.AddListener(OnRestartClicked);
        
        StartCoroutine(WaitAndRefresh());
    }
    
    private System.Collections.IEnumerator WaitAndRefresh()
    {
        yield return new WaitForSeconds(0.1f);
        RefreshUI();
    }
    
    private void Update()
    {
        // Refresh UI every frame to show current state
        if (GameManager.Instance != null && GameManager.Instance.GetCurrentPlayer() != null)
        {
            UpdateGameInfo();
            UpdatePlayButtonState();
        }
    }
    
    public void RefreshUI()
    {
        if (GameManager.Instance == null) return;
        
        UpdatePlayerHands();
        UpdateTableCards();
        UpdateGameInfo();
        UpdatePlayButtonState();
    }
    
    private void UpdatePlayerHands()
    {
        if (GameManager.Instance == null) return;
        
        GamePlayer dealer = GameManager.Instance.GetDealer();
        GamePlayer nonDealer = GameManager.Instance.GetNonDealer();
        
        if (dealer == null || nonDealer == null) return;
        
        // Determine if each player's cards should be selectable
        bool dealerCanSelect = ShouldPlayerCardsBeSelectable(dealer);
        bool nonDealerCanSelect = ShouldPlayerCardsBeSelectable(nonDealer);
        
        // Only update if hands changed
        if (dealerCardUIs.Count != dealer.Hand.Count)
            UpdateHandDisplay(dealer, dealerHandContainer, dealerCardUIs, dealerCanSelect);
        else
            UpdateHandSelectability(dealer, dealerHandContainer, dealerCardUIs, dealerCanSelect);
        
        if (nonDealerCardUIs.Count != nonDealer.Hand.Count)
            UpdateHandDisplay(nonDealer, nonDealerHandContainer, nonDealerCardUIs, nonDealerCanSelect);
        else
            UpdateHandSelectability(nonDealer, nonDealerHandContainer, nonDealerCardUIs, nonDealerCanSelect);
    }
    
    // CRITICAL: This determines if a player's cards should be clickable
    private bool ShouldPlayerCardsBeSelectable(GamePlayer player)
    {
        if (GameManager.Instance == null) return false;
        if (player == null) return false;
        if (GameManager.Instance.GetCurrentPhase() != GameManager.GamePhase.Playing) return false;
        
        // Cards are only selectable if:
        // 1. It's this player's turn
        // 2. The player is human
        // 3. The game is waiting for human input
        // 4. Player has cards in hand
        return player == GameManager.Instance.GetCurrentPlayer() &&
               player.IsHuman() &&
               GameManager.Instance.IsWaitingForHumanInput() &&
               player.HandSize() > 0;
    }
    
    private void UpdateHandSelectability(GamePlayer player, Transform container, List<CardUI> cardUIs, bool selectable)
    {
        for (int i = 0; i < cardUIs.Count && i < player.Hand.Count; i++)
        {
            cardUIs[i].Initialize(player.Hand[i], selectable);
        }
    }
    
    private void UpdateHandDisplay(GamePlayer player, Transform container, List<CardUI> cardUIs, bool selectable)
    {
        if (player == null || container == null || cardPrefab == null)
        {
            Debug.LogError("UpdateHandDisplay: Missing required components");
            return;
        }
        
        // Clear old cards
        foreach (Transform child in container)
        {
            Destroy(child.gameObject);
        }
        cardUIs.Clear();

        // Create new cards
        for (int i = 0; i < player.Hand.Count; i++)
        {
            GameObject cardObj = Instantiate(cardPrefab, container);
            CardUI cardUI = cardObj.AddComponent<CardUI>();
            cardUI.Initialize(player.Hand[i], selectable);
            cardUIs.Add(cardUI);
            
            StartCoroutine(AnimateCardAppearance(cardUI, i * 0.05f));
        }
    }
    
    private void UpdateTableCards()
    {
        if (GameManager.Instance == null) return;
        
        List<PlayingCard> tableCards = GameManager.Instance.GetTableCards();
        
        if (tableCards == null) return;
        
        if (tableCardUIs.Count != tableCards.Count)
        {
            foreach (Transform child in tableCardsContainer)
            {
                Destroy(child.gameObject);
            }
            tableCardUIs.Clear();
            
            for (int i = 0; i < tableCards.Count; i++)
            {
                GameObject cardObj = Instantiate(cardPrefab, tableCardsContainer);
                CardUI cardUI = cardObj.AddComponent<CardUI>();
                cardUI.Initialize(tableCards[i], false);
                tableCardUIs.Add(cardUI);
                
                StartCoroutine(AnimateCardAppearance(cardUI, i * 0.05f));
            }
        }
    }
    
    private void UpdateGameInfo()
    {
        if (GameManager.Instance == null) return;
        
        GamePlayer currentPlayer = GameManager.Instance.GetCurrentPlayer();
        GamePlayer dealer = GameManager.Instance.GetDealer();
        GamePlayer nonDealer = GameManager.Instance.GetNonDealer();
        GameDeck deck = GameManager.Instance.GetDeck();
        
        if (currentPlayer == null || dealer == null || nonDealer == null || deck == null)
            return;
        
        // Update turn indicator
        if (currentPlayerText != null)
        {
            string playerType = currentPlayer.IsHuman() ? "(HUMAN)" : "(AI)";
            string waitingStatus = "";
            
            if (currentPlayer.IsHuman() && GameManager.Instance.IsWaitingForHumanInput())
            {
                waitingStatus = " - YOUR TURN!";
            }
            else if (currentPlayer.IsAI())
            {
                waitingStatus = " - AI thinking...";
            }
            
            currentPlayerText.text = $"Current Turn: {currentPlayer.Name} {playerType}{waitingStatus}";
        }
        
        if (deckCountText != null)
            deckCountText.text = $"Cards in Deck: {deck.CardsRemaining()}";
        
        if (dealerScoreText != null)
        {
            string typeIndicator = dealer.IsHuman() ? " (Human)" : " (AI)";
            dealerScoreText.text = $"Dealer: {dealer.Name}{typeIndicator}\nScore: {dealer.Score}";
        }
        
        if (nonDealerScoreText != null)
        {
            string typeIndicator = nonDealer.IsHuman() ? " (Human)" : " (AI)";
            nonDealerScoreText.text = $"Non-Dealer: {nonDealer.Name}{typeIndicator}\nScore: {nonDealer.Score}";
        }
        
        if (gameStatusText != null)
        {
            GameManager.GamePhase phase = GameManager.Instance.GetCurrentPhase();
            if (phase == GameManager.GamePhase.GameOver)
            {
                GamePlayer winner = (dealer.Score >= ScoringManager.Instance.WinScore) ? dealer : nonDealer;
                gameStatusText.text = $"Game Over!\n{winner.Name} Wins!";
                if (restartButton != null)
                    restartButton.gameObject.SetActive(true);
            }
            else
            {
                gameStatusText.text = "Playing...";
            }
        }
    }
    
    private void UpdatePlayButtonState()
    {
        if (playCardButton == null) return;
        if (GameManager.Instance == null) return;
        
        GamePlayer currentPlayer = GameManager.Instance.GetCurrentPlayer();
        
        // Enable play button only if:
        // 1. Game is playing
        // 2. Current player is human
        // 3. Game is waiting for input
        // 4. A card is selected
        bool shouldEnable = GameManager.Instance.GetCurrentPhase() == GameManager.GamePhase.Playing &&
                           currentPlayer != null &&
                           currentPlayer.IsHuman() &&
                           GameManager.Instance.IsWaitingForHumanInput() &&
                           selectedCard != null;
        
        playCardButton.interactable = shouldEnable;
    }
    
    public void OnCardSelected(CardUI cardUI, PlayingCard card)
    {
        // Deselect previous card
        if (selectedCard != null && selectedCard != cardUI)
            selectedCard.SetSelected(false);
        
        // If clicking the same card, deselect it
        if (selectedCard == cardUI)
        {
            selectedCard = null;
        }
        else
        {
            selectedCard = cardUI;
        }
        
        UpdatePlayButtonState();
    }
    
    private void OnPlayCardClicked()
    {
        if (GameManager.Instance == null)
        {
            Debug.LogWarning("GameManager not available!");
            return;
        }
        
        if (selectedCard == null)
        {
            Debug.LogWarning("No card selected!");
            return;
        }
        
        GamePlayer currentPlayer = GameManager.Instance.GetCurrentPlayer();
        
        if (currentPlayer == null)
        {
            Debug.LogWarning("No current player!");
            return;
        }
        
        // CRITICAL: Verify it's actually a human player's turn
        if (!currentPlayer.IsHuman())
        {
            Debug.LogWarning("It's not a human player's turn!");
            return;
        }
        
        // CRITICAL: Verify game is waiting for human input
        if (!GameManager.Instance.IsWaitingForHumanInput())
        {
            Debug.LogWarning("Game is not waiting for human input!");
            return;
        }

        // Find the index of the selected card
        int cardIndex = -1;
        for (int i = 0; i < currentPlayer.Hand.Count; i++)
        {
            if (currentPlayer.Hand[i] == selectedCard.Card)
            {
                cardIndex = i;
                break;
            }
        }
        
        if (cardIndex == -1)
        {
            Debug.LogWarning("Selected card not found in hand!");
            return;
        }

        // Animate and play the card
        StartCoroutine(AnimateCardToTable(selectedCard));
        
        // Play the card
        GameManager.Instance.PlayCard(currentPlayer, cardIndex);
        
        // Clear selection
        selectedCard.SetSelected(false);
        selectedCard = null;
        
        // Update UI
        UpdatePlayButtonState();
    }
    
    private System.Collections.IEnumerator AnimateCardToTable(CardUI cardUI)
    {
        if (cardUI == null || cardUI.gameObject == null)
            yield break;
        
        RectTransform cardRect = cardUI.GetComponent<RectTransform>();
        if (cardRect == null)
            yield break;
        
        Vector3 startPos = cardRect.position;
        Vector3 endPos = tableCardsContainer.position;
        float duration = 0.3f;
        float elapsedTime = 0f;
        
        while (elapsedTime < duration)
        {
            if (cardUI == null || cardUI.gameObject == null)
                yield break;
            
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / duration;
            cardRect.position = Vector3.Lerp(startPos, endPos, t);
            yield return null;
        }
        
        if (cardUI != null && cardUI.gameObject != null)
            cardRect.position = endPos;
    }
    
    private System.Collections.IEnumerator AnimateCardAppearance(CardUI cardUI, float delay)
    {
        yield return new WaitForSeconds(delay);
        
        if (cardUI == null || cardUI.gameObject == null)
            yield break;
        
        RectTransform cardRect = cardUI.GetComponent<RectTransform>();
        if (cardRect == null)
            yield break;
        
        cardRect.localScale = Vector3.zero;
        float duration = 0.3f;
        float elapsedTime = 0f;
        
        while (elapsedTime < duration)
        {
            if (cardUI == null || cardUI.gameObject == null)
                yield break;
            
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / duration;
            cardRect.localScale = Vector3.one * t;
            yield return null;
        }
        
        if (cardUI != null && cardUI.gameObject != null)
            cardRect.localScale = Vector3.one;
    }
    
    private void OnRestartClicked()
    {
        selectedCard = null;
        GameManager.Instance.InitializeGame();
        playCardButton.interactable = false;
        if (restartButton != null)
            restartButton.gameObject.SetActive(false);
        RefreshUI();
    }
}