using System;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;

// Pins the Game view to a fixed size, one per CasinoLayout profile, so
// screenshots are reproducible and canvas units equal screen pixels.
//
// Why this exists: on Free Aspect the Game view is whatever size the window
// happens to be, and with CanvasScaler matchWidthOrHeight = 1 that silently
// changes the canvas width on every run (observed: 1350x600, then 1182x600).
// Two screenshots of the same build were not comparable to each other, let
// alone to a mockup. A fixed size makes the design loop mean something.
//
// Drop <project>/gameview.txt containing "WxH" (e.g. 720x1280) and reload to
// switch breakpoint unattended; the Casino > Game View menu does the same by hand.
//
// The Game view size list is internal editor API, hence the reflection. It is
// wrapped so that an API change in a future Unity breaks the pin with a clear
// warning rather than breaking the editor. Worst case: set it by hand from the
// Game view dropdown and nothing else is affected.
[InitializeOnLoad]
public static class GameViewSizePin
{
    // One entry per CasinoLayout profile, so every breakpoint can be checked.
    private const string Prefix = "Casino ";
    private static readonly (int w, int h) Wide = (1280, 720);
    private static readonly (int w, int h) Compact = (1024, 768);
    private static readonly (int w, int h) PortraitSize = (720, 1280);

    // Tooling drops <project>/gameview.txt containing "WxH" to switch shape
    // between runs without a human touching the Game view dropdown.
    private static string RequestPath =>
        Path.GetFullPath(Path.Combine(Application.dataPath, "..", "gameview.txt"));

    static GameViewSizePin() => EditorApplication.delayCall += () => ApplyRequested();

    // Called directly by AutoVerifyPlay before it enters Play mode. Both used to
    // schedule themselves with delayCall, so whichever ran second decided whether
    // the requested size was actually in effect for the run: a switch would be
    // silently ignored roughly half the time. Explicit ordering removes the race.
    public static void ApplyRequested() => Pin(Requested(), false);

    [MenuItem("Casino/Game View/Wide 1280x720")]
    private static void PinWide() => Pin(Wide, true);
    [MenuItem("Casino/Game View/Compact 1024x768")]
    private static void PinCompact() => Pin(Compact, true);
    [MenuItem("Casino/Game View/Portrait 720x1280")]
    private static void PinPortrait() => Pin(PortraitSize, true);

    private static (int w, int h) Requested()
    {
        try
        {
            if (!File.Exists(RequestPath)) return Wide;
            var parts = File.ReadAllText(RequestPath).Trim().ToLowerInvariant().Split('x');
            if (parts.Length == 2 && int.TryParse(parts[0], out int w) && int.TryParse(parts[1], out int h)
                && w > 0 && h > 0)
                return (w, h);
        }
        catch { }
        return Wide;
    }

    private static void Pin((int w, int h) size, bool verbose)
    {
        int Width = size.w, Height = size.h;
        string Label = Prefix + Width + "x" + Height;
        Debug.Log($"GameViewSizePin: requesting {Label}");
        try
        {
            // Do not assume which assembly holds these: UnityEditor.dll is a
            // type-forwarding facade over UnityEditor.CoreModule.dll, and which
            // one actually owns a given internal type has moved between versions.
            var sizesType = FindEditorType("UnityEditor.GameViewSizes");
            var groupEnum = FindEditorType("UnityEditor.GameViewSizeGroupType");
            var sizeType = FindEditorType("UnityEditor.GameViewSize");
            var sizeKind = FindEditorType("UnityEditor.GameViewSizeType");
            var gameViewType = FindEditorType("UnityEditor.GameView");
            if (sizesType == null || sizeType == null || sizeKind == null ||
                groupEnum == null || gameViewType == null)
            {
                Warn(verbose, "internal GameView types not found");
                return;
            }

            object sizes = typeof(ScriptableSingleton<>).MakeGenericType(sizesType)
                .GetProperty("instance", BindingFlags.Public | BindingFlags.Static)
                .GetValue(null);
            object currentGroup = sizesType.GetProperty("currentGroupType").GetValue(sizes);
            object group = sizesType.GetMethod("GetGroup")
                .Invoke(sizes, new[] { Enum.ToObject(groupEnum, (int)currentGroup) });

            int index = IndexOfLabel(group, Label);
            if (index < 0)
            {
                object entry = sizeType
                    .GetConstructor(new[] { sizeKind, typeof(int), typeof(int), typeof(string) })
                    .Invoke(new[] { Enum.Parse(sizeKind, "FixedResolution"), Width, Height, Label });
                group.GetType().GetMethod("AddCustomSize").Invoke(group, new[] { entry });
                index = IndexOfLabel(group, Label);
            }
            if (index < 0)
            {
                Warn(verbose, "custom size was added but could not be found again");
                return;
            }

            // Only touch an already-open Game view; do not force one open on a
            // headless or batch load.
            var open = Resources.FindObjectsOfTypeAll(gameViewType);
            if (open.Length == 0)
            {
                Warn(verbose, "no Game view is open");
                return;
            }

            var selected = gameViewType.GetProperty("selectedSizeIndex",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            foreach (var window in open)
            {
                if ((int)selected.GetValue(window) == index) continue;
                selected.SetValue(window, index);
                ((EditorWindow)window).Repaint();
            }
            Debug.Log($"Game view pinned to {Label}");
        }
        catch (Exception e)
        {
            Warn(verbose, e.Message);
        }
    }

    private static Type FindEditorType(string fullName)
    {
        foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
        {
            if (!asm.GetName().Name.StartsWith("UnityEditor")) continue;
            var found = asm.GetType(fullName, false);
            if (found != null) return found;
        }
        return null;
    }

    private static int IndexOfLabel(object group, string label)
    {
        var type = group.GetType();
        int total = (int)type.GetMethod("GetTotalCount").Invoke(group, null);
        var get = type.GetMethod("GetGameViewSize");
        for (int i = 0; i < total; i++)
        {
            object size = get.Invoke(group, new object[] { i });
            var text = size.GetType().GetProperty("displayText").GetValue(size) as string;
            if (text != null && text.Contains(label)) return i;
        }
        return -1;
    }

    private static void Warn(bool verbose, string reason)
    {
        Debug.LogWarning(
            $"Could not pin the Game view ({reason}). Set it by hand: " +
            "Game view dropdown > + > Fixed Resolution.");
    }
}
