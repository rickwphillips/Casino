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

        private int _pointsForLittleCasino;  // 2 of Spades
        public int PointsForLittleCasino
        {
            get => _pointsForLittleCasino;
            set => _pointsForLittleCasino = value;
        }

        // Per-card points
        private int _pointsPerAce;
        public int PointsPerAce
        {
            get => _pointsPerAce;
            set => _pointsPerAce = value;
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

        // Constructor with default values
        public ScoringConfig()
        {
            _variantName = "Standard";
            _pointsForMostCards = 3;
            _pointsForMostSpades = 1;
            _pointsForBigCasino = 2;
            _pointsForLittleCasino = 1;
            _pointsPerAce = 1;
            _pointsPerSweep = 1;
            _winScore = 21;
        }
    }
