# Casino UI Design Plan

Working plan for redesigning the game UI using Claude Code's design tooling.
Written 2026-08-04. Status: **Stages 0-3 done. Awaiting Rick's pick of a
direction, which unblocks Stage 4 (layout engine rework).**

## Decisions already made (by Rick, 2026-08-04)

1. **Visual direction: explore 2-3 directions.** Do not just refine the existing
   green-felt/gold/navy look, and do not go straight to a bold reinvention.
   Build several distinct identities and pick from real options.
2. **Art assets: yes, introduce them.** This project currently has zero art assets.
   PNGs and a TMP font asset may land in `Assets/` with tracked `.meta` files.
3. **Target: desktop AND mobile.** This is the constraint with the largest
   architectural consequence. See "Why mobile is the hard part" below.

## Current state of the UI (verified, not assumed)

- **No art assets exist.** The card back is a 64x96 `Texture2D` generated in code
  with a diagonal lattice (`CardUI.cs:22-43`). Card faces are a TMP string like
  `"5♥"` on a flat `Image`. No pips, no court art, no corner indices.
- ~~**~25 colors are hardcoded**~~ Resolved by Stage 1. There were 39, all in
  `UIManager.cs` and nowhere else; they now route through `CasinoTheme`.
- **Layout is imperative C#.** `UIManager.EnforceLayout()` pins every element to a
  fixed reference canvas (now 1280x720) with `matchWidthOrHeight = 1` (match height).
- **`layout-report.txt`** (gitignored, repo root) dumps runtime geometry at startup
  and again ~1s later. It is the existing ground truth for what rendered where.

### Why mobile is the hard part

`matchWidthOrHeight = 1` plus fixed anchors is a landscape assumption baked into
the layout engine. Portrait breaks it: the right-side vertical button stack, the
top-right score panel, and 80x120 cards all fail at ~390pt wide. Stage 4 is
therefore not "port the mockup" but "make the layout engine breakpoint-aware,
then port."

`Assets/Scripts/SafeArea.cs` was deleted in the 2026-08-04 cleanup as dead code.
Mobile makes it relevant again. Recover with `git checkout <pre-cleanup-sha> --
Assets/Scripts/SafeArea.cs` when Stage 4 needs it. Do not restore it earlier: it
is attached to nothing and would just be dead code a second time.

## Capability inventory (what the tooling is actually good for)

| Capability | Fit | Notes |
|---|---|---|
| `artifact-design` skill | High | Governs palette/type decisions. Has a useful list of AI-default looks to avoid |
| `Artifact` tool | High | Self-contained HTML at a private re-deployable URL. Home for the spec and mockups |
| `claude-in-chrome` | High | Exact viewports, screenshots, GIFs. Closes the visual loop |
| Read-on-images | High | Required for critiquing real game screenshots |
| `canvas-design` skill | Narrow | Card BACKS, felt, title art only. Explicitly wrong for HUD and card faces |
| `DesignSync` tool | Long-run | Authenticates fine; `list_projects` returned `[]`, so nothing exists yet |
| `artifact-diagramming` | Medium | Turn flow, build-state machine. Not visual design |
| `dataviz` | Low | Only the scoring summary panel |

### Correction worth remembering

`canvas-design` is a poor fit for card faces. It is built for abstract,
museum-grade compositions with deliberately sparse text and warns against
anything reading as game-like. Pip layouts and court cards are the opposite
problem: a systematic, legible grid. Split the art work three ways:

- `canvas-design` for card backs, felt texture, title art
- Hand-authored vector work for card faces, pips, indices
- A TMP font asset for typography (the single largest visual lever available)

## Decision: the reference resolution is 1280x720 (Rick, 2026-08-04)

Supersedes the 800x600 assumption throughout this document. 800x600 was chosen
before there was any way to look at the game; every observed run has been
widescreen. Consequences:

- `UIManager.EnforceLayout()` now sets `referenceResolution = 1280x720`. One
  line; revert it if the wide layout reads worse than the stretched 4:3 one.
- Stage 3 mockups are authored at **1280x720**, and the anchors-plus-offsets
  1:1 mapping described at the bottom of this file now refers to that size.
- `Assets/Scripts/Editor/GameViewSizePin.cs` pins the Game view to a fixed
  1280x720 on every editor load, so canvas == screen == reference and
  screenshots are reproducible. Menu fallback: **Casino > Pin Game View to
  1280x720**. It reflects into internal editor API (`GameViewSizes`,
  `GameView.selectedSizeIndex`) and is wrapped so an API change degrades to a
  warning telling you to set it by hand.
- The landscape problem in Stage 4 is not only a mobile problem. The layout
  already came apart on desktop at any aspect wider than 4:3. Breakpoint work
  buys correctness on the machine Rick develops on, not just on phones.

## Stages

**Stage 0 — Screenshot hook.** BLOCKING everything else. Built:
`Assets/Scripts/ScreenshotCapture.cs`. Self-installing via
`[RuntimeInitializeOnLoadMethod]`, so there is nothing to wire in `Scene.unity`.
Writes `casino-v{version}-{timestamp}-{label}.png` to the gitignored
`screenshots/` folder (`persistentDataPath/screenshots` in a player). Three
triggers:

- automatic, once, 3s in (the opening deal tweens ~12 ghost cards and is still
  in flight before ~2s; after it the board waits on human input and holds still)
- **F9** on demand; hold Shift for a 2x supersampled capture
- dropping `screenshot.flag` in the repo root, polled every 0.5s — the
  agent-drivable trigger, mirroring `auto-verify.flag`

Uses the new Input System (`activeInputHandler: 1`), so `Keyboard.current`, not
legacy `Input.GetKeyDown`. **Verified working 2026-08-04**: first run produced
`casino-v1.0.0-20260804-112123-settled.png`. The same run also confirmed the
staged dead-code cleanup did not break `Scene.unity` — the game deals and plays.
F9 is still unconfirmed on macOS (F-keys may be claimed by the OS); the flag
trigger is the fallback.

### What the first screenshot revealed

**The 800x600 reference canvas is a fiction unless the Game view is pinned to it.**
That first run had a Free Aspect Game view at 691x307. With `matchWidthOrHeight = 1`
the canvas locks height at 600 and lets width run — so the canvas was **1350x600**,
not 800x600. Every edge-anchored element flew to the far edges of a canvas 69%
wider than the reference: the right-hand button stack and score panel sat ~550
units right of where an 800-wide mockup would put them, with a large empty gap
through the middle.

Two consecutive runs of the same build gave canvas widths of 1350 and 1182. Two
screenshots of the same build were not comparable to each other, let alone to a
mockup.

**Stage 1 — `CasinoTheme` seam. DONE 2026-08-04.** `Assets/Scripts/CasinoTheme.cs`.
All 39 color literals lived in `UIManager.cs` and nowhere else; the diff was
exactly 39 insertions and 39 deletions, one for one. The before/after screenshots
were taken at different viewport sizes, so that pair confirms the *palette* is
unchanged, not a pixel-identical layout. Two tiers: a `Palette` block of raw hues for a
wholesale re-skin, and semantic tokens (`ButtonPrimary`, `BuildOwnedByPlayer`,
`PlayerAccent`, `CardSuggested`) that are all `UIManager` ever names.

Static class, not a ScriptableObject, deliberately: `UIManager` builds its layout
entirely in code with no scene wiring, and a SO would need an inspector reference
hand-placed in `Scene.unity`. For per-variant theming later, change the property
bodies; the call sites do not move.

The extraction collapsed four pairs that were the same color reached by different
paths — notably the move banner and round-summary title, and the score-panel stats
and summary columns — so player/opponent now read as one consistent pair.

**Stage 2 — Visual identity spec. DONE 2026-08-04.**
<https://claude.ai/code/artifact/a5a58c55-67e3-470a-ac06-cd6e685d509a>
Checked in at `docs/design/stage2-identity-spec.html`.

Scoped to **structure, not skin**, to avoid pre-empting the three directions:
token architecture, the palette as a *reference* instantiation (not a
recommendation), the four type roles, a proposed 4-unit spacing scale, card
anatomy at real unit sizes, and the state vocabulary. Three findings from it:

- **Two state collisions in the shipped code, not restyling issues.**
  `HighlightTableCardsForCapture` reuses `SetSelected(true)`, so the identical Sky
  tint plus 1.15 scale means both "you picked this" and "the AI is taking this".
  Separately, `SetSuggested` marks both the Suggest hint's recommendation and hand
  cards that can take a selected table card: advice and availability sharing one green.
- **Four states the rules require and the UI does not show:** malleable single vs
  locked multi-build, opponent-build-as-steal-target, trail-locked-because-you-own-a-build,
  and which table cards a held card could capture.
- **Corner indices are a hard requirement, not a style choice.** Build cards
  overlap with 16 units showing, so the centred glyph is hidden on every card but
  the last; a build's contents are currently unreadable without opening it.

Three decisions are needed before Stage 3 and are listed in the spec's last
section: whether the felt green is non-negotiable, how much the UI should teach,
and indices-only vs full pips.

**Stage 3 — Three desktop directions. DONE 2026-08-04.**
<https://claude.ai/code/artifact/cc80a7e0-2c83-4f42-b5e6-d183ba849451>
Checked in at `docs/design/stage3-directions.html`.

One Artifact, three switchable boards, each live at exactly 1280x720. Built on
assumed answers to the Stage 2 questions (felt survives in one direction only,
teach moderately, indices without pips) because Rick was away; revisit if any
assumption was wrong.

- **A / Parlor** keeps the felt, lit and railed, with a serif rank face. Most art
  cost, worst portrait story (right-hand action rail).
- **B / Slate** drops the felt for a neutral ground and a sidebar instrument
  panel; actions move to a horizontal bar under the hand. Cheapest, best portrait
  story, least atmosphere.
- **C / Paper** is a printed rules sheet: light ground, ink and card red only,
  state carried by rule weight and displacement rather than fill.

The boards are interactive: clicking a hand card runs a JS port of
`CaptureChecker` (rank matches, sum subsets, builds at value, and combining an
opponent's single build with loose cards) and shows the takeable sets. The
position is rigged so every Stage 2 gap is on screen at once: a raisable
opponent build, a locked multi-build of yours, and Trail disabled with its reason.

Rick picks one, THEN a responsive pass on the winner only. Do not build
3 directions x 2 breakpoints; that is 6 mockups and 5 get discarded.

**Verification note.** Chrome MCP was unavailable (two browsers connected, and
picking one needs Rick). Headless Chrome from the shell works with no extension
and no browser choice:

```bash
"/Applications/Google Chrome.app/Contents/MacOS/Google Chrome" --headless=new \
  --disable-gpu --hide-scrollbars --force-device-scale-factor=1 \
  --virtual-time-budget=3000 --window-size=1280,1000 \
  --screenshot=out.png "file://$PWD/page.html"
```

It caught three real layout bugs, two of them one root cause: **whitespace
between anonymous flex items is stripped**, so `</b> can take` rendered as
`</b>can take` in all three directions and `Your pile 12` ran together. Wrap
mixed inline content in a single element inside a flex container.

**Stage 4 — Layout engine rework** for breakpoints, then port, then verify by
diffing the real `layout-report.txt` against the spec numbers.

**Stage 5 — Art assets**, split three ways as described above.

**Stage 6 — DesignSync library**, only once the component vocabulary stops moving.
Maintaining HTML components mirroring Unity components is real duplication cost and
is only worth paying against a settled vocabulary.

## Known risk

An HTML mockup can lie about Unity. TMP text metrics, sprite scaling, and layout
group behavior differ from CSS. The Stage 4 report diff is the mitigation. Expect
the first port to need adjustment rather than dropping in clean.

The reference canvas (now 1280x720) is what makes mockups useful as specs rather than
impressions: anchors plus pixel offsets are effectively absolute positioning, so
a mockup built at exactly 1280x720 maps close to 1:1 onto `RectTransform` values.
