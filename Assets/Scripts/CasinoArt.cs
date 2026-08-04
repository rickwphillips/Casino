using System.Collections.Generic;
using UnityEngine;

// Procedural art for the Parlor direction. This project ships no art assets, so
// the felt, the card back and the table rail are generated once at runtime and
// shared. Colors come from CasinoTheme, so a re-skin still only edits the palette.
//
// Stage 5 replaces these with real PNGs. Until then this is what makes the felt
// a lit table rather than a flat green rectangle, which was the whole point of
// picking Parlor.
public static class CasinoArt
{
    private static Sprite feltSprite, cardBackSprite, railSprite;
    private static readonly Dictionary<string, Sprite> cache = new();

    // ---------------------------------------------------------------
    // Rounded surfaces.
    //
    // A Unity Image with no sprite is a hard rectangle: no radius, no border,
    // no shadow. Parlor's panels, buttons and cards all have all three, so the
    // whole surface treatment has to be generated and 9-sliced.
    //
    // Every sprite here is built with a signed-distance rounded rect, which
    // gives clean antialiased edges at any size, and sliced with a border of
    // radius + margin so corners never stretch.
    // ---------------------------------------------------------------

    private static float RoundedSdf(float x, float y, float w, float h, float r)
    {
        float qx = Mathf.Abs(x - w / 2f) - (w / 2f - r);
        float qy = Mathf.Abs(y - h / 2f) - (h / 2f - r);
        return Mathf.Sqrt(Mathf.Max(qx, 0) * Mathf.Max(qx, 0) + Mathf.Max(qy, 0) * Mathf.Max(qy, 0))
             + Mathf.Min(Mathf.Max(qx, qy), 0) - r;
    }

    private static Sprite Build(string key, int radius, int margin,
        System.Func<float, int, int, Color> shade)
    {
        if (cache.TryGetValue(key, out var hit) && hit != null) return hit;
        int slice = radius + margin;
        int n = slice * 2 + 4;
        var tex = new Texture2D(n, n, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
        float w = n - margin * 2, h = n - margin * 2;

        for (int y = 0; y < n; y++)
            for (int x = 0; x < n; x++)
                tex.SetPixel(x, y, shade(RoundedSdf(x - margin + 0.5f, y - margin + 0.5f, w, h, radius), x, y));
        tex.Apply();

        var sprite = Sprite.Create(tex, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f), 100f, 0,
            SpriteMeshType.FullRect, new Vector4(slice, slice, slice, slice));
        cache[key] = sprite;
        return sprite;
    }

    // White rounded rect. Left untinted so Image.color still carries card state.
    public static Sprite RoundedFill(int radius) =>
        Build($"fill{radius}", radius, 1, (d, x, y) =>
            new Color(1f, 1f, 1f, Mathf.Clamp01(0.5f - d)));

    // Filled panel with a hairline border, baked because panels are not tinted.
    public static Sprite Panel(int radius, Color fill, Color stroke, float strokeWidth = 1f)
    {
        string key = $"panel{radius}|{fill}|{stroke}|{strokeWidth}";
        return Build(key, radius, 1, (d, x, y) =>
        {
            float inside = Mathf.Clamp01(0.5f - d);
            float edge = Mathf.Clamp01(0.5f - Mathf.Abs(d + strokeWidth / 2f) + strokeWidth / 2f);
            var c = Color.Lerp(fill, stroke, Mathf.Clamp01(edge * stroke.a));
            return new Color(c.r, c.g, c.b, Mathf.Max(fill.a * inside, edge * stroke.a));
        });
    }

    // Soft drop shadow, offset downward. Sits behind a card as its own Image so
    // the card face above it can still be tinted by state without tinting this.
    public static Sprite Shadow(int radius, int blur, int dy) =>
        Build($"shadow{radius}|{blur}|{dy}", radius, blur + dy, (d, x, y) =>
        {
            float s = Mathf.Clamp01(1f - (d + dy * 0.5f) / Mathf.Max(blur, 1));
            return new Color(0f, 0f, 0f, 0.38f * s * s * s);
        });


    // Lit table: brightest above centre, falling off to the edges. Small and
    // bilinear-filtered, because the Image stretches it over the whole screen
    // and a gradient has no detail to lose.
    public static Sprite Felt()
    {
        if (feltSprite != null) return feltSprite;
        const int N = 128;
        var tex = new Texture2D(N, N, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
        var core = CasinoTheme.TableFeltCore;
        var mid = CasinoTheme.TableFeltMid;
        var edge = CasinoTheme.TableFeltEdge;

        for (int y = 0; y < N; y++)
            for (int x = 0; x < N; x++)
            {
                float dx = (x / (float)(N - 1) - 0.5f) / 0.62f;
                float dy = (y / (float)(N - 1) - 0.62f) / 0.46f;
                float d = Mathf.Clamp01(Mathf.Sqrt(dx * dx + dy * dy));
                Color c = d < 0.46f
                    ? Color.Lerp(core, mid, d / 0.46f)
                    : Color.Lerp(mid, edge, (d - 0.46f) / 0.54f);
                tex.SetPixel(x, y, c);
            }
        tex.Apply();
        feltSprite = Sprite.Create(tex, new Rect(0, 0, N, N), new Vector2(0.5f, 0.5f));
        return feltSprite;
    }

    // Gilt lattice on bronze with a brass edge, replacing the navy card back.
    public static Sprite CardBack()
    {
        if (cardBackSprite != null) return cardBackSprite;
        const int W = 64, H = 96;
        var tex = new Texture2D(W, H, TextureFormat.RGBA32, false);
        var field = CasinoTheme.CardBackBase;
        var gilt = CasinoTheme.CardBackLattice;
        var border = CasinoTheme.CardBackBorder;

        for (int y = 0; y < H; y++)
            for (int x = 0; x < W; x++)
            {
                bool onBorder = x < 2 || y < 2 || x >= W - 2 || y >= H - 2;
                bool stripe = ((x + y) % 12) < 3 || ((x - y + 960) % 12) < 3;
                tex.SetPixel(x, y, onBorder ? border : stripe ? gilt : field);
            }
        tex.Apply();
        cardBackSprite = Sprite.Create(tex, new Rect(0, 0, W, H), new Vector2(0.5f, 0.5f));
        return cardBackSprite;
    }

    // Rounded hairline frame, drawn 9-sliced so it keeps a 1px edge at any size.
    // This is the brass rail that makes the board read as a table with edges.
    public static Sprite Rail()
    {
        if (railSprite != null) return railSprite;
        const int N = 32, R = 10;
        var tex = new Texture2D(N, N, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
        var brass = CasinoTheme.TableRail;

        for (int y = 0; y < N; y++)
            for (int x = 0; x < N; x++)
            {
                // Signed distance to a rounded rectangle inset by 1px.
                float qx = Mathf.Abs(x - (N - 1) / 2f) - ((N - 1) / 2f - 1 - R);
                float qy = Mathf.Abs(y - (N - 1) / 2f) - ((N - 1) / 2f - 1 - R);
                float d = Mathf.Sqrt(Mathf.Max(qx, 0) * Mathf.Max(qx, 0) +
                                     Mathf.Max(qy, 0) * Mathf.Max(qy, 0))
                          + Mathf.Min(Mathf.Max(qx, qy), 0) - R;
                float a = Mathf.Clamp01(1.4f - Mathf.Abs(d));   // ~1.5px ring, soft edge
                tex.SetPixel(x, y, new Color(brass.r, brass.g, brass.b, brass.a * a));
            }
        tex.Apply();
        railSprite = Sprite.Create(tex, new Rect(0, 0, N, N), new Vector2(0.5f, 0.5f),
            100f, 0, SpriteMeshType.FullRect, new Vector4(R + 2, R + 2, R + 2, R + 2));
        return railSprite;
    }
}
