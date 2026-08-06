# Casino

A Unity implementation of Casino, the fishing card game — one human player against an
AI opponent, with configurable scoring variants.

The rules below are **Rick's family rules**, which is what the engine actually implements.
They differ from the generic Casino rules you'll find elsewhere, most notably in that
captures are *chosen* rather than automatic.

## The game

A standard 52-card deck. One player is the **Dealer**, the other the **Non-Dealer**;
in the shipped 1-player setup you are the Non-Dealer and the AI deals.

Each deck is dealt 4 cards to you, 4 to the AI, and 4 face-up to the table. The
Non-Dealer plays first. When both hands are empty another 4 and 4 are dealt — no further
cards are added to the table — until the deck runs out.

### Three plays per turn

On your turn you play exactly one card from your hand as a **Sweep**, a **Trail**,
or a **Build**.

**Sweep** — capture from the table. A played card *can* take every table card of matching
rank plus every set of cards summing to its value, all at once. It does not have to:
**you choose which cards and sets to take**, and a partial capture is legal. Play a 9 into
a table holding `9 ♣, 5 ♦, 4 ♠, 6 ♥, 3 ♦` and you may take the lone 9, or the 5+4, or the
6+3, or any combination of them, or all three groups together.

Aces count 1, number cards their face value. Face cards capture only their own rank —
a Jack takes Jacks and nothing else.

**Trail** — lay the card on the table. Always available *except* when you own a build:
a build owner must sweep or build.

**Build** — combine your card with table cards and declare a value. Numeric builds run
1–10; face cards may build only with matching ranks (J=11, Q=12, K=13 for capture matching).
You must be holding a card that captures the declared value, and you must **keep** holding
one — the engine rejects any play that would leave one of your builds uncapturable.

### Builds in detail

A build starts as a **single build** and can be modified:

- **Raise it** — add a card to increase the declared value. Ownership transfers to whoever
  raised it. You must hold the new capture value.
- **Add at value** — add another set worth the same value. This converts the build into a
  **multi-build** and transfers ownership.

**Two builds of the same value never coexist.** Declaring a build worth the same as one
already on the table joins that stack instead of starting a second one, exactly as if you
had added at value: the build becomes a multi-build and ownership transfers to you. So
building 7+2 as a 9 and later 6+3 as a 9 leaves one locked 9-build of four cards, not two
raisable 9s.

**Multi-builds are locked**: once a build is multi, no one can raise it or add to it. It
can only be captured.

An opponent's single build is **sweep material** — you can capture it, and you can combine
it with loose table cards in the same sweep to steal the whole thing. You may never combine
your *own* build with other cards; you capture it as-is.

### Sweeps, table cards, and winning

Clearing the table completely — every card and every build — scores a sweep. Sweep counts
reset each deck.

Remaining table cards are awarded once, at deck exhaustion, to the last player who captured.
(The Connecticut variant awards them after each 4-card hand instead.)

Scores accumulate across decks. When a deck is scored, the dealer role swaps and a new deck
is dealt. **The first player to reach the win score (11) wins.** If both cross in the same
deck the higher score takes it; if they cross tied, play continues.

## Scoring variants

Three presets ship in `Assets/ScorePresets/`. The active one is selected on the
`ScoringManager` component in the scene — currently **Rick's New England**.

| | Rick's New England | Standard | Connecticut |
|---|---|---|---|
| Most cards | 1 | 3 | 3 |
| Most spades | 1 | 1 | 1 |
| Big Casino (10 ♦) | 3 | 2 | 2 |
| Little Casino (2 ♠) | 2 | 1 | 1 |
| Each ace | 1 | 1 | 1 |
| Each sweep | 0 | 1 | 1 |
| Win score | 11 | 11 | 11 |
| Table cards awarded | at deck end | at deck end | after each hand |

"Most cards" and "most spades" pay nothing on a tie. Rick's New England distributes exactly
11 points per deck, so a deck can win the game outright.

Every value above — including which card is Big or Little Casino — is a field on a
`ScoreVariables` ScriptableObject, so new variants are assets, not code.

## AI

Difficulty is set per player on the `GameManager` component (default **Medium**).

- **Easy** — random valid moves.
- **Medium** — prefers captures, weights aces, Big/Little Casino and spades, and will add
  to builds. Prefers stealing an opponent's build when one is available.
- **Hard** — strategic evaluation with lookahead: sweeps first, then the scoring cards,
  then card count; adds to *and* raises builds, and minimizes what it gives away when
  trailing.

The Hard evaluator also drives the **Suggest** button, so hints are as good as the Hard AI.

## Playing

Click a hand card and any table cards you want, then press **Sweep**, **Trail**, or
**Build**. Selection is table-first: picking table cards highlights the hand cards that
can act on them. **Suggest** asks the Hard AI what it would do. The captured-pile viewer
docks to either side; the draw pile shows what's left.

The build version is stamped in the bottom-right corner as `v1.1.0`.

## Development

The rules engine is pure C# with no Unity dependencies, so it tests without launching
the editor:

```bash
bash Tests~/run-tests.sh
```

This compiles `PlayingCard`, `CaptureChecker`, `Build` and `GamePlayer` with the Unity
editor's bundled Roslyn and runs the capture, build, and sweep suites as console
executables. Set `UNITY_EDITOR` to pick a specific editor install.

Built with Unity 6000.2.10f1. See `CLAUDE.md` for architecture notes and `CHANGELOG.md`
for release history.
