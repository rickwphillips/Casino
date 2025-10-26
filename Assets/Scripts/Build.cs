using System.Collections.Generic;
using System.Linq;

public class Build
{
    private readonly List<PlayingCard> _cards = new();
    private readonly int _declaredValue;
    private readonly GamePlayer _owner;

    public IReadOnlyList<PlayingCard> Cards => _cards;
    public int DeclaredValue => _declaredValue;
    public GamePlayer Owner => _owner;

    public Build(List<PlayingCard> cards, int declaredValue, GamePlayer owner)
    {
        _cards.AddRange(cards);
        _declaredValue = declaredValue;
        _owner = owner;
    }

    public void AddCard(PlayingCard card)
    {
        _cards.Add(card);
    }

    public void AddCards(List<PlayingCard> cards)
    {
        _cards.AddRange(cards);
    }

    public bool ContainsCard(PlayingCard card)
    {
        return _cards.Contains(card);
    }

    public override string ToString()
    {
        return $"Build of {_declaredValue} ({string.Join(" + ", _cards.Select(c => c.ToString()))}) owned by {_owner.Name}";
    }
}
