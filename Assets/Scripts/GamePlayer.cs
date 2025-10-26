using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class GamePlayer
{
    private readonly string _name;
    private readonly List<PlayingCard> _hand = new();
    private readonly List<PlayingCard> _capturedCards = new();
    private int _sweepCount;
    private int _score;

    public string Name => _name;
    public IReadOnlyList<PlayingCard> Hand => _hand;
    public IReadOnlyList<PlayingCard> CapturedCards => _capturedCards;
    public int SweepCount => _sweepCount;
    public int Score => _score;

    public GamePlayer(string name)
    {
        _name = name;
    }

    public void AddCard(PlayingCard card)
    {
        _hand.Add(card);
    }

    public void AddCards(List<PlayingCard> cards)
    {
        _hand.AddRange(cards);
    }

    public PlayingCard PlayCard(int index)
    {
        if (!IsValidCardIndex(index))
        {
            Debug.LogWarning("Invalid card index!");
            return null;
        }

        var card = _hand[index];
        _hand.RemoveAt(index);
        return card;
    }

    public int HandSize()
    {
        return _hand.Count;
    }

    public void ClearHand()
    {
        _hand.Clear();
    }

    public void AddScore(int points)
    {
        _score += points;
    }

    public void IncrementSweepCount()
    {
        _sweepCount++;
    }

    private bool IsValidCardIndex(int index)
    {
        return index >= 0 && index < _hand.Count;
    }

    public override string ToString()
    {
        var handStr = string.Join(" | ", _hand.Select((card, i) => $"[{i}] {card}"));
        return $"{_name} - Hand: {(_hand.Count > 0 ? handStr : "Empty")} | Captured: {_capturedCards.Count} | Score: {_score}";
    }
}