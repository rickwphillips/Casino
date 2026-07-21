using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Represents a single Dark Souls-style NPC invader that can invade a location
/// (a Casino scoring variant / table) and take over the AI opponent's seat.
///
/// This is a lightweight, purely-cosmetic identity: it re-themes an AI opponent
/// and supplies flavor text. It never changes the underlying card rules.
/// </summary>
public class NpcInvasion
{
    /// <summary>Display name of the invader (e.g. "Maldron, the Assassin").</summary>
    public string InvaderName { get; }

    /// <summary>AI skill the invader plays with once they take the seat.</summary>
    public AIPlayer.Difficulty Difficulty { get; }

    /// <summary>Line shown when the invader arrives at the table.</summary>
    public string ArrivalTaunt { get; }

    /// <summary>Line shown if the invader wins the game.</summary>
    public string VictoryTaunt { get; }

    /// <summary>Line shown if the invader is defeated.</summary>
    public string DefeatTaunt { get; }

    public NpcInvasion(
        string invaderName,
        AIPlayer.Difficulty difficulty,
        string arrivalTaunt,
        string victoryTaunt,
        string defeatTaunt)
    {
        InvaderName = invaderName;
        Difficulty = difficulty;
        ArrivalTaunt = arrivalTaunt;
        VictoryTaunt = victoryTaunt;
        DefeatTaunt = defeatTaunt;
    }

    public override string ToString() =>
        $"{InvaderName} ({Difficulty})";

    /// <summary>
    /// The roster of invaders that haunt each location. Keys are matched against
    /// the active scoring variant name (case-insensitive). Any location not listed
    /// here falls back to <see cref="Wanderers"/> so that <b>all</b> locations can
    /// be invaded.
    /// </summary>
    public static readonly Dictionary<string, List<NpcInvasion>> RosterByLocation =
        new(System.StringComparer.OrdinalIgnoreCase)
        {
            ["Standard"] = new()
            {
                new NpcInvasion(
                    "Maldron, the Assassin",
                    AIPlayer.Difficulty.Hard,
                    "A dark spirit has invaded! Maldron, the Assassin, slips into the seat...",
                    "Maldron fades away, your souls in hand. Git gud.",
                    "Maldron is vanquished. Your hollow persists another round."),
                new NpcInvasion(
                    "Longfinger Kirk",
                    AIPlayer.Difficulty.Medium,
                    "A dark spirit has invaded! Longfinger Kirk raises his thorned deck...",
                    "Kirk claims the pot. The thorns draw blood.",
                    "Kirk retreats to the Cathedral, cards scattered."),
            },
            ["Connecticut"] = new()
            {
                new NpcInvasion(
                    "Armorer Dennis",
                    AIPlayer.Difficulty.Medium,
                    "A dark spirit has invaded! Armorer Dennis eyes the table hungrily...",
                    "Dennis hoards the winnings. Ahh, that's good soul.",
                    "Dennis is bested. He wanted your cards, not this."),
                new NpcInvasion(
                    "Xanthous Ashen",
                    AIPlayer.Difficulty.Hard,
                    "A dark spirit has invaded! Xanthous Ashen kindles the felt...",
                    "Ashen rakes the pot into the flame. All returns to cinder.",
                    "Ashen is extinguished. The kindling holds."),
            },
            ["Rick's New England"] = new()
            {
                new NpcInvasion(
                    "The Nameless Dealer",
                    AIPlayer.Difficulty.Hard,
                    "A dark spirit has invaded! The Nameless Dealer takes the seat in silence...",
                    "The Nameless Dealer collects, and says nothing.",
                    "The Nameless Dealer bows out. Silence, at last."),
                new NpcInvasion(
                    "Patches the Unbreakable",
                    AIPlayer.Difficulty.Easy,
                    "A dark spirit has invaded! Patches offers a friendly game, palms already itching...",
                    "Patches takes it all and kicks you off the ledge. Hehehe.",
                    "Patches feigns friendship once more. 'No hard feelings, eh?'"),
            },
        };

    /// <summary>Fallback invaders for any un-rostered location.</summary>
    public static readonly List<NpcInvasion> Wanderers = new()
    {
        new NpcInvasion(
            "A Wandering Phantom",
            AIPlayer.Difficulty.Medium,
            "A dark spirit has invaded! A Wandering Phantom materializes across the table...",
            "The Phantom claims your souls and dissolves into mist.",
            "The Phantom is banished. The table is yours again."),
    };

    /// <summary>Returns the invader roster for a given location name.</summary>
    public static List<NpcInvasion> ForLocation(string locationName)
    {
        if (!string.IsNullOrEmpty(locationName) &&
            RosterByLocation.TryGetValue(locationName, out var roster) &&
            roster.Count > 0)
        {
            return roster;
        }

        return Wanderers;
    }
}
