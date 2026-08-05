# Design artifacts

Self-contained HTML, no external assets or network calls. Open directly in a browser:

```bash
open docs/design/stage3-directions.html
```

These are the checked-in copies of the published Artifacts, so the work survives
without a claude.ai link. If you edit one, republish it to the same URL rather
than minting a new one (see `docs/UI-DESIGN-PLAN.md` for the URLs).

| File | Stage | What it is |
|---|---|---|
| `stage2-identity-spec.html` | 2 | Structural spec: token architecture, palette as a *reference* instantiation, four type roles, spacing scale, card anatomy, state vocabulary. Deliberately fixes structure and not skin, so it does not pre-empt the direction choice. |
| `stage3-directions.html` | 3 | Three switchable boards, each live at exactly 1280x720. Interactive: clicking a hand card runs a JS port of `CaptureChecker` and reports the takeable sets. |
| `stage3-*.png` | 3 | Reference renders of each direction, in case you want a look without a browser. |
| `parlor-*.png` | 4-5 | Shots of the shipped Unity UI, not mockups. |

## The shipped-UI shots

These come out of `CasinoAutoPlay`, which plays a full game unattended and
screenshots the moments worth keeping. That matters: everything before them was
photographed on an opening deal or a board staged by `CasinoStatePreview`, which
proves the code compiles and is wired, but not that it is wired to live state.

| File | What it proves |
|---|---|
| `parlor-build-inplay.png` | Build badge in owner colour, the `RAISABLE` tag, and a table card tinted rust because the opponent is taking it. All from real play. |
| `parlor-round-summary.png` | Deck scoring, over a scrim, with opaque panels. Earlier versions let the cards behind read through the panel. |
| `parlor-gameover.png` | The end of a game: winner, final score, target, themed button. |
| `parlor-partial-capture.png` | A 9 taking only 6♥+3♦ and leaving 9♣ and 5♦+4♠ behind. An engine that captured the maximum union automatically would have taken all five, so this is the shot that shows these are Rick's rules and not generic Casino. |
| `parlor-build-raised.png` | An opponent's build of 6 raised to 8, badge flipped to player blue as ownership transferred, and Trail disabled reading "Trail (own build)". |
| `parlor-build-locked-vs-raisable.png` | Both build states in one frame: a player-owned LOCKED multi-build beside the AI's RAISABLE single. |
| `parlor-pile-viewer.png` | The captured-pile viewer open, left-edge furniture stepped aside. Also the only shot showing the per-deck stats populated (Cards 15, Spades 4, Big/Little yes/no) rather than zeroed just after a deck was scored. |
| `parlor-wide-ingame.png`, `parlor-compact-ingame.png`, `parlor-portrait-ingame.png` | The three layout profiles. |

To retake any of them: `echo full > autoplay.flag; echo run > auto-verify.flag`,
then focus Unity. For a different profile, put `WxH` in `gameview.txt` first.

The Stage 3 board is rigged to a position that exercises every gap Stage 2 found:
a raisable opponent build of 5, a locked multi-build of 8 that is yours, and Trail
disabled because owning a build makes trailing illegal.

## Rendering these headlessly

Useful for checking a change without a browser extension or a browser picker:

```bash
"/Applications/Google Chrome.app/Contents/MacOS/Google Chrome" --headless=new \
  --disable-gpu --hide-scrollbars --force-device-scale-factor=1 \
  --virtual-time-budget=3000 --window-size=1280,1000 \
  --screenshot=out.png "file://$PWD/docs/design/stage3-directions.html"
```

This caught three real layout bugs during Stage 3, two of them one root cause:
whitespace between anonymous flex items is stripped, so `</b> can take` rendered
as `</b>can take`. Wrap mixed inline content in a single element inside a flex
container.
