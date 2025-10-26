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

    [Header("Scoring Presets")]
    [SerializeField] private ScoreVariables standardVariant;
    [SerializeField] private ScoreVariables connecticutVariant;
    [SerializeField] private ScoreVariables customVariant;

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

        // Register variants
        _variants.Add(standardVariant.VariantName.ToLower(), standardVariant.CreateConfig());
        _variants.Add(connecticutVariant.VariantName.ToLower(), connecticutVariant.CreateConfig());
        
        if (customVariant != null)
        {
            _variants.Add(customVariant.VariantName.ToLower(), customVariant.CreateConfig());
        }

        // Set default variant
        SetVariant(connecticutVariant.VariantName);
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
    public int PointsForLittleCasino => _currentConfig.PointsForLittleCasino;
    public int PointsPerAce => _currentConfig.PointsPerAce;
    public int PointsPerSweep => _currentConfig.PointsPerSweep;
    public int WinScore => _currentConfig.WinScore;
    public string CurrentVariant => _currentConfig.VariantName;

    // Variant queries
    public IEnumerable<string> AvailableVariants => _variants.Keys;
    public bool HasVariant(string variantName) => 
        _variants.ContainsKey(variantName.ToLower());
}