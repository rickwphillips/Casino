using UnityEngine;

[CreateAssetMenu(fileName = "ScoringVariables", menuName = "Casino/Scoring Variables")]
public class ScoreVariables : ScriptableObject
{
    [Header("Variant Information")]
    [SerializeField] private string variantName = "Custom";
    
    [Header("Card Count Scoring")]
    [Tooltip("Points awarded for having the most cards at the end of a round")]
    [SerializeField] private int pointsForMostCards = 3;
    
    [Header("Suit-Based Scoring")]
    [Tooltip("Points awarded for capturing the most spades")]
    [SerializeField] private int pointsForMostSpades = 1;
    
    [Header("Special Card Scoring")]
    [Tooltip("Points for capturing the Big Casino card")]
    [SerializeField] private int pointsForBigCasino = 2;
    [Tooltip("Suit of the Big Casino card")]
    [SerializeField] private PlayingCard.Suit bigCasinoSuit = PlayingCard.Suit.Diamonds;
    [Tooltip("Rank of the Big Casino card")]
    [SerializeField] private PlayingCard.Rank bigCasinoRank = PlayingCard.Rank.Ten;
    
    [Tooltip("Points for capturing the Little Casino card")]
    [SerializeField] private int pointsForLittleCasino = 1;
    [Tooltip("Suit of the Little Casino card")]
    [SerializeField] private PlayingCard.Suit littleCasinoSuit = PlayingCard.Suit.Spades;
    [Tooltip("Rank of the Little Casino card")]
    [SerializeField] private PlayingCard.Rank littleCasinoRank = PlayingCard.Rank.Two;
    
    [Header("Per-Card Scoring")]
    [Tooltip("Points awarded for each Ace captured")]
    [SerializeField] private int pointsPerAce = 1;
    
    [Tooltip("Points awarded for each Two captured")]
    [SerializeField] private int pointsPerTwo = 0;
    
    [Tooltip("Points awarded for each Three captured")]
    [SerializeField] private int pointsPerThree = 0;
    
    [Tooltip("Points awarded for each Four captured")]
    [SerializeField] private int pointsPerFour = 0;
    
    [Tooltip("Points awarded for each Five captured")]
    [SerializeField] private int pointsPerFive = 0;
    
    [Tooltip("Points awarded for each Six captured")]
    [SerializeField] private int pointsPerSix = 0;
    
    [Tooltip("Points awarded for each Seven captured")]
    [SerializeField] private int pointsPerSeven = 0;
    
    [Tooltip("Points awarded for each Eight captured")]
    [SerializeField] private int pointsPerEight = 0;
    
    [Tooltip("Points awarded for each Nine captured")]
    [SerializeField] private int pointsPerNine = 0;
    
    [Tooltip("Points awarded for each Ten captured")]
    [SerializeField] private int pointsPerTen = 0;
    
    [Tooltip("Points awarded for each Jack captured")]
    [SerializeField] private int pointsPerJack = 0;
    
    [Tooltip("Points awarded for each Queen captured")]
    [SerializeField] private int pointsPerQueen = 0;
    
    [Tooltip("Points awarded for each King captured")]
    [SerializeField] private int pointsPerKing = 0;
    
    [Header("Achievement Scoring")]
    [Tooltip("Points awarded for each sweep performed")]
    [SerializeField] private int pointsPerSweep = 1;
    
    [Header("Game Configuration")]
    [Tooltip("Score required to win the game")]
    [Range(1, 50)]
    [SerializeField] private int winScore = 21;

    [Tooltip("When to award remaining table cards to last capturer: AfterEachHand (traditional) or OnlyAtGameEnd")]
    [SerializeField] private TableCardAwardTiming tableCardAwardTiming = TableCardAwardTiming.AfterEachHand;

    public enum TableCardAwardTiming
    {
        AfterEachHand,      // Traditional: Award after each 4-card hand
        OnlyAtGameEnd       // Variant: Only award when entire deck is exhausted
    }

    // Public accessors with validation
    public string VariantName => variantName;
    public int PointsForMostCards => Mathf.Max(0, pointsForMostCards);
    public int PointsForMostSpades => Mathf.Max(0, pointsForMostSpades);
    public int PointsForBigCasino => Mathf.Max(0, pointsForBigCasino);
    public PlayingCard.Suit BigCasinoSuit => bigCasinoSuit;
    public PlayingCard.Rank BigCasinoRank => bigCasinoRank;
    public int PointsForLittleCasino => Mathf.Max(0, pointsForLittleCasino);
    public PlayingCard.Suit LittleCasinoSuit => littleCasinoSuit;
    public PlayingCard.Rank LittleCasinoRank => littleCasinoRank;
    public int PointsPerAce => Mathf.Max(0, pointsPerAce);
    public int PointsPerTwo => Mathf.Max(0, pointsPerTwo);
    public int PointsPerThree => Mathf.Max(0, pointsPerThree);
    public int PointsPerFour => Mathf.Max(0, pointsPerFour);
    public int PointsPerFive => Mathf.Max(0, pointsPerFive);
    public int PointsPerSix => Mathf.Max(0, pointsPerSix);
    public int PointsPerSeven => Mathf.Max(0, pointsPerSeven);
    public int PointsPerEight => Mathf.Max(0, pointsPerEight);
    public int PointsPerNine => Mathf.Max(0, pointsPerNine);
    public int PointsPerTen => Mathf.Max(0, pointsPerTen);
    public int PointsPerJack => Mathf.Max(0, pointsPerJack);
    public int PointsPerQueen => Mathf.Max(0, pointsPerQueen);
    public int PointsPerKing => Mathf.Max(0, pointsPerKing);
    public int PointsPerSweep => Mathf.Max(0, pointsPerSweep);
    public int WinScore => Mathf.Max(1, winScore);
    public TableCardAwardTiming TableCardTiming => tableCardAwardTiming;

    // Method to create a ScoringConfig from these variables
    public ScoringConfig CreateConfig()
    {
        return new ScoringConfig
        {
            VariantName = VariantName,
            PointsForMostCards = PointsForMostCards,
            PointsForMostSpades = PointsForMostSpades,
            PointsForBigCasino = PointsForBigCasino,
            BigCasinoSuit = BigCasinoSuit,
            BigCasinoRank = BigCasinoRank,
            PointsForLittleCasino = PointsForLittleCasino,
            LittleCasinoSuit = LittleCasinoSuit,
            LittleCasinoRank = LittleCasinoRank,
            PointsPerAce = PointsPerAce,
            PointsPerTwo = PointsPerTwo,
            PointsPerThree = PointsPerThree,
            PointsPerFour = PointsPerFour,
            PointsPerFive = PointsPerFive,
            PointsPerSix = PointsPerSix,
            PointsPerSeven = PointsPerSeven,
            PointsPerEight = PointsPerEight,
            PointsPerNine = PointsPerNine,
            PointsPerTen = PointsPerTen,
            PointsPerJack = PointsPerJack,
            PointsPerQueen = PointsPerQueen,
            PointsPerKing = PointsPerKing,
            PointsPerSweep = PointsPerSweep,
            WinScore = WinScore,
            TableCardTiming = TableCardTiming
        };
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        // Ensure values don't go negative
        pointsForMostCards = Mathf.Max(0, pointsForMostCards);
        pointsForMostSpades = Mathf.Max(0, pointsForMostSpades);
        pointsForBigCasino = Mathf.Max(0, pointsForBigCasino);
        pointsForLittleCasino = Mathf.Max(0, pointsForLittleCasino);
        pointsPerAce = Mathf.Max(0, pointsPerAce);
        pointsPerTwo = Mathf.Max(0, pointsPerTwo);
        pointsPerThree = Mathf.Max(0, pointsPerThree);
        pointsPerFour = Mathf.Max(0, pointsPerFour);
        pointsPerFive = Mathf.Max(0, pointsPerFive);
        pointsPerSix = Mathf.Max(0, pointsPerSix);
        pointsPerSeven = Mathf.Max(0, pointsPerSeven);
        pointsPerEight = Mathf.Max(0, pointsPerEight);
        pointsPerNine = Mathf.Max(0, pointsPerNine);
        pointsPerTen = Mathf.Max(0, pointsPerTen);
        pointsPerJack = Mathf.Max(0, pointsPerJack);
        pointsPerQueen = Mathf.Max(0, pointsPerQueen);
        pointsPerKing = Mathf.Max(0, pointsPerKing);
        pointsPerSweep = Mathf.Max(0, pointsPerSweep);
        winScore = Mathf.Max(1, winScore);
    }
#endif
}
