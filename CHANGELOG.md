# Changelog

All notable changes to the Casino card game. Versions follow semver.
The app version lives in `ProjectSettings/ProjectSettings.asset` (`bundleVersion`)
and renders in the bottom-right corner of the game UI (`v{Application.version}`),
so every screenshot identifies its build. Each release gets a `v<version>` git tag.

## [1.1.0] - 2026-08-05

The UI redesign: a chosen visual direction, a layout engine that survives more
than one screen shape, and a harness that plays whole games so the UI is checked
against the parts of a game nobody had ever looked at.

### Visual direction (Parlor)
- `CasinoTheme` palette, with every colour in the UI routed through it
- `CasinoArt` procedural art: lit felt with a separate tiled grain layer, a gilt
  card back with a brass edge, and a rounded rail. No binary art assets
- Source Serif 4 (SIL OFL) for card ranks and display text, via a generated TMP
  asset. It replaced Libre Baskerville, whose swash J and old-style figures meant
  a row of ranks never shared a baseline
- Card faces gained corner indices, so a build stack is readable when overlapped

### State the rules require, made visible
- Four independent card states (selected, capturable, suggested, opponent
  taking) instead of two overloaded ones, so no state is carried by hue alone
- Build ownership in colour, and RAISABLE vs LOCKED tags distinguishing a
  malleable single build from a locked multi
- Trail states its refusal reason when you own a build

### Layout
- `CasinoLayout` profiles chosen by aspect ratio (Wide, Compact, Portrait), so a
  new screen shape is a new Profile rather than an edit to `UIManager`
- Scoreboard reorganised, with a `THIS DECK` label separating the cumulative
  score from per-deck capture counts that reset when a deck is scored
- Game-over panel states winner, final score and target; previously an empty box
  with a stock button, with the result in corner text
- Modal panels are opaque over a scrim, which also blocks clicks to the board

### Tooling
- `CasinoAutoPlay` plays complete games unattended and screenshots the moments
  worth keeping. Everything before it was verified on an opening deal, which
  never reaches captures, sweeps, round ends, deck exhaustion or game over
- `ScreenshotCapture`, `GameViewSizePin` (comparable screenshots), and
  `Tests~/check-layout.py` (geometry check across all three profiles)
- `runInBackground` enabled: Play used to stop whenever the editor lost focus,
  which is indistinguishable from a hang
- `AutoVerifyPlay` no longer wedges the editor by restarting Play mid-transition

## [1.0.0] - 2026-08-04

First complete, playable release after the project's revival.

### Rules engine (Rick's family rules)
- Three plays per turn: Sweep, Trail, or Build
- Chosen sweeps: player picks which matching cards/sets to take, including
  simultaneous multi-set captures; partial captures allowed
- Builds 1-10 and same-rank face builds (J/Q/K); multi-builds
- Single builds are malleable (raisable, ownership transfers to the raiser);
  multi-builds are locked; add-at-value locks a build as multi
- Opponents' single builds are sweep material combinable with table cards
  (a steal); owners may never combine their own builds
- Build owners must always hold true on the build capture
- Trail freely unless owning a build; last capturer takes the table at deck
  exhaustion; win at 11 (configurable), higher score wins if both cross, ties
  play on
- 31 out-of-Unity tests in `Tests~/` (`bash Tests~/run-tests.sh`)

### Game
- Full 1-player game vs AI (Easy/Medium/Hard; Medium adds to builds, Hard
  adds and raises, both prefer steals)
- Complete UI: three-play buttons, table-first selection with hand-card
  highlighting, in-place build stacks, move banner pacing, card backs,
  dealing animations, dwindling draw pile, per-deck scoring summary,
  dockable captured-pile viewer, suggestions

### Tooling
- Repo repaired to open from a fresh clone (.meta files tracked, packages
  pinned, ProjectVersion.txt committed)
- AutoVerifyPlay editor loop, layout-report ground truth, SceneView2D
