using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

[RequireComponent(typeof(Button))]
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

        // Ensure button component is available immediately
        if (button == null)
            button = GetComponent<Button>();

        UpdateDisplay();
    }

    private void UpdateDisplay()
    {
        // Ensure rankSuitText is initialized before using it
        if (rankSuitText == null)
            rankSuitText = GetComponentInChildren<TextMeshProUGUI>();

        if (rankSuitText != null && card != null)
        {
            string rankDisplay = GetRankDisplay(card.rank);
            string suitEmoji = GetSuitEmoji(card.suit);
            rankSuitText.text = $"{rankDisplay}{suitEmoji}";

            // Set color based on suit - red for Hearts/Diamonds, black for Clubs/Spades
            rankSuitText.color = GetSuitColor(card.suit);
        }

        // Ensure button exists and set interactable state
        if (button == null)
            button = GetComponent<Button>();

        if (button != null)
            button.interactable = isSelectable;

        UpdateVisuals();
    }

    private string GetRankDisplay(PlayingCard.Rank rank)
    {
        return rank switch
        {
            PlayingCard.Rank.Ace => "A",
            PlayingCard.Rank.Two => "2",
            PlayingCard.Rank.Three => "3",
            PlayingCard.Rank.Four => "4",
            PlayingCard.Rank.Five => "5",
            PlayingCard.Rank.Six => "6",
            PlayingCard.Rank.Seven => "7",
            PlayingCard.Rank.Eight => "8",
            PlayingCard.Rank.Nine => "9",
            PlayingCard.Rank.Ten => "10",
            PlayingCard.Rank.Jack => "J",
            PlayingCard.Rank.Queen => "Q",
            PlayingCard.Rank.King => "K",
            _ => rank.ToString()
        };
    }

    private string GetSuitEmoji(PlayingCard.Suit suit)
    {
        return suit switch
        {
            PlayingCard.Suit.Hearts => "♥",
            PlayingCard.Suit.Diamonds => "♦",
            PlayingCard.Suit.Clubs => "♣",
            PlayingCard.Suit.Spades => "♠",
            _ => suit.ToString()
        };
    }

    private Color GetSuitColor(PlayingCard.Suit suit)
    {
        return suit switch
        {
            PlayingCard.Suit.Hearts => new Color(0.9f, 0.1f, 0.1f),      // Red
            PlayingCard.Suit.Diamonds => new Color(0.9f, 0.1f, 0.1f),    // Red
            PlayingCard.Suit.Clubs => Color.black,                        // Black
            PlayingCard.Suit.Spades => Color.black,                       // Black
            _ => Color.black
        };
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
  private static WaitForSeconds _waitForSeconds0_1 = new(0.1f);

  public static UIManager Instance { get; private set; }
    
    [SerializeField] private Transform dealerHandContainer;
    [SerializeField] private Transform nonDealerHandContainer;
    [SerializeField] private Transform tableCardsContainer;
    [SerializeField] private Transform buildsContainer;
    [SerializeField] private TextMeshProUGUI currentPlayerText;
    [SerializeField] private TextMeshProUGUI deckCountText;
    [SerializeField] private TextMeshProUGUI gameStatusText;
    [SerializeField] private TextMeshProUGUI dealerScoreText;
    [SerializeField] private TextMeshProUGUI nonDealerScoreText;
    [SerializeField] private GameObject cardPrefab;
    [SerializeField] private GameObject buildPrefab;
    [SerializeField] private Button playCardButton;
    [SerializeField] private Button restartButton;

    private CardUI selectedCard = null;
    private List<CardUI> dealerCardUIs = new();
    private List<CardUI> nonDealerCardUIs = new();
    private List<CardUI> tableCardUIs = new();
    private List<GameObject> buildUIs = new();
    
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
        yield return _waitForSeconds0_1;
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
        UpdateBuilds();
        UpdateGameInfo();
    }
    
    private void UpdatePlayerHands()
    {
        GamePlayer dealer = GameManager.Instance.GetDealer();
        GamePlayer nonDealer = GameManager.Instance.GetNonDealer();

        // All cards are now selectable at all times
        if (dealerCardUIs.Count != dealer.Hand.Count)
            UpdateHandDisplay(dealer, dealerHandContainer, dealerCardUIs, true);
        else
            UpdateHandSelectability(dealer, dealerHandContainer, dealerCardUIs, true);

        if (nonDealerCardUIs.Count != nonDealer.Hand.Count)
            UpdateHandDisplay(nonDealer, nonDealerHandContainer, nonDealerCardUIs, true);
        else
            UpdateHandSelectability(nonDealer, nonDealerHandContainer, nonDealerCardUIs, true);
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

    private void UpdateBuilds()
    {
        if (buildsContainer == null)
        {
            Debug.LogWarning("BuildsContainer is not assigned in UIManager");
            return;
        }

        List<Build> activeBuilds = GameManager.Instance.GetActiveBuilds();

        // Always recreate build UI to show current state
        foreach (var buildUI in buildUIs)
        {
            if (buildUI != null)
                Destroy(buildUI);
        }
        buildUIs.Clear();

        // Create UI for each build
        for (int i = 0; i < activeBuilds.Count; i++)
        {
            Build build = activeBuilds[i];
            GameObject buildObj = CreateBuildUI(build);
            buildObj.transform.SetParent(buildsContainer, false);
            buildUIs.Add(buildObj);
        }
    }

    private GameObject CreateBuildUI(Build build)
    {
        // Create a container for this build
        GameObject buildContainer = new($"Build_{build.DeclaredValue}");
        RectTransform buildRect = buildContainer.AddComponent<RectTransform>();
        buildRect.sizeDelta = new Vector2(200, 150);

        // Add a background panel
        Image bgImage = buildContainer.AddComponent<Image>();
        bgImage.color = new Color(0.2f, 0.2f, 0.3f, 0.8f);

        // Add a border based on owner
        Outline outline = buildContainer.AddComponent<Outline>();
        outline.effectColor = build.Owner == GameManager.Instance.GetDealer() ? Color.red : Color.blue;
        outline.effectDistance = new Vector2(2, -2);

        // Create header text showing owner and value
        GameObject headerObj = new("Header");
        headerObj.transform.SetParent(buildContainer.transform, false);
        RectTransform headerRect = headerObj.AddComponent<RectTransform>();
        headerRect.anchorMin = new Vector2(0, 0.8f);
        headerRect.anchorMax = new Vector2(1, 1);
        headerRect.offsetMin = Vector2.zero;
        headerRect.offsetMax = Vector2.zero;

        TextMeshProUGUI headerText = headerObj.AddComponent<TextMeshProUGUI>();
        headerText.text = $"{build.Owner.Name}'s Build\nValue: {build.DeclaredValue}";
        headerText.fontSize = 14;
        headerText.fontStyle = FontStyles.Bold;
        headerText.color = Color.white;
        headerText.alignment = TextAlignmentOptions.Center;

        // Create cards container
        GameObject cardsObj = new("Cards");
        cardsObj.transform.SetParent(buildContainer.transform, false);
        RectTransform cardsRect = cardsObj.AddComponent<RectTransform>();
        cardsRect.anchorMin = new Vector2(0, 0);
        cardsRect.anchorMax = new Vector2(1, 0.8f);
        cardsRect.offsetMin = new Vector2(5, 5);
        cardsRect.offsetMax = new Vector2(-5, -5);

        // Add horizontal layout for cards
        UnityEngine.UI.HorizontalLayoutGroup layout = cardsObj.AddComponent<UnityEngine.UI.HorizontalLayoutGroup>();
        layout.spacing = 5;
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childControlWidth = false;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;

        // Add each card in the build
        foreach (PlayingCard card in build.Cards)
        {
            GameObject miniCard = Instantiate(cardPrefab, cardsObj.transform);
            CardUI cardUI = miniCard.AddComponent<CardUI>();
            cardUI.Initialize(card, false);

            // Make cards smaller in build display
            RectTransform cardRect = miniCard.GetComponent<RectTransform>();
            if (cardRect != null)
            {
                cardRect.sizeDelta = new Vector2(40, 60);
            }
        }

        return buildContainer;
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
    
    /// <summary>
    /// Highlights the AI's selected hand card before they play it.
    /// </summary>
    public System.Collections.IEnumerator HighlightAICard(GamePlayer aiPlayer, int cardIndex, float delay = 0.8f)
    {
        // Determine which hand to use
        var cardUIs = aiPlayer == GameManager.Instance.GetDealer() ? dealerCardUIs : nonDealerCardUIs;

        // Validate card index
        if (cardIndex < 0 || cardIndex >= cardUIs.Count)
        {
            Debug.LogWarning($"Invalid card index {cardIndex} for AI player {aiPlayer.Name}");
            yield break;
        }

        // Highlight the selected card
        var selectedCardUI = cardUIs[cardIndex];
        selectedCardUI.SetSelected(true);

        // Wait for the delay so player can see the selection
        yield return new WaitForSeconds(delay);

        // Deselect the card
        if (selectedCardUI != null && selectedCardUI.gameObject != null)
            selectedCardUI.SetSelected(false);
    }

    /// <summary>
    /// Highlights table cards that will be captured by the AI, then waits for a delay before continuing.
    /// </summary>
    public System.Collections.IEnumerator HighlightTableCardsForCapture(List<PlayingCard> cardsToHighlight, float delay = 0.5f)
    {
        // Find and highlight the matching table card UIs
        var highlightedCardUIs = new List<CardUI>();
        foreach (var cardToHighlight in cardsToHighlight)
        {
            var matchingCardUI = tableCardUIs.FirstOrDefault(ui => ui.Card == cardToHighlight);
            if (matchingCardUI != null)
            {
                matchingCardUI.SetSelected(true);
                highlightedCardUIs.Add(matchingCardUI);
            }
        }

        // Wait for the delay so player can see the selection
        yield return new WaitForSeconds(delay);

        // Deselect the cards (though they'll likely be removed from table after this)
        foreach (var cardUI in highlightedCardUIs)
        {
            if (cardUI != null && cardUI.gameObject != null)
                cardUI.SetSelected(false);
        }
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