using System.Collections.Generic;
using UnityEngine;
public class GameDeck
{
    private List<PlayingCard> cards = new();
    
    public GameDeck()
    {
        Initialize();
    }
    
    private void Initialize()
    {
        cards.Clear();
        foreach (PlayingCard.Suit suit in System.Enum.GetValues(typeof(PlayingCard.Suit)))
        {
            foreach (PlayingCard.Rank rank in System.Enum.GetValues(typeof(PlayingCard.Rank)))
            {
                cards.Add(new PlayingCard(suit, rank));
            }
        }
    }
    
    public void Shuffle()
    {
        // Fisher-Yates shuffle
        for (int i = cards.Count - 1; i > 0; i--)
        {
            int randomIndex = Random.Range(0, i + 1);
            
            // Swap
            PlayingCard temp = cards[i];
            cards[i] = cards[randomIndex];
            cards[randomIndex] = temp;
        }
    }
    
    public PlayingCard DrawCard()
    {
        if (cards.Count == 0)
        {
            Debug.LogWarning("Deck is empty!");
            return null;
        }
        
        PlayingCard drawnCard = cards[cards.Count - 1];
        cards.RemoveAt(cards.Count - 1);
        return drawnCard;
    }
    
    public List<PlayingCard> DrawCards(int count)
    {
        List<PlayingCard> drawnCards = new List<PlayingCard>();
        for (int i = 0; i < count; i++)
        {
            PlayingCard card = DrawCard();
            if (card != null)
                drawnCards.Add(card);
        }
        return drawnCards;
    }
    
    public int CardsRemaining()
    {
        return cards.Count;
    }
    
    public void Reset()
    {
        Initialize();
    }
}