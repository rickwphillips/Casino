# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## What this is

A Unity 6000.2.10f1 (arm64) implementation of Casino, the fishing card game. Single scene
(`Assets/Scenes/Scene.unity`), C#, ScriptableObject scoring presets. Currently a 1-player game:
Human (non-dealer) vs AI (dealer). Version lives in `ProjectSettings/ProjectSettings.asset`
(`bundleVersion`, currently 1.0.0) and renders bottom-right in-game as `v{Application.version}`,
so every screenshot identifies its build. Releases are tagged `v<version>` with a `CHANGELOG.md` entry.

Docs/planning for the project live outside this repo at
`/Users/rickphillips/FreddyRhetorickContexts/royal-casino/` (docs only, no code). That folder's
`CLAUDE.md` describes a *Royal Casino* variant (face cards as 11/12/13, Dominican/Hungarian
rulesets) that this codebase does not implement — treat it as aspirational, not as a spec.
Do not confuse this project with `FreddyRhetorickContexts/CardGame` (`RPDeckBuilding`), a
separate deck-builder prototype.

## Commands

```bash
# Rules-engine tests — no Unity install/launch needed. Compiles the pure-logic
# scripts with the editor's bundled Roslyn and runs them as console executables.
bash Tests~/run-tests.sh

# Run one suite: the script loops over Tests~/*Tests.cs, so temporarily point it
# at a single file, or set UNITY_EDITOR to pick a specific editor install.
UNITY_EDITOR=/Applications/Unity/Hub/Editor/6000.2.10f1/Unity.app/Contents bash Tests~/run-tests.sh
```

There is no build/lint command in the repo — building is done through the Unity editor.
`Tests~/` is a Unity-ignored folder (trailing `~`), which is why these tests live outside
the asset pipeline and compile standalone.

To exercise the game itself: `touch auto-verify.flag` in the repo root, then focus Unity —
`Assets/Scripts/Editor/AutoVerifyPlay.cs` (re)starts Play mode automatically.

## The rules the code implements

These are Rick's family rules, not the generic Casino rules. `README.md` was rewritten and now
matches: it documents win at 11 and captures as chosen rather than automatic. (This note used to
warn that the README still said 21 and "largest possible capture automatically"; that warning
outlived the problem and was itself the stale thing, which cost a pass of chasing a bug that had
already been fixed.) When rules and prose disagree, the engine + `Tests~/` win.

- **Three plays per turn: Sweep, Trail, or Build.**
- **Sweep (capture)** is optional and partial: the played card *can* take all matching-rank cards
  and all sets summing to its value simultaneously, but the player **chooses** which to take.
  Face cards capture only their own rank.
- **Trail** is always free *except* when you own a build (owner must sweep or build).
- **Build**: numeric values 1–10, or same-rank face builds (J/Q/K; J=11 Q=12 K=13 for matching).
  You must hold the capture card. Multi-builds stack several sets of one value.
- Single builds are malleable (raisable; ownership transfers to the raiser). Multi-builds are
  locked. Adding at value locks a build as multi.
- An **opponent's** single build is sweep material and may be combined with table cards (a steal);
  owners may never combine their own build.
- Win at 11 (configurable per preset). If both cross in the same hand the higher score wins;
  a tie plays on.
- Sweeps score per round and reset each round. Table cards are awarded once, at deck exhaustion,
  to the last capturer.

## Architecture

Singleton `MonoBehaviour` managers with logic pushed down into pure C# classes.

**Pure logic (no Unity dependencies — this is what `Tests~/` compiles):**
`PlayingCard.cs`, `CaptureChecker.cs`, `Build.cs`, `GamePlayer.cs`. If you add logic that the tests
should cover, it has to land in one of these four (or be added to the `csc` invocation in
`run-tests.sh`) and must not touch `UnityEngine` beyond what's already referenced.

- `CaptureChecker.cs` — the rules kernel. `GetValidCaptures` (max union), `IsExactCaptureSet` /
  `IsExactCaptureSetWithBuilds` (validates a *chosen* sweep, including build steals),
  `PossibleBuildValues` (multi-build aware), `CanPartitionExact`, `BuildCaptureValue`
  (face cards → 11/12/13) vs `GetCardValue` (table/sum value).
- `Build.cs` — ownership + malleability state machine. `ModifyBuild` (raise; throws on multi),
  `AddToBuild` (add at value → forces `IsMultiBuild`, transfers ownership).

**Orchestration:**
- `GameManager.cs` (~1000 lines) — turn flow, dealing, scoring, game/round phases
  (`GamePhase.Playing/RoundEnd/GameOver`). The move entry points are `TryCaptureSelected`,
  `TryCreateBuild`, `TryAddToBuild`, `TryRaiseBuild`, and `PlayCard(..., forceTrail)`; each
  validates against `CaptureChecker` and returns bool rather than throwing.
  `GetSuggestionForCurrentPlayer()` delegates to the Hard AI evaluator.
- `AIPlayer.cs` — Easy (random valid) / Medium (captures, adds to builds) / Hard (strategic
  evaluation with lookahead, adds *and* raises). Both Medium and Hard prefer steals. The Hard
  evaluator doubles as the human's Suggest hint, so changes there change hints too.
- `ScoreVariables.cs` (ScriptableObject) / `ScoringConfig.cs` / `ScoringManager.cs` with presets in
  `Assets/ScorePresets/`. Presets carry per-category point values, `winScore`, and
  `tableCardAwardTiming`. Note the C# defaults still say 21; every shipped preset sets 11.

  `ScoringManager` has three inspector slots — `standardVariant`, `connecticutVariant`,
  `customVariant` — and a `selectedVariant` enum. **The scene ships `selectedVariant: 2`
  (Custom), and the Custom slot holds `RicksNewEnglandVariant`.** So the live ruleset is
  Rick's New England, despite "Custom" being the selection; the enum name is a slot, not a
  ruleset. Rick's New England pays exactly 11 points per deck (1 cards + 1 spades + 3 Big
  Casino + 2 Little Casino + 4 aces, sweeps worth 0), matching `winScore`, so a single deck
  can win outright.

**UI:**
`UIManager.cs` (~2000 lines) is the only UI path and owns the *entire* layout in code:
`EnforceLayout()` sets a 1280x720 reference canvas (`matchWidthOrHeight = 1`, match height)
and hides leftover scene objects. Every color it draws comes from `CasinoTheme.cs`; there
are no raw `new Color(...)` literals left in `UIManager`, and new ones should not be added.
**Do not hand-edit scene UI positions** — runtime code will override them. `RefreshUI`,
`OnCardSelected` (table-first selection with hand highlighting), `AnimateDeal` /
`AnimateCapture` / `AnimateTrail` ghost-card animations, `ShowMove` banner pacing,
`ShowRoundSummary`, pile viewer, draw pile.

The scene wires buttons in code, not through UnityEvents — `Scene.unity` contains zero
`m_MethodName` entries, so searching the scene for a handler name will never find anything.

## Workflow gotchas

- `layout-report.txt` (gitignored) is written by `UIManager` at startup and is the **ground truth**
  for UI geometry — read it before diagnosing any layout complaint. First-frame values can be
  stale; a settled snapshot is written ~1s later.
- Editor pref `ScriptCompilationDuringPlay=2` (stop play + recompile) is set, so script edits stop
  a running play session instead of silently running old code.
- **The whole verify loop is drivable without the user.** `touch auto-verify.flag`, then
  `osascript -e 'tell application "Unity" to activate'` — focusing the editor recompiles and
  re-enters Play. `ScreenshotCapture.cs` then writes a settled PNG to `screenshots/` 3s in.
  Drop `screenshot.flag` for an extra shot mid-session, or press F9 (Shift+F9 for 2x).
- **Real mouse clicks, when a harness is not enough.** `Tests~/click.swift`
  (`swiftc -O Tests~/click.swift -o /tmp/click`, then `/tmp/click <x> <y>`) posts
  genuine CGEvents. AppleScript's `click at` does NOT work here: it goes through the
  accessibility layer and resolves to the nearest element, which for a Unity Game
  view is the window, so the game never sees the click. Map game coordinates with
  `screencapture -x` of the desktop plus the Game view's origin and scale.
  This is the only way to test what a player actually experiences, including
  whether a button is enabled at all. Driving click handlers directly walks past
  `interactable`, and a disabled button is often the game's real refusal message.
- **Two harnesses, and they test different layers.** `autoplay.flag` runs
  `CasinoAutoPlay`, which plays a full game by calling `GameManager` directly: that
  proves the rules and the board survive captures, sweeps, round ends and game over,
  but it never touches selection, highlighting, or the hint line.
  `interaction-probe.flag` runs `CasinoInteractionProbe`, which clicks like a player
  and writes the hint text back to `interaction-probe.txt` after each step. Drive
  cards with `CardUI.SimulateClick()`, never `UIManager.OnCardSelected` alone: a
  click is `SetSelected` *then* `OnCardSelected`, and calling only the second leaves
  `isSelected` stale so the whole UI behaves as though nothing was picked up. A
  probe that got this wrong reported the UI as silent when the probe was the fault.
- **Keep Unity focused for the whole of an unattended run.** `ProjectSettings.asset`
  has `runInBackground: 0`, so a Play session stops executing the instant the editor
  loses focus. Coroutines freeze mid-wait and resume only when it comes back. This
  reads exactly like a hang: the flag is consumed at scene load (still focused), then
  the transcript never grows again. Two sessions were lost to diagnosing it as an
  infinite loop in the AI evaluator. If a harness must survive losing focus, flip
  the setting rather than assuming the code is at fault.
- **Pin the Game view before trusting a screenshot.** `Editor/GameViewSizePin.cs` forces a fixed
  1280x720 on editor load (menu: **Casino > Pin Game View to 1280x720**), which makes canvas
  units equal screen pixels. On Free Aspect the canvas width floats with the window (observed
  1350 and 1182 on consecutive runs) and no two screenshots are comparable.
- **All `.meta` files are tracked.** The original `.gitignore` excluded them and broke fresh clones
  for nine months (fixed 2026-08-03). Keep it that way; `Packages/manifest.json` +
  `packages-lock.json` and `ProjectVersion.txt` are committed for the same reason.
- `Assets/Scripts/Editor/SceneView2D.cs` forces a 2D scene view (this is a pure-UI game).
- **Quit the Unity editor before editing `Scene.unity` from outside it.** The editor holds the
  scene in memory and will clobber on-disk changes when it saves. Batch mode also refuses while
  the project lock (`Temp/UnityLockfile`) is held.
- Scene roots are down to eight: Main Camera, Global Light 2D, ScoringManager, GameManager,
  GameLogger, Canvas, EventSystem, UIManager. A legacy inactive `UISetup` root (a second canvas
  plus a text-overlay UI) was removed in the 2026-08-04 cleanup; don't reintroduce a parallel
  UI hierarchy.

## Workspace conventions

`/Users/rickphillips/FreddyRhetorickContexts/.cursor/rules/coding-paradigms.mdc` applies workspace-wide;
most of it is TypeScript/React-specific and doesn't transfer, but two things do: prefer declarative
expression (LINQ, expression-bodied members, pattern-matching `switch` — the existing code already
leans this way) over manual loops and mutation, and keep duplication low by extracting shared logic
rather than copy-pasting.
