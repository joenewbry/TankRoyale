using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace TankRoyale.Editor
{
    /// <summary>
    /// Headless build entry points. Called via Unity -executeMethod.
    /// Usage:
    ///   /Applications/Unity/Hub/Editor/6000.3.10f1/Unity.app/Contents/MacOS/Unity \
    ///     -batchmode -quit \
    ///     -projectPath /Users/joe/dev/TankRoyale/Unity/TankRoyale \
    ///     -executeMethod TankRoyale.Editor.BuildScript.BuildWebGL \
    ///     -logFile /tmp/unity-webgl-build.log
    /// </summary>
    public static class BuildScript
    {
        private static readonly string[] Scenes = {
            "Assets/Scenes/Arena.unity",
            // Add MainMenu scene path here when created
        };

        private static string WebGLOutputPath =>
            Path.Combine(Application.dataPath, "..", "..", "..", "Builds", "WebGL");

        [MenuItem("TankRoyale/Build/WebGL (Dev)")]
        public static void BuildWebGL()
        {
            string outPath = Path.GetFullPath(WebGLOutputPath);
            Directory.CreateDirectory(outPath);

            // Filter to scenes that actually exist
            string[] validScenes = Scenes
                .Where(s => File.Exists(Path.Combine(Application.dataPath, "..", s.Replace("Assets/", ""))))
                .ToArray();

            if (validScenes.Length == 0)
            {
                // Fallback: use whatever is in EditorBuildSettings
                validScenes = EditorBuildSettings.scenes
                    .Where(s => s.enabled)
                    .Select(s => s.path)
                    .ToArray();
            }

            Debug.Log($"[BuildScript] Building WebGL to: {outPath}");
            Debug.Log($"[BuildScript] Scenes: {string.Join(", ", validScenes)}");

            var options = new BuildPlayerOptions
            {
                scenes = validScenes,
                locationPathName = outPath,
                target = BuildTarget.WebGL,
                options = BuildOptions.None,
            };

            // WebGL-specific settings
            PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Gzip;
            PlayerSettings.WebGL.template = "APPLICATION:Default";
            PlayerSettings.runInBackground = true;

            var report = BuildPipeline.BuildPlayer(options);
            var summary = report.summary;

            if (summary.result == BuildResult.Succeeded)
            {
                Debug.Log($"[BuildScript] ✅ WebGL build succeeded: {summary.totalSize / 1024 / 1024} MB in {summary.totalTime.TotalSeconds:F1}s");
                Debug.Log($"[BuildScript] Output: {outPath}");
            }
            else
            {
                Debug.LogError($"[BuildScript] ❌ Build FAILED: {summary.result} — {summary.totalErrors} errors");
                EditorApplication.Exit(1);
            }
        }

        [MenuItem("TankRoyale/Build/Configure WebGL Settings")]
        public static void ConfigureWebGLSettings()
        {
            PlayerSettings.companyName = "DigitalSurfaceLabs";
            PlayerSettings.productName = "Tank Royale";
            PlayerSettings.SetApplicationIdentifier(BuildTargetGroup.WebGL, "com.digitalsurfacelabs.tankroyale");

            // WebGL
            PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Gzip;
            PlayerSettings.WebGL.linkerTarget = WebGLLinkerTarget.Wasm;
            PlayerSettings.WebGL.exceptionSupport = WebGLExceptionSupport.None; // smaller build
            PlayerSettings.runInBackground = true;

            // Quality — use "Low" for WebGL by default
            QualitySettings.SetQualityLevel(0, applyExpensiveChanges: true);

            // Make sure Arena is in build settings
            var existing = EditorBuildSettings.scenes.ToList();
            string arenaPath = "Assets/Scenes/Arena.unity";
            if (!existing.Any(s => s.path == arenaPath))
            {
                existing.Insert(0, new EditorBuildSettingsScene(arenaPath, true));
                EditorBuildSettings.scenes = existing.ToArray();
            }

            Debug.Log("[BuildScript] ✅ WebGL settings configured.");
            AssetDatabase.SaveAssets();
        }
    }
}
