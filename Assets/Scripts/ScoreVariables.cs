using UnityEngine;

[CreateAssetMenu(fileName = "ScoringVariables", menuName = "Casino/Scoring Variables")]
public class ScoreVariables : ScriptableObject
{
    [Header("Variant Information")]
    [SerializeField] private readonly string variantName = "Custom";
    
    [Header("Card Count Scoring")]
    [Tooltip("Points awarded for having the most cards at the end of a round")]
    [SerializeField] private readonly int pointsForMostCards = 3;
    
    [Header("Suit-Based Scoring")]
    [Tooltip("Points awarded for capturing the most spades")]
    [SerializeField] private readonly int pointsForMostSpades = 1;
    
    [Header("Special Card Scoring")]
    [Tooltip("Points for capturing the 10 of Diamonds")]
    [SerializeField] private readonly int pointsForBigCasino = 2;
    
    [Tooltip("Points for capturing the 2 of Spades")]
    [SerializeField] private readonly int pointsForLittleCasino = 1;
    
    [Header("Per-Card Scoring")]
    [Tooltip("Points awarded for each Ace captured")]
    [SerializeField] private readonly int pointsPerAce = 1;
    
    [Header("Achievement Scoring")]
    [Tooltip("Points awarded for each sweep performed")]
    [SerializeField] private readonly int pointsPerSweep = 1;
    
    [Header("Game Configuration")]
    [Tooltip("Score required to win the game")]
    [Range(1, 50)]
    [SerializeField] private readonly int winScore = 21;

    // Public accessors with validation
    public string VariantName => variantName;
    public int PointsForMostCards => Mathf.Max(0, pointsForMostCards);
    public int PointsForMostSpades => Mathf.Max(0, pointsForMostSpades);
    public int PointsForBigCasino => Mathf.Max(0, pointsForBigCasino);
    public int PointsForLittleCasino => Mathf.Max(0, pointsForLittleCasino);
    public int PointsPerAce => Mathf.Max(0, pointsPerAce);
    public int PointsPerSweep => Mathf.Max(0, pointsPerSweep);
    public int WinScore => Mathf.Max(1, winScore);

    // Method to create a ScoringConfig from these variables
    public ScoringConfig CreateConfig()
    {
        return new ScoringConfig
        {
            VariantName = VariantName,
            PointsForMostCards = PointsForMostCards,
            PointsForMostSpades = PointsForMostSpades,
            PointsForBigCasino = PointsForBigCasino,
            PointsForLittleCasino = PointsForLittleCasino,
            PointsPerAce = PointsPerAce,
            PointsPerSweep = PointsPerSweep,
            WinScore = WinScore
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
        pointsPerSweep = Mathf.Max(0, pointsPerSweep);
        winScore = Mathf.Max(1, winScore);
    }
#endif
    }