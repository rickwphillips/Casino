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

        public Zone OpponentHand, Table, PlayerHand;
        // One scoreboard for both sides. There were two zones here, one per
        // player, which is what put the same fact in two corners.
        public Zone Score;
        // The game's voice, and its history. Message is a toast that fades;
        // LogButton toggles LogPanel, which keeps every move that scrolled past.
        // These replaced a gold line across the middle of the table, where text
        // sat on top of the cards it was describing.
        public Zone Message, LogButton, LogPanel;
        // Each player's take, face up and stacked so only the pips show,
        // on that player's own side of the right rail, plus the full grid one
        // of them opens. The grid used to dock to the left edge, which is now
        // the plaque's column, so it centres over the table instead.
        public Zone PlayerCaptured, AiCaptured, PileViewer;
        public Zone Suggest, PlayerAces, AiAces;
        // Two homes for the draw pile: it sits beside whoever is dealing
        // this deck, and moves when the deal does.
        public Zone DrawPileAi, DrawPileHuman;
        public Zone Version, GameOver;

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
        //   columns  312 | 1fr | 120      rows  112 | 1fr | 46 | 158    padding 14
        //   "score  opp     aitake"
        //   "score  table   aitake"
        //   "msg    hint    youtake"
        //   "log    hand    youtake"
        // Left column is the game talking: plaque, then toast, then the log it
        // opens into. Right column is the two takes, one per side. The play
        // column spans x 326..1160, centre 743, which is 103 right of the canvas
        // centre; the -52 offsets predate that and still read fine, so the play
        // area sits slightly left of its column rather than dead centre in it.

        OpponentHand = new Zone(0.5f, 1f, -52, -14, 460, 112),
        Table        = new Zone(0.5f, 0.5f, -52, 46, 620, 150),
        PlayerHand   = new Zone(0.5f, 0f, -52, 14, 460, 158),

        // The plaque takes the top-left corner, the first thing read on a page.
        Score = new Zone(0f, 1f, 14, -14, 196, 210),
        // Directly under the plaque: the toast, and the button that opens the
        // log beneath it. Both hug the same left margin so the column reads as
        // one voice rather than three unrelated widgets.
        Message   = new Zone(0f, 1f, 14, -232, 242, 56),
        LogButton = new Zone(0f, 1f, 264, -232, 34, 34),
        LogPanel  = new Zone(0f, 1f, 14, -296, 284, 340),

        // ActionCenter is only the fallback anchor (a build selected with no
        // hand card); normally the stack sits over the selected card itself.
        ActionCenter = new Vector2(-52, 180),
        ActionHeight = 44, ActionGap = 8,

        // Advice, not a move: a circled "?" in the one corner nothing else
        // wants, now that the takes own the right rail.
        Suggest = new Zone(0f, 0f, 14, 14, 40, 40),

        // Each side's take, face up and stacked, on that side's end of the
        // right rail: the AI's above, yours below, the same seating order the
        // plaque uses. Narrow enough to leave the coin shelves their room, and
        // tall enough that a full 26-card take still fans wide enough to read:
        // the fan spacing is (height - card) / (count - 1), so height is the
        // only thing standing between a stack of pips and a white brick.
        AiCaptured     = new Zone(1f, 1f, -14, -14, 100, 318),
        PlayerCaptured = new Zone(1f, 0f, -14, 14, 100, 318),
        // Over the table, not down the left edge where the plaque now lives.
        PileViewer = new Zone(0.5f, 0.5f, -52, 0, 460, 420),

        // Ace splashes land inboard of their owner's take, so a shelf grows
        // toward the table instead of into the stack beside it.
        PlayerAces = new Zone(1f, 0f, -114, 14, 200, 64),
        AiAces     = new Zone(1f, 1f, -114, -14, 200, 64),

        // The draw pile rides with the dealer: at the AI's elbow up top when it
        // deals (clear of the opponent hand, which starts at x 358), at yours
        // down below when you do.
        DrawPileAi    = new Zone(0f, 1f, 254, -14, 96, 132),
        DrawPileHuman = new Zone(0f, 0f, 254, 14, 96, 132),

        Version  = new Zone(0f, 0f, 62, 6, 120, 18),
        GameOver = new Zone(0.5f, 0.5f, 0, 0, 440, 240),
        };

    // -----------------------------------------------------------------
    // Compact: 4:3 and similar. Same arrangement, tighter rail.
    // -----------------------------------------------------------------
    public static readonly Profile Compact = new()
    {
        Name = "Compact", Reference = new Vector2(1024, 768), Match = 1f,
        CardSize = new Vector2(76, 114), RowSpacing = 10f,

        // Same Parlor grid, tighter: columns 280 | 1fr | 108, padding 12.
        OpponentHand = new Zone(0.5f, 1f, -48, -12, 420, 104),
        Table        = new Zone(0.5f, 0.5f, -48, 40, 560, 140),
        PlayerHand   = new Zone(0.5f, 0f, -48, 12, 420, 140),

        Score = new Zone(0f, 1f, 12, -12, 180, 196),
        Message   = new Zone(0f, 1f, 12, -216, 224, 52),
        LogButton = new Zone(0f, 1f, 244, -216, 32, 32),
        LogPanel  = new Zone(0f, 1f, 12, -276, 264, 320),

        ActionCenter = new Vector2(-48, 158),
        ActionHeight = 42, ActionGap = 8,
        Suggest = new Zone(0f, 0f, 12, 12, 38, 38),

        AiCaptured     = new Zone(1f, 1f, -12, -12, 94, 200),
        PlayerCaptured = new Zone(1f, 0f, -12, 12, 94, 200),
        PileViewer = new Zone(0.5f, 0.5f, -48, 0, 420, 400),
        PlayerAces = new Zone(1f, 0f, -104, 12, 180, 60),
        AiAces     = new Zone(1f, 1f, -104, -12, 180, 60),

        DrawPileAi    = new Zone(0f, 1f, 200, -12, 88, 120),
        DrawPileHuman = new Zone(0f, 0f, 186, 12, 84, 114),

        Version  = new Zone(0f, 0f, 58, 4, 120, 18),
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

        // The plaque takes the top-left of the header, the toast and log button
        // the space beside it. Untested on a device like the rest of this
        // profile.
        Score     = new Zone(0f, 1f, 14, -14, 176, 196),
        Message   = new Zone(1f, 1f, -14, -14, 470, 56),
        LogButton = new Zone(1f, 1f, -14, -80, 40, 40),
        LogPanel  = new Zone(1f, 1f, -14, -128, 470, 300),

        OpponentHand = new Zone(0.5f, 1f, 0, -292, 684, 120),

        // The taller header pushes the whole upper stack down, so the table
        // drops with it. The space below the table belongs to the action stack
        // over the selected card.
        Table      = new Zone(0.5f, 0.5f, 0, 20, 684, 300),

        ActionCenter = new Vector2(0, 396),
        ActionHeight = 52, ActionGap = 10,
        // Thumb-sized, clear of the version stamp.
        Suggest = new Zone(0f, 0f, 14, 34, 48, 48),

        // No right rail here, so the takes sit at the outer ends of the two
        // hand rows rather than in a column of their own.
        AiCaptured     = new Zone(1f, 1f, -8, -292, 76, 120),
        PlayerCaptured = new Zone(1f, 0f, -8, 230, 76, 120),
        PileViewer = new Zone(0.5f, 0.5f, 0, 0, 660, 560),
        PlayerAces = new Zone(1f, 0f, -92, 96, 200, 60),
        AiAces     = new Zone(0f, 1f, 14, -420, 200, 56),

        PlayerHand = new Zone(0.5f, 0f, 0, 230, 684, 150),
        DrawPileAi    = new Zone(0f, 1f, 20, -440, 76, 104),
        DrawPileHuman = new Zone(0f, 0f, 20, 118, 76, 104),
        Version    = new Zone(1f, 0f, -10, 6, 120, 18),
        GameOver   = new Zone(0.5f, 0.5f, 0, 0, 640, 300),
    };
}
