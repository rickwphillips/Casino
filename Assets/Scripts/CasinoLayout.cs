using UnityEngine;

// Where everything goes, at every screen shape.
//
// Before this, UIManager.EnforceLayout() held one hardcoded arrangement tuned for
// a single canvas size. That is a landscape assumption baked into the layout
// engine: with matchWidthOrHeight = 1 the canvas width floats with the window, so
// edge-anchored elements drift apart on wide screens and collide in portrait.
//
// A Profile is one complete arrangement. Pick() chooses by aspect ratio, so
// adding a shape means adding a Profile, not editing UIManager.
//
// Coordinates are canvas units in the profile's own reference resolution, and the
// pivot always equals the anchor, which makes every Zone read as "this corner,
// this far in, this big". They are directly comparable to layout-report.txt.
public static class CasinoLayout
{
    public readonly struct Zone
    {
        public readonly Vector2 Anchor, Pos, Size;
        public Zone(float ax, float ay, float px, float py, float w, float h)
        {
            Anchor = new Vector2(ax, ay); Pos = new Vector2(px, py); Size = new Vector2(w, h);
        }
    }

    public sealed class Profile
    {
        public string Name;
        public Vector2 Reference;
        public float Match;          // CanvasScaler.matchWidthOrHeight: 1 = height, 0 = width
        public Vector2 CardSize;
        public float RowSpacing;

        public Zone OpponentHand, Table, PlayerHand, Hint;
        public Zone Score, PlayerPile, AiPile, Suggest;
        public Zone DrawPile, TurnText, StatusText, Version, GameOver;

        // Actions stack vertically above the selected hand card (one card per
        // turn, so the options belong to it). The profile describes button
        // height and gap; UIManager anchors the stack to the card each refresh.
        public Vector2 ActionCenter;   // fallback stack anchor: x offset, y from bottom
        public float ActionHeight, ActionGap;
    }

    // Resolved on first read rather than in a field initializer.
    //
    // `= Wide` here looked equivalent and was not: static field initializers run
    // in declaration order, and Wide is declared below this line, so Active
    // captured null. Nothing noticed for as long as every reader ran after
    // Pick(). The first caller to read Active during Start(), before
    // EnforceLayout had chosen a profile, took a NullReferenceException that
    // aborted UI construction halfway, leaving a board with no buttons.
    private static Profile active;
    public static Profile Active => active ??= Wide;

    public static Profile Pick(float width, float height)
    {
        float aspect = height <= 0 ? 1.777f : width / height;
        active = aspect < 0.95f ? Portrait
               : aspect < 1.45f ? Compact
               : Wide;
        return active;
    }

    // -----------------------------------------------------------------
    // Wide: 16:9 desktop, the shape the game is actually played in.
    //
    // The play area is centred on the space left over after the rail and the
    // draw-pile column, not on the canvas, which is why the x offsets are -69
    // rather than 0. The score panel is the right rail; the action buttons
    // left it for a contextual row above the hand, because a button that only
    // exists in response to a selection belongs next to the selection.
    // -----------------------------------------------------------------
    public static readonly Profile Wide = new()
    {
        Name = "Wide", Reference = new Vector2(1280, 720), Match = 1f,
        CardSize = new Vector2(80, 120), RowSpacing = 14f,

        // Ported from the Parlor mockup's grid, not re-invented:
        //   columns  168 | 1fr | 272      rows  112 | 1fr | 46 | 158    padding 14
        //   "deck  opp    score"
        //   "deck  table  score"
        //   "piles hint   actions"
        //   "piles hand   actions"
        // Left column is deck over piles; the right rail is score over actions.
        // The play column spans x 182..994, so its centre is 588, which is 52
        // left of the canvas centre. That is where the -52 offsets come from.

        OpponentHand = new Zone(0.5f, 1f, -52, -14, 460, 112),
        Table        = new Zone(0.5f, 0.5f, -52, 46, 620, 150),
        // Above the table, not below it: the felt between hand and table now
        // belongs to the action stack that grows over the selected card.
        Hint         = new Zone(0.5f, 0f, -52, 490, 560, 46),
        PlayerHand   = new Zone(0.5f, 0f, -52, 14, 460, 158),

        Score = new Zone(1f, 1f, -14, -14, 272, 278),
        // ActionCenter is only the fallback anchor (a build selected with no
        // hand card); normally the stack sits over the selected card itself.
        ActionCenter = new Vector2(-52, 180),
        ActionHeight = 44, ActionGap = 8,

        // Advice, not a move: a circled "?" tucked above the version stamp.
        Suggest = new Zone(1f, 0f, -14, 30, 40, 40),

        // Left column, top to bottom: draw pile, status, then the two piles.
        DrawPile   = new Zone(0f, 0f, 50, 396, 96, 132),
        TurnText   = new Zone(0f, 0f, 14, 130, 170, 24),
        StatusText = new Zone(0f, 0f, 14, 104, 170, 24),
        PlayerPile = new Zone(0f, 0f, 14, 62, 168, 34),
        AiPile     = new Zone(0f, 0f, 14, 20, 168, 34),

        Version  = new Zone(1f, 0f, -10, 4, 120, 18),
        GameOver = new Zone(0.5f, 0.5f, 0, 0, 440, 240),
        };

    // -----------------------------------------------------------------
    // Compact: 4:3 and similar. Same arrangement, tighter rail.
    // -----------------------------------------------------------------
    public static readonly Profile Compact = new()
    {
        Name = "Compact", Reference = new Vector2(1024, 768), Match = 1f,
        CardSize = new Vector2(76, 114), RowSpacing = 10f,

        // Same Parlor grid, tighter: columns 140 | 1fr | 236, padding 12.
        // Play column is x 152..776, centre 464, i.e. 48 left of canvas centre.
        OpponentHand = new Zone(0.5f, 1f, -48, -12, 420, 104),
        Table        = new Zone(0.5f, 0.5f, -48, 40, 560, 140),
        Hint         = new Zone(0.5f, 0f, -48, 500, 500, 44),
        PlayerHand   = new Zone(0.5f, 0f, -48, 12, 420, 140),

        Score = new Zone(1f, 1f, -12, -12, 236, 272),
        ActionCenter = new Vector2(-48, 158),
        ActionHeight = 42, ActionGap = 8,
        Suggest = new Zone(1f, 0f, -12, 28, 38, 38),

        DrawPile   = new Zone(0f, 0f, 38, 416, 88, 120),
        TurnText   = new Zone(0f, 0f, 12, 122, 180, 24),
        StatusText = new Zone(0f, 0f, 12, 96, 180, 24),
        PlayerPile = new Zone(0f, 0f, 12, 56, 140, 32),
        AiPile     = new Zone(0f, 0f, 12, 18, 140, 32),

        Version  = new Zone(1f, 0f, -10, 4, 120, 18),
        GameOver = new Zone(0.5f, 0.5f, 0, 0, 420, 230),
        };

    // -----------------------------------------------------------------
    // Portrait: phone. Match width, not height, or the whole board scales
    // down to fit a tall canvas and the cards become unreadable.
    //
    // The right-hand rail cannot survive here: at 720 units wide a 256-wide
    // rail is a third of the screen. Score becomes a top bar, actions become
    // a bar sitting directly above the hand, which is also the only part of
    // the screen a thumb reaches comfortably.
    // -----------------------------------------------------------------
    public static readonly Profile Portrait = new()
    {
        Name = "Portrait", Reference = new Vector2(720, 1280), Match = 0f,
        CardSize = new Vector2(72, 108), RowSpacing = 6f,

        // 220 tall, not 92: the score panel's internal layout is authored for a
        // column of eight stat lines and is not itself profile-aware, so a short
        // wide bar makes its children collide. Give it the height it needs.
        Score        = new Zone(0.5f, 1f, 0, -10, 700, 272),
        OpponentHand = new Zone(0.5f, 1f, 0, -292, 684, 120),
        PlayerPile   = new Zone(0f, 1f, 14, -420, 336, 34),
        AiPile       = new Zone(1f, 1f, -14, -420, 336, 34),

        // The taller score bar pushes the whole upper stack down, so the table
        // drops with it. The hint sits above the table; the space below it
        // belongs to the action stack over the selected card.
        Table      = new Zone(0.5f, 0.5f, 0, 20, 684, 300),
        Hint       = new Zone(0.5f, 0f, 0, 816, 660, 56),

        ActionCenter = new Vector2(0, 396),
        ActionHeight = 52, ActionGap = 10,
        // Thumb-sized, clear of the version stamp.
        Suggest = new Zone(1f, 0f, -14, 34, 48, 48),

        PlayerHand = new Zone(0.5f, 0f, 0, 230, 684, 150),
        // 92 put the pile's count label hard against the bottom edge, where it
        // rendered clipped. The zone is the card; the label hangs below it.
        DrawPile   = new Zone(0f, 0f, 20, 118, 76, 104),
        TurnText   = new Zone(0f, 0f, 110, 164, 260, 24),
        StatusText = new Zone(0f, 0f, 110, 138, 260, 24),
        Version    = new Zone(1f, 0f, -10, 6, 120, 18),
        GameOver   = new Zone(0.5f, 0.5f, 0, 0, 640, 300),
    };
}
