using UnityEngine;

[System.Serializable]
public class ScoringConfig
{
    public string variantName = "Standard";
    
    // Points for specific cards/achievements
    public int pointsForMostCards = 3;
    public int pointsForMostSpades = 1;
    public int pointsForBigCasino = 2;      // 10 of Diamonds
    public int pointsForLittleCasino = 1;   // 2 of Spades
    public int pointsPerAce = 1;
    public int pointsPerSweep = 1;
    
    public int winScore = 21;
}