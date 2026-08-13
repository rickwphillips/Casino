# Changelog

All notable changes to the Casino card game. Versions follow semver.
The app version lives in `ProjectSettings/ProjectSettings.asset` (`bundleVersion`)
and renders in the bottom-right corner of the game UI (`v{Application.version}`),
so every screenshot identifies its build. Each release gets a `v<version>` git tag.

## [1.4.0] - 2026-08-13

### Settings that stay set
- The title panel's choices (win total, AI difficulty) persist across
  launches via PlayerPrefs (IndexedDB on the web build): change them once,
  and every later visit opens with them applied
- Harness runs skip the title, so their explicit overrides stay deterministic
- `CasinoTitleProbe` verifies persistence across two sessions: one run arms
  23/Hard and leaves it saved, the next must open with it applied, then
  reverts to 21/Medium

## [1.3.0] - 2026-08-12

The game goes public: a Web build live on the portfolio, and the polish that
came from actually watching it played there.

### Web
- WebGL build target (`Casino > Build Web`, or batch `-buildTarget WebGL
  -executeMethod WebBuild.Build`): Gzip with the decompression fallback, ~15MB,
  no server configuration required
- Live at rickwphillips.com/app/projects/casino/, deployed by
  `deploy-casino.sh`; listed on the portfolio Projects page
- Gzip + fallback are committed player settings, so the build is reproducible

### Table
- The trophy-coin shelves move to the left rail: AI's under the toast, the
  human's above the utility buttons (beside the draw piles in Portrait), both
  rows growing toward the table. The right rail keeps the takes and the plaque

### Opening
- The splash signs its maker: "A FREDDY RHETORICK CARD GAME"
- The AI waits to be watched: the game deals behind the title, so when the AI
  led, its lead-in clock expired before the board was visible and the first
  move looked instant. The AI's turn now holds until the title is dismissed

## [1.2.0] - 2026-08-11

The parlor gets finished furniture and a front door: one scoreboard plaque in
the same seat on every screen shape, layouts that wrap instead of colliding, a
splash and settings on the title, and the shipped rules corrected to 21.

### Scoreboard, coins, and the right rail
- One score plaque replaces the two floating score lines; leader struck in
  ivory, trailer in brass, and it holds the right centre in all three profiles
- Trophy coins (aces, Big/Little Casino, majorities) splash in on capture and
  sit on shelves beside each player's take
- Captured cards render as face-up pip stacks that wrap into columns when a
  take outgrows its zone; clicking one opens the pile grid over the table
- Deck count stamped on the deck itself, centred on the visible top card

### Layout that yields instead of overlapping
- The table row wraps at its zone's width (`ApplyTableWrap`), multi-card builds
  keeping their natural footprint; the Portrait zone stops short of the plaque
- The hand fans, sinks below the bottom edge (landscape), lifts a hovered card
  upright, and a gold pool lights the felt on your turn, shrinking with the hand
- Both hand containers lose a scene-shipped 0.75 scale that had drawn hand
  cards three-quarter size for years
- Portrait: AI draw pile level with the AI hand row, coins on the right rail

### The game's voice
- The centre move banner and mid-table hint line are gone; moves announce in a
  fading toast under the plaque and persist in a scrollable move log opened
  from a bottom-left button
- The Trail button no longer appears while you own a build (an owner must
  sweep or build), and the play preview says so instead of promising a trail
- `GameManager.aiLeadInDelay` (3.2s) paces the AI when it leads a fresh deal

### Title, splash, and settings
- A splash (the four suits on house ink) holds for a breath and fades into the
  title; a click skips it
- The title gains a Settings panel, closed by default: win total stepper and
  AI difficulty (Easy/Medium/Hard, both seats, live AIs included). Session-only
- All three score presets now ship the real rule, win at 21; the old 11 was a
  testing shortcut and is now the autoplay harness's explicit override

### Harnesses
- `CasinoTitleProbe` (`title-probe.flag`) walks splash, panel, both settings
  changed and reverted, dismissal
- `CasinoInteractionProbe` asserts that deselecting the hand card clears the
  table selection, and that Trail is hidden while a build is owned
- `autoplay.flag` accepts "hard" to play the whole game against the Hard AI

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
