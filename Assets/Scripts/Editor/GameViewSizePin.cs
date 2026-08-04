using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;

// Pins the Game view to a fixed 1280x720 so screenshots are reproducible and
// map 1:1 onto the design reference canvas.
//
// Why this exists: on Free Aspect the Game view is whatever size the window
// happens to be, and with CanvasScaler matchWidthOrHeight = 1 that silently
// changes the canvas width on every run (observed: 1350x600, then 1182x600).
// Two screenshots of the same build were not comparable to each other, let
// alone to a mockup. A fixed size makes the design loop mean something.
//
// The Game view size list is internal editor API, hence the reflection. It is
// wrapped so that an API change in a future Unity breaks the pin with a clear
// warning rather than breaking the editor. Worst case: set it by hand from the
// Game view dropdown and nothing else is affected.
[InitializeOnLoad]
public static class GameViewSizePin
{
    private const int Width = 1280;
    private const int Height = 720;
    private const string Label = "Casino 1280x720";

    static GameViewSizePin() => EditorApplication.delayCall += () => Pin(false);

    [MenuItem("Casino/Pin Game View to 1280x720")]
    private static void PinFromMenu() => Pin(true);

    private static void Pin(bool verbose)
    {
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

            int index = IndexOfLabel(group);
            if (index < 0)
            {
                object size = sizeType
                    .GetConstructor(new[] { sizeKind, typeof(int), typeof(int), typeof(string) })
                    .Invoke(new[] { Enum.Parse(sizeKind, "FixedResolution"), Width, Height, Label });
                group.GetType().GetMethod("AddCustomSize").Invoke(group, new[] { size });
                index = IndexOfLabel(group);
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
            if (verbose) Debug.Log($"Game view pinned to {Label}");
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

    private static int IndexOfLabel(object group)
    {
        var type = group.GetType();
        int total = (int)type.GetMethod("GetTotalCount").Invoke(group, null);
        var get = type.GetMethod("GetGameViewSize");
        for (int i = 0; i < total; i++)
        {
            object size = get.Invoke(group, new object[] { i });
            var text = size.GetType().GetProperty("displayText").GetValue(size) as string;
            if (text != null && text.Contains(Label)) return i;
        }
        return -1;
    }

    private static void Warn(bool verbose, string reason)
    {
        if (verbose) Debug.LogWarning($"Could not pin the Game view ({reason}). Set it by hand: Game view dropdown > + > Fixed Resolution {Width}x{Height}.");
    }
}
