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
