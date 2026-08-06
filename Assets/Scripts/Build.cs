using System.Collections.Generic;
using System.Linq;

public class Build
{
    private readonly List<PlayingCard> _cards = new();
    private int _declaredValue;
    private GamePlayer _owner;
    private bool _isMultiBuild;

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

    // Add a card at the build's existing value: a new group joins the stack,
    // the build becomes (or stays) multi, and the adder takes ownership.
    public void AddToBuild(PlayingCard card, GamePlayer newOwner)
    {
        _cards.Add(card);
        _owner = newOwner;
        _isMultiBuild = true;
    }

    // The build a newly declared group of this value must join, or null to start
    // a fresh one. Two builds of a single value never coexist on the table, so a
    // second 9 joins the first rather than standing beside it.
    public static Build FindMergeTarget(IEnumerable<Build> builds, int declaredValue) =>
        builds?.FirstOrDefault(b => b.DeclaredValue == declaredValue);

    // Merge a whole group of the same declared value into this build. The stack
    // grows, which locks the build as multi (no longer raisable by either side)
    // and passes ownership to the builder.
    public void MergeGroup(List<PlayingCard> cards, GamePlayer newOwner)
    {
        _cards.AddRange(cards);
        _owner = newOwner;
        _isMultiBuild = true;
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
