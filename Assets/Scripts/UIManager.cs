using System.Collections;
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
    private bool isSuggested = false;    // the game is advising this play
    private bool isCapturable = false;   // the board makes this takeable
    private bool isOpponentTaking = false; // the AI is about to take this
    private Image backImage;
    private Image faceImage;
    private Image shadowImage;
    private TextMeshProUGUI cornerText;

    // Procedural card back, generated once and shared (no art assets yet).
    public static Sprite CardBackSprite => CasinoArt.CardBack();

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

    // Cards are three layers, because one Image cannot be both a soft shadow and
    // a state-tinted face: the shadow would take the tint. Root stays fully
    // transparent and carries the click (UGUI raycasts the rect, not the alpha).
    private void EnsureSurface()
    {
        if (cardImage == null) cardImage = GetComponent<Image>();
        if (cardImage != null)
        {
            // Root is the click target only: fully transparent, no sprite. UGUI
            // raycasts the rect rather than the alpha, so this still receives
            // clicks. Leaving it opaque put a sharp-cornered rect under the
            // rounded face and undid the radius.
            cardImage.sprite = null;
            cardImage.color = new Color(0, 0, 0, 0);
        }

        if (shadowImage == null)
        {
            GameObject sh = new("Shadow");
            sh.transform.SetParent(transform, false);
            sh.transform.SetSiblingIndex(0);
            var r = sh.AddComponent<RectTransform>();
            r.anchorMin = Vector2.zero; r.anchorMax = Vector2.one;
            r.offsetMin = new Vector2(-8, -11);   // room for the blur and the drop
            r.offsetMax = new Vector2(8, 5);
            shadowImage = sh.AddComponent<Image>();
            shadowImage.sprite = CasinoArt.Shadow(6, 8, 3);
            shadowImage.type = Image.Type.Sliced;
            shadowImage.raycastTarget = false;
        }

        if (faceImage == null)
        {
            GameObject face = new("Face");
            face.transform.SetParent(transform, false);
            face.transform.SetSiblingIndex(1);
            var r = face.AddComponent<RectTransform>();
            r.anchorMin = Vector2.zero; r.anchorMax = Vector2.one;
            r.offsetMin = Vector2.zero; r.offsetMax = Vector2.zero;
            faceImage = face.AddComponent<Image>();
            faceImage.sprite = CasinoArt.RoundedFill(5);
            faceImage.type = Image.Type.Sliced;
            faceImage.raycastTarget = false;
        }
    }

    private void UpdateDisplay()
    {
        EnsureSurface();
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
                CasinoType.ApplySerif(rankSuitText);
            }
        }

        UpdateCornerIndex();

        // Ensure button exists and set interactable state
        if (button == null)
            button = GetComponent<Button>();

        if (button != null)
            button.interactable = isSelectable;

        UpdateVisuals();
    }

    // Corner index. Not decoration: cards in a build overlap with only 16 units
    // showing, so the centred glyph is hidden on every card but the top one. A
    // build's contents are unreadable without this.
    private void UpdateCornerIndex()
    {
        if (cornerText == null)
        {
            GameObject corner = new("Index");
            corner.transform.SetParent(transform, false);
            var r = corner.AddComponent<RectTransform>();
            r.anchorMin = new Vector2(0, 1);
            r.anchorMax = new Vector2(0, 1);
            r.pivot = new Vector2(0, 1);
            r.anchoredPosition = new Vector2(4, -3);
            r.sizeDelta = new Vector2(26, 30);
            cornerText = corner.AddComponent<TextMeshProUGUI>();
            cornerText.raycastTarget = false;
            cornerText.alignment = TextAlignmentOptions.TopLeft;
            cornerText.enableWordWrapping = false;
            cornerText.lineSpacing = -34f;   // stack the suit tight under the rank
            CasinoType.ApplySerif(cornerText);
        }

        bool show = !isFaceDown && card != null;
        cornerText.gameObject.SetActive(show);
        if (!show) return;

        // Scale with the card, so build minis stay legible without a second path.
        var rect = transform as RectTransform;
        float k = rect != null ? Mathf.Clamp(rect.rect.width / 80f, 0.6f, 1.2f) : 1f;
        cornerText.fontSize = 15f * k;
        cornerText.rectTransform.anchoredPosition = new Vector2(4f * k, -3f * k);
        cornerText.color = GetSuitColor(card.suit);
        cornerText.text = $"{GetRankDisplay(card.rank)}\n<size=80%>{GetSuitEmoji(card.suit)}</size>";
    }

    private static string GetRankDisplay(PlayingCard.Rank rank)
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
            PlayingCard.Suit.Hearts => CasinoTheme.SuitRed,
            PlayingCard.Suit.Diamonds => CasinoTheme.SuitRed,
            PlayingCard.Suit.Clubs => CasinoTheme.SuitBlack,
            PlayingCard.Suit.Spades => CasinoTheme.SuitBlack,
            _ => CasinoTheme.SuitBlack
        };
    }
    
    // One state, one appearance. The order matters: what the opponent is doing
    // outranks what you picked, which outranks advice, which outranks a plain
    // fact about the board.
    private void UpdateVisuals()
    {
        EnsureSurface();
        if (faceImage == null) return;

        faceImage.color =
            isFaceDown        ? CasinoTheme.CardFace          // back child draws on top
          : isOpponentTaking  ? CasinoTheme.CardOpponentTaking
          : isSelected        ? CasinoTheme.CardSelected
          : isSuggested       ? CasinoTheme.CardSuggested
          : isCapturable      ? CasinoTheme.CardCapturable
          : CasinoTheme.CardFace;

        // Scale is the second channel, so no state is carried by hue alone.
        float scale = isSelected ? 1.15f : isOpponentTaking ? 1.08f : 1f;
        transform.localScale = Vector3.one * scale;
    }

    public void SetFaceDown(bool faceDown)
    {
        isFaceDown = faceDown;
        UpdateDisplay();
    }

    // Advice from the Suggest evaluator. Optional guidance, not a board fact.
    public void SetSuggested(bool suggested)
    {
        isSuggested = suggested;
        UpdateVisuals();
    }

    // A fact about the board: this card can be taken by the current selection.
    public void SetCapturable(bool capturable)
    {
        isCapturable = capturable;
        UpdateVisuals();
    }

    // The opponent is taking this card. Must never look like your own selection.
    public void SetOpponentTaking(bool taking)
    {
        isOpponentTaking = taking;
        UpdateVisuals();
    }

    public void ClearHighlights()
    {
        isSuggested = isCapturable = isOpponentTaking = false;
        UpdateVisuals();
    }
    
    private void OnCardClicked()
    {
        if (!isSelectable) return;
        SetSelected(!isSelected);
        UIManager.Instance.OnCardSelected(this, card);
    }

    // Verification hook: a click is these two calls together, and the order
    // matters. Calling UIManager.OnCardSelected on its own leaves isSelected
    // stale, so UIManager resolves the selection to null and every downstream
    // check behaves as though nothing was picked up. A probe that did exactly
    // that reported the UI as silent when the real fault was the probe.
    // Returns false when the card refused the click, which is itself worth
    // asserting: cards are unselectable when it is not your turn.
    public bool SimulateClick()
    {
        if (!isSelectable) return false;
        OnCardClicked();
        return true;
    }

    public bool IsSelected => isSelected;

    // Selection scale is applied here so programmatic deselection shrinks
    // the card too - previously only clicks animated, leaving stale
    // enlarged cards that looked multi-selected.
    public void SetSelected(bool selected)
    {
        isSelected = selected;
        UpdateVisuals();   // owns the scale too, so programmatic deselection shrinks
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

    // Built at runtime into the scene's empty GameOverPanel; see BuildGameOverContents.
    private TextMeshProUGUI gameOverTitle, gameOverResult;
    private GameObject modalScrim;

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
    private TextMeshProUGUI versionText;
    private TextMeshProUGUI humanScoreLine;
    private TextMeshProUGUI aiScoreLine;
    private Transform canvasTransform;
    private bool lastHumanTurn;

    // Screen size the current layout was built for. Drives the resize check in
    // Update; zero forces a pass on the first frame.
    private Vector2Int layoutScreen;

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

    // Builds selected as sweep material or (when exactly one) for raise/add
    private readonly List<Build> selectedBuilds = new();
    private readonly List<GameObject> selectedBuildRoots = new();
    private Build selectedBuild => selectedBuilds.Count == 1 ? selectedBuilds[0] : null;
    
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
                restartButton = gameOverPanel.GetComponentInChildren<Button>();

            // Hide the panel after finding the button
            gameOverPanel.SetActive(false);
        }

        // Add listener to restart button after it's been found
        if (restartButton != null)
        {
            // Clear any existing listeners first to avoid duplicates
            restartButton.onClick.RemoveAllListeners();
            restartButton.onClick.AddListener(OnRestartClicked);
        }
        else
        {
            Debug.LogWarning("Restart button is null - cannot add listener!");
        }

        CreateRuntimeUI();
        if (!TitleSuppressed) CreateTitleScreen();
        StartCoroutine(WaitAndRefresh());
    }

    // --- Title screen -------------------------------------------------
    //
    // The game used to open mid-deal: no name, no ruleset, no moment before the
    // first card. This is that moment. It is a full-canvas overlay rather than a
    // second scene, because UIManager builds the whole UI in code and another
    // scene would need its own canvas, camera and wiring for one static screen.
    //
    // GameManager still deals on Start exactly as it always has, and the overlay
    // simply covers the result. Nothing is gated: deferring InitializeGame would
    // leave deck/dealer/nonDealer null while RefreshUI runs, and the deal is the
    // only thing the player misses behind the title. So AnimateDeal defers
    // instead, and Deal replays it. That animation is pure ghost cards over an
    // already-dealt board, so replaying it changes no game state.

    // Set by the harnesses, which drive the board directly and must not have to
    // click through an opening screen.
    public static bool SkipTitle;

    // Same idiom as auto-verify.flag / screenshot.flag, but NOT consumed: an
    // unattended verify loop wants the board in every run, not just the first.
    private static bool TitleSuppressed
    {
        get
        {
            if (SkipTitle) return true;
            try
            {
                return System.IO.File.Exists(System.IO.Path.Combine(
                    Application.dataPath, "..", "skip-title.flag"));
            }
            catch { return false; }
        }
    }

    private GameObject titleScreen;
    private Vector3Int pendingDeal = Vector3Int.zero;

    public bool TitleIsUp => titleScreen != null && titleScreen.activeSelf;

    public void DismissTitle()
    {
        if (titleScreen == null || !titleScreen.activeSelf) return;
        titleScreen.SetActive(false);

        if (pendingDeal != Vector3Int.zero)
        {
            var d = pendingDeal;
            pendingDeal = Vector3Int.zero;
            StartCoroutine(DealSequence(d.x, d.y, d.z));
        }
        RefreshUI();
    }

    private void CreateTitleScreen()
    {
        titleScreen = new GameObject("TitleScreen");
        titleScreen.transform.SetParent(canvasTransform, false);
        var root = titleScreen.AddComponent<RectTransform>();
        root.anchorMin = Vector2.zero;
        root.anchorMax = Vector2.one;
        root.offsetMin = root.offsetMax = Vector2.zero;

        // Opaque, and made of the felt itself rather than a wash over the board.
        // A translucent veil was the first attempt and it failed twice over: at
        // 0.90 the dealt cards still read through as grey card-shaped patches
        // (the same mistake the round summary made), and anything the player can
        // half-see before the game starts is a distraction, not depth.
        var veil = titleScreen.AddComponent<Image>();
        veil.sprite = CasinoArt.Felt();
        veil.color = veil.sprite != null ? Color.white : CasinoTheme.TitleVeil;
        veil.raycastTarget = true;   // swallow clicks aimed at the board behind

        var grain = new GameObject("TitleGrain");
        grain.transform.SetParent(titleScreen.transform, false);
        var gr = grain.AddComponent<RectTransform>();
        gr.anchorMin = Vector2.zero;
        gr.anchorMax = Vector2.one;
        gr.offsetMin = gr.offsetMax = Vector2.zero;
        var grainImage = grain.AddComponent<Image>();
        grainImage.sprite = CasinoArt.FeltGrain();
        grainImage.type = Image.Type.Tiled;
        // Half strength, unlike the board's. The grain was tuned to sit under a
        // full board of cards and panels; across an empty screen the same
        // amount reads as static rather than cloth.
        grainImage.color = new Color(1f, 1f, 1f, 0.5f);
        grainImage.raycastTarget = false;

        // The same brass rail the board wears, so the title is the table with
        // nothing on it yet rather than a separate screen.
        var rail = new GameObject("TitleRail");
        rail.transform.SetParent(titleScreen.transform, false);
        var rr = rail.AddComponent<RectTransform>();
        rr.anchorMin = Vector2.zero;
        rr.anchorMax = Vector2.one;
        rr.offsetMin = new Vector2(8, 8);
        rr.offsetMax = new Vector2(-8, -8);
        var railImage = rail.AddComponent<Image>();
        railImage.sprite = CasinoArt.Rail();
        railImage.type = Image.Type.Sliced;
        railImage.raycastTarget = false;

        // Everything below is centred and under 600 units wide, which fits both
        // the 1280x720 landscape references and the 720-wide portrait one, so the
        // title needs no per-profile Zone.

        var wordmark = CreateText("Wordmark", titleScreen.transform);
        wordmark.text = "CASINO";
        wordmark.fontSize = 78;
        // TMP counts tracking after the last glyph too, so wide spacing drifts the
        // optical centre left by half a step. The +9 puts it back.
        wordmark.characterSpacing = 18;
        wordmark.alignment = TextAlignmentOptions.Center;
        wordmark.color = CasinoTheme.Headline;
        CasinoType.ApplySerif(wordmark);
        Pin(wordmark.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(9, 92), new Vector2(600, 104));

        var rule = new GameObject("TitleRule");
        rule.transform.SetParent(titleScreen.transform, false);
        var ruleImage = rule.AddComponent<Image>();
        ruleImage.color = CasinoTheme.TitleRule;
        ruleImage.raycastTarget = false;
        Pin(rule.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), new Vector2(0, 44), new Vector2(280, 1.5f));

        // Which ruleset you are about to play is not cosmetic here: the scene
        // ships Rick's New England in the "Custom" slot, and the point totals
        // differ enough between variants to change how a hand is played.
        var ruleset = CreateText("Ruleset", titleScreen.transform);
        ruleset.text = RulesetLine();
        ruleset.fontSize = 17;
        ruleset.alignment = TextAlignmentOptions.Center;
        ruleset.color = CasinoTheme.TextMuted;
        Pin(ruleset.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0, 16), new Vector2(560, 28));

        var deal = new GameObject("DealButton");
        deal.transform.SetParent(titleScreen.transform, false);
        deal.AddComponent<RectTransform>();
        Surface(deal.AddComponent<Image>(), 6, CasinoTheme.ButtonPrimary, CasinoTheme.ButtonPrimaryBorder);
        Pin(deal.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), new Vector2(0, -50), new Vector2(200, 54));
        deal.AddComponent<Button>().onClick.AddListener(DismissTitle);

        var dealLabel = CreateText("Label", deal.transform);
        dealLabel.text = "Deal";
        dealLabel.fontSize = 22;
        dealLabel.alignment = TextAlignmentOptions.Center;
        dealLabel.color = CasinoTheme.ButtonPrimaryLabel;
        CasinoType.ApplySerif(dealLabel);
        var dl = dealLabel.rectTransform;
        dl.anchorMin = Vector2.zero;
        dl.anchorMax = Vector2.one;
        dl.offsetMin = dl.offsetMax = Vector2.zero;

        // The three plays, stated once. A player who has never met Casino has no
        // way to guess that trailing is a move rather than a forfeit.
        var plays = CreateText("Plays", titleScreen.transform);
        plays.text = "SWEEP     TRAIL     BUILD";
        plays.fontSize = 13;
        plays.characterSpacing = 8;
        plays.alignment = TextAlignmentOptions.Center;
        // TextFaint is the version-stamp weight and vanished against the felt.
        plays.color = CasinoTheme.TitlePlays;
        Pin(plays.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(4, -112), new Vector2(560, 22));

        // The board's version stamp is behind the title, and a screenshot that
        // cannot name its own build is the thing CLAUDE.md exists to prevent.
        var version = CreateText("TitleVersion", titleScreen.transform);
        version.text = $"v{Application.version}";
        version.fontSize = 12;
        version.alignment = TextAlignmentOptions.BottomRight;
        version.color = CasinoTheme.TextFaint;
        Pin(version.rectTransform, new Vector2(1f, 0f), new Vector2(-10, 4), new Vector2(120, 18));
    }

    private static string RulesetLine()
    {
        var s = ScoringManager.Instance;
        return s == null
            ? "The fishing card game"
            : $"{s.CurrentVariant}   ·   first to {s.WinScore}";
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
        feltImage.sprite = CasinoArt.Felt();
        // White tint lets the sprite's own gradient through; the flat theme color
        // is only a fallback for when sprite generation fails.
        feltImage.color = feltImage.sprite != null ? Color.white : CasinoTheme.TableFelt;
        feltImage.raycastTarget = false;

        // Fabric grain over the felt, tiled so it stays sharp at any screen size.
        GameObject grain = new("TableGrain");
        grain.transform.SetParent(canvasTransform, false);
        grain.transform.SetSiblingIndex(1);
        var grainRect = grain.AddComponent<RectTransform>();
        grainRect.anchorMin = Vector2.zero;
        grainRect.anchorMax = Vector2.one;
        grainRect.offsetMin = grainRect.offsetMax = Vector2.zero;
        var grainImage = grain.AddComponent<Image>();
        grainImage.sprite = CasinoArt.FeltGrain();
        grainImage.type = Image.Type.Tiled;
        grainImage.color = Color.white;
        grainImage.raycastTarget = false;

        // Brass rail: the inset frame that makes the board read as a table with
        // edges rather than a coloured window. 9-sliced so it stays a hairline.
        GameObject rail = new("TableRail");
        rail.transform.SetParent(canvasTransform, false);
        rail.transform.SetSiblingIndex(1);
        var railRect = rail.AddComponent<RectTransform>();
        railRect.anchorMin = Vector2.zero;
        railRect.anchorMax = Vector2.one;
        railRect.offsetMin = new Vector2(8, 8);
        railRect.offsetMax = new Vector2(-8, -8);
        var railImage = rail.AddComponent<Image>();
        railImage.sprite = CasinoArt.Rail();
        railImage.type = Image.Type.Sliced;
        railImage.color = Color.white;
        railImage.raycastTarget = false;

        // The runtime score panel replaces the two floating score texts
        if (dealerScoreText != null) dealerScoreText.gameObject.SetActive(false);
        if (nonDealerScoreText != null) nonDealerScoreText.gameObject.SetActive(false);

        // Positions are not set here: the buttons live in a contextual row above
        // the hand, and UpdateActionButtons lays out whichever ones are visible.
        sweepButton = CreateActionButton("SweepButton", "Sweep", out sweepButtonLabel);
        Surface(sweepButton.GetComponent<Image>(), 5, CasinoTheme.ButtonPrimary, CasinoTheme.ButtonPrimaryBorder);
        sweepButtonLabel.color = CasinoTheme.ButtonPrimaryLabel;
        sweepButton.onClick.AddListener(OnSweepClicked);

        trailButton = CreateActionButton("TrailButton", "Trail", out trailButtonLabel);
        trailButton.onClick.AddListener(OnTrailClicked);

        buildButton = CreateActionButton("BuildButton", "Build", out buildButtonLabel);
        buildButton.onClick.AddListener(OnBuildClicked);

        // Suggest is an afterthought, not a move: a circled "?" in the corner,
        // outside the action row. EnforceLayout places and sizes it per profile.
        suggestButton = CreateActionButton("SuggestButton", "?", out suggestButtonLabel);
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
        CasinoType.ApplySerif(hintText);
        hintText.fontStyle = FontStyles.Italic;
        hintText.color = CasinoTheme.HintText;
        hintText.text = "";

        versionText = CreateText("VersionText", canvasTransform);
        var verRect = versionText.rectTransform;
        verRect.anchorMin = new Vector2(1, 0);
        verRect.anchorMax = new Vector2(1, 0);
        verRect.pivot = new Vector2(1, 0);
        verRect.anchoredPosition = new Vector2(-10, 4);
        verRect.sizeDelta = new Vector2(120, 18);
        versionText.fontSize = 10;
        versionText.alignment = TextAlignmentOptions.BottomRight;
        versionText.color = CasinoTheme.TextFaint;
        versionText.text = $"v{Application.version}";

        CreateScoreLines();
        CreateAceRows();
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
        // 9-sliced sprites divide their border by (sprite PPU / canvas reference
        // PPU). CasinoArt builds sprites at 100 PPU, so if the canvas disagrees
        // the borders collapse toward zero, sliced degenerates to the stretched
        // centre, and every rounded corner renders square. That cost an afternoon.
        scaler.referencePixelsPerUnit = 100f;
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;

        // Every number below comes from the profile, so a new screen shape is a
        // new Profile in CasinoLayout rather than an edit here.
        var L = CasinoLayout.Pick(Screen.width, Screen.height);
        scaler.referenceResolution = L.Reference;
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = L.Match;
        layoutScreen = new Vector2Int(Screen.width, Screen.height);

        // Reparent the four card containers directly under the canvas
        var keep = new HashSet<GameObject>();

        keep.Add(Place(dealerHandContainer, L.OpponentHand));
        keep.Add(Place(nonDealerHandContainer, L.PlayerHand));
        // Builds render inline in the table row; no separate builds area
        keep.Add(Place(tableCardsContainer, L.Table));

        EnsureRowLayout(dealerHandContainer);
        EnsureRowLayout(nonDealerHandContainer);
        EnsureRowLayout(tableCardsContainer);

        // The scene's generic Play button is replaced by the explicit
        // Sweep / Trail / Build actions - leave it out of keep so it hides.

        keep.Add(PlaceText(currentPlayerText, L.TurnText, 14));
        keep.Add(PlaceText(gameStatusText, L.StatusText, 13));
        if (deckCountText != null) deckCountText.gameObject.SetActive(false);

        // Runtime-created furniture. These used to keep whatever position their
        // constructor gave them, which is why the score panel and the action
        // rail drifted apart on a wide canvas: nothing owned them after creation.
        PlaceByName("ScoreHuman", L.PlayerScore);
        PlaceByName("ScoreAI", L.AiScore);
        PlaceByName("AcesHuman", L.PlayerAces);
        PlaceByName("AcesAI", L.AiAces);
        PlaceByName("HumanPile", L.PlayerPile);
        PlaceByName("AIPile", L.AiPile);
        PlaceDrawPile();
        PlaceByName("HintText", L.Hint);
        PlaceByName("VersionText", L.Version);

        // Move buttons are not placed here: which ones exist depends on the
        // current selection, so UpdateActionButtons owns their row layout.
        // Suggest is static furniture: a quiet circled "?" in the corner,
        // radius half the height so the rounded rect renders as a circle.
        PlaceByName("SuggestButton", L.Suggest);
        if (suggestButton != null)
        {
            Surface(suggestButton.GetComponent<Image>(), (int)(L.Suggest.Size.y / 2f),
                CasinoTheme.PileButton, CasinoTheme.ButtonBorder);
            suggestButtonLabel.text = "?";
            suggestButtonLabel.fontSize = L.Suggest.Size.y * 0.5f;
            suggestButtonLabel.color = CasinoTheme.TextMuted;
        }

        // Game over panel: centered card
        if (gameOverPanel != null)
        {
            keep.Add(Place(gameOverPanel.transform, L.GameOver));
            var img = gameOverPanel.GetComponent<Image>();
            if (img != null) Surface(img, 8, CasinoTheme.GameOverPanel, CasinoTheme.PanelBorder);
            BuildGameOverContents();
        }

        // Our runtime objects stay too
        foreach (Transform child in canvasTransform)
        {
            if (child.name == "TableFelt" || child.name == "TableGrain" || child.name == "TableRail" ||
                child.name == "ScoreHuman" || child.name == "ScoreAI" ||
                child.name == "AcesHuman" || child.name == "AcesAI" ||
                child.name == "BuildButton" || child.name == "SuggestButton" ||
                child.name == "TrailButton" || child.name == "SweepButton" ||
                child.name == "HintText" || child.name == "HumanPile" ||
                child.name == "AIPile" || child.name == "CapturedPanel" ||
                child.name == "DrawPile" || child.name == "ScoreSummary" ||
                child.name == "MoveBanner" || child.name == "VersionText" ||
                child.name == "ModalScrim" || child.name == "TitleScreen")
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
        text.color = CasinoTheme.TextMuted;
        return go;
    }

    // --- Profile-driven placement -----------------------------------
    // Same job as ReAnchor, but taking a Zone so the caller never spells out
    // coordinates. Anchor and pivot are deliberately equal: a Zone reads as
    // "this corner, this far in, this big", which is what layout-report shows.

    // Cards come from a prefab whose size is baked in, so every instantiation
    // site has to be told the profile's size or portrait keeps desktop cards.
    // Build minis are 0.7 of a full card, which is the 56x80-to-80x120 ratio the
    // stack fan was originally drawn at.
    // Give a flat Image the Parlor surface treatment: rounded, hairline brass
    // border, colour baked into the sprite so the Image itself stays untinted.
    private static void Surface(Image img, int radius, Color fill, Color stroke, float strokeWidth = 1f)
    {
        if (img == null) return;
        img.sprite = CasinoArt.Panel(radius, fill, stroke, strokeWidth);
        img.type = Image.Type.Sliced;
        img.color = Color.white;
    }

    private static Vector2 CardSize(float scale = 1f) => CasinoLayout.Active.CardSize * scale;

    private static void SizeCard(GameObject card, float scale = 1f)
    {
        var r = card != null ? card.GetComponent<RectTransform>() : null;
        if (r != null) r.sizeDelta = CardSize(scale);
    }

    private GameObject Place(Transform t, CasinoLayout.Zone z) =>
        ReAnchor(t, z.Anchor, z.Pos, z.Size);

    private GameObject PlaceText(TextMeshProUGUI text, CasinoLayout.Zone z, float fontSize)
    {
        if (text == null) return null;
        var go = Place(text.transform, z);
        text.fontSize = fontSize;
        text.alignment = TextAlignmentOptions.BottomLeft;
        text.color = CasinoTheme.TextMuted;
        return go;
    }

    // The runtime furniture is found by name rather than held in fields, because
    // that is already how EnforceLayout decides what to keep. Missing is fine:
    // a layout pass can run before every piece exists.
    private void PlaceByName(string name, CasinoLayout.Zone z)
    {
        var child = canvasTransform.Find(name);
        if (child != null) Place(child, z);
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
        layout.spacing = CasinoLayout.Active.RowSpacing;
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childControlWidth = false;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;
    }


    private Button CreateActionButton(string name, string label, out TextMeshProUGUI labelText)
    {
        GameObject go = new(name);
        go.transform.SetParent(canvasTransform, false);
        var rect = go.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0);
        rect.anchorMax = new Vector2(0.5f, 0);
        rect.pivot = new Vector2(0, 0);
        rect.sizeDelta = new Vector2(160, 48);   // placeholder; the row layout sizes it

        var image = go.AddComponent<Image>();
        Surface(image, 5, CasinoTheme.ButtonSecondary, CasinoTheme.ButtonBorder);

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
        labelText.color = CasinoTheme.ButtonLabel;
        CasinoType.ApplySerif(labelText);

        return button;
    }

    // The scene's GameOverPanel ships as an empty box with a stock Button in it.
    // The first full autoplay run ended on a panel that said nothing at all: no
    // title, no winner, no final score, with the result exiled to 13pt status
    // text in the bottom-left corner. This gives the panel the three things the
    // end of a game has to state, and drags the stock button into the theme.
    //
    // Built here rather than in the scene because UIManager owns the whole layout
    // in code; anything hand-placed in Scene.unity gets overwritten at runtime.
    private void BuildGameOverContents()
    {
        var panel = gameOverPanel.transform;

        if (gameOverTitle == null)
        {
            gameOverTitle = CreateText("Title", panel);
            gameOverTitle.fontSize = 30;
            gameOverTitle.fontStyle = FontStyles.Bold;
            gameOverTitle.alignment = TextAlignmentOptions.Center;
            gameOverTitle.color = CasinoTheme.Headline;
            CasinoType.ApplySerif(gameOverTitle);
            gameOverTitle.text = "Game Over";
        }
        Pin(gameOverTitle.rectTransform, new Vector2(0.5f, 1f), new Vector2(0, -22), new Vector2(-32, 38));

        if (gameOverResult == null)
        {
            gameOverResult = CreateText("Result", panel);
            gameOverResult.fontSize = 16;
            gameOverResult.alignment = TextAlignmentOptions.Center;
            gameOverResult.color = CasinoTheme.TextPrimary;
            gameOverResult.richText = true;
        }
        Pin(gameOverResult.rectTransform, new Vector2(0.5f, 1f), new Vector2(0, -68), new Vector2(-40, 96));

        // The stock button is the only surface in the game that never got a
        // Parlor treatment; it was still default white.
        if (restartButton != null)
        {
            Pin(restartButton.GetComponent<RectTransform>(), new Vector2(0.5f, 0f), new Vector2(0, 24), new Vector2(180, 44));
            var bimg = restartButton.GetComponent<Image>();
            if (bimg != null) Surface(bimg, 6, CasinoTheme.ButtonPrimary, CasinoTheme.ButtonPrimaryBorder);
            var blabel = restartButton.GetComponentInChildren<TextMeshProUGUI>();
            if (blabel != null)
            {
                blabel.text = "Play again";
                blabel.color = CasinoTheme.ButtonPrimaryLabel;
                blabel.fontSize = 16;
                blabel.alignment = TextAlignmentOptions.Center;
            }
        }
    }

    // Modals need two things, and I fixed the wrong one twice before getting here.
    //
    // Sibling order is the first: paint order on a canvas is sibling order, and
    // both modals are created long before the cards and animation ghosts that
    // have to sit behind them, so they must re-raise whenever anything new is
    // parented to the canvas.
    //
    // Opacity is the second, and it was the actual cause of cards appearing
    // "over" the summary. The panel is 0.97 alpha, and a white card under a
    // near-opaque dark panel still reads as a grey card-shaped smudge. Sorting
    // was never going to fix something that was already behind. A scrim covers
    // the board outright, and blocks clicks to a board that must not accept them
    // while a modal is up.
    private void RaiseModals()
    {
        bool open = (summaryPanel != null && summaryPanel.activeSelf)
                 || (gameOverPanel != null && gameOverPanel.activeSelf);

        EnsureScrim();
        modalScrim.SetActive(open);

        if (open)
        {
            // Scrim first, then the panels, so the panels land above it.
            modalScrim.transform.SetAsLastSibling();
            if (summaryPanel != null && summaryPanel.activeSelf)
                summaryPanel.transform.SetAsLastSibling();
            if (gameOverPanel != null && gameOverPanel.activeSelf)
                gameOverPanel.transform.SetAsLastSibling();
        }

        // Last of all: the title outranks every modal. RefreshUI can raise a
        // panel while the title is still up, and nothing behind it is reachable.
        if (TitleIsUp) titleScreen.transform.SetAsLastSibling();
    }

    private void EnsureScrim()
    {
        if (modalScrim != null) return;

        modalScrim = new GameObject("ModalScrim");
        modalScrim.transform.SetParent(canvasTransform, false);
        var r = modalScrim.AddComponent<RectTransform>();
        r.anchorMin = Vector2.zero;
        r.anchorMax = Vector2.one;
        r.offsetMin = r.offsetMax = Vector2.zero;

        var img = modalScrim.AddComponent<Image>();
        img.color = CasinoTheme.ModalScrim;
        img.raycastTarget = true;      // swallow clicks aimed at the board behind
        modalScrim.SetActive(false);
    }

    // anchor == pivot, so the offset reads as "this far in from that edge",
    // matching how CasinoLayout.Zone describes everything else.
    private static void Pin(RectTransform r, Vector2 anchor, Vector2 pos, Vector2 size)
    {
        if (r == null) return;
        r.anchorMin = r.anchorMax = r.pivot = anchor;
        // A stretched width is expressed as a negative sizeDelta, which only
        // works when the anchors are separated; keep those cases stretched.
        if (size.x < 0)
        {
            r.anchorMin = new Vector2(0, anchor.y);
            r.anchorMax = new Vector2(1, anchor.y);
            r.pivot = new Vector2(0.5f, anchor.y);
        }
        r.anchoredPosition = pos;
        r.sizeDelta = size;
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
        Surface(bg, 8, CasinoTheme.PileViewerPanel, CasinoTheme.PanelBorder);

        capturedTitle = CreateText("Title", capturedPanel.transform);
        var tr = capturedTitle.rectTransform;
        tr.anchorMin = new Vector2(0, 0.88f);
        tr.anchorMax = Vector2.one;
        tr.offsetMin = new Vector2(10, 0);
        tr.offsetMax = new Vector2(-10, -4);
        capturedTitle.fontSize = 16;
        capturedTitle.fontStyle = FontStyles.Bold;
        capturedTitle.alignment = TextAlignmentOptions.Center;
        capturedTitle.color = CasinoTheme.TextPrimary;

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

        // The count sits ON the deck, big, not in a caption under it: the
        // pile is its own label. UpdateDrawPile keeps this child on top of
        // the card-back layers it rebuilds.
        drawPileLabel = CreateText("Count", drawPile.transform);
        var lr = drawPileLabel.rectTransform;
        lr.anchorMin = Vector2.zero;
        lr.anchorMax = Vector2.one;
        lr.offsetMin = lr.offsetMax = Vector2.zero;
        drawPileLabel.fontSize = 36;
        drawPileLabel.fontStyle = FontStyles.Bold;
        CasinoType.ApplySerif(drawPileLabel);
        drawPileLabel.alignment = TextAlignmentOptions.Center;
        drawPileLabel.color = CasinoTheme.Palette.Ivory;
        drawPileLabel.raycastTarget = false;
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

        // An empty deck draws no layers, so a number would float on felt;
        // the pile vanishing is the message.
        drawPileLabel.text = remaining > 0 ? remaining.ToString() : "";
    }

    // Deal animation: face-down ghosts fly from the draw pile to each hand
    // (and the table on the opening deal), staggered like real dealing.
    public void AnimateDeal(int toNonDealer, int toDealer, int toTable)
    {
        // Behind the title the whole animation is invisible, and it is the one
        // thing worth seeing before the first move. Hold it for the Deal button.
        if (TitleIsUp)
        {
            pendingDeal = new Vector3Int(toNonDealer, toDealer, toTable);
            return;
        }
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
        Surface(img, 4, CasinoTheme.PileButton, CasinoTheme.PileBorder);
        var btn = go.AddComponent<Button>();

        label = CreateText("Label", go.transform);
        var lr = label.rectTransform;
        lr.anchorMin = Vector2.zero;
        lr.anchorMax = Vector2.one;
        lr.offsetMin = new Vector2(8, 0);
        lr.offsetMax = new Vector2(-8, 0);
        label.fontSize = 13;
        label.alignment = TextAlignmentOptions.Left;
        label.color = CasinoTheme.TextPrimary;
        return btn;
    }

    // ---- Verification surface -------------------------------------------
    // Everything below exists so an unattended run can drive the UI the way a
    // player does. CasinoAutoPlay deliberately calls GameManager directly, which
    // exercises the rules but skips this layer entirely: selection, highlighting,
    // the hint line, and the refusal messages are all UIManager's, and none of
    // them were reachable without a human at the keyboard.

    public void TogglePileViewer(bool human) => TogglePile(human);

    public IReadOnlyList<CardUI> HumanHandCardUIs => nonDealerCardUIs;
    public IReadOnlyList<CardUI> TableCardUIs => tableCardUIs;

    public void PressSuggest() => OnSuggestClicked();

    // Presses Build the way the UI allows, and reports whether the press was
    // possible at all. Calling OnBuildClicked directly is what an earlier probe
    // did, and it walks straight past the interactable gate, which is not an
    // implementation detail here: a disabled button IS the refusal. Driving the
    // handler produced a "silent refusal" bug report for a state no player can
    // reach by clicking.
    public bool PressBuild()
    {
        if (buildButton == null || !buildButton.gameObject.activeInHierarchy || !buildButton.interactable) return false;
        OnBuildClicked();
        return true;
    }

    // The action buttons carry most of the game's feedback in their labels, not
    // in the hint line: "Not a build", "No 9 in hand", "Face builds: one rank",
    // "Locked (multi)", "Raise to 8". A probe that reads only the hint misses
    // the channel the player is actually looking at.
    public string BuildButtonState => ButtonState(buildButton, buildButtonLabel);
    public string SweepButtonState => ButtonState(sweepButton, sweepButtonLabel);
    public string TrailButtonState => ButtonState(trailButton, trailButtonLabel);

    // Visibility is now part of the state: a button that does not apply to the
    // current selection is not on screen at all, and a probe needs to see that.
    private static string ButtonState(Button b, TextMeshProUGUI label) =>
        b == null ? "(missing)"
        : !b.gameObject.activeSelf ? "hidden"
        : $"\"{(label != null ? label.text : "?")}\" {(b.interactable ? "enabled" : "disabled")}";

    // The hint line is the game's whole explanatory voice: what a selection can
    // take, why a build was refused, what the evaluator would do. Reading it back
    // is how a probe asserts the UI said the right thing.
    public string CurrentHint => hintText != null ? hintText.text : "";
    // ---------------------------------------------------------------------

    private void TogglePile(bool human)
    {
        if (capturedPanel.activeSelf && pileShowsHuman == human)
        {
            capturedPanel.SetActive(false);
            SetLeftEdgeVisible(true);
            return;
        }
        pileShowsHuman = human;
        pileShownCount = -1;
        capturedPanel.SetActive(true);
        SetLeftEdgeVisible(false);
        RebuildPilePanel();
    }

    // The open pile viewer occupies the left edge; the draw pile and status
    // texts that live there step aside while it is open.
    private void SetLeftEdgeVisible(bool visible)
    {
        if (drawPile != null) drawPile.SetActive(visible);
        if (currentPlayerText != null) currentPlayerText.gameObject.SetActive(visible);
        if (gameStatusText != null) gameStatusText.gameObject.SetActive(visible);
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

    public bool IsSummaryOpen => summaryPanel != null && summaryPanel.activeSelf;

    // What the Continue button does. Split out so automation can advance a round
    // without synthesising a click.
    public void ContinueSummary()
    {
        if (summaryPanel != null) summaryPanel.SetActive(false);
        RaiseModals();   // drops the scrim once nothing is open
        var action = summaryContinue;
        summaryContinue = null;
        action?.Invoke();
    }

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

        // The prompt is wider than the panel, so it stuck out either side of an
        // open summary reading "Your turn. Select a card, t...nd Build."
        if (hintText != null) hintText.text = "";

        RaiseModals();
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
        Surface(bg, 8, CasinoTheme.RoundSummaryPanel, CasinoTheme.PanelBorder);

        summaryTitle = CreateText("Title", summaryPanel.transform);
        var tr = summaryTitle.rectTransform;
        tr.anchorMin = new Vector2(0, 0.88f);
        tr.anchorMax = Vector2.one;
        tr.offsetMin = new Vector2(12, 0);
        tr.offsetMax = new Vector2(-12, -6);
        summaryTitle.fontSize = 18;
        summaryTitle.fontStyle = FontStyles.Bold;
        summaryTitle.alignment = TextAlignmentOptions.Center;
        CasinoType.ApplySerif(summaryTitle);
        summaryTitle.color = CasinoTheme.Headline;

        summaryLeft = CreateText("Left", summaryPanel.transform);
        var lr = summaryLeft.rectTransform;
        lr.anchorMin = new Vector2(0.04f, 0.2f);
        lr.anchorMax = new Vector2(0.48f, 0.86f);
        lr.offsetMin = lr.offsetMax = Vector2.zero;
        summaryLeft.fontSize = 15;
        summaryLeft.alignment = TextAlignmentOptions.TopLeft;
        summaryLeft.color = CasinoTheme.PlayerAccent;

        summaryRight = CreateText("Right", summaryPanel.transform);
        var rr = summaryRight.rectTransform;
        rr.anchorMin = new Vector2(0.52f, 0.2f);
        rr.anchorMax = new Vector2(0.96f, 0.86f);
        rr.offsetMin = rr.offsetMax = Vector2.zero;
        summaryRight.fontSize = 15;
        summaryRight.alignment = TextAlignmentOptions.TopLeft;
        summaryRight.color = CasinoTheme.OpponentAccent;

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
        Surface(img, 5, CasinoTheme.ButtonPrimary, CasinoTheme.ButtonPrimaryBorder);
        var btn = go.AddComponent<Button>();
        btn.onClick.AddListener(ContinueSummary);

        var label = CreateText("Label", go.transform);
        var lbr = label.rectTransform;
        lbr.anchorMin = Vector2.zero;
        lbr.anchorMax = Vector2.one;
        lbr.offsetMin = lbr.offsetMax = Vector2.zero;
        label.text = "Continue";
        label.fontSize = 20;
        label.alignment = TextAlignmentOptions.Center;
        label.color = CasinoTheme.ButtonLabel;

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
        rect.sizeDelta = CardSize();

        var ui = ghost.GetComponent<CardUI>() ?? ghost.AddComponent<CardUI>();
        ui.Initialize(card, false);
        ui.SetFaceDown(faceDown);

        // A ghost parents to the canvas, so it lands as the last sibling and
        // paints over everything, including an open modal. Raising the modal when
        // it opens is not enough: deal and trail animations keep firing while the
        // summary is up, so each new ghost jumps back above it. Re-raise instead.
        RaiseModals();

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

    // The scoreboard panel is gone: the trophy coins carry the prizes, so
    // all that is left worth stating is the running score and the target.
    // One quiet line per side, sitting with that side's shelf of coins.
    private void CreateScoreLines()
    {
        humanScoreLine = CreateText("ScoreHuman", canvasTransform);
        humanScoreLine.alignment = TextAlignmentOptions.MidlineRight;
        humanScoreLine.fontSize = 15;
        CasinoType.ApplySerif(humanScoreLine);
        humanScoreLine.color = CasinoTheme.TextMuted;

        aiScoreLine = CreateText("ScoreAI", canvasTransform);
        aiScoreLine.alignment = TextAlignmentOptions.MidlineLeft;
        aiScoreLine.fontSize = 15;
        CasinoType.ApplySerif(aiScoreLine);
        aiScoreLine.color = CasinoTheme.TextMuted;
    }

    private RectTransform humanAcesRow, aiAcesRow;
    private readonly Dictionary<string, GameObject> humanTrophies = new();
    private readonly Dictionary<string, GameObject> aiTrophies = new();

    private static string SuitGlyph(PlayingCard.Suit s) => s switch
    {
        PlayingCard.Suit.Spades => "\u2660",
        PlayingCard.Suit.Hearts => "\u2665",
        PlayingCard.Suit.Diamonds => "\u2666",
        _ => "\u2663",
    };

    private static string RankLabel(PlayingCard.Rank r) => r switch
    {
        PlayingCard.Rank.Ace => "A",
        PlayingCard.Rank.Jack => "J",
        PlayingCard.Rank.Queen => "Q",
        PlayingCard.Rank.King => "K",
        _ => ((int)r + 1).ToString(),
    };

    // Trophy coins are event-driven, not slots: nothing renders until a
    // single-card prize is actually captured, and then the coin splashes in
    // on the capturer's side. Coins accumulate for the round in capture
    // order and clear when the piles do.
    private void CreateAceRows()
    {
        humanAcesRow = CreateAceRow("AcesHuman");
        aiAcesRow = CreateAceRow("AcesAI");
    }

    private RectTransform CreateAceRow(string name)
    {
        GameObject go = new(name);
        go.transform.SetParent(canvasTransform, false);
        var rect = go.AddComponent<RectTransform>();
        rect.sizeDelta = new Vector2(220, 64);   // EnforceLayout re-places it
        return rect;
    }

    private void UpdateAceRows(GamePlayer human, GamePlayer ai)
    {
        SyncTrophyRow(humanAcesRow, humanTrophies, human, rightAligned: true);
        SyncTrophyRow(aiAcesRow, aiTrophies, ai, rightAligned: false);
    }

    // The suit whose majority pays. Spades in every shipped preset, but the
    // coin takes whatever this returns, so a future variant can pay hearts
    // and the trophy follows the config, not the tradition.
    private static PlayingCard.Suit MajoritySuit => PlayingCard.Suit.Spades;

    // A standard deck: majorities lock at over half of these. The coin marks
    // the moment the prize is mathematically yours, not merely a lead.
    private const int DeckCards = 52, SuitCards = 13;

    // Which coins this player has earned, in capture order. The devices come
    // from the scoring config, never from a constant: whatever card a preset
    // declares as Big or Little Casino is what its coin is struck with, and
    // the majority coins follow MajoritySuit.
    private static List<(string key, string device, string mark)> Trophies(GamePlayer p)
    {
        var sm = ScoringManager.Instance;
        var list = new List<(string, string, string)>();
        foreach (var c in p.CapturedCards)
        {
            if (c.rank == PlayingCard.Rank.Ace)
                list.Add(($"A{c.suit}", "A", SuitGlyph(c.suit)));
            else if (sm != null && sm.PointsForBigCasino > 0 &&
                     c.rank == sm.BigCasinoRank && c.suit == sm.BigCasinoSuit)
                list.Add(("BIG", RankLabel(c.rank), SuitGlyph(c.suit)));
            else if (sm != null && sm.PointsForLittleCasino > 0 &&
                     c.rank == sm.LittleCasinoRank && c.suit == sm.LittleCasinoSuit)
                list.Add(("LITTLE", RankLabel(c.rank), SuitGlyph(c.suit)));
        }

        // Majorities: struck the moment more than half the deck (or suit) is
        // in the pile, because from there the other player cannot catch up.
        if (sm != null && sm.PointsForMostCards > 0 && p.CapturedCards.Count > DeckCards / 2)
            list.Add(("CARDS", "C", "♠♥\n♦♣"));
        if (sm != null && sm.PointsForMostSpades > 0 &&
            p.CapturedCards.Count(c => c.suit == MajoritySuit) > SuitCards / 2)
            list.Add(("SUIT", MajoritySuit.ToString().Substring(0, 1), SuitGlyph(MajoritySuit)));

        return list;
    }

    private void SyncTrophyRow(RectTransform row, Dictionary<string, GameObject> icons,
                               GamePlayer p, bool rightAligned)
    {
        if (row == null || p == null) return;

        var desired = Trophies(p);
        var keys = new HashSet<string>(desired.Select(t => t.key));

        // Round over, piles cleared: the trophies leave with them, no ceremony.
        foreach (var key in icons.Keys.Where(k => !keys.Contains(k)).ToList())
        {
            Destroy(icons[key]);
            icons.Remove(key);
        }

        bool added = false;
        foreach (var (key, device, mark) in desired)
        {
            if (icons.ContainsKey(key)) continue;
            var coin = CasinoCoin.Create(row, device, mark, glow: key == "BIG");
            coin.Splash();
            icons[key] = coin.gameObject;
            added = true;
        }

        if (added) ReflowTrophyRow(row, icons, desired, rightAligned);
    }

    // A tossed-on-the-felt cluster, capped at the screen edge: the human row
    // grows leftward from the corner, the AI row grows rightward, so a long
    // round never pushes a coin off the canvas.
    private void ReflowTrophyRow(RectTransform row,
                                 Dictionary<string, GameObject> icons,
                                 List<(string key, string device, string mark)> order,
                                 bool rightAligned)
    {
        int n = 0, i = 0;
        foreach (var (key, _, _) in order) if (icons.ContainsKey(key)) n++;
        float cap = row.sizeDelta.x / 2f - 26f;
        foreach (var (key, _, _) in order)
        {
            if (!icons.TryGetValue(key, out var go)) continue;
            float x = rightAligned ? cap - (n - 1 - i) * 50f : -cap + i * 50f;
            var r = (RectTransform)go.transform;
            r.anchoredPosition = new Vector2(x, (i % 2 == 0) ? 2f : -3f);
            r.localRotation = Quaternion.Euler(0, 0, (i % 2 == 0) ? -6f : 5f);
            i++;
        }
    }


    private System.Collections.IEnumerator WaitAndRefresh()
    {
        yield return _waitForSeconds0_1;
        RefreshUI();
    }
    
    private void Update()
    {
        // Paint order on a canvas is sibling order, and the title is created
        // before every card, ghost and build that RefreshUI spawns underneath
        // it, so a single SetAsLastSibling at creation does not hold. The first
        // run showed the score panel and the dealt hand drawing straight over
        // the title. Re-raising while it is up is one call on a handful of
        // frames, and it is the only thing that cannot get out of step.
        if (TitleIsUp) titleScreen.transform.SetAsLastSibling();

        // A window resize can cross a breakpoint, which changes the whole
        // arrangement, not just its scale. Re-run the layout when the screen
        // actually changes size; comparing two ints per frame is free, and
        // EnforceLayout is not cheap enough to run unconditionally.
        if (canvasTransform != null &&
            (Screen.width != layoutScreen.x || Screen.height != layoutScreen.y))
        {
            try { EnforceLayout(); }
            catch (System.Exception e) { Debug.LogError($"EnforceLayout on resize failed: {e}"); }
        }

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
                // Not once the game is over: the seat is still nominally the
                // human's, so without this the "Your turn" prompt sits under the
                // game-over panel inviting a move that cannot be made.
                if (humanTurn && hintText != null &&
                    GameManager.Instance.GetCurrentPhase() != GameManager.GamePhase.GameOver)
                    hintText.text = "Your turn. Select a card, then Play, or add table cards and Build.";
            }
        }
    }
    
    // Design-verification hook for CasinoStatePreview. Puts one card in each of
    // the four states side by side so they can be compared directly; an ordinary
    // board never shows more than one or two at once. Presentational only.
    public void ApplyStatePreview()
    {
        var hand = nonDealerCardUIs;   // human is non-dealer, bottom row
        if (hand == null || hand.Count < 4) return;
        hand[0].SetCapturable(true);
        hand[1].SetSuggested(true);
        hand[2].SetOpponentTaking(true);
        hand[3].SetSelected(true);
        if (hintText != null)
            hintText.text = "State preview:  capturable / suggested / opponent taking / selected";
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

        // The human's hand stays at the bottom no matter who is dealing;
        // the dealer swap must not flip the seats on screen.
        GamePlayer top = dealer.IsHuman() ? nonDealer : dealer;
        GamePlayer bottom = dealer.IsHuman() ? dealer : nonDealer;

        UpdateOneHand(top, dealerHandContainer, dealerCardUIs);
        UpdateOneHand(bottom, nonDealerHandContainer, nonDealerCardUIs);
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

        // Create new cards
        for (int i = 0; i < player.Hand.Count; i++)
        {
            GameObject cardObj = Instantiate(cardPrefab, container);
            SizeCard(cardObj);

            // Get existing CardUI component or add one if it doesn't exist
            CardUI cardUI = cardObj.GetComponent<CardUI>() ?? cardObj.AddComponent<CardUI>();

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
                SizeCard(cardObj);

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

        // Stacks are recreated below; stale selections would point at
        // destroyed roots
        DeselectBuild();

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
        hitArea.color = CasinoTheme.InvisibleHitArea;
        var rootButton = root.AddComponent<Button>();
        rootButton.transition = Selectable.Transition.None;
        rootButton.onClick.AddListener(() => OnBuildStackClicked(build, root));
        int n = build.Cards.Count;
        Vector2 miniSize = CardSize(0.7f);
        float step = miniSize.x * (16f / 56f);   // same overlap ratio the fan was drawn at
        rect.sizeDelta = new Vector2(miniSize.x + (n - 1) * step, miniSize.y + 26f);

        for (int i = 0; i < n; i++)
        {
            GameObject mini = Instantiate(cardPrefab, root.transform);
            CardUI cardUI = mini.GetComponent<CardUI>();
            if (cardUI == null) cardUI = mini.AddComponent<CardUI>();
            cardUI.Initialize(build.Cards[i], false);

            // The stack is one clickable unit: children must not swallow the
            // click before it reaches the root's button
            foreach (var g in mini.GetComponentsInChildren<Graphic>())
                g.raycastTarget = false;

            var r = mini.GetComponent<RectTransform>();
            r.anchorMin = r.anchorMax = new Vector2(0.5f, 0.5f);
            r.sizeDelta = miniSize;
            r.anchoredPosition = new Vector2(
                -(rect.sizeDelta.x - miniSize.x) / 2f + i * step, (i % 2) * -3f);
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
        // RoundedFill is white, so Image.color still carries the owner colour.
        // Radius half the badge size makes it a disc, as drawn.
        bi.sprite = CasinoArt.RoundedFill(15);
        bi.type = Image.Type.Sliced;
        bi.color = mine ? CasinoTheme.BuildOwnedByPlayer
                        : CasinoTheme.BuildOwnedByOpponent;
        bi.raycastTarget = false;

        var badgeText = CreateText("Value", badge.transform);
        var btr = badgeText.rectTransform;
        btr.anchorMin = Vector2.zero;
        btr.anchorMax = Vector2.one;
        btr.offsetMin = btr.offsetMax = Vector2.zero;
        badgeText.fontSize = 16;
        badgeText.fontStyle = FontStyles.Bold;
        badgeText.alignment = TextAlignmentOptions.Center;
        badgeText.color = CasinoTheme.BuildBadgeLabel;
        badgeText.raycastTarget = false;
        badgeText.text = build.DeclaredValue switch
        {
            11 => "J", 12 => "Q", 13 => "K", _ => build.DeclaredValue.ToString()
        };

        // A single build is malleable: raisable, and stealable by the opponent.
        // A multi-build is locked and can only be taken whole. Without this they
        // render identically and the rule is invisible on the board.
        GameObject tag = new("Lock");
        tag.transform.SetParent(root.transform, false);
        var tr = tag.AddComponent<RectTransform>();
        tr.anchorMin = tr.anchorMax = new Vector2(0.5f, 0);
        tr.pivot = new Vector2(0.5f, 0);
        tr.anchoredPosition = new Vector2(0, -3);
        tr.sizeDelta = new Vector2(62, 15);
        var ti = tag.AddComponent<Image>();
        Surface(ti, 3,
            build.IsMultiBuild ? CasinoTheme.BuildTagLocked : CasinoTheme.BuildTagRaisable,
            CasinoTheme.PileBorder);
        ti.raycastTarget = false;

        var tagText = CreateText("Text", tag.transform);
        var ttr = tagText.rectTransform;
        ttr.anchorMin = Vector2.zero;
        ttr.anchorMax = Vector2.one;
        ttr.offsetMin = ttr.offsetMax = Vector2.zero;
        tagText.fontSize = 9;
        tagText.characterSpacing = 6f;
        tagText.alignment = TextAlignmentOptions.Center;
        tagText.color = CasinoTheme.BuildTagLabel;
        tagText.raycastTarget = false;
        tagText.text = build.IsMultiBuild ? "LOCKED" : "RAISABLE";

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

                // The panel now states the result in full, so repeating it in the
                // corner just says the same thing twice, and said it in seat names
                // ("Non-Dealer Wins!") while the panel said "You win".
                gameStatusText.text = "";
                playCardButton.interactable = false;
                ShowGameOverResult(dealer, nonDealer, winner);
                if (hintText != null) hintText.text = "";
                if (gameOverPanel != null && !gameOverPanel.activeSelf)
                {
                    gameOverPanel.SetActive(true);
                    RaiseModals();
                }
            }
            else
            {
                gameStatusText.text = "Playing...";
                if (gameOverPanel != null && gameOverPanel.activeSelf)
                {
                    gameOverPanel.SetActive(false);
                    RaiseModals();
                }
            }
        }

        UpdateScorePanel(dealer, nonDealer);
    }

    // The deck sits beside whoever is dealing this deck, and follows the
    // deal when it swaps between rounds.
    private void PlaceDrawPile()
    {
        var L = CasinoLayout.Active;
        var d = GameManager.Instance != null ? GameManager.Instance.GetDealer() : null;
        PlaceByName("DrawPile", d != null && d.IsHuman() ? L.DrawPileHuman : L.DrawPileAi);
    }

    private void UpdateScorePanel(GamePlayer dealer, GamePlayer nonDealer)
    {
        if (ScoringManager.Instance == null) return;

        GamePlayer human = dealer.IsHuman() ? dealer : nonDealer;
        GamePlayer ai = dealer.IsHuman() ? nonDealer : dealer;

        // The banked totals; the coins beside them show the deck in progress.
        if (humanScoreLine != null)
        {
            int target = ScoringManager.Instance.WinScore;
            // <alpha> is a state switch in TMP, not a paired tag; nothing
            // follows the target, so it never needs switching back.
            humanScoreLine.text = $"YOU  <b>{human.Score}</b>  <alpha=#99>· first to {target}";
            aiScoreLine.text = $"AI  <b>{ai.Score}</b>";
        }

        UpdateAceRows(human, ai);
        PlaceDrawPile();

        if (humanPileLabel != null)
        {
            humanPileLabel.text = $"Your pile  {human.CapturedCards.Count}";
            aiPileLabel.text = $"AI pile  {ai.CapturedCards.Count}";
        }
        if (capturedPanel != null && capturedPanel.activeSelf)
            RebuildPilePanel();
    }

    // Who won, by how much, and against what target.
    //
    // The target is stated because a final score routinely overshoots it: a deck
    // pays out all its points at once (11 under Rick's New England), so crossing
    // 11 usually lands well above it. Do not try to explain the overshoot here.
    // An earlier version blamed it on a tied deck, which sounded plausible and
    // was simply false: the final state cannot distinguish a tie-extended game
    // from an ordinary one, and the first game it rendered on (13 to 9, two
    // decks, no tie) it was wrong.
    private void ShowGameOverResult(GamePlayer dealer, GamePlayer nonDealer, GamePlayer winner)
    {
        if (gameOverResult == null) return;

        GamePlayer human = dealer.IsHuman() ? dealer : nonDealer;
        GamePlayer ai = dealer.IsHuman() ? nonDealer : dealer;
        int target = ScoringManager.Instance != null ? ScoringManager.Instance.WinScore : 0;

        string you = ColorUtility.ToHtmlStringRGB(CasinoTheme.PlayerAccent);
        string them = ColorUtility.ToHtmlStringRGB(CasinoTheme.OpponentAccent);
        string headline = winner.IsHuman() ? "You win" : "The AI wins";

        gameOverTitle.text = headline;
        gameOverResult.text =
            $"<size=120%><color=#{you}>You {human.Score}</color>"
          + $"   <color=#{them}>AI {ai.Score}</color></size>"
          + $"\n\n<size=88%>First to {target}</size>";
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

    // Hand-first guidance: with a card picked up and nothing chosen on the table,
    // mark what that card could take.
    //
    // This is the maximum union, which is the honest answer to "what is
    // available" under these rules: the player still chooses which of those sets
    // to take, because sweeps here are chosen and partial rather than automatic.
    //
    // Deliberately sets no hint. UpdateActionButtons runs immediately after and
    // owns that line, with a more specific message ("Play takes: ..." or "Play
    // will trail ..."), so anything written here is overwritten within the same
    // frame. UpdateActionButtons already computes this exact capture set for the
    // Sweep button; it just never showed it on the table.
    private void ShowWhatThisCardTakes(PlayingCard card)
    {
        var gm = GameManager.Instance;
        if (gm == null || card == null) return;

        var table = gm.GetTableCards();
        if (table == null) return;

        var takeable = CaptureChecker.GetValidCaptures(card, table);
        foreach (var ui in tableCardUIs)
            if (ui != null)
                ui.SetCapturable(takeable.Contains(ui.Card));
    }

    // Table-first guidance: light up every hand card that can take exactly
    // the current table selection.
    private void HighlightCapturingHandCards()
    {
        var gm = GameManager.Instance;
        if (gm == null || !gm.IsWaitingForHumanInput()) return;

        var player = gm.GetCurrentPlayer();
        var handUIs = player.IsHuman() ? nonDealerCardUIs : dealerCardUIs; // human = bottom row
        var chosen = buildSelection.Select(ui => ui.Card).ToList();
        bool anySelection = chosen.Count > 0 || selectedBuilds.Count > 0;

        int takers = 0;
        foreach (var ui in handUIs)
        {
            if (ui == null) continue;
            bool canTake = anySelection && CaptureChecker.IsExactCaptureSetWithBuilds(
                ui.Card, player, chosen, selectedBuilds.ToList());
            ui.SetCapturable(canTake);
            if (canTake) takers++;
        }

        if (anySelection && hintText != null && selectedCard == null)
        {
            hintText.text = takers > 0
                ? $"{takers} card(s) in your hand can take this - they're highlighted."
                : "No card in your hand can take this selection.";
        }

        // The other direction. Stage 2 listed "which table cards a held card
        // could capture" as a state the rules require and the UI did not show,
        // and it stayed unshown because this method only ever ran table-to-hand.
        // Picking up a card and being told nothing is the commonest thing a
        // player does; CasinoInteractionProbe caught it saying nothing at all.
        if (!anySelection && selectedCard != null)
            ShowWhatThisCardTakes(selectedCard.Card);
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
        foreach (var root in selectedBuildRoots)
            if (root != null) root.transform.localScale = Vector3.one;
        selectedBuilds.Clear();
        selectedBuildRoots.Clear();
    }

    private void OnBuildStackClicked(Build build, GameObject root)
    {
        var gm = GameManager.Instance;
        if (gm == null || !gm.IsWaitingForHumanInput()) return;

        int i = selectedBuilds.IndexOf(build);
        if (i >= 0)
        {
            if (selectedBuildRoots[i] != null)
                selectedBuildRoots[i].transform.localScale = Vector3.one;
            selectedBuilds.RemoveAt(i);
            selectedBuildRoots.RemoveAt(i);
        }
        else
        {
            selectedBuilds.Add(build);
            selectedBuildRoots.Add(root);
            root.transform.localScale = Vector3.one * 1.1f;

            bool mine = build.Owner == gm.GetCurrentPlayer();
            if (build.IsMultiBuild)
                hintText.text = "Multi-build: locked. Takeable only at its value.";
            else if (mine)
                hintText.text = $"Your build of {build.DeclaredValue}: take it at value, raise it, or add to it.";
            else
                hintText.text = $"Build of {build.DeclaredValue}: take it, steal it in a sweep, raise, or add.";
        }

        HighlightCapturingHandCards();
        UpdateActionButtons();
    }

    private void ClearSuggestionHighlights()
    {
        foreach (var ui in dealerCardUIs) if (ui != null) ui.ClearHighlights();
        foreach (var ui in nonDealerCardUIs) if (ui != null) ui.ClearHighlights();
        foreach (var ui in tableCardUIs) if (ui != null) ui.ClearHighlights();
    }

    private void UpdateActionButtons()
    {
        if (buildButton == null) return;

        bool humanTurn = GameManager.Instance != null && GameManager.Instance.IsWaitingForHumanInput();

        // Trailing is a free choice unless you own a build
        bool ownsBuild = humanTurn &&
            GameManager.Instance.PlayerOwnsBuild(GameManager.Instance.GetCurrentPlayer());
        trailButton.interactable = humanTurn && selectedCard != null && !ownsBuild
                                   && buildSelection.Count == 0;
        trailButtonLabel.text = ownsBuild ? "Trail (own build)" : "Trail";

        // Sweep: takes the chosen cards/builds, or everything that applies
        bool canSweep = false;
        if (humanTurn && selectedCard != null)
        {
            var gm = GameManager.Instance;
            if (buildSelection.Count > 0 || selectedBuilds.Count > 0)
            {
                canSweep = CaptureChecker.IsExactCaptureSetWithBuilds(selectedCard.Card,
                    gm.GetCurrentPlayer(),
                    buildSelection.Select(ui => ui.Card).ToList(),
                    selectedBuilds.ToList());
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

        // A single selected build (no cards) turns the Build button into add
        // or raise; with cards alongside, the selection is sweep material
        if (humanTurn && selectedBuild != null && buildSelection.Count == 0)
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
            ApplyActionRow(humanTurn);
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

        ApplyActionRow(humanTurn);
        UpdatePlayPreview(humanTurn);
    }

    // Which buttons exist, in what order, and where.
    //
    // The old rail showed all four buttons all the time, mostly disabled, in a
    // corner the player's eye had no reason to visit. The row instead answers
    // "what can I do with what I just picked": nothing selected shows only
    // Suggest, and a selection summons the actions that apply to it, most
    // likely play first (leftmost), directly above the hand. Disabled buttons
    // still appear when their label carries the refusal ("No 9 in hand",
    // "Locked (multi)") - the refusal is feedback, and hiding it would make
    // the game silent exactly when the player is confused.
    private void ApplyActionRow(bool humanTurn)
    {
        bool chosenSet = selectedCard != null && (buildSelection.Count > 0 || selectedBuilds.Count > 0);
        bool loneCard = selectedCard != null && buildSelection.Count == 0
                        && selectedBuilds.Count == 0 && selectedBuild == null;
        bool buildContext = selectedBuild != null || (selectedCard != null && buildSelection.Count > 0);

        bool showSweep = humanTurn && (sweepButton.interactable || chosenSet);
        bool showBuild = humanTurn && buildContext;
        bool showTrail = humanTurn && loneCard;

        sweepButton.gameObject.SetActive(showSweep);
        buildButton.gameObject.SetActive(showBuild);
        trailButton.gameObject.SetActive(showTrail);
        suggestButton.gameObject.SetActive(humanTurn);
        if (!humanTurn) return;

        // Most likely play leftmost: enabled beats disabled, then Sweep over
        // Build over Trail (a capture is almost always the best available move,
        // and when it is not, Sweep is not enabled). Suggest is not in the row
        // at all: it is advice, and lives as a circled "?" in the corner.
        var row = new List<(Button b, TextMeshProUGUI label, int rank)>();
        if (showSweep) row.Add((sweepButton, sweepButtonLabel, (sweepButton.interactable ? 100 : 0) + 3));
        if (showBuild) row.Add((buildButton, buildButtonLabel, (buildButton.interactable ? 100 : 0) + 2));
        if (showTrail) row.Add((trailButton, trailButtonLabel, (trailButton.interactable ? 100 : 0) + 1));
        row.Sort((a, b) => b.rank.CompareTo(a.rank));

        if (row.Count > 0) LayoutActionRow(row);
    }

    // Only one card can be played per turn, so the options stack vertically
    // above the card that summoned them - the most likely action nearest the
    // card - rather than sitting in a fixed row. Buttons size to their labels
    // ("Sweep 9s", "Need another to take it") and clamp to the canvas edge so
    // a stack over an end card never clips. When only a build is selected
    // (no hand card yet), the stack falls back to the profile's row anchor.
    private void LayoutActionRow(List<(Button b, TextMeshProUGUI label, int rank)> row)
    {
        var L = CasinoLayout.Active;
        var canvasRect = (RectTransform)canvasTransform;
        float halfW = canvasRect.rect.width / 2f;
        float halfH = canvasRect.rect.height / 2f;

        float xCenter, y;
        if (selectedCard != null)
        {
            var cardRect = (RectTransform)selectedCard.transform;
            Vector2 local = canvasTransform.InverseTransformPoint(cardRect.position);
            // Card top in canvas units, honouring the selection's 1.15 scale.
            float top = local.y + cardRect.rect.height * cardRect.localScale.y * (1f - cardRect.pivot.y);
            xCenter = local.x;
            y = top + 10f;
        }
        else
        {
            xCenter = L.ActionCenter.x;
            y = L.ActionCenter.y - halfH;   // profile y is bottom-anchored
        }

        for (int i = 0; i < row.Count; i++)
        {
            float text = row[i].label.GetPreferredValues(row[i].label.text).x;
            float w = Mathf.Clamp(text + 36f, 96f, 260f);

            var rect = row[i].b.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0f);
            rect.anchoredPosition = new Vector2(
                Mathf.Clamp(xCenter, -halfW + w / 2f + 10f, halfW - w / 2f - 10f), y);
            rect.sizeDelta = new Vector2(w, L.ActionHeight);
            y += L.ActionHeight + L.ActionGap;
        }
    }

    // Live preview of what the Play button will do with the selected card
    private void UpdatePlayPreview(bool humanTurn)
    {
        if (hintText == null || !humanTurn) return;

        // Hand card + selection: preview the chosen sweep (cards and builds)
        if (selectedCard != null && (buildSelection.Count > 0 || selectedBuilds.Count > 0))
        {
            var chosen = buildSelection.Select(ui => ui.Card).ToList();
            var parts = chosen.Select(CardName).ToList();
            parts.AddRange(selectedBuilds.Select(b => $"build({b.DeclaredValue})"));
            bool ok = CaptureChecker.IsExactCaptureSetWithBuilds(selectedCard.Card,
                GameManager.Instance.GetCurrentPlayer(), chosen, selectedBuilds.ToList());
            hintText.text = ok
                ? $"Sweep takes: {string.Join(" ", parts)}"
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
        if (!player.IsHuman()) return;

        // Say why. These two guards used to return in silence, so pressing Build
        // with an incomplete selection did nothing and explained nothing. Found by
        // CasinoInteractionProbe, which reads the hint line back after each press:
        // the screenshot looked fine, the hint was the giveaway.
        if (selectedCard == null)
        {
            hintText.text = "Select a card from your hand first, then the table cards to build with.";
            return;
        }

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
        {
            hintText.text = $"Select the table cards to build with {CardName(selectedCard.Card)}.";
            return;
        }

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

        var handUIs = player.IsHuman() ? nonDealerCardUIs : dealerCardUIs; // human = bottom row
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
            SelectSuggested(handUIs[action.CardIndex],
                $"Suggestion: build {action.DeclaredValue} with {CardName(handCard)}. Capture it next turn with your {action.DeclaredValue}.");
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
            SelectSuggested(handUIs[action.CardIndex],
                $"Suggestion: play {CardName(handCard)} to capture {what}.");
        }
        else
        {
            SelectSuggested(handUIs[action.CardIndex],
                $"Suggestion: no captures available. Trail {CardName(handCard)} (gives up the least).");
        }
    }

    // Asking for advice used to leave you unable to take it.
    //
    // OnSuggestClicked clears the selection before highlighting its
    // recommendation, and every action button requires a selected card, so the
    // buttons all went dead the moment you asked. The probe caught the exact
    // contradiction: the hint read "Trail K♣ (gives up the least)" while the
    // Trail button sat disabled.
    //
    // Selecting the recommended card leaves the move one press away. The advice
    // channel stays distinct from the selection channel, as Stage 2 intended:
    // the card is both suggested and selected, which is honest, because the
    // evaluator proposed it and the UI has now picked it up on your behalf.
    //
    // Order matters. UpdateActionButtons writes its own preview into the hint
    // ("Play takes: ...", "Play will trail ..."), so the suggestion text has to
    // be written after it or it is overwritten in the same frame.
    private void SelectSuggested(CardUI card, string message)
    {
        if (card != null)
        {
            card.SetSelected(true);
            selectedCard = card;
            HighlightCapturingHandCards();
            UpdateActionButtons();
        }
        if (hintText != null) hintText.text = message;
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
            moveBanner.color = CasinoTheme.Headline;
            CasinoType.ApplySerif(moveBanner);
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

        // Cards and/or builds selected: take exactly those (the chosen sweep)
        bool hasSelection = buildSelection.Count > 0 || selectedBuilds.Count > 0;
        if (hasSelection && !forceTrail)
        {
            var chosen = buildSelection.Select(ui => ui.Card).ToList();
            if (GameManager.Instance.TryCaptureSelected(currentPlayer, cardIndex, chosen, selectedBuilds.ToList()))
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

        if (hasSelection && forceTrail)
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
                matchingCardUI.SetOpponentTaking(true);
                highlightedCardUIs.Add(matchingCardUI);
            }
        }

        // Wait for the delay so player can see the selection
        yield return new WaitForSeconds(delay);

        // Deselect the cards (though they'll likely be removed from table after this)
        foreach (var cardUI in highlightedCardUIs)
        {
            if (cardUI != null && cardUI.gameObject != null)
                cardUI.SetOpponentTaking(false);
        }
    }

    private void OnRestartClicked()
    {
        if (gameOverPanel != null)
            gameOverPanel.SetActive(false);
        RaiseModals();

        if (GameManager.Instance != null)
            GameManager.Instance.InitializeGame();
        else
            Debug.LogError("Restart failed: GameManager.Instance is null");

        if (playCardButton != null)
            playCardButton.interactable = true;

        RefreshUI();
    }
}