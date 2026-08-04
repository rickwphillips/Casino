using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.InputSystem;

// Stage 0 of the UI redesign: a way to actually see the game.
//
// Captures the exact game viewport (no window chrome, known resolution) to
// <project>/screenshots/ in the editor, or persistentDataPath in a player.
//
// Three triggers, mirroring the existing layout-report / auto-verify.flag pattern:
//   - automatic, once, at the settled frame (same ~1s delay UIManager uses)
//   - F9 on demand; hold Shift for a 2x supersampled capture
//   - dropping <project>/screenshot.flag, which tooling can do without a keypress
//
// Self-installing: no scene wiring, no prefab, nothing to hand-edit in Scene.unity.
public class ScreenshotCapture : MonoBehaviour
{
    // The opening deal spawns ~12 ghost cards at 0.07s intervals and tweens each,
    // so anything under ~2s catches cards mid-flight. Wait it out: the human is
    // non-dealer and moves first, so the board then sits still awaiting input.
    private const float SettledDelay = 3f;
    private const float FlagPollInterval = 0.5f;

    private static string Root =>
        Application.isEditor
            ? Path.GetFullPath(Path.Combine(Application.dataPath, "..", "screenshots"))
            : Path.Combine(Application.persistentDataPath, "screenshots");

    private static string FlagPath =>
        Path.GetFullPath(Path.Combine(Application.dataPath, "..", "screenshot.flag"));

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Install()
    {
        var host = new GameObject("ScreenshotCapture");
        host.AddComponent<ScreenshotCapture>();
        DontDestroyOnLoad(host);
    }

    private void Start()
    {
        StartCoroutine(CaptureWhenSettled());
        StartCoroutine(WatchFlag());
    }

    private void Update()
    {
        var keyboard = Keyboard.current;
        if (keyboard == null || !keyboard[Key.F9].wasPressedThisFrame) return;
        Capture("manual", keyboard.shiftKey.isPressed ? 2 : 1);
    }

    private IEnumerator CaptureWhenSettled()
    {
        yield return new WaitForSeconds(SettledDelay);
        Capture("settled", 1);
    }

    // Lets tooling ask for a shot without anyone touching the keyboard.
    private IEnumerator WatchFlag()
    {
        var wait = new WaitForSeconds(FlagPollInterval);
        while (true)
        {
            yield return wait;
            if (!File.Exists(FlagPath)) continue;
            try { File.Delete(FlagPath); } catch { }
            Capture("flag", 1);
        }
    }

    // Screenshots are diagnostics: a failure here must never take the game down.
    private static void Capture(string label, int superSize)
    {
        try
        {
            Directory.CreateDirectory(Root);
            string name = $"casino-v{Application.version}-{System.DateTime.Now:yyyyMMdd-HHmmss}-{label}.png";
            string path = Path.Combine(Root, name);
            ScreenCapture.CaptureScreenshot(path, superSize);
            Debug.Log($"Screenshot ({label}, {Screen.width}x{Screen.height}, x{superSize}): {path}");
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"screenshot failed: {e.Message}");
        }
    }
}
