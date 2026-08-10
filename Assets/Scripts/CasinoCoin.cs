using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// A struck trophy coin: gold rim, brass face, the suit (or any glyph) ghosted
// across the whole face as an impression, a device struck over it in relief,
// and a sheen that sweeps the metal every few seconds.
//
// Deliberately generic: the device and watermark are parameters, so the same
// coin mints an ace trophy ("A" over ♠), Big Casino ("10" over ♦), Little
// Casino ("2" over ♠), or anything else worth tossing onto the felt. All
// colors come from CasinoTheme.Coin*; a coin is a coin everywhere.
//
// Owns its own animation: Splash() plays the entrance, the shimmer runs for
// the coin's whole life, and destroying the GameObject cancels both.
public class CasinoCoin : MonoBehaviour
{
    private RectTransform shine;
    private float diameter;

    // Coins minted this session; staggers the shimmer phase so a cluster
    // never flashes in unison, which reads as an alert, not light on metal.
    private static int minted;

    public static CasinoCoin Create(Transform parent, string device, string watermark,
                                    float diameter = 46f, bool glow = false)
    {
        var go = new GameObject($"Coin-{device}{watermark}");
        go.transform.SetParent(parent, false);
        var r = go.AddComponent<RectTransform>();
        r.anchorMin = r.anchorMax = new Vector2(0.5f, 0.5f);
        r.sizeDelta = new Vector2(diameter, diameter);

        var coin = go.AddComponent<CasinoCoin>();
        coin.diameter = diameter;
        coin.Build(device, watermark, glow);
        return coin;
    }

    // The entrance: lands big and bright, shrinks onto the felt with a small
    // dip past full size so it reads as arriving, not appearing.
    public void Splash() => StartCoroutine(SplashIn());

    private void Build(string device, string watermark, bool glow)
    {
        float d = diameter;

        // The halo, for coins worth shouting about (Big Casino): a soft gold
        // ring behind the rim that breathes rather than blinks.
        if (glow)
        {
            var glowGo = new GameObject("Glow");
            glowGo.transform.SetParent(transform, false);
            var glr = glowGo.AddComponent<RectTransform>();
            glr.anchorMin = glr.anchorMax = new Vector2(0.5f, 0.5f);
            glr.sizeDelta = new Vector2(d * 1.9f, d * 1.9f);
            var glowImg = glowGo.AddComponent<Image>();
            glowImg.sprite = CasinoArt.Glow(Mathf.RoundToInt(d / 2f), Mathf.RoundToInt(d * 0.45f));
            glowImg.color = CasinoTheme.CoinGlow;
            glowImg.raycastTarget = false;
            StartCoroutine(Breathe(glowImg));
        }

        // Radius = half the rect, so the rounded rect renders as a circle.
        var rim = gameObject.AddComponent<Image>();
        rim.sprite = CasinoArt.RoundedFill(Mathf.RoundToInt(d / 2f));
        rim.type = Image.Type.Sliced;
        rim.color = CasinoTheme.CoinRim;
        rim.raycastTarget = false;

        var faceGo = new GameObject("Face");
        faceGo.transform.SetParent(transform, false);
        var fr = faceGo.AddComponent<RectTransform>();
        fr.anchorMin = Vector2.zero;
        fr.anchorMax = Vector2.one;
        float inset = d * 0.055f;
        fr.offsetMin = new Vector2(inset, inset);
        fr.offsetMax = new Vector2(-inset, -inset);
        var face = faceGo.AddComponent<Image>();
        face.sprite = CasinoArt.RoundedFill(Mathf.RoundToInt(d / 2f - inset));
        face.type = Image.Type.Sliced;
        face.color = CasinoTheme.CoinFace;
        face.raycastTarget = false;
        // The face doubles as the mask so the shimmer never leaves the coin.
        faceGo.AddComponent<Mask>().showMaskGraphic = true;

        // The impression: the watermark struck INTO the face, across the
        // whole coin. Depth comes from the light catching the incision's
        // lower lip: a light copy a hair below, the dark strike over it.
        // Multi-glyph watermarks (the four-suit cards coin) shrink to fit.
        bool multiMark = watermark.Length > 1;
        float markSize = d * (multiMark ? 0.40f : 0.92f);

        var markLight = Text(faceGo.transform, "WatermarkLight");
        markLight.rectTransform.anchoredPosition = new Vector2(0, -d * 0.035f);
        markLight.fontSize = markSize;
        markLight.lineSpacing = multiMark ? -14f : 0f;
        markLight.color = CasinoTheme.CoinWatermarkLight;
        markLight.text = watermark;

        var mark = Text(faceGo.transform, "Watermark");
        mark.fontSize = markSize;
        mark.lineSpacing = multiMark ? -14f : 0f;
        mark.color = CasinoTheme.CoinWatermark;
        mark.text = watermark;

        // The device: raised OUT of the face, the opposite relief. A dark
        // cast shadow below, the bright letter over it.
        // A two-character device ("10") needs a smaller strike than "A".
        float deviceSize = d * (device.Length > 1 ? 0.46f : 0.59f);

        var shadow = Text(faceGo.transform, "DeviceShadow");
        shadow.rectTransform.anchoredPosition = new Vector2(0, -d * 0.035f);
        shadow.fontSize = deviceSize;
        shadow.fontStyle = FontStyles.Bold;
        CasinoType.ApplySerif(shadow);
        shadow.color = CasinoTheme.CoinDeviceShadow;
        shadow.text = device;

        var stamp = Text(faceGo.transform, "Device");
        stamp.fontSize = deviceSize;
        stamp.fontStyle = FontStyles.Bold;
        CasinoType.ApplySerif(stamp);
        stamp.color = CasinoTheme.CoinDevice;
        stamp.text = device;

        // The shimmer: a tilted sheen, parked outside the mask between sweeps.
        var shineGo = new GameObject("Shine");
        shineGo.transform.SetParent(faceGo.transform, false);
        shine = shineGo.AddComponent<RectTransform>();
        shine.anchorMin = shine.anchorMax = new Vector2(0.5f, 0.5f);
        shine.sizeDelta = new Vector2(d * 0.26f, d * 1.6f);
        shine.localRotation = Quaternion.Euler(0, 0, 24f);
        shine.anchoredPosition = new Vector2(-d, 0);
        var shineImg = shineGo.AddComponent<Image>();
        shineImg.sprite = CasinoArt.RoundedFill(Mathf.RoundToInt(d * 0.13f));
        shineImg.type = Image.Type.Sliced;
        shineImg.color = CasinoTheme.CoinShine;
        shineImg.raycastTarget = false;

        StartCoroutine(Shimmer(minted++));
    }

    private IEnumerator SplashIn()
    {
        var group = gameObject.AddComponent<CanvasGroup>();
        var r = (RectTransform)transform;
        const float dur = 0.5f;
        for (float t = 0; t < dur; t += Time.deltaTime)
        {
            float k = t / dur;
            float ease = 1f - (1f - k) * (1f - k);              // decelerate
            float scale = Mathf.Lerp(2.6f, 1f, ease)
                        - 0.08f * Mathf.Sin(k * Mathf.PI);      // dip past 1 and settle
            r.localScale = Vector3.one * scale;
            group.alpha = Mathf.Clamp01(k * 3f);
            yield return null;
        }
        r.localScale = Vector3.one;
        group.alpha = 1f;
    }

    // Quiet and irregular on purpose: the gap and the sweep direction vary
    // every cycle, so a shelf of coins reads as ambient light on metal, not
    // as a synchronized effect asking to be watched.
    private IEnumerator Shimmer(int phase)
    {
        float span = diameter * 0.95f;
        const float sweep = 0.75f;
        yield return new WaitForSeconds(1.2f + phase % 6 * 0.7f);
        while (true)
        {
            float dir = Random.value < 0.5f ? 1f : -1f;
            for (float t = 0; t < sweep; t += Time.deltaTime)
            {
                shine.anchoredPosition = new Vector2(dir * Mathf.Lerp(-span, span, t / sweep), 0);
                yield return null;
            }
            shine.anchoredPosition = new Vector2(-span * 2f, 0);   // parked behind the mask
            yield return new WaitForSeconds(Random.Range(3.5f, 8f));
        }
    }

    // The glow's shimmer: a slow sine on alpha, never fully off, so the coin
    // reads as radiant rather than signalling.
    private IEnumerator Breathe(Image glowImg)
    {
        Color baseColor = glowImg.color;
        float t = Random.value * 10f;   // desync multiple glowing coins
        while (true)
        {
            t += Time.deltaTime;
            float k = 0.65f + 0.35f * Mathf.Sin(t * 2.1f);
            glowImg.color = new Color(baseColor.r, baseColor.g, baseColor.b, baseColor.a * k);
            yield return null;
        }
    }

    private static TextMeshProUGUI Text(Transform parent, string name)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var text = go.AddComponent<TextMeshProUGUI>();
        var rect = text.rectTransform;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
        text.alignment = TextAlignmentOptions.Center;
        text.raycastTarget = false;
        return text;
    }
}
