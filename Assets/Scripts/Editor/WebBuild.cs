using UnityEditor;
using UnityEngine;

// Web build for the portfolio (rickwphillips.com/casino). Gzip with the
// decompression fallback so any static host serves it with zero header
// configuration; Brotli would be smaller but needs Content-Encoding set
// server-side, and the portfolio is a static export.
public static class WebBuild
{
    [MenuItem("Casino/Build Web (build/web)")]
    public static void Build()
    {
        PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Gzip;
        PlayerSettings.WebGL.decompressionFallback = true;

        var report = BuildPipeline.BuildPlayer(
            new[] { "Assets/Scenes/Scene.unity" },
            "build/web",
            BuildTarget.WebGL,
            BuildOptions.None);
        Debug.Log($"Web build: {report.summary.result}, " +
                  $"{report.summary.totalSize / (1024 * 1024)}MB, {report.summary.totalErrors} errors -> build/web");
    }
}
