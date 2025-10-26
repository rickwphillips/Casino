using UnityEngine;
public class PlayingCard {
    public enum Suit { Hearts, Diamonds, Clubs, Spades }
    public enum Rank { Ace, Two, Three, Four, Five, Six, Seven, Eight, Nine, Ten, Jack, Queen, King }
    
    public Suit suit;
    public Rank rank;
    
    public PlayingCard(Suit s, Rank r) {
        suit = s;
        rank = r;
    }
    
    public override string ToString() {
        return $"{rank} of {suit}";
    }
}
