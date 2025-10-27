using UnityEngine;

[System.Serializable]
public class ScoringConfig
{
    // Variant identification
    private string _variantName;
    public string VariantName
    {
        get => _variantName;
        set => _variantName = value;
    }

    // Card count bonus
    private int _pointsForMostCards;
    public int PointsForMostCards
    {
        get => _pointsForMostCards;
        set => _pointsForMostCards = value;
    }

    // Spades bonus
    private int _pointsForMostSpades;
    public int PointsForMostSpades
    {
        get => _pointsForMostSpades;
        set => _pointsForMostSpades = value;
    }

    // Special card points
    private int _pointsForBigCasino;    // 10 of Diamonds
    public int PointsForBigCasino
    {
        get => _pointsForBigCasino;
        set => _pointsForBigCasino = value;
    }

    private PlayingCard.Suit _bigCasinoSuit;
    public PlayingCard.Suit BigCasinoSuit
    {
        get => _bigCasinoSuit;
        set => _bigCasinoSuit = value;
    }

    private PlayingCard.Rank _bigCasinoRank;
    public PlayingCard.Rank BigCasinoRank
    {
        get => _bigCasinoRank;
        set => _bigCasinoRank = value;
    }

    private int _pointsForLittleCasino;  // 2 of Spades
    public int PointsForLittleCasino
    {
        get => _pointsForLittleCasino;
        set => _pointsForLittleCasino = value;
    }

    private PlayingCard.Suit _littleCasinoSuit;
    public PlayingCard.Suit LittleCasinoSuit
    {
        get => _littleCasinoSuit;
        set => _littleCasinoSuit = value;
    }

    private PlayingCard.Rank _littleCasinoRank;
    public PlayingCard.Rank LittleCasinoRank
    {
        get => _littleCasinoRank;
        set => _littleCasinoRank = value;
    }

    // Per-card points for all 13 ranks
    private int _pointsPerAce;
    public int PointsPerAce
    {
        get => _pointsPerAce;
        set => _pointsPerAce = value;
    }

    private int _pointsPerTwo;
    public int PointsPerTwo
    {
        get => _pointsPerTwo;
        set => _pointsPerTwo = value;
    }

    private int _pointsPerThree;
    public int PointsPerThree
    {
        get => _pointsPerThree;
        set => _pointsPerThree = value;
    }

    private int _pointsPerFour;
    public int PointsPerFour
    {
        get => _pointsPerFour;
        set => _pointsPerFour = value;
    }

    private int _pointsPerFive;
    public int PointsPerFive
    {
        get => _pointsPerFive;
        set => _pointsPerFive = value;
    }

    private int _pointsPerSix;
    public int PointsPerSix
    {
        get => _pointsPerSix;
        set => _pointsPerSix = value;
    }

    private int _pointsPerSeven;
    public int PointsPerSeven
    {
        get => _pointsPerSeven;
        set => _pointsPerSeven = value;
    }

    private int _pointsPerEight;
    public int PointsPerEight
    {
        get => _pointsPerEight;
        set => _pointsPerEight = value;
    }

    private int _pointsPerNine;
    public int PointsPerNine
    {
        get => _pointsPerNine;
        set => _pointsPerNine = value;
    }

    private int _pointsPerTen;
    public int PointsPerTen
    {
        get => _pointsPerTen;
        set => _pointsPerTen = value;
    }

    private int _pointsPerJack;
    public int PointsPerJack
    {
        get => _pointsPerJack;
        set => _pointsPerJack = value;
    }

    private int _pointsPerQueen;
    public int PointsPerQueen
    {
        get => _pointsPerQueen;
        set => _pointsPerQueen = value;
    }

    private int _pointsPerKing;
    public int PointsPerKing
    {
        get => _pointsPerKing;
        set => _pointsPerKing = value;
    }

    // Achievement points
    private int _pointsPerSweep;
    public int PointsPerSweep
    {
        get => _pointsPerSweep;
        set => _pointsPerSweep = value;
    }

    // Game configuration
    private int _winScore;
    public int WinScore
    {
        get => _winScore;
        set => _winScore = value;
    }

    private ScoreVariables.TableCardAwardTiming _tableCardTiming;
    public ScoreVariables.TableCardAwardTiming TableCardTiming
    {
        get => _tableCardTiming;
        set => _tableCardTiming = value;
    }

    // Constructor with default values
    public ScoringConfig()
    {
        _variantName = "Standard";
        _pointsForMostCards = 3;
        _pointsForMostSpades = 1;
        _pointsForBigCasino = 2;
        _bigCasinoSuit = PlayingCard.Suit.Diamonds;
        _bigCasinoRank = PlayingCard.Rank.Ten;
        _pointsForLittleCasino = 1;
        _littleCasinoSuit = PlayingCard.Suit.Spades;
        _littleCasinoRank = PlayingCard.Rank.Two;
        _pointsPerAce = 1;
        _pointsPerTwo = 0;
        _pointsPerThree = 0;
        _pointsPerFour = 0;
        _pointsPerFive = 0;
        _pointsPerSix = 0;
        _pointsPerSeven = 0;
        _pointsPerEight = 0;
        _pointsPerNine = 0;
        _pointsPerTen = 0;
        _pointsPerJack = 0;
        _pointsPerQueen = 0;
        _pointsPerKing = 0;
        _pointsPerSweep = 1;
        _winScore = 21;
        _tableCardTiming = ScoreVariables.TableCardAwardTiming.AfterEachHand;
    }
}
