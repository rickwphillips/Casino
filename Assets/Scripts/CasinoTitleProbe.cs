using System.Collections;
using System.IO;
using UnityEngine;

// Verifies the title screen and its settings panel, the one part of the UI
// every other harness deliberately skips (SkipTitle). Walks the panel the
// way a player does: closed by default, opened, the win total stepped up
// and back, then the title dismissed onto the live board.
//
// Drop <project>/title-probe.flag. Never active without the flag.
public class CasinoTitleProbe : MonoBehaviour
{
    private static string Root => Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
    private static string FlagPath => Path.Combine(Root, "title-probe.flag");

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Install()
    {
        if (!File.Exists(FlagPath)) return;
        try { File.Delete(FlagPath); } catch { }
        var host = new GameObject("CasinoTitleProbe");
        host.AddComponent<CasinoTitleProbe>();
        DontDestroyOnLoad(host);
    }

    private void Start() => StartCoroutine(Run());

    private IEnumerator Run()
    {
        yield return new WaitForSecondsRealtime(2.5f);
        var ui = UIManager.Instance;
        if (ui == null || !ui.TitleIsUp)
        {
            Debug.LogWarning("CasinoTitleProbe: no title screen to probe (skip-title.flag present?)");
            yield break;
        }

        ScreenshotCapture.Capture("title-closed");
        yield return new WaitForSecondsRealtime(0.6f);

        ui.ToggleTitleSettings();
        yield return new WaitForSecondsRealtime(0.6f);
        ScreenshotCapture.Capture("title-settings-open");

        ui.StepTitleWinScore(+1);
        ui.StepTitleWinScore(+1);
        yield return new WaitForSecondsRealtime(0.6f);
        ScreenshotCapture.Capture("title-win-stepped");

        ui.StepTitleWinScore(-1);
        ui.StepTitleWinScore(-1);
        yield return new WaitForSecondsRealtime(0.3f);

        ui.DismissTitle();
        yield return new WaitForSecondsRealtime(1.2f);
        ScreenshotCapture.Capture("title-dismissed");
    }
}
