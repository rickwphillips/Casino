using UnityEngine;

// The single source of truth for every color the UI draws.
//
// Before this existed, ~39 raw `new Color(r,g,b)` literals were scattered through
// UIManager, so a palette change meant a hunt-and-peck pass over a 2000-line file.
// Now it is a one-file edit.
//
// Two tiers on purpose:
//   Palette  - the raw hues. A wholesale re-skin edits only this block.
//   Semantic - what a color *means* (a panel, an owned build, the player's side).
//              UIManager only ever names these, never a raw hue.
//
// Deliberately a static class, not a ScriptableObject: UIManager builds its entire
// layout in code with no scene wiring, and a SO would need an inspector reference
// hand-placed in Scene.unity. If per-variant theming is ever wanted, swap the
// bodies of the semantic properties to read from a SO - the call sites do not move.
public static class CasinoTheme
{
    // ---------------------------------------------------------------
    // Palette - raw hues. Re-skin here.
    // ---------------------------------------------------------------
    public static class Palette
    {
        public static readonly Color Felt = new(0.08f, 0.29f, 0.15f);
        public static readonly Color FeltLight = new(0.18f, 0.42f, 0.30f);

        public static readonly Color Navy = new(0.10f, 0.16f, 0.38f);
        public static readonly Color NavyLight = new(0.22f, 0.32f, 0.60f);

        public static readonly Color Gold = new(1f, 0.90f, 0.50f);
        public static readonly Color GoldDeep = new(0.72f, 0.60f, 0.25f);
        public static readonly Color Cream = new(1f, 0.95f, 0.60f);

        public static readonly Color Ink = new(0.04f, 0.06f, 0.09f);
        public static readonly Color Slate = new(0.05f, 0.07f, 0.10f);
        public static readonly Color SlateBlue = new(0.12f, 0.16f, 0.22f);
        public static readonly Color Midnight = new(0.05f, 0.08f, 0.12f);

        public static readonly Color CardRed = new(0.90f, 0.10f, 0.10f);
        public static readonly Color Mint = new(0.75f, 1f, 0.80f);
        public static readonly Color Blush = new(1f, 0.80f, 0.75f);
        public static readonly Color Sky = new(0.70f, 0.90f, 1f);
        public static readonly Color Leaf = new(0.70f, 1f, 0.70f);
        public static readonly Color BuildBlue = new(0.20f, 0.45f, 0.85f);
        public static readonly Color BuildRust = new(0.75f, 0.25f, 0.20f);
    }

    // ---------------------------------------------------------------
    // Surfaces
    // ---------------------------------------------------------------
    public static Color TableFelt => Palette.Felt;
    public static Color GameOverPanel => Palette.Midnight.WithAlpha(0.96f);
    public static Color PileViewerPanel => Palette.Ink.WithAlpha(0.96f);
    public static Color RoundSummaryPanel => Palette.Ink.WithAlpha(0.97f);
    public static Color ScorePanel => Palette.Slate.WithAlpha(0.92f);
    public static Color PileButton => Palette.SlateBlue.WithAlpha(0.92f);

    // Transparent-but-raycastable: an Image needs a non-zero alpha to receive
    // clicks, so builds get an invisible hit area rather than a visible one.
    public static Color InvisibleHitArea => new(0f, 0f, 0f, 0.01f);

    // ---------------------------------------------------------------
    // Buttons
    // ---------------------------------------------------------------
    public static Color ButtonPrimary => Palette.GoldDeep.WithAlpha(0.95f);   // Sweep, Continue
    public static Color ButtonSecondary => Palette.FeltLight.WithAlpha(0.95f); // Build, Trail, Suggest
    public static Color ButtonLabel => Color.white;

    // ---------------------------------------------------------------
    // Type
    // ---------------------------------------------------------------
    public static Color TextPrimary => Color.white;
    public static Color TextMuted => Color.white.WithAlpha(0.85f);
    public static Color TextFaint => Color.white.WithAlpha(0.35f);   // version stamp
    public static Color HintText => Palette.Cream;
    public static Color Headline => Palette.Gold;                    // move banner, summary title

    // Player and opponent read as a consistent pair everywhere they appear:
    // score panel stats and round-summary columns.
    public static Color PlayerAccent => Palette.Mint;
    public static Color OpponentAccent => Palette.Blush;

    // ---------------------------------------------------------------
    // Cards
    // ---------------------------------------------------------------
    public static Color CardFace => Color.white;
    public static Color CardBackBase => Palette.Navy;
    public static Color CardBackLattice => Palette.NavyLight;
    public static Color SuitRed => Palette.CardRed;
    public static Color SuitBlack => Color.black;

    // Card selection states. These are the visual vocabulary the rules expose to
    // the player, so they are the ones most likely to change in a redesign.
    public static Color CardSelected => Palette.Sky;
    public static Color CardSuggested => Palette.Leaf;

    // ---------------------------------------------------------------
    // Builds
    // ---------------------------------------------------------------
    public static Color BuildOwnedByPlayer => Palette.BuildBlue.WithAlpha(0.95f);
    public static Color BuildOwnedByOpponent => Palette.BuildRust.WithAlpha(0.95f);
    public static Color BuildBadgeLabel => Color.white;

    private static Color WithAlpha(this Color c, float a) => new(c.r, c.g, c.b, a);
}
