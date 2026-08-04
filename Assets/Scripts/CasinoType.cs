using TMPro;
using UnityEngine;

// Typography for the Parlor direction.
//
// The Stage 2 spec fixed four type roles. Parlor assigns a serif to the two that
// carry the game's character: the card ranks, which are read at a glance rather
// than parsed, and display text (banner, summary, headings). Interface and data
// stay on the TMP default sans, because tabular figures that do not jitter matter
// more there than personality.
//
// The face is Libre Baskerville (SIL OFL 1.1, see Assets/Fonts/OFL.txt), chosen
// over a Garamond or a Playfair because card ranks are set small and those have
// hairlines that vanish. It is a transitional serif designed for screen, with a
// tall x-height and unambiguous figures.
//
// Every call is null-safe: if the TMP asset has not been generated yet the UI
// simply keeps the default font rather than rendering nothing.
public static class CasinoType
{
    private const string SerifPath = "Fonts/CasinoSerif";

    private static TMP_FontAsset serif;
    private static bool looked;

    public static TMP_FontAsset Serif
    {
        get
        {
            if (looked) return serif;
            looked = true;
            serif = Resources.Load<TMP_FontAsset>(SerifPath);
            if (serif == null)
                Debug.LogWarning($"CasinoType: {SerifPath} not found. Falling back to the " +
                                 "TMP default. Run Casino > Fonts > Rebuild TMP font asset.");
            return serif;
        }
    }

    // Card ranks and display text.
    public static void ApplySerif(TMP_Text text)
    {
        if (text != null && Serif != null) text.font = Serif;
    }
}
