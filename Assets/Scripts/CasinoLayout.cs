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
        // A pool of light behind your hand while the turn is yours. Bigger than
        // the hand on every side, because a glow with a visible edge is a box.
        public Zone HandGlow;
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

        // 132, not 112: the hand containers carried a 0.75 scale that made
        // their cards draw smaller than the zone implied. With that gone a
        // 120-tall card needs a zone that can actually hold one.
        OpponentHand = new Zone(0.5f, 1f, -52, -14, 460, 132),
        Table        = new Zone(0.5f, 0.5f, -52, 46, 620, 150),
        // Sunk below the bottom edge on purpose: the hand is held, not laid
        // out, and cards you are holding run off the bottom of the view. The
        // fan drops the outer cards further, so they clip more than the middle.
        PlayerHand   = new Zone(0.5f, 0f, -52, -30, 460, 158),
        HandGlow     = new Zone(0.5f, 0f, -52, -104, 660, 300),

        // The plaque sits at the right centre between the two takes, the same
        // seat it holds in portrait: one player per side of the scoreboard.
        Score = new Zone(1f, 0.5f, -14, 0, 196, 210),
        // The toast takes the top-left corner the plaque vacated, stopping
        // short of the AI draw pile at x 254.
        Message   = new Zone(0f, 1f, 14, -14, 230, 56),

        // ActionCenter is only the fallback anchor (a build selected with no
        // hand card); normally the stack sits over the selected card itself.
        ActionCenter = new Vector2(-52, 180),
        ActionHeight = 44, ActionGap = 8,

        // The two round utility buttons pair up in the bottom-left corner, and
        // the log opens upward out of its own button. The log button used to
        // float beside the toast, where it read as punctuation on a line that
        // is usually empty rather than as a control.
        Suggest   = new Zone(0f, 0f, 14, 14, 40, 40),
        LogButton = new Zone(0f, 0f, 64, 14, 40, 40),
        LogPanel  = new Zone(0f, 0f, 14, 64, 300, 360),

        // Each side's take, face up and stacked, on that side's end of the
        // right rail: the AI's above, yours below, the same seating order the
        // plaque uses. Short enough to leave the plaque the middle of the
        // rail; a take that outgrows its zone wraps into further columns
        // (SyncCapturedStack), so height no longer decides readability.
        AiCaptured     = new Zone(1f, 1f, -14, -14, 100, 220),
        PlayerCaptured = new Zone(1f, 0f, -14, 14, 100, 220),
        // Over the table, not down the left edge where the plaque now lives.
        PileViewer = new Zone(0.5f, 0.5f, -52, 0, 460, 420),

        // Coin shelves on the left edge: the AI's under the toast, the
        // human's above the utility buttons, both growing toward the table.
        PlayerAces = new Zone(0f, 0f, 14, 64, 200, 64),
        AiAces     = new Zone(0f, 1f, 14, -80, 200, 64),

        // The draw pile rides with the dealer: at the AI's elbow up top when it
        // deals (clear of the opponent hand, which starts at x 358), at yours
        // down below when you do.
        DrawPileAi    = new Zone(0f, 1f, 254, -14, 96, 132),
        DrawPileHuman = new Zone(0f, 0f, 254, 14, 96, 132),

        Version  = new Zone(0f, 0f, 116, 6, 120, 18),
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
        PlayerHand   = new Zone(0.5f, 0f, -48, -26, 420, 140),
        HandGlow     = new Zone(0.5f, 0f, -48, -96, 600, 270),

        Score = new Zone(1f, 0.5f, -12, 0, 180, 196),
        // Stops short of the AI draw pile at x 200.
        Message   = new Zone(0f, 1f, 12, -12, 184, 52),

        ActionCenter = new Vector2(-48, 158),
        ActionHeight = 42, ActionGap = 8,
        Suggest   = new Zone(0f, 0f, 12, 12, 38, 38),
        LogButton = new Zone(0f, 0f, 58, 12, 38, 38),
        LogPanel  = new Zone(0f, 0f, 12, 58, 280, 340),

        AiCaptured     = new Zone(1f, 1f, -12, -12, 94, 200),
        PlayerCaptured = new Zone(1f, 0f, -12, 12, 94, 200),
        PileViewer = new Zone(0.5f, 0.5f, -48, 0, 420, 400),
        PlayerAces = new Zone(0f, 0f, 12, 58, 180, 60),
        AiAces     = new Zone(0f, 1f, 12, -72, 180, 60),

        DrawPileAi    = new Zone(0f, 1f, 200, -12, 88, 120),
        DrawPileHuman = new Zone(0f, 0f, 186, 12, 84, 114),

        Version  = new Zone(0f, 0f, 106, 4, 120, 18),
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

        // The plaque sits at the right centre, in the dead band between the
        // table row and the hand, where nothing else lives. The toast and log
        // button keep the header. Untested on a device like the rest of this
        // profile.
        Score     = new Zone(1f, 0.5f, -14, 0, 176, 196),
        Message   = new Zone(1f, 1f, -14, -14, 500, 60),

        OpponentHand = new Zone(0.5f, 1f, 0, -292, 684, 120),

        // The taller header pushes the whole upper stack down, so the table
        // drops with it. The space below the table belongs to the action stack
        // over the selected card. The zone stops short of the plaque on the
        // right; ApplyTableWrap folds a crowded row into a second one at the
        // zone's width instead of letting it grow under the plaque.
        Table      = new Zone(0.5f, 0.5f, -70, 20, 460, 300),

        ActionCenter = new Vector2(0, 396),
        ActionHeight = 52, ActionGap = 10,
        // Thumb-sized, clear of the version stamp, paired with the log button.
        Suggest   = new Zone(0f, 0f, 14, 34, 48, 48),
        LogButton = new Zone(0f, 0f, 72, 34, 48, 48),
        LogPanel  = new Zone(0f, 0f, 14, 92, 480, 400),

        // The right edge reads top to bottom as the AI's take, its coins, the
        // plaque, the human's coins, the human's take: one player per side of
        // the scoreboard.
        AiCaptured     = new Zone(1f, 1f, -8, -292, 76, 120),
        PlayerCaptured = new Zone(1f, 0f, -8, 230, 76, 120),
        PileViewer = new Zone(0.5f, 0.5f, 0, 0, 660, 560),
        // Coin shelves ride beside each seat's draw pile on the left.
        PlayerAces = new Zone(0f, 0f, 104, 118, 200, 60),
        AiAces     = new Zone(0f, 1f, 104, -300, 200, 56),

        // Portrait keeps the hand fully on screen: there is no spare height
        // below it to clip into, only the action stack and the thumb rail.
        PlayerHand = new Zone(0.5f, 0f, 0, 230, 684, 150),
        HandGlow   = new Zone(0.5f, 0f, 0, 170, 720, 280),
        // Level with the AI hand row, not adrift in the middle of the felt.
        DrawPileAi    = new Zone(0f, 1f, 20, -300, 76, 104),
        DrawPileHuman = new Zone(0f, 0f, 20, 118, 76, 104),
        Version    = new Zone(1f, 0f, -10, 6, 120, 18),
        GameOver   = new Zone(0.5f, 0.5f, 0, 0, 640, 300),
    };
}
