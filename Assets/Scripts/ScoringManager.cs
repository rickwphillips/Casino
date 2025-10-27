using UnityEngine;
using System.Collections.Generic;
using System.Linq;

[DisallowMultipleComponent]
public class ScoringManager : MonoBehaviour
{
    // Singleton pattern
    private static ScoringManager _instance;
    public static ScoringManager Instance 
    { 
        get => _instance;
        private set => _instance = value;
    }

    public enum VariantSelection
    {
        Standard,
        Connecticut,
        Custom
    }

    [Header("Scoring Presets")]
    [SerializeField] private ScoreVariables standardVariant;
    [SerializeField] private ScoreVariables connecticutVariant;
    [SerializeField] private ScoreVariables customVariant;

    [Header("Active Variant")]
    [Tooltip("Select which variant to use for scoring")]
    [SerializeField] private VariantSelection selectedVariant = VariantSelection.Connecticut;

    // Configuration storage
    private readonly Dictionary<string, ScoringConfig> _variants = new();
    private ScoringConfig _currentConfig;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            InitializeConfigs();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void InitializeConfigs()
    {
        if (standardVariant == null || connecticutVariant == null)
        {
            Debug.LogError("Scoring variants not assigned in inspector!");
            return;
        }

        // Register variants - check for duplicates
        RegisterVariant(standardVariant);
        RegisterVariant(connecticutVariant);
        
        if (customVariant != null)
        {
            RegisterVariant(customVariant);
        }

        // Set variant based on inspector selection
        string variantName = selectedVariant switch
        {
            VariantSelection.Standard => standardVariant?.VariantName,
            VariantSelection.Connecticut => connecticutVariant?.VariantName,
            VariantSelection.Custom => customVariant?.VariantName,
            _ => connecticutVariant?.VariantName
        };

        if (!string.IsNullOrEmpty(variantName))
        {
            SetVariant(variantName);
        }
        else
        {
            Debug.LogError($"Selected variant {selectedVariant} has no assigned ScoreVariables asset!");
        }
    }

    private void RegisterVariant(ScoreVariables variant)
    {
        if (variant == null)
        {
            Debug.LogWarning("Attempted to register null variant!");
            return;
        }

        string variantKey = variant.VariantName.ToLower();
        
        if (_variants.ContainsKey(variantKey))
        {
            Debug.LogWarning($"Variant '{variant.VariantName}' is already registered! Skipping duplicate.");
            return;
        }

        _variants.Add(variantKey, variant.CreateConfig());
        Debug.Log($"Registered variant: {variant.VariantName}");
    }

    public void SetVariant(string variantName)
    {
        var normalizedName = variantName.ToLower();
        
        if (_variants.TryGetValue(normalizedName, out var config))
        {
            _currentConfig = config;
            Debug.Log($"Switched to {config.VariantName} variant");
        }
        else
        {
            Debug.LogWarning($"Unknown variant: {variantName}");
        }
    }

    // Declarative accessors
    public ScoringConfig CurrentConfig => _currentConfig;
    public int PointsForMostCards => _currentConfig.PointsForMostCards;
    public int PointsForMostSpades => _currentConfig.PointsForMostSpades;
    public int PointsForBigCasino => _currentConfig.PointsForBigCasino;
    public PlayingCard.Suit BigCasinoSuit => _currentConfig.BigCasinoSuit;
    public PlayingCard.Rank BigCasinoRank => _currentConfig.BigCasinoRank;
    public int PointsForLittleCasino => _currentConfig.PointsForLittleCasino;
    public PlayingCard.Suit LittleCasinoSuit => _currentConfig.LittleCasinoSuit;
    public PlayingCard.Rank LittleCasinoRank => _currentConfig.LittleCasinoRank;
    public int PointsPerAce => _currentConfig.PointsPerAce;
    public int PointsPerTwo => _currentConfig.PointsPerTwo;
    public int PointsPerThree => _currentConfig.PointsPerThree;
    public int PointsPerFour => _currentConfig.PointsPerFour;
    public int PointsPerFive => _currentConfig.PointsPerFive;
    public int PointsPerSix => _currentConfig.PointsPerSix;
    public int PointsPerSeven => _currentConfig.PointsPerSeven;
    public int PointsPerEight => _currentConfig.PointsPerEight;
    public int PointsPerNine => _currentConfig.PointsPerNine;
    public int PointsPerTen => _currentConfig.PointsPerTen;
    public int PointsPerJack => _currentConfig.PointsPerJack;
    public int PointsPerQueen => _currentConfig.PointsPerQueen;
    public int PointsPerKing => _currentConfig.PointsPerKing;
    public int PointsPerSweep => _currentConfig.PointsPerSweep;
    public int WinScore => _currentConfig.WinScore;
    public ScoreVariables.TableCardAwardTiming TableCardTiming => _currentConfig.TableCardTiming;
    public string CurrentVariant => _currentConfig.VariantName;

    // Variant queries
    public IEnumerable<string> AvailableVariants => _variants.Keys;
    public bool HasVariant(string variantName) => 
        _variants.ContainsKey(variantName.ToLower());
}
