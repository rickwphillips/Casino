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
        public Zone Score, PlayerPile, AiPile;
        public Zone DrawPile, TurnText, StatusText, Version, GameOver;

        // Actions are a run of identical buttons: a vertical rail in landscape,
        // a horizontal bar in portrait. One description covers both.
        public Vector2 ActionAnchor, ActionFirst, ActionStep, ActionSize;

        public Zone Action(int i) => new(
            ActionAnchor.x, ActionAnchor.y,
            ActionFirst.x + ActionStep.x * i, ActionFirst.y + ActionStep.y * i,
            ActionSize.x, ActionSize.y);
    }

    public static Profile Active { get; private set; } = Wide;

    public static Profile Pick(float width, float height)
    {
        float aspect = height <= 0 ? 1.777f : width / height;
        Active = aspect < 0.95f ? Portrait
               : aspect < 1.45f ? Compact
               : Wide;
        return Active;
    }

    // -----------------------------------------------------------------
    // Wide: 16:9 desktop, the shape the game is actually played in.
    //
    // The play area is centred on the space left over after the rail and the
    // draw-pile column, not on the canvas, which is why the x offsets are -69
    // rather than 0. Score, piles and actions are one contiguous right rail;
    // previously the score sat top-right and the buttons bottom-right with
    // ~160 units of dead felt between two things that belong together.
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
        Hint         = new Zone(0.5f, 0f, -52, 172, 560, 46),
        PlayerHand   = new Zone(0.5f, 0f, -52, 14, 460, 158),

        Score = new Zone(1f, 1f, -14, -14, 272, 232),
        ActionAnchor = new Vector2(1, 0),
        ActionFirst  = new Vector2(-14, 30),   // clears the version stamp at y 4..22
        ActionStep   = new Vector2(0, 54),
        ActionSize   = new Vector2(272, 46),

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
        Hint         = new Zone(0.5f, 0f, -48, 152, 500, 44),
        PlayerHand   = new Zone(0.5f, 0f, -48, 12, 420, 140),

        Score = new Zone(1f, 1f, -12, -12, 236, 220),
        ActionAnchor = new Vector2(1, 0),
        ActionFirst  = new Vector2(-12, 28),
        ActionStep   = new Vector2(0, 50),
        ActionSize   = new Vector2(236, 44),

        DrawPile   = new Zone(0f, 0f, 38, 416, 88, 120),
        TurnText   = new Zone(0f, 0f, 12, 122, 150, 24),
        StatusText = new Zone(0f, 0f, 12, 96, 150, 24),
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
        Score        = new Zone(0.5f, 1f, 0, -10, 700, 220),
        OpponentHand = new Zone(0.5f, 1f, 0, -240, 684, 120),
        PlayerPile   = new Zone(0f, 1f, 14, -368, 336, 34),
        AiPile       = new Zone(1f, 1f, -14, -368, 336, 34),

        Table      = new Zone(0.5f, 0.5f, 0, 60, 684, 300),
        Hint       = new Zone(0.5f, 0f, 0, 470, 660, 56),

        ActionAnchor = new Vector2(0.5f, 0),
        ActionFirst  = new Vector2(-258, 396),
        ActionStep   = new Vector2(172, 0),
        ActionSize   = new Vector2(166, 52),

        PlayerHand = new Zone(0.5f, 0f, 0, 230, 684, 150),
        DrawPile   = new Zone(0f, 0f, 20, 92, 76, 104),
        TurnText   = new Zone(0f, 0f, 110, 138, 260, 24),
        StatusText = new Zone(0f, 0f, 110, 112, 260, 24),
        Version    = new Zone(1f, 0f, -10, 6, 120, 18),
        GameOver   = new Zone(0.5f, 0.5f, 0, 0, 640, 300),
    };
}
