# Changelog

All notable changes to the Casino card game. Versions follow semver.
The app version lives in `ProjectSettings/ProjectSettings.asset` (`bundleVersion`)
and renders in the bottom-right corner of the game UI (`v{Application.version}`),
so every screenshot identifies its build. Each release gets a `v<version>` git tag.

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
