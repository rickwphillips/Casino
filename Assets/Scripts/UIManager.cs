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
    private bool isFaceDown = false;
    private bool isSuggested = false;
    private Image backImage;

    // Procedural card back: navy field with a diagonal lattice, generated
    // once and shared (no art assets in this project).
    private static Sprite cardBackSprite;
    public static Sprite CardBackSprite
    {
        get
        {
            if (cardBackSprite != null) return cardBackSprite;
            var tex = new Texture2D(64, 96, TextureFormat.RGBA32, false);
            var navy = new Color(0.10f, 0.16f, 0.38f);
            var light = new Color(0.22f, 0.32f, 0.60f);
            for (int y = 0; y < 96; y++)
                for (int x = 0; x < 64; x++)
                {
                    bool stripe = ((x + y) % 12) < 3 || ((x - y + 960) % 12) < 3;
                    tex.SetPixel(x, y, stripe ? light : navy);
                }
            tex.Apply();
            cardBackSprite = Sprite.Create(tex, new Rect(0, 0, 64, 96), new Vector2(0.5f, 0.5f));
            return cardBackSprite;
        }
    }
    
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

        if (button == null)
            button = GetComponent<Button>();

        UpdateDisplay();
    }

    private void UpdateDisplay()
    {
        // Ensure rankSuitText is initialized before using it
        if (rankSuitText == null)
            rankSuitText = GetComponentInChildren<TextMeshProUGUI>();

        // Card back: white border from the base image, patterned inset child
        if (isFaceDown)
        {
            if (backImage == null)
            {
                GameObject back = new("CardBack");
                back.transform.SetParent(transform, false);
                var r = back.AddComponent<RectTransform>();
                r.anchorMin = Vector2.zero;
                r.anchorMax = Vector2.one;
                r.offsetMin = new Vector2(4, 4);
                r.offsetMax = new Vector2(-4, -4);
                backImage = back.AddComponent<Image>();
                backImage.sprite = CardBackSprite;
                backImage.raycastTarget = false;
            }
            backImage.gameObject.SetActive(true);
        }
        else if (backImage != null)
        {
            backImage.gameObject.SetActive(false);
        }

        if (rankSuitText != null && card != null)
        {
            if (isFaceDown)
            {
                rankSuitText.text = "";
            }
            else
            {
                string rankDisplay = GetRankDisplay(card.rank);
                string suitEmoji = GetSuitEmoji(card.suit);
                rankSuitText.text = $"{rankDisplay}{suitEmoji}";

                // Set color based on suit - red for Hearts/Diamonds, black for Clubs/Spades
                rankSuitText.color = GetSuitColor(card.suit);
            }
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
        if (cardImage == null) return;

        if (isFaceDown)
            cardImage.color = Color.white;                   // white border, back child on top
        else if (isSelected)
            cardImage.color = new(0.7f, 0.9f, 1f);          // selected: blue
        else if (isSuggested)
            cardImage.color = new(0.7f, 1f, 0.7f);          // suggested: green
        else
            cardImage.color = Color.white;
    }

    public void SetFaceDown(bool faceDown)
    {
        isFaceDown = faceDown;
        UpdateDisplay();
    }

    public void SetSuggested(bool suggested)
    {
        isSuggested = suggested;
        UpdateVisuals();
    }
    
    private void OnCardClicked()
    {
        if (!isSelectable) return;
        SetSelected(!isSelected);
        UIManager.Instance.OnCardSelected(this, card);
    }

    public bool IsSelected => isSelected;

    // Selection scale is applied here so programmatic deselection shrinks
    // the card too - previously only clicks animated, leaving stale
    // enlarged cards that looked multi-selected.
    public void SetSelected(bool selected)
    {
        isSelected = selected;
        UpdateVisuals();
        transform.localScale = Vector3.one * (selected ? 1.15f : 1f);
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
    [SerializeField] private GameObject gameOverPanel;

    private CardUI selectedCard = null;
    private List<CardUI> dealerCardUIs = new();
    private List<CardUI> nonDealerCardUIs = new();
    private List<CardUI> tableCardUIs = new();
    private List<GameObject> buildUIs = new();

    // Runtime-created 1-player UI (build action, suggestions, scoring panel)
    private readonly List<CardUI> buildSelection = new();
    private Button buildButton;
    private Button suggestButton;
    private Button trailButton;
    private Button sweepButton;
    private TextMeshProUGUI buildButtonLabel;
    private TextMeshProUGUI suggestButtonLabel;
    private TextMeshProUGUI trailButtonLabel;
    private TextMeshProUGUI sweepButtonLabel;
    private TextMeshProUGUI hintText;
    private TextMeshProUGUI scoreHeaderText;
    private TextMeshProUGUI humanStatsText;
    private TextMeshProUGUI aiStatsText;
    private Transform canvasTransform;
    private bool lastHumanTurn;

    // Captured-pile viewer
    private Button humanPileButton;
    private Button aiPileButton;
    private TextMeshProUGUI humanPileLabel;
    private TextMeshProUGUI aiPileLabel;
    private GameObject capturedPanel;
    private Transform capturedGrid;
    private TextMeshProUGUI capturedTitle;
    private bool pileShowsHuman;
    private int pileShownCount = -1;

    // Draw pile
    private GameObject drawPile;
    private TextMeshProUGUI drawPileLabel;
    private int lastDeckCount = -1;

    // Build selected for raising (single-group builds are malleable)
    private Build selectedBuild;
    private GameObject selectedBuildRoot;
    
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

        // Setup GameOverPanel and find restart button before hiding it
        if (gameOverPanel != null)
        {
            // Ensure panel is active first so we can find components
            gameOverPanel.SetActive(true);

            // Find restart button within GameOverPanel if not explicitly assigned
            if (restartButton == null)
            {
                restartButton = gameOverPanel.GetComponentInChildren<Button>();
                Debug.Log("Auto-detected restart button: " + (restartButton != null ? restartButton.name : "null"));
            }

            // Hide the panel after finding the button
            gameOverPanel.SetActive(false);
            Debug.Log("GameOverPanel hidden at start");
        }

        // Add listener to restart button after it's been found
        if (restartButton != null)
        {
            // Clear any existing listeners first to avoid duplicates
            restartButton.onClick.RemoveAllListeners();
            restartButton.onClick.AddListener(OnRestartClicked);
            Debug.Log($"Restart button listener added to: {restartButton.name}, interactable: {restartButton.interactable}");

            // Test log to verify button component
            Debug.Log($"Restart button component details - GameObject: {restartButton.gameObject.name}, Active: {restartButton.gameObject.activeInHierarchy}");
        }
        else
        {
            Debug.LogWarning("Restart button is null - cannot add listener!");
        }

        CreateRuntimeUI();
        StartCoroutine(WaitAndRefresh());
    }

    // ---------------------------------------------------------------
    // Runtime-created UI for 1-player mode: Build + Suggest buttons,
    // hint line, and the scoring panel. Created in code so no scene
    // rewiring is required.
    // ---------------------------------------------------------------
    private void CreateRuntimeUI()
    {
        Canvas canvas = tableCardsContainer != null
            ? tableCardsContainer.GetComponentInParent<Canvas>()
            : FindFirstObjectByType<Canvas>();

        if (canvas == null)
        {
            Debug.LogError("UIManager: no Canvas found; runtime UI not created");
            return;
        }
        canvasTransform = canvas.transform;

        // Full-screen felt backdrop, drawn behind everything else
        GameObject felt = new("TableFelt");
        felt.transform.SetParent(canvasTransform, false);
        felt.transform.SetAsFirstSibling();
        var feltRect = felt.AddComponent<RectTransform>();
        feltRect.anchorMin = Vector2.zero;
        feltRect.anchorMax = Vector2.one;
        feltRect.offsetMin = Vector2.zero;
        feltRect.offsetMax = Vector2.zero;
        var feltImage = felt.AddComponent<Image>();
        feltImage.color = new Color(0.08f, 0.29f, 0.15f);
        feltImage.raycastTarget = false;

        // The runtime score panel replaces the two floating score texts
        if (dealerScoreText != null) dealerScoreText.gameObject.SetActive(false);
        if (nonDealerScoreText != null) nonDealerScoreText.gameObject.SetActive(false);

        sweepButton = CreateActionButton("SweepButton", "Sweep",
            new Vector2(-16, 16), out sweepButtonLabel);
        sweepButton.GetComponent<Image>().color = new Color(0.72f, 0.6f, 0.25f, 0.95f);
        sweepButton.onClick.AddListener(OnSweepClicked);

        trailButton = CreateActionButton("TrailButton", "Trail",
            new Vector2(-16, 70), out trailButtonLabel);
        trailButton.onClick.AddListener(OnTrailClicked);

        buildButton = CreateActionButton("BuildButton", "Build",
            new Vector2(-16, 124), out buildButtonLabel);
        buildButton.onClick.AddListener(OnBuildClicked);

        suggestButton = CreateActionButton("SuggestButton", "Suggest",
            new Vector2(-16, 178), out suggestButtonLabel);
        suggestButton.onClick.AddListener(OnSuggestClicked);

        hintText = CreateText("HintText", canvasTransform);
        var hintRect = hintText.rectTransform;
        hintRect.anchorMin = new Vector2(0.5f, 0);
        hintRect.anchorMax = new Vector2(0.5f, 0);
        hintRect.pivot = new Vector2(0.5f, 0);
        hintRect.anchoredPosition = new Vector2(0, 145);
        hintRect.sizeDelta = new Vector2(470, 54);
        hintText.fontSize = 15;
        hintText.alignment = TextAlignmentOptions.Center;
        hintText.color = new Color(1f, 0.95f, 0.6f);
        hintText.text = "";

        CreateScorePanel();
        CreatePileViewer();
        CreateDrawPile();

        // The draw pile replaces the deck-count text
        if (deckCountText != null)
            deckCountText.gameObject.SetActive(false);

        // The layout pass must never take the game down with it, and the
        // report must be written even when it fails.
        try
        {
            EnforceLayout();
        }
        catch (System.Exception e)
        {
            Debug.LogError($"EnforceLayout failed: {e}");
        }
        finally
        {
            WriteLayoutReport(canvas);
            StartCoroutine(SettledReport(canvas));
        }

        UpdateActionButtons();
    }

    // Second snapshot after the canvas has actually rebuilt: the first-frame
    // report can show stale pre-scaler values.
    private System.Collections.IEnumerator SettledReport(Canvas canvas)
    {
        yield return new WaitForSeconds(1f);
        WriteLayoutReport(canvas);
    }

    // ---------------------------------------------------------------
    // The scene's hand-placed layout is unusable (off-screen Play
    // button, labels outside their panels, stray stretched boxes).
    // Take ownership: re-anchor every element we use to a clean plan
    // on the 800x600 reference canvas, and hide everything else.
    // ---------------------------------------------------------------
    private void EnforceLayout()
    {
        // The scene canvas is not a fullscreen overlay, which put every
        // anchored element (including the Play button) at positions unrelated
        // to the window. Force overlay mode + a stable scaler first.
        Canvas canvas = canvasTransform.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = canvas.GetComponent<CanvasScaler>();
        if (scaler == null) scaler = canvas.gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(800, 600);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 1f; // match height: vertical layout stays put

        // Reparent the four card containers directly under the canvas
        var keep = new HashSet<GameObject>();

        keep.Add(ReAnchor(dealerHandContainer, new Vector2(0.5f, 1), new Vector2(0, -12), new Vector2(420, 100)));
        keep.Add(ReAnchor(nonDealerHandContainer, new Vector2(0.5f, 0), new Vector2(0, 14), new Vector2(420, 120)));
        // Builds render inline in the table row; no separate builds area
        keep.Add(ReAnchor(tableCardsContainer, new Vector2(0.5f, 0.5f), new Vector2(20, 10), new Vector2(560, 130)));

        EnsureRowLayout(dealerHandContainer);
        EnsureRowLayout(nonDealerHandContainer);
        EnsureRowLayout(tableCardsContainer);

        // The scene's generic Play button is replaced by the explicit
        // Sweep / Trail / Build actions - leave it out of keep so it hides.

        // Status texts: bottom-left stack (the only corner nothing else uses)
        keep.Add(ReAnchorText(currentPlayerText, new Vector2(14, 84), 14));
        keep.Add(ReAnchorText(deckCountText, new Vector2(14, 54), 13));
        keep.Add(ReAnchorText(gameStatusText, new Vector2(14, 24), 13));

        // Game over panel: centered card
        if (gameOverPanel != null)
        {
            keep.Add(ReAnchor(gameOverPanel.transform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(420, 230)));
            var img = gameOverPanel.GetComponent<Image>();
            if (img != null) img.color = new Color(0.05f, 0.08f, 0.12f, 0.96f);
        }

        // Our runtime objects stay too
        foreach (Transform child in canvasTransform)
        {
            if (child.name == "TableFelt" || child.name == "ScorePanel" ||
                child.name == "BuildButton" || child.name == "SuggestButton" ||
                child.name == "TrailButton" || child.name == "SweepButton" ||
                child.name == "HintText" || child.name == "HumanPile" ||
                child.name == "AIPile" || child.name == "CapturedPanel" ||
                child.name == "DrawPile" || child.name == "ScoreSummary" ||
                child.name == "MoveBanner")
            {
                keep.Add(child.gameObject);
            }
        }

        // Everything else on this canvas is scene debris: hide it
        foreach (Transform child in canvasTransform)
        {
            if (!keep.Contains(child.gameObject))
                child.gameObject.SetActive(false);
        }

        WriteLayoutReport(canvas);
    }

    // Ground truth for layout debugging: what actually rendered where.
    // Written to <project>/layout-report.txt on every startup.
    private void WriteLayoutReport(Canvas canvas)
    {
        try
        {
            var sb = new System.Text.StringBuilder();
            var canvasRect = canvasTransform as RectTransform;
            var scaler = canvas.GetComponent<CanvasScaler>();
            if (scaler != null)
                sb.AppendLine($"scaler mode={scaler.uiScaleMode} ref={scaler.referenceResolution} matchWoH={scaler.matchWidthOrHeight}");
            sb.AppendLine($"screen={Screen.width}x{Screen.height}  canvas mode={canvas.renderMode}  scaleFactor={canvas.scaleFactor:F3}");
            sb.AppendLine($"canvas pixelRect={canvas.pixelRect}  canvas units={canvasRect.rect.width:F0}x{canvasRect.rect.height:F0}");
            var allCanvases = FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var cv in allCanvases)
                sb.AppendLine($"canvas '{cv.gameObject.name}' active={cv.gameObject.activeInHierarchy} mode={cv.renderMode}");
            sb.AppendLine("--- direct children of main canvas ---");
            foreach (Transform child in canvasTransform)
            {
                var r = child as RectTransform;
                sb.AppendLine($"{child.gameObject.name,-24} active={child.gameObject.activeSelf,-5} " +
                    (r != null ? $"anchors=({r.anchorMin.x:F1},{r.anchorMin.y:F1})-({r.anchorMax.x:F1},{r.anchorMax.y:F1}) pos={r.anchoredPosition} size={r.sizeDelta}" : ""));
            }
            System.IO.File.WriteAllText(
                System.IO.Path.Combine(Application.dataPath, "..", "layout-report.txt"),
                sb.ToString());
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"layout report failed: {e.Message}");
        }
    }

    private GameObject ReAnchor(Transform t, Vector2 anchor, Vector2 pos, Vector2 size)
    {
        if (t == null) return null;
        t.SetParent(canvasTransform, false);
        var rect = t as RectTransform;
        if (rect != null)
        {
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = anchor;
            rect.anchoredPosition = pos;
            rect.sizeDelta = size;
        }
        return t.gameObject;
    }

    private GameObject ReAnchorText(TextMeshProUGUI text, Vector2 pos, float fontSize)
    {
        if (text == null) return null;
        var go = ReAnchor(text.transform, new Vector2(0, 0), pos, new Vector2(170, 26));
        text.fontSize = fontSize;
        text.alignment = TextAlignmentOptions.BottomLeft;
        text.color = new Color(1f, 1f, 1f, 0.85f);
        return go;
    }

    private void EnsureRowLayout(Transform container)
    {
        if (container == null) return;

        // A conflicting layout component blocks AddComponent (it returns null,
        // which crashed the whole layout pass). Remove it first.
        var existing = container.GetComponent<LayoutGroup>();
        if (existing != null && !(existing is HorizontalLayoutGroup))
            DestroyImmediate(existing);

        var layout = container.GetComponent<HorizontalLayoutGroup>();
        if (layout == null)
            layout = container.gameObject.AddComponent<HorizontalLayoutGroup>();
        if (layout == null) return;
        layout.spacing = 8;
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childControlWidth = false;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;
    }


    private Button CreateActionButton(string name, string label, Vector2 anchoredPos, out TextMeshProUGUI labelText)
    {
        GameObject go = new(name);
        go.transform.SetParent(canvasTransform, false);
        var rect = go.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(1, 0);
        rect.anchorMax = new Vector2(1, 0);
        rect.pivot = new Vector2(1, 0);
        rect.anchoredPosition = anchoredPos;
        rect.sizeDelta = new Vector2(160, 48);

        var image = go.AddComponent<Image>();
        image.color = new Color(0.18f, 0.42f, 0.3f, 0.95f);

        var button = go.AddComponent<Button>();

        labelText = CreateText("Label", go.transform);
        var lr = labelText.rectTransform;
        lr.anchorMin = Vector2.zero;
        lr.anchorMax = Vector2.one;
        lr.offsetMin = Vector2.zero;
        lr.offsetMax = Vector2.zero;
        labelText.text = label;
        labelText.fontSize = 20;
        labelText.alignment = TextAlignmentOptions.Center;
        labelText.color = Color.white;

        return button;
    }

    private TextMeshProUGUI CreateText(string name, Transform parent)
    {
        GameObject go = new(name);
        go.transform.SetParent(parent, false);
        go.AddComponent<RectTransform>();
        return go.AddComponent<TextMeshProUGUI>();
    }

    // Pile buttons under the score panel + a toggleable panel showing the
    // actual captured cards.
    private void CreatePileViewer()
    {
        humanPileButton = CreatePileButton("HumanPile", new Vector2(-12, -258), out humanPileLabel);
        humanPileButton.onClick.AddListener(() => TogglePile(true));

        aiPileButton = CreatePileButton("AIPile", new Vector2(-12, -300), out aiPileLabel);
        aiPileButton.onClick.AddListener(() => TogglePile(false));

        // Docked to the left edge - never covers the table or the controls
        capturedPanel = new GameObject("CapturedPanel");
        capturedPanel.transform.SetParent(canvasTransform, false);
        var rect = capturedPanel.AddComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0, 0.5f);
        rect.anchoredPosition = new Vector2(10, 20);
        rect.sizeDelta = new Vector2(180, 500);
        var bg = capturedPanel.AddComponent<Image>();
        bg.color = new Color(0.04f, 0.06f, 0.09f, 0.96f);

        capturedTitle = CreateText("Title", capturedPanel.transform);
        var tr = capturedTitle.rectTransform;
        tr.anchorMin = new Vector2(0, 0.88f);
        tr.anchorMax = Vector2.one;
        tr.offsetMin = new Vector2(10, 0);
        tr.offsetMax = new Vector2(-10, -4);
        capturedTitle.fontSize = 16;
        capturedTitle.fontStyle = FontStyles.Bold;
        capturedTitle.alignment = TextAlignmentOptions.Center;
        capturedTitle.color = Color.white;

        GameObject grid = new("Grid");
        grid.transform.SetParent(capturedPanel.transform, false);
        var gr = grid.AddComponent<RectTransform>();
        gr.anchorMin = Vector2.zero;
        gr.anchorMax = new Vector2(1, 0.88f);
        gr.offsetMin = new Vector2(10, 10);
        gr.offsetMax = new Vector2(-10, 0);
        var layout = grid.AddComponent<GridLayoutGroup>();
        layout.cellSize = new Vector2(40, 56);
        layout.spacing = new Vector2(4, 4);
        layout.childAlignment = TextAnchor.UpperLeft;
        capturedGrid = grid.transform;

        capturedPanel.SetActive(false);
    }

    // Visible draw pile bottom-left: a stack of card backs that thins out
    // as the deck empties.
    private void CreateDrawPile()
    {
        drawPile = new GameObject("DrawPile");
        drawPile.transform.SetParent(canvasTransform, false);
        var rect = drawPile.AddComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0, 0);
        rect.anchoredPosition = new Vector2(30, 130);
        rect.sizeDelta = new Vector2(96, 132);

        drawPileLabel = CreateText("Count", drawPile.transform);
        var lr = drawPileLabel.rectTransform;
        lr.anchorMin = new Vector2(0, 0);
        lr.anchorMax = new Vector2(1, 0);
        lr.pivot = new Vector2(0.5f, 1);
        lr.anchoredPosition = new Vector2(0, -4);
        lr.sizeDelta = new Vector2(96, 22);
        drawPileLabel.fontSize = 13;
        drawPileLabel.alignment = TextAlignmentOptions.Center;
        drawPileLabel.color = new Color(1f, 1f, 1f, 0.85f);
    }

    private void UpdateDrawPile(int remaining)
    {
        if (drawPile == null || remaining == lastDeckCount) return;
        lastDeckCount = remaining;

        // Clear old layers (keep the label)
        for (int i = drawPile.transform.childCount - 1; i >= 0; i--)
        {
            var child = drawPile.transform.GetChild(i);
            if (child.name != "Count") Destroy(child.gameObject);
        }

        // One visual layer per ~8 cards, up to 7 layers
        int layers = remaining == 0 ? 0 : Mathf.Clamp(1 + (remaining - 1) / 8, 1, 7);
        for (int i = 0; i < layers; i++)
        {
            GameObject layer = new($"Layer{i}");
            layer.transform.SetParent(drawPile.transform, false);
            layer.transform.SetSiblingIndex(0); // under the label
            var r = layer.AddComponent<RectTransform>();
            r.anchorMin = r.anchorMax = r.pivot = new Vector2(0, 1);
            r.anchoredPosition = new Vector2(i * 2f, -(layers - i) * 2f);
            r.sizeDelta = new Vector2(76, 108);
            var img = layer.AddComponent<Image>();
            img.sprite = CardUI.CardBackSprite;
            img.raycastTarget = false;
        }

        drawPileLabel.text = remaining > 0 ? $"Deck: {remaining}" : "Deck empty";
    }

    // Deal animation: face-down ghosts fly from the draw pile to each hand
    // (and the table on the opening deal), staggered like real dealing.
    public void AnimateDeal(int toNonDealer, int toDealer, int toTable)
    {
        StartCoroutine(DealSequence(toNonDealer, toDealer, toTable));
    }

    private System.Collections.IEnumerator DealSequence(int toNonDealer, int toDealer, int toTable)
    {
        if (drawPile == null) yield break;
        Vector3 from = drawPile.transform.position;
        var wait = new WaitForSeconds(0.07f);

        for (int i = 0; i < Mathf.Max(toNonDealer, toDealer); i++)
        {
            if (i < toNonDealer && nonDealerHandContainer != null)
            { SpawnGhostCore(null, from, nonDealerHandContainer.position, true); yield return wait; }
            if (i < toDealer && dealerHandContainer != null)
            { SpawnGhostCore(null, from, dealerHandContainer.position, true); yield return wait; }
        }
        for (int i = 0; i < toTable; i++)
        {
            if (tableCardsContainer != null)
            { SpawnGhostCore(null, from, tableCardsContainer.position, true); yield return wait; }
        }
    }

    private Button CreatePileButton(string name, Vector2 pos, out TextMeshProUGUI label)
    {
        GameObject go = new(name);
        go.transform.SetParent(canvasTransform, false);
        var rect = go.AddComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(1, 1);
        rect.anchoredPosition = pos;
        rect.sizeDelta = new Vector2(250, 36);
        var img = go.AddComponent<Image>();
        img.color = new Color(0.12f, 0.16f, 0.22f, 0.92f);
        var btn = go.AddComponent<Button>();

        label = CreateText("Label", go.transform);
        var lr = label.rectTransform;
        lr.anchorMin = Vector2.zero;
        lr.anchorMax = Vector2.one;
        lr.offsetMin = new Vector2(8, 0);
        lr.offsetMax = new Vector2(-8, 0);
        label.fontSize = 14;
        label.alignment = TextAlignmentOptions.Left;
        label.color = Color.white;
        return btn;
    }

    private void TogglePile(bool human)
    {
        if (capturedPanel.activeSelf && pileShowsHuman == human)
        {
            capturedPanel.SetActive(false);
            return;
        }
        pileShowsHuman = human;
        pileShownCount = -1;
        capturedPanel.SetActive(true);
        RebuildPilePanel();
    }

    private void RebuildPilePanel()
    {
        var gm = GameManager.Instance;
        if (gm == null) return;
        GamePlayer dealer = gm.GetDealer();
        GamePlayer nonDealer = gm.GetNonDealer();
        GamePlayer human = dealer.IsHuman() ? dealer : nonDealer;
        GamePlayer target = pileShowsHuman ? human : (human == dealer ? nonDealer : dealer);

        if (target.CapturedCards.Count == pileShownCount) return;
        pileShownCount = target.CapturedCards.Count;

        capturedTitle.text = $"{(pileShowsHuman ? "Your" : "AI's")} pile ({pileShownCount})";
        foreach (Transform child in capturedGrid)
            Destroy(child.gameObject);
        foreach (var card in target.CapturedCards)
        {
            GameObject mini = Instantiate(cardPrefab, capturedGrid);
            var ui = mini.GetComponent<CardUI>() ?? mini.AddComponent<CardUI>();
            ui.Initialize(card, false);
        }
    }

    // ------------- between-decks scoring summary -------------

    private GameObject summaryPanel;
    private TextMeshProUGUI summaryLeft;
    private TextMeshProUGUI summaryRight;
    private TextMeshProUGUI summaryTitle;
    private System.Action summaryContinue;

    public void ShowRoundSummary(GamePlayer dealer, GamePlayer nonDealer,
        Dictionary<string, int> dealerRound, Dictionary<string, int> nonDealerRound,
        System.Action onContinue)
    {
        if (summaryPanel == null)
            CreateSummaryPanel();

        GamePlayer human = dealer.IsHuman() ? dealer : nonDealer;
        GamePlayer ai = human == dealer ? nonDealer : dealer;
        var humanRound = human == dealer ? dealerRound : nonDealerRound;
        var aiRound = ai == dealer ? dealerRound : nonDealerRound;

        summaryTitle.text = $"Deck complete - scoring   (first to {ScoringManager.Instance.WinScore})";
        summaryLeft.text = SummaryColumn("You", human, humanRound);
        summaryRight.text = SummaryColumn("AI", ai, aiRound);

        summaryContinue = onContinue;
        summaryPanel.SetActive(true);
    }

    private string SummaryColumn(string label, GamePlayer p, Dictionary<string, int> round)
    {
        int earned = round.Values.Sum();
        string lines = round.Count > 0
            ? string.Join("\n", round.Select(kv => $"{kv.Key}  +{kv.Value}"))
            : "no points this deck";
        return $"{label}\n\n{lines}\n\nThis deck: +{earned}\nTotal: {p.Score}";
    }

    private void CreateSummaryPanel()
    {
        summaryPanel = new GameObject("ScoreSummary");
        summaryPanel.transform.SetParent(canvasTransform, false);
        var rect = summaryPanel.AddComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = new Vector2(520, 380);
        var bg = summaryPanel.AddComponent<Image>();
        bg.color = new Color(0.04f, 0.06f, 0.09f, 0.97f);

        summaryTitle = CreateText("Title", summaryPanel.transform);
        var tr = summaryTitle.rectTransform;
        tr.anchorMin = new Vector2(0, 0.88f);
        tr.anchorMax = Vector2.one;
        tr.offsetMin = new Vector2(12, 0);
        tr.offsetMax = new Vector2(-12, -6);
        summaryTitle.fontSize = 18;
        summaryTitle.fontStyle = FontStyles.Bold;
        summaryTitle.alignment = TextAlignmentOptions.Center;
        summaryTitle.color = new Color(1f, 0.9f, 0.5f);

        summaryLeft = CreateText("Left", summaryPanel.transform);
        var lr = summaryLeft.rectTransform;
        lr.anchorMin = new Vector2(0.04f, 0.2f);
        lr.anchorMax = new Vector2(0.48f, 0.86f);
        lr.offsetMin = lr.offsetMax = Vector2.zero;
        summaryLeft.fontSize = 15;
        summaryLeft.alignment = TextAlignmentOptions.TopLeft;
        summaryLeft.color = new Color(0.75f, 1f, 0.8f);

        summaryRight = CreateText("Right", summaryPanel.transform);
        var rr = summaryRight.rectTransform;
        rr.anchorMin = new Vector2(0.52f, 0.2f);
        rr.anchorMax = new Vector2(0.96f, 0.86f);
        rr.offsetMin = rr.offsetMax = Vector2.zero;
        summaryRight.fontSize = 15;
        summaryRight.alignment = TextAlignmentOptions.TopLeft;
        summaryRight.color = new Color(1f, 0.8f, 0.75f);

        // Continue button
        GameObject go = new("Continue");
        go.transform.SetParent(summaryPanel.transform, false);
        var br = go.AddComponent<RectTransform>();
        br.anchorMin = new Vector2(0.5f, 0);
        br.anchorMax = new Vector2(0.5f, 0);
        br.pivot = new Vector2(0.5f, 0);
        br.anchoredPosition = new Vector2(0, 14);
        br.sizeDelta = new Vector2(190, 46);
        var img = go.AddComponent<Image>();
        img.color = new Color(0.72f, 0.6f, 0.25f, 0.95f);
        var btn = go.AddComponent<Button>();
        btn.onClick.AddListener(() =>
        {
            summaryPanel.SetActive(false);
            var action = summaryContinue;
            summaryContinue = null;
            action?.Invoke();
        });

        var label = CreateText("Label", go.transform);
        var lbr = label.rectTransform;
        lbr.anchorMin = Vector2.zero;
        lbr.anchorMax = Vector2.one;
        lbr.offsetMin = lbr.offsetMax = Vector2.zero;
        label.text = "Continue";
        label.fontSize = 20;
        label.alignment = TextAlignmentOptions.Center;
        label.color = Color.white;

        summaryPanel.SetActive(false);
    }

    // ------------- move animations (ghost cards, layout-independent) -------------

    public void AnimateCapture(PlayingCard played, List<PlayingCard> captured, bool byHuman)
    {
        Vector3 target = (byHuman ? humanPileButton : aiPileButton).transform.position;
        SpawnGhost(FindCardUI(played), target);
        foreach (var c in captured)
            SpawnGhost(FindCardUI(c), target);
    }

    public void AnimateTrail(PlayingCard played)
    {
        if (tableCardsContainer != null)
            SpawnGhost(FindCardUI(played), tableCardsContainer.position);
    }

    private CardUI FindCardUI(PlayingCard card)
    {
        return dealerCardUIs.Concat(nonDealerCardUIs).Concat(tableCardUIs)
            .FirstOrDefault(ui => ui != null && ui.Card == card);
    }

    private void SpawnGhost(CardUI source, Vector3 targetWorld)
    {
        if (source == null) return;
        SpawnGhostCore(source.Card, source.transform.position, targetWorld, false);
    }

    private void SpawnGhostCore(PlayingCard card, Vector3 startWorld, Vector3 targetWorld, bool faceDown)
    {
        if (cardPrefab == null) return;

        GameObject ghost = Instantiate(cardPrefab, canvasTransform);
        ghost.name = "Ghost";
        var rect = ghost.GetComponent<RectTransform>();
        rect.position = startWorld;
        rect.sizeDelta = new Vector2(80, 120);

        var ui = ghost.GetComponent<CardUI>() ?? ghost.AddComponent<CardUI>();
        ui.Initialize(card, false);
        ui.SetFaceDown(faceDown);

        var group = ghost.AddComponent<CanvasGroup>();
        group.blocksRaycasts = false;

        StartCoroutine(TweenGhost(rect, targetWorld));
    }

    private System.Collections.IEnumerator TweenGhost(RectTransform ghost, Vector3 target)
    {
        Vector3 start = ghost.position;
        float duration = 0.45f, t = 0f;
        while (t < duration)
        {
            if (ghost == null) yield break;
            t += Time.deltaTime;
            float k = t / duration;
            k = k * k * (3f - 2f * k); // smoothstep
            ghost.position = Vector3.Lerp(start, target, k);
            ghost.localScale = Vector3.one * (1f - 0.5f * k);
            yield return null;
        }
        if (ghost != null) Destroy(ghost.gameObject);
    }

    private void CreateScorePanel()
    {
        GameObject panel = new("ScorePanel");
        panel.transform.SetParent(canvasTransform, false);
        var rect = panel.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(1, 1);
        rect.anchorMax = new Vector2(1, 1);
        rect.pivot = new Vector2(1, 1);
        rect.anchoredPosition = new Vector2(-12, -12);
        rect.sizeDelta = new Vector2(250, 240);

        var bg = panel.AddComponent<Image>();
        bg.color = new Color(0.05f, 0.07f, 0.1f, 0.92f);
        bg.raycastTarget = false;

        scoreHeaderText = CreateText("Header", panel.transform);
        var hr = scoreHeaderText.rectTransform;
        hr.anchorMin = new Vector2(0, 0.85f);
        hr.anchorMax = Vector2.one;
        hr.offsetMin = new Vector2(8, 0);
        hr.offsetMax = new Vector2(-8, -4);
        scoreHeaderText.fontSize = 17;
        scoreHeaderText.fontStyle = FontStyles.Bold;
        scoreHeaderText.alignment = TextAlignmentOptions.Center;
        scoreHeaderText.color = Color.white;

        humanStatsText = CreateText("HumanStats", panel.transform);
        var hu = humanStatsText.rectTransform;
        hu.anchorMin = new Vector2(0, 0.44f);
        hu.anchorMax = new Vector2(1, 0.85f);
        hu.offsetMin = new Vector2(10, 0);
        hu.offsetMax = new Vector2(-10, 0);
        humanStatsText.fontSize = 14;
        humanStatsText.alignment = TextAlignmentOptions.TopLeft;
        humanStatsText.color = new Color(0.75f, 1f, 0.8f);

        aiStatsText = CreateText("AIStats", panel.transform);
        var ai = aiStatsText.rectTransform;
        ai.anchorMin = new Vector2(0, 0.03f);
        ai.anchorMax = new Vector2(1, 0.44f);
        ai.offsetMin = new Vector2(10, 0);
        ai.offsetMax = new Vector2(-10, 0);
        aiStatsText.fontSize = 14;
        aiStatsText.alignment = TextAlignmentOptions.TopLeft;
        aiStatsText.color = new Color(1f, 0.8f, 0.75f);
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

            // Re-apply selectability when the human's turn starts or ends
            bool humanTurn = GameManager.Instance.IsWaitingForHumanInput();
            if (humanTurn != lastHumanTurn)
            {
                lastHumanTurn = humanTurn;
                UpdatePlayerHands();
                UpdateTableCards();
                UpdateActionButtons();
                if (humanTurn && hintText != null)
                    hintText.text = "Your turn. Select a card, then Play, or add table cards and Build.";
            }
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

        UpdateOneHand(dealer, dealerHandContainer, dealerCardUIs);
        UpdateOneHand(nonDealer, nonDealerHandContainer, nonDealerCardUIs);
    }

    // Human hand: face up, selectable only while input is expected.
    // AI hand: face down, never selectable.
    private void UpdateOneHand(GamePlayer player, Transform container, List<CardUI> cardUIs)
    {
        bool isHuman = player.IsHuman();
        bool selectable = isHuman && GameManager.Instance.IsWaitingForHumanInput();

        if (cardUIs.Count != player.Hand.Count)
            UpdateHandDisplay(player, container, cardUIs, selectable, !isHuman);
        else
            UpdateHandSelectability(player, cardUIs, selectable, !isHuman);
    }

    private void UpdateHandSelectability(GamePlayer player, List<CardUI> cardUIs, bool selectable, bool faceDown)
    {
        for (int i = 0; i < cardUIs.Count && i < player.Hand.Count; i++)
        {
            cardUIs[i].Initialize(player.Hand[i], selectable);
            cardUIs[i].SetFaceDown(faceDown);
        }
    }

    private void UpdateHandDisplay(GamePlayer player, Transform container, List<CardUI> cardUIs, bool selectable, bool faceDown = false)
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

            // Get existing CardUI component or add one if it doesn't exist
            CardUI cardUI = cardObj.GetComponent<CardUI>();
            if (cardUI == null)
            {
                cardUI = cardObj.AddComponent<CardUI>();
                Debug.Log("Added CardUI component");
            }
            else
            {
                Debug.Log("Using existing CardUI component");
            }

            cardUI.Initialize(player.Hand[i], selectable);
            cardUI.SetFaceDown(faceDown);
            cardUIs.Add(cardUI);

            StartCoroutine(AnimateCardAppearance(cardUI, i * 0.05f));
        }
    }
    
    private void UpdateTableCards()
    {
        List<PlayingCard> tableCards = GameManager.Instance.GetTableCards();

        // Table cards are clickable during the human's turn so they can be
        // multi-selected as build material.
        bool selectable = GameManager.Instance.IsWaitingForHumanInput();

        if (tableCardUIs.Count != tableCards.Count)
        {
            buildSelection.Clear();
            foreach (Transform child in tableCardsContainer)
            {
                Destroy(child.gameObject);
            }
            tableCardUIs.Clear();

            for (int i = 0; i < tableCards.Count; i++)
            {
                GameObject cardObj = Instantiate(cardPrefab, tableCardsContainer);

                // Get existing CardUI component or add one if it doesn't exist
                CardUI cardUI = cardObj.GetComponent<CardUI>();
                if (cardUI == null)
                    cardUI = cardObj.AddComponent<CardUI>();

                cardUI.Initialize(tableCards[i], selectable);
                tableCardUIs.Add(cardUI);

                StartCoroutine(AnimateCardAppearance(cardUI, i * 0.05f));
            }
        }
        else
        {
            buildSelection.Clear(); // Initialize() below resets selection visuals
            for (int i = 0; i < tableCardUIs.Count; i++)
                tableCardUIs[i].Initialize(tableCards[i], selectable);
        }
        UpdateActionButtons();
    }

    private void UpdateBuilds()
    {
        // Builds live inline in the table row - a stack sits among the loose
        // cards instead of moving to a separate area.
        if (tableCardsContainer == null) return;

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
            buildObj.transform.SetParent(tableCardsContainer, false);
            buildUIs.Add(buildObj);
        }
    }

    // A build renders as a physical stack: overlapped, slightly fanned cards
    // with a small owner-colored value badge. No box, no header.
    private GameObject CreateBuildUI(Build build)
    {
        GameObject root = new($"Build_{build.DeclaredValue}");
        var rect = root.AddComponent<RectTransform>();

        // Clickable so single-group builds can be selected for raising
        var hitArea = root.AddComponent<Image>();
        hitArea.color = new Color(0, 0, 0, 0.01f);
        var rootButton = root.AddComponent<Button>();
        rootButton.transition = Selectable.Transition.None;
        rootButton.onClick.AddListener(() => OnBuildStackClicked(build, root));
        int n = build.Cards.Count;
        const float step = 16f;
        rect.sizeDelta = new Vector2(56 + (n - 1) * step, 92);

        for (int i = 0; i < n; i++)
        {
            GameObject mini = Instantiate(cardPrefab, root.transform);
            CardUI cardUI = mini.GetComponent<CardUI>();
            if (cardUI == null) cardUI = mini.AddComponent<CardUI>();
            cardUI.Initialize(build.Cards[i], false);

            var r = mini.GetComponent<RectTransform>();
            r.anchorMin = r.anchorMax = new Vector2(0.5f, 0.5f);
            r.sizeDelta = new Vector2(56, 80);
            r.anchoredPosition = new Vector2(
                -(rect.sizeDelta.x - 56) / 2f + i * step, (i % 2) * -3f);
            r.localRotation = Quaternion.Euler(0, 0, (i - (n - 1) / 2f) * -2f);
        }

        // Owner-colored value badge on the top card
        bool mine = build.Owner.IsHuman();
        GameObject badge = new("Badge");
        badge.transform.SetParent(root.transform, false);
        var br = badge.AddComponent<RectTransform>();
        br.anchorMin = br.anchorMax = new Vector2(1, 1);
        br.pivot = new Vector2(1, 1);
        br.anchoredPosition = new Vector2(8, 8);
        br.sizeDelta = new Vector2(30, 30);
        var bi = badge.AddComponent<Image>();
        bi.color = mine ? new Color(0.2f, 0.45f, 0.85f, 0.95f)
                        : new Color(0.75f, 0.25f, 0.2f, 0.95f);

        var badgeText = CreateText("Value", badge.transform);
        var btr = badgeText.rectTransform;
        btr.anchorMin = Vector2.zero;
        btr.anchorMax = Vector2.one;
        btr.offsetMin = btr.offsetMax = Vector2.zero;
        badgeText.fontSize = 16;
        badgeText.fontStyle = FontStyles.Bold;
        badgeText.alignment = TextAlignmentOptions.Center;
        badgeText.color = Color.white;
        badgeText.text = build.DeclaredValue switch
        {
            11 => "J", 12 => "Q", 13 => "K", _ => build.DeclaredValue.ToString()
        };

        return root;
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

        UpdateDrawPile(deck.CardsRemaining());
        
        if (dealerScoreText != null)
            dealerScoreText.text = $"Dealer: {dealer.Name}\nScore: {dealer.Score}";
        
        if (nonDealerScoreText != null)
            nonDealerScoreText.text = $"Non-Dealer: {nonDealer.Name}\nScore: {nonDealer.Score}";
        
        if (gameStatusText != null)
        {
            GameManager.GamePhase phase = GameManager.Instance.GetCurrentPhase();
            if (phase == GameManager.GamePhase.GameOver)
            {
                GamePlayer winner = dealer.Score > nonDealer.Score ? dealer : nonDealer;
                gameStatusText.text = $"Game Over!\n{winner.Name} Wins!";
                playCardButton.interactable = false;
                if (gameOverPanel != null && !gameOverPanel.activeSelf)
                {
                    gameOverPanel.SetActive(true);
                    Debug.Log("Game Over - showing GameOverPanel");

                    // Verify restart button state when panel is shown
                    if (restartButton != null)
                    {
                        Debug.Log($"Restart button state: interactable={restartButton.interactable}, enabled={restartButton.enabled}, gameObject.active={restartButton.gameObject.activeInHierarchy}");
                    }
                }
            }
            else
            {
                gameStatusText.text = "Playing...";
                if (gameOverPanel != null)
                    gameOverPanel.SetActive(false);
            }
        }

        UpdateScorePanel(dealer, nonDealer);
    }

    private void UpdateScorePanel(GamePlayer dealer, GamePlayer nonDealer)
    {
        if (scoreHeaderText == null || ScoringManager.Instance == null) return;

        scoreHeaderText.text = $"First to {ScoringManager.Instance.WinScore} wins";

        GamePlayer human = dealer.IsHuman() ? dealer : nonDealer;
        GamePlayer ai = dealer.IsHuman() ? nonDealer : dealer;

        humanStatsText.text = PlayerStats(human, "You");
        aiStatsText.text = PlayerStats(ai, "AI");

        if (humanPileLabel != null)
        {
            humanPileLabel.text = $"Your pile: {human.CapturedCards.Count} cards  (view)";
            aiPileLabel.text = $"AI pile: {ai.CapturedCards.Count} cards  (view)";
        }
        if (capturedPanel != null && capturedPanel.activeSelf)
            RebuildPilePanel();
    }

    private string PlayerStats(GamePlayer p, string label)
    {
        var sm = ScoringManager.Instance;
        var captured = p.CapturedCards;
        int spades = captured.Count(c => c.suit == PlayingCard.Suit.Spades);
        int aces = captured.Count(c => c.rank == PlayingCard.Rank.Ace);
        bool big = captured.Any(c => c.suit == sm.BigCasinoSuit && c.rank == sm.BigCasinoRank);
        bool little = captured.Any(c => c.suit == sm.LittleCasinoSuit && c.rank == sm.LittleCasinoRank);

        return $"{label} ({p.Name})  Score: {p.Score}\n" +
               $"Cards: {captured.Count}   Spades: {spades}\n" +
               $"Aces: {aces}   10♦: {(big ? "yes" : "no")}   2♠: {(little ? "yes" : "no")}\n" +
               $"Sweeps: {p.SweepCount}";
    }
    
    public void OnCardSelected(CardUI cardUI, PlayingCard card)
    {
        // Table card: pick what to sweep (or build with). Table-first is the
        // primary flow: select table cards and the hand shows who can take them.
        if (tableCardUIs.Contains(cardUI))
        {
            if (!buildSelection.Remove(cardUI))
            {
                // Only outright-impossible pairings are refused up front
                if (selectedCard != null)
                {
                    bool handIsFace = IsFaceRank(selectedCard.Card.rank);
                    bool tableIsFace = IsFaceRank(cardUI.Card.rank);
                    if (handIsFace && cardUI.Card.rank != selectedCard.Card.rank)
                    {
                        cardUI.SetSelected(false);
                        hintText.text = $"A {selectedCard.Card.rank} only takes {selectedCard.Card.rank}s.";
                        return;
                    }
                    if (!handIsFace && tableIsFace)
                    {
                        cardUI.SetSelected(false);
                        hintText.text = $"{CardName(selectedCard.Card)} can't take a face card.";
                        return;
                    }
                }
                buildSelection.Add(cardUI);
            }

            HighlightCapturingHandCards();
            UpdateActionButtons();
            return;
        }

        // Hand card: strictly single selection; one card per turn.
        // Table selection is kept - selecting the hand card is step two
        // of the table-first flow.
        if (selectedCard != null && selectedCard != cardUI)
            selectedCard.SetSelected(false);

        selectedCard = cardUI.IsSelected ? cardUI : null;
        HighlightCapturingHandCards();
        UpdateActionButtons();
    }

    // Table-first guidance: light up every hand card that can take exactly
    // the current table selection.
    private void HighlightCapturingHandCards()
    {
        var gm = GameManager.Instance;
        if (gm == null || !gm.IsWaitingForHumanInput()) return;

        var player = gm.GetCurrentPlayer();
        var handUIs = player == gm.GetDealer() ? dealerCardUIs : nonDealerCardUIs;
        var chosen = buildSelection.Select(ui => ui.Card).ToList();

        int takers = 0;
        foreach (var ui in handUIs)
        {
            if (ui == null) continue;
            bool canTake = chosen.Count > 0 && CaptureChecker.IsExactCaptureSet(ui.Card, chosen);
            ui.SetSuggested(canTake);
            if (canTake) takers++;
        }

        if (chosen.Count > 0 && hintText != null && selectedCard == null)
        {
            hintText.text = takers > 0
                ? $"{takers} card(s) in your hand can take this - they're highlighted."
                : "No card in your hand can take this selection.";
        }
    }

    private int HandCardIndex(GamePlayer player, PlayingCard card)
    {
        for (int i = 0; i < player.Hand.Count; i++)
            if (player.Hand[i] == card) return i;
        return -1;
    }

    private void ClearSelections()
    {
        if (selectedCard != null) selectedCard.SetSelected(false);
        selectedCard = null;
        foreach (var ui in buildSelection)
            if (ui != null) ui.SetSelected(false);
        buildSelection.Clear();
        DeselectBuild();
        UpdateActionButtons();
    }

    private void DeselectBuild()
    {
        if (selectedBuildRoot != null)
            selectedBuildRoot.transform.localScale = Vector3.one;
        selectedBuild = null;
        selectedBuildRoot = null;
    }

    private void OnBuildStackClicked(Build build, GameObject root)
    {
        var gm = GameManager.Instance;
        if (gm == null || !gm.IsWaitingForHumanInput()) return;

        if (selectedBuild == build)
        {
            DeselectBuild();
            UpdateActionButtons();
            return;
        }

        DeselectBuild();
        selectedBuild = build;
        selectedBuildRoot = root;
        root.transform.localScale = Vector3.one * 1.1f;

        if (build.IsMultiBuild)
            hintText.text = "Multi-build: the value is locked. It can only be taken.";
        else
            hintText.text = $"Build of {build.DeclaredValue}: select a hand card to raise it.";

        UpdateActionButtons();
    }

    private void ClearSuggestionHighlights()
    {
        foreach (var ui in dealerCardUIs) if (ui != null) ui.SetSuggested(false);
        foreach (var ui in nonDealerCardUIs) if (ui != null) ui.SetSuggested(false);
        foreach (var ui in tableCardUIs) if (ui != null) ui.SetSuggested(false);
    }

    private void UpdateActionButtons()
    {
        if (buildButton == null) return;

        bool humanTurn = GameManager.Instance != null && GameManager.Instance.IsWaitingForHumanInput();
        buildButton.gameObject.SetActive(humanTurn);
        suggestButton.gameObject.SetActive(humanTurn);
        trailButton.gameObject.SetActive(humanTurn);
        sweepButton.gameObject.SetActive(humanTurn);

        // Trailing is a free choice unless you own a build
        bool ownsBuild = humanTurn &&
            GameManager.Instance.PlayerOwnsBuild(GameManager.Instance.GetCurrentPlayer());
        trailButton.interactable = humanTurn && selectedCard != null && !ownsBuild
                                   && buildSelection.Count == 0;
        trailButtonLabel.text = ownsBuild ? "Trail (own build)" : "Trail";

        // Sweep: takes the chosen table cards, or everything that applies
        bool canSweep = false;
        if (humanTurn && selectedCard != null)
        {
            var gm = GameManager.Instance;
            if (buildSelection.Count > 0)
            {
                canSweep = CaptureChecker.IsExactCaptureSet(selectedCard.Card,
                    buildSelection.Select(ui => ui.Card).ToList());
            }
            else
            {
                canSweep = CaptureChecker.GetValidCaptures(selectedCard.Card, gm.GetTableCards()).Count > 0
                    || gm.GetActiveBuilds().Any(b =>
                        b.DeclaredValue == CaptureChecker.BuildCaptureValue(selectedCard.Card));
            }
        }
        sweepButton.interactable = canSweep;
        sweepButtonLabel.text = selectedCard != null && canSweep
            ? $"Sweep {SweepName(selectedCard.Card)}" : "Sweep";

        bool hasBuildSelection = humanTurn && selectedCard != null && buildSelection.Count > 0;

        // A selected build stack turns the Build button into add or raise
        if (humanTurn && selectedBuild != null)
        {
            var gmr = GameManager.Instance;
            var raiser = gmr.GetCurrentPlayer();

            // Same value: add a new group (allowed even on multi-builds)
            if (selectedCard != null &&
                CaptureChecker.BuildCaptureValue(selectedCard.Card) == selectedBuild.DeclaredValue)
            {
                bool holdsAnother = raiser.Hand.Any(c => c != selectedCard.Card &&
                    CaptureChecker.BuildCaptureValue(c) == selectedBuild.DeclaredValue);
                buildButton.interactable = holdsAnother;
                buildButtonLabel.text = holdsAnother
                    ? $"Add to {SweepName(selectedCard.Card)}"
                    : "Need another to take it";
            }
            else if (selectedBuild.IsMultiBuild || selectedCard == null)
            {
                buildButton.interactable = false;
                buildButtonLabel.text = selectedBuild.IsMultiBuild ? "Locked (multi)" : "Raise: pick a card";
            }
            else
            {
                int newValue = selectedBuild.Cards.Sum(c => CaptureChecker.GetCardValue(c))
                               + CaptureChecker.GetCardValue(selectedCard.Card);
                bool valid = newValue > selectedBuild.DeclaredValue && newValue <= 10 &&
                             raiser.Hand.Any(c => c != selectedCard.Card &&
                                 CaptureChecker.GetCardValue(c) == newValue);
                buildButton.interactable = valid;
                buildButtonLabel.text = valid ? $"Raise to {newValue}"
                    : newValue > 10 ? $"{newValue} is too high"
                    : $"No {newValue} in hand";
            }
            UpdatePlayPreview(humanTurn);
            return;
        }

        if (hasBuildSelection)
        {
            var player = GameManager.Instance.GetCurrentPlayer();
            var rank = selectedCard.Card.rank;
            bool anyFace = IsFaceRank(rank) || buildSelection.Any(ui => IsFaceRank(ui.Card.rank));

            if (anyFace)
            {
                // Face build: all one rank, and you must hold another of it
                bool sameRank = IsFaceRank(rank) && buildSelection.All(ui => ui.Card.rank == rank);
                bool holds = sameRank && player.Hand.Any(c => c != selectedCard.Card && c.rank == rank);
                buildButton.interactable = holds;
                buildButtonLabel.text = holds ? $"Build {rank}s"
                    : sameRank ? $"No {rank} in hand" : "Face builds: one rank";
            }
            else
            {
                // All values these cards can stack into (multi-builds included)
                var candidates = CaptureChecker.PossibleBuildValues(
                    selectedCard.Card, buildSelection.Select(ui => ui.Card).ToList());
                var buildable = candidates.Where(v => player.Hand.Any(c =>
                    c != selectedCard.Card && CaptureChecker.GetCardValue(c) == v)).ToList();

                buildButton.interactable = buildable.Count > 0;
                buildButtonLabel.text = buildable.Count > 0 ? $"Build {buildable.Max()}"
                    : candidates.Count > 0 ? $"No {string.Join("/", candidates)} in hand"
                    : "Not a build";
            }
        }
        else
        {
            buildButton.interactable = false;
            buildButtonLabel.text = "Build";
        }

        UpdatePlayPreview(humanTurn);
    }

    // Live preview of what the Play button will do with the selected card
    private void UpdatePlayPreview(bool humanTurn)
    {
        if (hintText == null || !humanTurn) return;

        // Hand card + table selection: preview the chosen sweep
        if (selectedCard != null && buildSelection.Count > 0)
        {
            var chosen = buildSelection.Select(ui => ui.Card).ToList();
            hintText.text = CaptureChecker.IsExactCaptureSet(selectedCard.Card, chosen)
                ? $"Play sweeps: {string.Join(" ", chosen.Select(CardName))}"
                : $"{CardName(selectedCard.Card)} can't take that selection.";
            return;
        }

        if (selectedCard == null || buildSelection.Count > 0) return;

        var gm = GameManager.Instance;
        PlayingCard card = selectedCard.Card;
        int value = CaptureChecker.BuildCaptureValue(card);

        var captures = CaptureChecker.GetValidCaptures(card, gm.GetTableCards());
        int buildCaptures = gm.GetActiveBuilds().Count(b => b.DeclaredValue == value);

        if (captures.Count > 0 || buildCaptures > 0)
        {
            string what = string.Join(" ", captures.Select(CardName));
            if (buildCaptures > 0)
                what += (what.Length > 0 ? " + " : "") + $"{buildCaptures} build(s)";
            bool sweep = captures.Count == gm.GetTableCards().Count && gm.GetActiveBuilds().Count == buildCaptures;
            hintText.text = $"Play takes: {what}{(sweep ? "  (SWEEP!)" : "")}";
        }
        else
        {
            hintText.text = $"Play will trail {CardName(card)} (no captures).";
        }
    }

    private void OnBuildClicked()
    {
        GamePlayer player = GameManager.Instance.GetCurrentPlayer();
        if (!player.IsHuman() || selectedCard == null)
            return;

        // Build-stack mode: same value adds a group, higher value raises
        if (selectedBuild != null)
        {
            int idx = HandCardIndex(player, selectedCard.Card);
            if (idx < 0) return;

            bool sameValue = CaptureChecker.BuildCaptureValue(selectedCard.Card) == selectedBuild.DeclaredValue;
            bool ok = sameValue
                ? GameManager.Instance.TryAddToBuild(player, idx, selectedBuild)
                : GameManager.Instance.TryRaiseBuild(player, idx, selectedBuild);

            if (ok)
            {
                hintText.text = "";
                ClearSelections();
            }
            else
            {
                hintText.text = sameValue
                    ? "Can't add: you need another card that takes the build."
                    : "Can't raise: you must hold the new total, and multi-builds are locked.";
            }
            return;
        }

        if (buildSelection.Count == 0)
            return;

        int cardIndex = HandCardIndex(player, selectedCard.Card);
        if (cardIndex < 0) return;

        var tableCardsForBuild = buildSelection.Select(ui => ui.Card).ToList();

        if (GameManager.Instance.TryCreateBuild(player, cardIndex, tableCardsForBuild))
        {
            hintText.text = "";
            ClearSelections();
        }
        else
        {
            hintText.text = "Invalid build: 1-10 (hold the capture card) or one face rank (hold another of it).";
        }
    }

    private static bool IsFaceRank(PlayingCard.Rank rank) =>
        rank == PlayingCard.Rank.Jack || rank == PlayingCard.Rank.Queen || rank == PlayingCard.Rank.King;

    private void OnSuggestClicked()
    {
        var gm = GameManager.Instance;
        if (gm == null || !gm.IsWaitingForHumanInput()) return;

        var action = gm.GetSuggestionForCurrentPlayer();
        if (action == null) return;

        GamePlayer player = gm.GetCurrentPlayer();
        ClearSelections();
        ClearSuggestionHighlights();

        var handUIs = player == gm.GetDealer() ? dealerCardUIs : nonDealerCardUIs;
        if (action.CardIndex < 0 || action.CardIndex >= handUIs.Count) return;

        PlayingCard handCard = player.Hand[action.CardIndex];
        handUIs[action.CardIndex].SetSuggested(true);

        if (action.Type == AIPlayer.AIAction.ActionType.CreateBuild)
        {
            foreach (var buildCard in action.BuildCards)
            {
                var ui = tableCardUIs.FirstOrDefault(t => t.Card == buildCard);
                if (ui != null) ui.SetSuggested(true);
            }
            hintText.text = $"Suggestion: build {action.DeclaredValue} with {CardName(handCard)}. Capture it next turn with your {action.DeclaredValue}.";
            return;
        }

        int playedValue = CaptureChecker.BuildCaptureValue(handCard);
        var captures = CaptureChecker.GetValidCaptures(handCard, gm.GetTableCards());
        var buildCaptures = gm.GetActiveBuilds().Where(b => b.DeclaredValue == playedValue).ToList();

        if (captures.Count > 0 || buildCaptures.Count > 0)
        {
            foreach (var cap in captures)
            {
                var ui = tableCardUIs.FirstOrDefault(t => t.Card == cap);
                if (ui != null) ui.SetSuggested(true);
            }
            string what = string.Join(", ", captures.Select(CardName));
            if (buildCaptures.Count > 0)
                what += (what.Length > 0 ? " and " : "") + $"{buildCaptures.Count} build(s)";
            hintText.text = $"Suggestion: play {CardName(handCard)} to capture {what}.";
        }
        else
        {
            hintText.text = $"Suggestion: no captures available. Trail {CardName(handCard)} (gives up the least).";
        }
    }

    private string CardName(PlayingCard card) => CaptureChecker.Describe(card);

    // Move banner: announces each play and its effect, then fades
    private TextMeshProUGUI moveBanner;
    private Coroutine bannerFade;

    public void ShowMove(string text)
    {
        if (moveBanner == null)
        {
            moveBanner = CreateText("MoveBanner", canvasTransform);
            var r = moveBanner.rectTransform;
            r.anchorMin = r.anchorMax = new Vector2(0.5f, 1);
            r.pivot = new Vector2(0.5f, 1);
            r.anchoredPosition = new Vector2(0, -118);
            r.sizeDelta = new Vector2(620, 40);
            moveBanner.fontSize = 21;
            moveBanner.fontStyle = FontStyles.Bold;
            moveBanner.alignment = TextAlignmentOptions.Center;
            moveBanner.color = new Color(1f, 0.9f, 0.5f);
        }
        moveBanner.text = text;
        moveBanner.alpha = 1f;
        if (bannerFade != null) StopCoroutine(bannerFade);
        bannerFade = StartCoroutine(FadeBanner());
    }

    private System.Collections.IEnumerator FadeBanner()
    {
        yield return new WaitForSeconds(1.6f);
        float t = 0f;
        while (t < 0.5f && moveBanner != null)
        {
            t += Time.deltaTime;
            moveBanner.alpha = 1f - t / 0.5f;
            yield return null;
        }
    }

    private string SweepName(PlayingCard c) =>
        IsFaceRank(c.rank) ? $"{c.rank}s" : $"{CaptureChecker.GetCardValue(c)}s";

    private void OnPlayCardClicked() => PlaySelectedCard(forceTrail: false);

    private void OnSweepClicked() => PlaySelectedCard(forceTrail: false);

    private void OnTrailClicked() => PlaySelectedCard(forceTrail: true);

    private void PlaySelectedCard(bool forceTrail)
    {
        if (GameManager.Instance == null || !GameManager.Instance.IsWaitingForHumanInput())
            return;

        if (selectedCard == null)
        {
            Debug.LogWarning("No card selected!");
            return;
        }

        GamePlayer currentPlayer = GameManager.Instance.GetCurrentPlayer();

        int cardIndex = HandCardIndex(currentPlayer, selectedCard.Card);
        if (cardIndex < 0)
            return;

        // Table cards selected: take exactly those (the player's chosen sweep)
        if (buildSelection.Count > 0 && !forceTrail)
        {
            var chosen = buildSelection.Select(ui => ui.Card).ToList();
            if (GameManager.Instance.TryCaptureSelected(currentPlayer, cardIndex, chosen))
            {
                ClearSuggestionHighlights();
                hintText.text = "";
                ClearSelections();
            }
            else
            {
                hintText.text = $"{CardName(selectedCard.Card)} can't take that selection.";
            }
            return;
        }

        if (buildSelection.Count > 0 && forceTrail)
        {
            hintText.text = "Deselect the table cards to trail.";
            return;
        }

        ClearSuggestionHighlights();
        hintText.text = "";
        GameManager.Instance.PlayCard(currentPlayer, cardIndex, forceTrail);
        ClearSelections();
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

    /// <summary>
    /// Public test method to manually trigger restart from Inspector or debug
    /// </summary>
    [ContextMenu("Test Restart Button")]
    public void TestRestartButton()
    {
        Debug.Log("TestRestartButton called manually!");
        OnRestartClicked();
    }

    private void OnRestartClicked()
    {
        Debug.Log("════════════════════════════════════════");
        Debug.Log("OnRestartClicked CALLED!");
        Debug.Log("════════════════════════════════════════");

        if (gameOverPanel != null)
        {
            Debug.Log($"Hiding game over panel (was active: {gameOverPanel.activeSelf})");
            gameOverPanel.SetActive(false);
        }
        else
        {
            Debug.LogWarning("gameOverPanel is null!");
        }

        if (GameManager.Instance != null)
        {
            Debug.Log("Calling GameManager.Instance.InitializeGame()");
            GameManager.Instance.InitializeGame();
        }
        else
        {
            Debug.LogError("GameManager.Instance is null!");
        }

        if (playCardButton != null)
        {
            playCardButton.interactable = true;
            Debug.Log("Play card button set to interactable");
        }

        RefreshUI();
        Debug.Log("════════════════════════════════════════");
        Debug.Log("OnRestartClicked COMPLETE!");
        Debug.Log("════════════════════════════════════════");
    }
}