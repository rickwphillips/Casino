using System.Collections.Generic;
using System.Linq;

public class Build
{
    private readonly List<PlayingCard> _cards = new();
    private int _declaredValue;
    private GamePlayer _owner;
    private readonly bool _isMultiBuild;

    public IReadOnlyList<PlayingCard> Cards => _cards;
    public int DeclaredValue => _declaredValue;
    public GamePlayer Owner => _owner;
    public bool IsMultiBuild => _isMultiBuild;

    public Build(List<PlayingCard> cards, int declaredValue, GamePlayer owner, bool isMultiBuild = false)
    {
        _cards.AddRange(cards);
        _declaredValue = declaredValue;
        _owner = owner;
        _isMultiBuild = isMultiBuild;
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

    public void ModifyBuild(PlayingCard card, int newValue, GamePlayer newOwner)
    {
        if (_isMultiBuild)
        {
            throw new System.InvalidOperationException("Cannot modify a multi-build");
        }

        _cards.Add(card);
        _declaredValue = newValue;
        _owner = newOwner;
    }

    public override string ToString()
    {
        string buildType = _isMultiBuild ? "Multi-Build" : "Build";
        return $"{buildType} of {_declaredValue} ({string.Join(" + ", _cards.Select(c => c.ToString()))}) owned by {_owner.Name}";
    }
}
