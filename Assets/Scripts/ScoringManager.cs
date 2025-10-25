using UnityEngine;

public class ScoringManager : MonoBehaviour
{
    public static ScoringManager Instance { get; private set; }
    
    private ScoringConfig currentConfig;
    
    private ScoringConfig standardConfig;
    private ScoringConfig connecticutConfig;
    
    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }
    
    private void Start()
    {
        InitializeConfigs();
    }
    
    private void InitializeConfigs()
    {
        standardConfig = new ScoringConfig
        {
            variantName = "Standard",
            pointsForMostCards = 3,
            pointsForMostSpades = 1,
            pointsForBigCasino = 2,
            pointsForLittleCasino = 1,
            pointsPerAce = 1,
            pointsPerSweep = 1,
            winScore = 21
        };
        
        connecticutConfig = new ScoringConfig
        {
            variantName = "Connecticut",
            pointsForMostCards = 1,
            pointsForMostSpades = 0,
            pointsForBigCasino = 3,
            pointsForLittleCasino = 2,
            pointsPerAce = 0,
            pointsPerSweep = 1,
            winScore = 21
        };
        
        SetVariant("Connecticut");
    }
    
    public void SetVariant(string variantName)
    {
        switch (variantName.ToLower())
        {
            case "standard":
                currentConfig = standardConfig;
                Debug.Log("Switched to Standard variant");
                break;
            case "connecticut":
                currentConfig = connecticutConfig;
                Debug.Log("Switched to Connecticut variant");
                break;
            default:
                Debug.LogWarning("Unknown variant: " + variantName);
                break;
        }
    }
    
    public ScoringConfig GetConfig() => currentConfig;
    public int GetPointsForMostCards() => currentConfig.pointsForMostCards;
    public int GetPointsForMostSpades() => currentConfig.pointsForMostSpades;
    public int GetPointsForBigCasino() => currentConfig.pointsForBigCasino;
    public int GetPointsForLittleCasino() => currentConfig.pointsForLittleCasino;
    public int GetPointsPerAce() => currentConfig.pointsPerAce;
    public int GetPointsPerSweep() => currentConfig.pointsPerSweep;
    public int GetWinScore() => currentConfig.winScore;
    public string GetCurrentVariant() => currentConfig.variantName;
}