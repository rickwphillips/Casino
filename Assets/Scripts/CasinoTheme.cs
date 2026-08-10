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

    // Hairline brass edges. Parlor puts a border on every raised surface; without
    // them the panels read as flat holes cut in the felt.
    public static Color PanelBorder => Palette.Brass.WithAlpha(0.40f);
    public static Color ButtonBorder => Palette.Brass.WithAlpha(0.50f);
    public static Color ButtonPrimaryBorder => Palette.Brass.WithAlpha(0.90f);
    public static Color PileBorder => Palette.Brass.WithAlpha(0.30f);
    public static Color Divider => Palette.Brass.WithAlpha(0.35f);

    // Behind an open modal. Dark enough that a white card underneath stops
    // reading through the panel, which is what made cards look like they were
    // floating on top of the round summary.
    public static Color ModalScrim => Palette.PanelDeep.WithAlpha(0.82f);

    // Opaque, not 0.96. These sit over the board, and a white card behind a
    // near-opaque dark panel still leaves a legible card-shaped patch: the round
    // summary looked like it had loose cards lying on top of the scoring columns
    // through three separate attempts to fix it as a sorting problem. The scrim
    // supplies the sense of depth that the transparency used to.
    public static Color GameOverPanel => Palette.PanelDeep;
    public static Color RoundSummaryPanel => Palette.PanelDeep;

    // The title screen sits on the felt rather than on a grey sheet, so this is
    // deliberately weaker than ModalScrim: it has to hide a dealt board without
    // hiding the table the game is played on.
    public static Color TitleVeil => Palette.FeltEdge.WithAlpha(0.90f);
    public static Color TitleRule => Palette.Brass.WithAlpha(0.55f);
    // Between TextMuted and TextFaint: the three plays are a caption, not a
    // stamp, and TextFaint vanished against the felt.
    public static Color TitlePlays => Palette.Parchment.WithAlpha(0.52f);

    public static Color PileViewerPanel => Palette.PanelDeep.WithAlpha(0.96f);
    public static Color ScorePanel => Palette.PanelInk.WithAlpha(0.94f);
    public static Color PileButton => Palette.PanelInk.WithAlpha(0.88f);

    // The scoreboard plaque. Both totals are always legible; the leader is
    // struck in ivory and whoever trails drops to brass, so a glance at the
    // brightness says who is ahead before you have read either number.
    public static Color ScoreLeader => Palette.Ivory;
    public static Color ScoreTrailing => Palette.Brass;
    public static Color ScoreCaptionYou => Palette.Mint.WithAlpha(0.78f);
    public static Color ScoreCaptionAi => Palette.Blush.WithAlpha(0.78f);
    public static Color ScorePip => Palette.Brass.WithAlpha(0.70f);

    // The pool of light behind your hand while the turn is yours. Faint on
    // purpose: it should register as the table being lit where you are sitting,
    // not as a highlight demanding to be clicked.
    public static Color TurnGlow => Palette.Gold.WithAlpha(0.16f);

    // Transparent-but-raycastable: an Image needs a non-zero alpha to receive
    // clicks, so builds get an invisible hit area rather than a visible one.
    public static Color InvisibleHitArea => new(0f, 0f, 0f, 0.01f);

    // ---------------------------------------------------------------
    // Buttons
    // ---------------------------------------------------------------
    public static Color ButtonPrimary => Palette.BrassDeep;                     // Sweep, Continue
    public static Color ButtonSecondary => Palette.PanelInk.WithAlpha(0.88f);   // Build, Trail, Suggest
    public static Color ButtonLabel => Palette.Parchment;
    // White, not the dark bronze it launched with: dark-on-brass measured
    // barely 4:1 and read as engraving, not as the label of the primary action.
    public static Color ButtonPrimaryLabel => Color.white;

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
    // Barely off white: hover says "this one is under the cursor", and it ranks
    // below every state above, so it can never repaint a card the board has
    // something to say about.
    public static Color CardHover => Palette.Parchment;

    // ---------------------------------------------------------------
    // Builds
    // ---------------------------------------------------------------
    public static Color BuildOwnedByPlayer => Palette.BuildBlue.WithAlpha(0.95f);
    public static Color BuildOwnedByOpponent => Palette.BuildRust.WithAlpha(0.95f);
    public static Color BuildBadgeLabel => Color.white;

    // (The score-badge tokens lived here until the badge panel itself was
    // retired in favour of the trophy coins below.)

    // Trophy coins: a captured ace splashes in as a struck coin on the
    // capturer's side. There is no idle state on purpose; an ace not yet
    // taken renders nothing at all. The stamp is tone-on-tone (dark bronze
    // into brass, with a light catch below the incision) because a minted
    // coin carries its device in relief, not in ink.
    public static Color CoinRim => Palette.Gold;
    public static Color CoinFace => Palette.Brass;
    // Two kinds of relief, deliberately opposite: the suit is struck INTO the
    // face (dark incision, light catching its lower lip), the device is
    // raised OUT of it (bright gold over a dark cast shadow). Sunk watermark,
    // raised letter: that is what keeps them apart at 46 pixels.
    // The impression started at 0.30 alpha, which read as tarnish, not a
    // suit; the coin has to answer "which ace" at a glance.
    public static Color CoinWatermark => Palette.Bronze.WithAlpha(0.62f);
    public static Color CoinWatermarkLight => Palette.Gold.WithAlpha(0.42f);
    public static Color CoinDevice => Palette.Gold;
    public static Color CoinDeviceShadow => Palette.Bronze;
    // Lowered from 0.38: at full strength the sweep read as a notification.
    public static Color CoinShine => Color.white.WithAlpha(0.20f);
    // Big Casino's halo; Breathe() modulates the alpha, this is its ceiling.
    public static Color CoinGlow => Palette.Gold.WithAlpha(0.50f);

    // A single build can be raised and stolen; a multi-build is locked and can
    // only be taken whole. They rendered identically before, so the tag carries
    // the distinction the rules depend on.
    public static Color BuildTagLocked => Palette.PanelDeep.WithAlpha(0.9f);
    public static Color BuildTagRaisable => Palette.BrassDeep.WithAlpha(0.9f);
    public static Color BuildTagLabel => Palette.Gold;

    private static Color WithAlpha(this Color c, float a) => new(c.r, c.g, c.b, a);
}
