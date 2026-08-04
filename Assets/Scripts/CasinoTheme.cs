using UnityEngine;

// The single source of truth for every color the UI draws.
//
// Currently instantiated as **Parlor**, the direction chosen at the end of
// Stage 3: the felt survives, but as a lit table with a brass rail rather than a
// flat green fill. Gilt-on-bronze card backs, parchment type, gold for things
// the board makes available.
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
        // Felt is three stops, not one: the table is lit from above-centre and
        // falls off to the edges. See UIManager.BuildFeltSprite.
        public static readonly Color FeltCore = new(0.118f, 0.392f, 0.220f);
        public static readonly Color FeltMid  = new(0.071f, 0.278f, 0.149f);
        public static readonly Color FeltEdge = new(0.039f, 0.180f, 0.098f);

        public static readonly Color Brass     = new(0.773f, 0.631f, 0.310f);
        public static readonly Color BrassDeep = new(0.576f, 0.459f, 0.165f);
        public static readonly Color Gilt      = new(0.557f, 0.455f, 0.200f);
        public static readonly Color Bronze    = new(0.361f, 0.290f, 0.118f);

        public static readonly Color Gold      = new(0.949f, 0.835f, 0.533f);
        public static readonly Color Parchment = new(0.937f, 0.902f, 0.808f);
        public static readonly Color Ivory     = new(1f, 0.953f, 0.816f);

        public static readonly Color PanelInk = new(0.024f, 0.094f, 0.063f);
        public static readonly Color PanelDeep = new(0.016f, 0.063f, 0.043f);

        public static readonly Color CardRed = new(0.784f, 0.063f, 0.180f);
        public static readonly Color Mint    = new(0.624f, 0.847f, 0.682f);
        public static readonly Color Blush   = new(0.910f, 0.682f, 0.604f);

        // The one cool note in a warm palette. Reserved for advice the game
        // offers, so it can never be mistaken for a fact about the board.
        public static readonly Color Counsel = new(0.749f, 0.847f, 0.910f);
        public static readonly Color Rust    = new(0.890f, 0.627f, 0.541f);

        public static readonly Color BuildBlue = new(0.184f, 0.435f, 0.796f);
        public static readonly Color BuildRust = new(0.690f, 0.227f, 0.169f);
    }

    // ---------------------------------------------------------------
    // Surfaces
    // ---------------------------------------------------------------
    public static Color TableFeltCore => Palette.FeltCore;
    public static Color TableFeltMid  => Palette.FeltMid;
    public static Color TableFeltEdge => Palette.FeltEdge;
    public static Color TableFelt => Palette.FeltMid;   // flat fallback if the sprite fails
    public static Color TableRail => Palette.Brass.WithAlpha(0.34f);

    public static Color GameOverPanel => Palette.PanelDeep.WithAlpha(0.96f);
    public static Color PileViewerPanel => Palette.PanelDeep.WithAlpha(0.96f);
    public static Color RoundSummaryPanel => Palette.PanelDeep.WithAlpha(0.97f);
    public static Color ScorePanel => Palette.PanelInk.WithAlpha(0.86f);
    public static Color PileButton => Palette.PanelInk.WithAlpha(0.74f);

    // Transparent-but-raycastable: an Image needs a non-zero alpha to receive
    // clicks, so builds get an invisible hit area rather than a visible one.
    public static Color InvisibleHitArea => new(0f, 0f, 0f, 0.01f);

    // ---------------------------------------------------------------
    // Buttons
    // ---------------------------------------------------------------
    public static Color ButtonPrimary => Palette.BrassDeep;                     // Sweep, Continue
    public static Color ButtonSecondary => Palette.PanelInk.WithAlpha(0.72f);   // Build, Trail, Suggest
    public static Color ButtonLabel => Palette.Parchment;
    public static Color ButtonPrimaryLabel => new(0.133f, 0.102f, 0.024f);

    // ---------------------------------------------------------------
    // Type
    // ---------------------------------------------------------------
    public static Color TextPrimary => Palette.Parchment;
    public static Color TextMuted => Palette.Parchment.WithAlpha(0.82f);
    public static Color TextFaint => Palette.Parchment.WithAlpha(0.32f);   // version stamp
    public static Color HintText => Palette.Gold;
    public static Color Headline => Palette.Gold;                          // move banner, summary title

    // Player and opponent read as a consistent pair everywhere they appear:
    // score panel stats and round-summary columns.
    public static Color PlayerAccent => Palette.Mint;
    public static Color OpponentAccent => Palette.Blush;

    // ---------------------------------------------------------------
    // Cards
    // ---------------------------------------------------------------
    public static Color CardFace => Color.white;
    public static Color CardBackBase => Palette.Bronze;
    public static Color CardBackLattice => Palette.Gilt;
    public static Color CardBackBorder => Palette.Brass;
    public static Color SuitRed => Palette.CardRed;
    public static Color SuitBlack => Color.black;

    // Card state vocabulary.
    //
    // Four grounds, two scale steps, and no state relies on hue alone - which is
    // the rule the Stage 2 spec set after finding that the shipped code used one
    // appearance for two meanings, twice over:
    //
    //   Selected        you chose this               ivory  + scale 1.15
    //   Capturable      the board makes this takeable gold   + no scale
    //   Suggested       the game is advising this     counsel (cool) + no scale
    //   OpponentTaking  the AI is taking this         rust   + scale 1.08
    //
    // Capturable and Suggested were both `SetSuggested` before, conflating a fact
    // about the board with optional advice. OpponentTaking was `SetSelected`, so
    // "you picked this" and "the opponent is taking this" looked identical.
    public static Color CardSelected => Palette.Ivory;
    public static Color CardCapturable => Palette.Gold;
    public static Color CardSuggested => Palette.Counsel;
    public static Color CardOpponentTaking => Palette.Rust;

    // ---------------------------------------------------------------
    // Builds
    // ---------------------------------------------------------------
    public static Color BuildOwnedByPlayer => Palette.BuildBlue.WithAlpha(0.95f);
    public static Color BuildOwnedByOpponent => Palette.BuildRust.WithAlpha(0.95f);
    public static Color BuildBadgeLabel => Color.white;

    // A single build can be raised and stolen; a multi-build is locked and can
    // only be taken whole. They rendered identically before, so the tag carries
    // the distinction the rules depend on.
    public static Color BuildTagLocked => Palette.PanelDeep.WithAlpha(0.9f);
    public static Color BuildTagRaisable => Palette.BrassDeep.WithAlpha(0.9f);
    public static Color BuildTagLabel => Palette.Gold;

    private static Color WithAlpha(this Color c, float a) => new(c.r, c.g, c.b, a);
}
