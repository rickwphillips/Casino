using System.Collections.Generic;
using System.Linq;
using UnityEngine;
public class GameDeck
{
    private List<PlayingCard> cards = new();
    
    public GameDeck()
    {
        Initialize();
    }
    
    private void Initialize() => cards = System.Enum.GetValues(typeof(PlayingCard.Suit))
        .Cast<PlayingCard.Suit>()
        .SelectMany(suit => System.Enum.GetValues(typeof(PlayingCard.Rank))
            .Cast<PlayingCard.Rank>()
            .Select(rank => new PlayingCard(suit, rank)))
        .ToList();
    
    public void Shuffle() => cards = cards
        .Select(card => new { card, random = Random.value })
        .OrderBy(x => x.random)
        .Select(x => x.card)
        .ToList();
    
    public PlayingCard DrawCard() {
        if (cards.Count == 0) {
            Debug.LogWarning("Deck is empty!");
            return null;
        }

        int lastIndex = cards.Count - 1;
        var card = cards[lastIndex];
        cards.RemoveAt(lastIndex);
        return card;
    }
    
    public List<PlayingCard> DrawCards(int count) => 
        Enumerable.Range(0, count)
            .Select(_ => DrawCard())
            .Where(card => card != null)
            .ToList();
    
    public int CardsRemaining() => cards.Count;
    
    public void Reset() => Initialize();
}