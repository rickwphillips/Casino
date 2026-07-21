using UnityEngine;

/// <summary>
/// Coordinates Dark Souls-style NPC invasions of Casino locations.
///
/// Every location (each scoring variant, and by fallback the plain card table)
/// can be invaded. When an invasion succeeds, a themed <see cref="NpcInvasion"/>
/// takes over an AI seat: it adopts the invader's difficulty and broadcasts
/// flavor text through the <see cref="GameLogger"/>. The card rules themselves
/// are never altered — invasions are opt-in cosmetic/difficulty events.
///
/// Attach this component to a GameObject in the scene (or leave it out entirely).
/// <see cref="GameManager"/> only invokes it when an instance is present, so the
/// base game is unaffected when no invasion manager exists.
/// </summary>
public class NpcInvasionManager : MonoBehaviour
{
    public static NpcInvasionManager Instance { get; private set; }

    [Header("Invasion Settings")]
    [Tooltip("Master switch. When off, no invasions occur.")]
    [SerializeField] private bool invasionsEnabled = true;

    [Tooltip("Probability (0-1) that an eligible AI seat is invaded at game start.")]
    [Range(0f, 1f)]
    [SerializeField] private float invasionChance = 0.35f;

    /// <summary>The invader currently occupying a seat, or null if none.</summary>
    public NpcInvasion ActiveInvader { get; private set; }

    /// <summary>True while an invasion is in progress.</summary>
    public bool IsInvasionActive => ActiveInvader != null;

    private void Awake()
    {
        if (Instance == null) { Instance = this; }
        else { Destroy(gameObject); }
    }

    /// <summary>
    /// Rolls for an invasion of the current location and, on success, hands the
    /// given AI seat to a themed invader. Returns the invader if one arrived,
    /// otherwise null. Safe to call with a null <paramref name="targetAI"/>.
    /// </summary>
    public NpcInvasion TryInvade(AIPlayer targetAI, GamePlayer targetSeat)
    {
        if (!invasionsEnabled || targetAI == null || targetSeat == null || !targetSeat.IsAI())
        {
            return null;
        }

        // Only one invader may hold the field at a time.
        if (IsInvasionActive)
        {
            return null;
        }

        if (Random.value > invasionChance)
        {
            return null;
        }

        string location = ResolveLocationName();
        var roster = NpcInvasion.ForLocation(location);
        var invader = roster[Random.Range(0, roster.Count)];

        ActiveInvader = invader;
        targetAI.SetDifficulty(invader.Difficulty);

        Log($"[INVASION] {location} has been invaded! {invader.InvaderName} " +
            $"seizes the {targetSeat.Name} seat.");
        Log(invader.ArrivalTaunt);

        return invader;
    }

    /// <summary>
    /// Resolves an active invasion once the game ends, logging the appropriate
    /// taunt, then clears the active invader.
    /// </summary>
    /// <param name="invaderWon">True if the invader's seat won the game.</param>
    public void ResolveInvasion(bool invaderWon)
    {
        if (ActiveInvader == null)
        {
            return;
        }

        Log(invaderWon ? ActiveInvader.VictoryTaunt : ActiveInvader.DefeatTaunt);
        ActiveInvader = null;
    }

    /// <summary>Clears any active invasion without logging (e.g. on restart).</summary>
    public void ClearInvasion()
    {
        ActiveInvader = null;
    }

    private static string ResolveLocationName()
    {
        if (ScoringManager.Instance != null)
        {
            string variant = ScoringManager.Instance.CurrentVariant;
            if (!string.IsNullOrEmpty(variant))
            {
                return variant;
            }
        }

        return "The Card Table";
    }

    private static void Log(string message)
    {
        if (GameLogger.Instance != null)
        {
            GameLogger.Instance.LogMessage(message);
        }
        else
        {
            Debug.Log(message);
        }
    }
}
