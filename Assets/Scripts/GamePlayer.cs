using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class GamePlayer
{
    public enum PlayerType { Human, AI }

    private readonly string _name;
    private readonly List<PlayingCard> _hand = new();
    private readonly List<PlayingCard> _capturedCards = new();
    private int _sweepCount;
    private int _score;
    private PlayerType _playerType;

    public string Name => _name;
    public IReadOnlyList<PlayingCard> Hand => _hand;
    public IReadOnlyList<PlayingCard> CapturedCards => _capturedCards;
    public int SweepCount => _sweepCount;
    public int Score => _score;
    public PlayerType Type => _playerType;

    public GamePlayer(string name, PlayerType playerType = PlayerType.AI)
    {
        _name = name;
        _playerType = playerType;
    }

    public bool IsHuman() => _playerType == PlayerType.Human;
    public bool IsAI() => _playerType == PlayerType.AI;

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

    public void AddCapturedCard(PlayingCard card)
    {
        _capturedCards.Add(card);
    }

    public void AddCapturedCards(List<PlayingCard> cards)
    {
        _capturedCards.AddRange(cards);
    }

    public void ClearCapturedCards()
    {
        _capturedCards.Clear();
    }

    public void ResetForNewGame()
    {
        _score = 0;
        _sweepCount = 0;
        _hand.Clear();
        _capturedCards.Clear();
    }

    private bool IsValidCardIndex(int index)
    {
        return index >= 0 && index < _hand.Count;
    }

    public override string ToString()
    {
        var handStr = string.Join(" | ", _hand.Select((card, i) => $"[{i}] {card}"));
        string typeStr = _playerType == PlayerType.Human ? "(Human)" : "(AI)";
        return $"{_name} {typeStr} - Hand: {(_hand.Count > 0 ? handStr : "Empty")} | Captured: {_capturedCards.Count} | Score: {_score}";
    }
}