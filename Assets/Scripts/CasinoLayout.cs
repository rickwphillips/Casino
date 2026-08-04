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
        CardSize = new Vector2(80, 120), RowSpacing = 10f,

        OpponentHand = new Zone(0.5f, 1f, -69, -14, 460, 110),
        Table        = new Zone(0.5f, 0.5f, -69, 20, 620, 140),
        PlayerHand   = new Zone(0.5f, 0f, -69, 18, 460, 130),
        Hint         = new Zone(0.5f, 0f, -69, 162, 560, 48),

        Score      = new Zone(1f, 1f, -12, -12, 256, 220),
        PlayerPile = new Zone(1f, 1f, -12, -238, 256, 34),
        AiPile     = new Zone(1f, 1f, -12, -276, 256, 34),

        ActionAnchor = new Vector2(1, 0),
        ActionFirst  = new Vector2(-12, 30),   // clears the version stamp at y 4..22
        ActionStep   = new Vector2(0, 54),
        ActionSize   = new Vector2(256, 46),

        DrawPile   = new Zone(0f, 0f, 28, 152, 96, 132),
        TurnText   = new Zone(0f, 0f, 16, 112, 190, 26),
        StatusText = new Zone(0f, 0f, 16, 86, 190, 26),
        Version    = new Zone(1f, 0f, -10, 4, 120, 18),
        GameOver   = new Zone(0.5f, 0.5f, 0, 0, 440, 240),
    };

    // -----------------------------------------------------------------
    // Compact: 4:3 and similar. Same arrangement, tighter rail.
    // -----------------------------------------------------------------
    public static readonly Profile Compact = new()
    {
        Name = "Compact", Reference = new Vector2(1024, 768), Match = 1f,
        CardSize = new Vector2(76, 114), RowSpacing = 8f,

        OpponentHand = new Zone(0.5f, 1f, -58, -12, 420, 104),
        Table        = new Zone(0.5f, 0.5f, -58, 16, 560, 132),
        PlayerHand   = new Zone(0.5f, 0f, -58, 14, 420, 124),
        Hint         = new Zone(0.5f, 0f, -58, 150, 500, 46),

        Score      = new Zone(1f, 1f, -10, -10, 226, 210),
        PlayerPile = new Zone(1f, 1f, -10, -226, 226, 32),
        AiPile     = new Zone(1f, 1f, -10, -262, 226, 32),

        ActionAnchor = new Vector2(1, 0),
        ActionFirst  = new Vector2(-10, 28),   // clears the version stamp at y 4..22
        ActionStep   = new Vector2(0, 50),
        ActionSize   = new Vector2(226, 44),

        DrawPile   = new Zone(0f, 0f, 24, 140, 88, 120),
        TurnText   = new Zone(0f, 0f, 14, 100, 180, 24),
        StatusText = new Zone(0f, 0f, 14, 76, 180, 24),
        Version    = new Zone(1f, 0f, -10, 4, 120, 18),
        GameOver   = new Zone(0.5f, 0.5f, 0, 0, 420, 230),
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
