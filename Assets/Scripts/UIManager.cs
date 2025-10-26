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
            else
                cardImage.color = Color.white;
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
        Debug.Log("WaitAndRefresh: Calling RefreshUI");
        RefreshUI();
    }
    
    private void Update()
    {
        // Refresh UI every frame to show current state
        if (GameManager.Instance != null && GameManager.Instance.GetCurrentPlayer() != null)
        {
            UpdateGameInfo();
        }
    }
    
    public void RefreshUI()
    {
        UpdatePlayerHands();
        UpdateTableCards();
        UpdateGameInfo();
    }
    
    private void UpdatePlayerHands()
    {
        GamePlayer dealer = GameManager.Instance.GetDealer();
        GamePlayer nonDealer = GameManager.Instance.GetNonDealer();
        
        // Only update if hands changed
        if (dealerCardUIs.Count != dealer.Hand.Count)
            UpdateHandDisplay(dealer, dealerHandContainer, dealerCardUIs, false);
        else
            UpdateHandSelectability(dealer, dealerHandContainer, dealerCardUIs, false);
        
        bool nonDealerCanPlay = GameManager.Instance.GetCurrentPlayer() == nonDealer && nonDealer.HandSize() > 0;
        if (nonDealerCardUIs.Count != nonDealer.Hand.Count)
            UpdateHandDisplay(nonDealer, nonDealerHandContainer, nonDealerCardUIs, nonDealerCanPlay);
        else
            UpdateHandSelectability(nonDealer, nonDealerHandContainer, nonDealerCardUIs, nonDealerCanPlay);
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
        Debug.Log("UpdateHandDisplay: player=" + (player != null ? player.Name : "null") + ", hand count=" + (player != null ? player.Hand.Count : 0));
        
        if (player == null || container == null || cardPrefab == null)
        {
            Debug.LogError("UpdateHandDisplay: Missing required field - player:" + (player == null) + " container:" + (container == null) + " prefab:" + (cardPrefab == null));
            return;
        }
        
        // Clear old cards
        foreach (Transform child in container)
        {
            Destroy(child.gameObject);
        }
        cardUIs.Clear();

        Debug.Log("Creating " + player.Hand.Count + " cards");

        // Create new cards
        for (int i = 0; i < player.Hand.Count; i++)
        {
            GameObject cardObj = Instantiate(cardPrefab, container);
            Debug.Log("Created card object " + i);
            CardUI cardUI = cardObj.AddComponent<CardUI>();
            Debug.Log("Added CardUI component");
            cardUI.Initialize(player.Hand[i], selectable);
            cardUIs.Add(cardUI);
            
            Debug.Log("Card " + i + " parent: " + (cardObj.transform.parent != null ? cardObj.transform.parent.name : "NULL"));
            Debug.Log("Card " + i + " active: " + cardObj.activeInHierarchy);
            
            StartCoroutine(AnimateCardAppearance(cardUI, i * 0.05f));
        }
    }
    
    private void UpdateTableCards()
    {
        List<PlayingCard> tableCards = GameManager.Instance.GetTableCards();
        
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
        GamePlayer currentPlayer = GameManager.Instance.GetCurrentPlayer();
        GamePlayer dealer = GameManager.Instance.GetDealer();
        GamePlayer nonDealer = GameManager.Instance.GetNonDealer();
        GameDeck deck = GameManager.Instance.GetDeck();
        
        if (currentPlayerText != null)
            currentPlayerText.text = $"Current Turn: {currentPlayer.Name}";
        
        if (deckCountText != null)
            deckCountText.text = $"Cards in Deck: {deck.CardsRemaining()}";
        
        if (dealerScoreText != null)
            dealerScoreText.text = $"Dealer: {dealer.Name}\nScore: {dealer.Score}";
        
        if (nonDealerScoreText != null)
            nonDealerScoreText.text = $"Non-Dealer: {nonDealer.Name}\nScore: {nonDealer.Score}";
        
        if (gameStatusText != null)
        {
            GameManager.GamePhase phase = GameManager.Instance.GetCurrentPhase();
            if (phase == GameManager.GamePhase.GameOver)
            {
                GamePlayer winner = (dealer.Score >= 21) ? dealer : nonDealer;
                gameStatusText.text = $"Game Over!\n{winner.Name} Wins!";
                playCardButton.interactable = false;
                if (restartButton != null)
                    restartButton.gameObject.SetActive(true);
            }
            else
            {
                gameStatusText.text = "Playing...";
            }
        }
    }
    
    public void OnCardSelected(CardUI cardUI, PlayingCard card)
    {
        if (selectedCard != null && selectedCard != cardUI)
            selectedCard.SetSelected(false);
        
        selectedCard = cardUI;
    }
    
    private void OnPlayCardClicked()
    {
        if (selectedCard == null)
        {
            Debug.LogWarning("No card selected!");
            return;
        }
        
        GamePlayer currentPlayer = GameManager.Instance.GetCurrentPlayer();

        int cardIndex = Enumerable.Range(0, currentPlayer.Hand.Count)
            .FirstOrDefault(i => currentPlayer.Hand[i] == selectedCard.Card);

        if (cardIndex != 0 || (cardIndex == 0 && currentPlayer.Hand[0] == selectedCard.Card))
        {
            StartCoroutine(AnimateCardToTable(selectedCard));
            GameManager.Instance.PlayCard(currentPlayer, cardIndex);
            selectedCard.SetSelected(false);
            selectedCard = null;
        }
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
        float duration = 0.5f;
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
        
        RefreshUI();
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
        GameManager.Instance.InitializeGame();
        playCardButton.interactable = true;
        if (restartButton != null)
            restartButton.gameObject.SetActive(false);
        RefreshUI();
    }
}