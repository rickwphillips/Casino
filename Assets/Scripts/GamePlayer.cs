using System.Collections.Generic;
using UnityEngine;

public class GamePlayer
{
    public string playerName;
    public List<PlayingCard> hand = new();
    public List<PlayingCard> capturedCards = new();
    public int sweepCount = 0;
    public int score = 0;
    
    public GamePlayer(string name)
    {
        playerName = name;
    }
    
    public void AddCard(PlayingCard card) => hand.Add(card);
    
    public void AddCards(List<PlayingCard> cards) => hand.AddRange(cards);
    
    public PlayingCard PlayCard(int index)
    {
        if (index >= 0 && index < hand.Count)
        {
            PlayingCard card = hand[index];
            hand.RemoveAt(index);
            return card;
        }
        Debug.LogWarning("Invalid card index!");
        return null;
    }
    
    public int HandSize() => hand.Count;
    
    public void ClearHand() => hand.Clear();
    
    public void AddScore(int points) => score += points;
    
    public override string ToString()
    {
        string handStr = "";
        for (int i = 0; i < hand.Count; i++)
        {
            handStr += $"[{i}] {hand[i]} | ";
        }
        return $"{playerName} - Hand: {(hand.Count > 0 ? handStr : "Empty")} | Captured: {capturedCards.Count} | Score: {score}";
    }
}