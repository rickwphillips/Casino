# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## What this is

A Unity 6000.2.10f1 (arm64) implementation of Casino, the fishing card game. Single scene
(`Assets/Scenes/Scene.unity`), C#, ScriptableObject scoring presets. Currently a 1-player game:
Human (non-dealer) vs AI (dealer). Version lives in `ProjectSettings/ProjectSettings.asset`
(`bundleVersion`, currently 1.1.0) and renders bottom-right in-game as `v{Application.version}`,
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

```bash
# macOS player. Quit the editor first: batch mode refuses while the project
# lock (Temp/UnityLockfile) is held.
/Applications/Unity/Hub/Editor/6000.2.10f1/Unity.app/Contents/MacOS/Unity \
  -quit -batchmode -nographics -projectPath "$PWD" \
  -buildOSXUniversalPlayer "$PWD/build/Casino.app" -logFile "$PWD/build-log.txt"
```

A player build catches what the editor cannot, and the first one ever run here
found two things. `EditorBuildSettings.asset` still listed the template's
`SampleScene.unity`, which does not exist, so the build would have shipped an
empty player. And `GameLogger.cs` had a stray `using UnityEditor.VersionControl`,
which compiles happily in the editor and fails every player build, because
`UnityEditor` is not available outside it. Editor-only namespaces in runtime
scripts are invisible until you build.

Player log (not the editor log) is at
`~/Library/Logs/DefaultCompany/Conn-Casino/Player.log`; screenshots land in
`~/Library/Application Support/com.DefaultCompany.2D-URP/screenshots/`. Both
paths come from `productName`/`applicationIdentifier`, which are still template
leftovers ("Conn-Casino", `com.DefaultCompany.2D-URP`); changing them moves the
save/log paths, so do it deliberately.

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
- **Two builds of one value never coexist.** Declaring a build at a value already on the table
  merges into that stack (`Build.FindMergeTarget` / `MergeGroup`), locking it multi and
  transferring ownership, rather than creating a second raisable build of the same value.
- An **opponent's** single build is sweep material and may be combined with table cards (a steal);
  owners may never combine their own build.
- Win at 21 (configurable per preset; the title settings can override per session, and the
  autoplay harness plays to 11 so runs stay short). If both cross in the same hand the
  higher score wins; a tie plays on.
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
  `tableCardAwardTiming`. Code defaults and every shipped preset agree on `winScore` 21
  (the 11 the presets carried through 2026-08-11 was a testing convenience, now the
  harness's job via `OverrideWinScore`).

  `ScoringManager` has three inspector slots — `standardVariant`, `connecticutVariant`,
  `customVariant` — and a `selectedVariant` enum. **The scene ships `selectedVariant: 2`
  (Custom), and the Custom slot holds `RicksNewEnglandVariant`.** So the live ruleset is
  Rick's New England, despite "Custom" being the selection; the enum name is a slot, not a
  ruleset. Rick's New England pays exactly 11 points per deck (1 cards + 1 spades + 3 Big
  Casino + 2 Little Casino + 4 aces, sweeps worth 0), so at `winScore` 21 a game is
  usually two decks; the autoplay harness overrides to 11 so one deck can still win.

**UI:**
`UIManager.cs` (~3500 lines) is the only UI path and owns the *entire* layout in code:
`EnforceLayout()` sets a 1280x720 reference canvas (`matchWidthOrHeight = 1`, match height),
applies the zones from `CasinoLayout.cs` (three Profiles — Wide / Compact / Portrait; each
zone is anchor+pos+size with pivot == anchor), and hides leftover scene objects. Every color
it draws comes from `CasinoTheme.cs`; there are no raw `new Color(...)` literals left in
`UIManager`, and new ones should not be added — `CasinoTheme.WithAlpha` is private, so tinted
colors are defined inside `CasinoTheme`, not built at call sites. **Do not hand-edit scene UI
positions** — runtime code will override them. `ReAnchor` also resets `localScale` to one
(the scene shipped a 0.75 scale on both hand containers for years; scale is part of the
arrangement now).

Layout since the 2026-08-10/11 "Parlor" redesign:

- **Right centre**: the score plaque (`Scoreboard`) — one plaque for both players, at the
  right centre of every profile with the AI's take above it and the human's below. Leader
  struck in `ScoreLeader` (ivory), trailer in `ScoreTrailing` (brass), a tie brightens both.
  The ephemeral message toast (`Message`) holds the top-left corner (top-right in Portrait);
  it is the object `UIManager.hintText` points at: call sites still just assign
  `hintText.text`, and `TickMessage()` in `Update()` watches the string for changes and
  drives the fade (hold 3.4s, fade 0.9s).
- **Move log**: `logPanel`, a `ScrollRect` + `RectMask2D` + one TMP with a `ContentSizeFitter`,
  appended by `ShowMove` — there is no centre banner any more. Toggled by the circled "≡"
  next to the "?" Suggest button in the bottom-left corner; the panel opens upward
  (`UIManager.ToggleMoveLog()` is the harness hook).
- **Right rail**: each player's captured cards as a face-up stack (`CapturedHuman`,
  `CapturedAI`), newest in front and lowest so each card underneath keeps its top-corner
  pips visible; the pip offset shrinks as the pile grows, and a stack that outgrows its
  zone wraps into further columns growing leftward. The trophy-coin shelves sit beside the
  takes. Clicking a stack opens the full grid centred over the table (`L.PileViewer`).
- **Bottom**: the human hand fans (`ApplyHandFan`) and is sunk below the screen edge so card
  bottoms clip (Portrait deliberately keeps the hand fully on screen instead). Hovering a
  selectable card lifts it (`HoverLift`), stands it upright, tints it `CardHover`, and raises
  its sibling index. A gold pool (`HandGlow`) lights the felt while the turn is the human's,
  scaled by `SizeHandGlow` from full at 4 cards down to `GlowFloor` at one.
- **Gone, do not reintroduce**: the mid-table gold hint line, the centre `MoveBanner`, the
  bottom-left pile-count boxes, and the `Current Turn:` / `Playing...` lines.

Interaction and animation core: `RefreshUI`, `OnCardSelected` (table-first selection with
hand highlighting; deselecting the hand card clears the table selection via
`ClearTableSelection`), `AnimateDeal` / `AnimateCapture` / `AnimateTrail` ghost-card
animations (capture ghosts intentionally shrink toward 0.5 scale in flight),
`ShowRoundSummary`, draw pile. `GameManager.aiLeadInDelay` (3.2s, vs `aiMoveDelay` 1.5s)
paces the AI when it leads a freshly dealt hand.

The scene wires buttons in code, not through UnityEvents — `Scene.unity` contains zero
`m_MethodName` entries, so searching the scene for a handler name will never find anything.

**The title screen** is a full-canvas overlay built in `CreateTitleScreen()`, not a second
scene. `GameManager` still deals on `Start` exactly as it always has and the overlay simply
covers the result: deferring `InitializeGame` would leave `deck`/`dealer`/`nonDealer` null
while `RefreshUI` runs. The only thing lost behind the title is the deal animation, so
`AnimateDeal` stashes its arguments in `pendingDeal` while `TitleIsUp` and `DismissTitle`
replays them; that animation is pure ghost cards over an already-dealt board, so replaying
it changes no game state. `Update()` re-raises the overlay every frame because cards, ghosts
and builds are all created *after* it and paint order is sibling order.

A **splash** (the four suits on dark ink, `CasinoTheme.SplashInk`/`SplashSuits`) covers the
title for ~1.3s then fades; a click skips it. It is a child of the title overlay, so
`SkipTitle` skips it too and the per-frame re-raise cannot separate them.

The title carries a **Settings** toggle whose panel starts closed. Two settings so far:
the win total, a stepper wired to `ScoringManager.OverrideWinScore` (session-only override;
presets and code default both say 21 now, and the autoplay harness overrides its own runs
to 11), and the AI difficulty, one button cycling
Easy/Medium/Hard through `GameManager.SetAIDifficulty` (both seats, live AIs included).
`title-probe.flag` runs `CasinoTitleProbe`, which walks it all unattended: splash, closed
panel, opened, both settings changed and reverted, dismissed.

Two ways to skip the title: `UIManager.SkipTitle` (set by `CasinoAutoPlay`,
`CasinoInteractionProbe` and `CasinoStatePreview` in their `Install()` methods), or dropping
`skip-title.flag` in the repo root. Unlike the other flags that one is **not consumed**,
because an unattended verify loop wants the board in every run rather than only the first
(so lift it before a `title-probe.flag` run and restore it after).

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
- **Never launch Unity without checking for a running instance first.**
  `ps aux | grep "[H]ub/Editor/6000"`. A second launch cannot open the project
  (the `Temp/UnityLockfile` blocks it) but it truncates `~/Library/Logs/Unity/Editor.log`
  on the way out, destroying the live editor's log. You are then debugging with no
  log at all, and the symptoms look like the editor hanging or ignoring code
  changes. This wasted more time today than any actual bug. To bring a running
  editor forward use `open -a <path to Unity.app>`, never the binary directly.
- **Focusing the editor does not reliably trigger a recompile.** It usually does, and then
  it stops: on 2026-08-07 two edits compiled on focus and the third did not, through a
  stopped-and-focused editor, for six minutes and four `open -a` calls. Meanwhile
  `auto-verify.flag` kept restarting Play against the *old* assembly and writing screenshots
  that looked like the new code had no effect. Force it instead:

  ```bash
  osascript -e 'tell application "System Events" to tell process "Unity" \
    to click menu item "Refresh" of menu "Assets" of menu bar 1'
  ```

  Stop Play first if it is running. Unlike clicking in the Game view, menu-bar clicks
  through the accessibility layer work fine. Always confirm with the assembly mtime below
  before believing a screenshot.
- **To check whether code compiled, look at the assembly, not the screen.**
  `stat -f %m Library/ScriptAssemblies/Assembly-CSharp.dll` against the source
  file's mtime says definitively whether the editor picked up an edit. Screenshots
  of the console only work when Unity is frontmost, and silently capture whatever
  else is there when it is not. For a definitive answer with no editor at all,
  quit it and run `-quit -batchmode -nographics -logFile compile.txt`, then grep
  for `error CS`.
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
  but it never touches selection, highlighting, or the message toast.
  `interaction-probe.flag` runs `CasinoInteractionProbe`, which clicks like a player
  and writes the toast text back to `interaction-probe.txt` after each step. Drive
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
- **Pin the Game view before trusting a screenshot.** `Editor/GameViewSizePin.cs` pins a fixed
  size on editor load, one per `CasinoLayout` profile (menu: **Casino > Game View** — Wide
  1280x720, Compact 1024x768, Portrait 720x1280), which makes canvas units equal screen
  pixels. Tooling switches profile unattended by dropping `gameview.txt` containing `WxH`
  (e.g. `720x1280`) in the repo root before a reload. On Free Aspect the canvas width floats
  with the window (observed 1350 and 1182 on consecutive runs) and no two screenshots are
  comparable.
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
