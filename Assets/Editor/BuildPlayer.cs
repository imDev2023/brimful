// Build the desktop player from the scenes in the build list (StretchBuilder sets them):
//
//   unity command script eval 'return Brimful.EditorTools.BuildPlayer.Mac();'
//
// Writes Builds/Mac/Brimful.app (gitignored). Windows needs Windows Build Support (Mono)
// added in Unity Hub first (CLAUDE.md).

using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;

namespace Brimful.EditorTools
{
    public static class BuildPlayer
    {
        public static string Mac()
        {
            // Universal (Intel and Apple Silicon): Unity's default is Apple Silicon only, and
            // this Mac is Intel, where that build fails with "bad CPU type" (2026-11-11).
            UnityEditor.OSXStandalone.UserBuildSettings.architecture = UnityEditor.Build.OSArchitecture.x64ARM64;
            return Build(BuildTarget.StandaloneOSX, "Builds/Mac/Brimful.app");
        }

        public static string Windows() => Build(BuildTarget.StandaloneWindows64, "Builds/Windows/Brimful.exe");

        static string Build(BuildTarget target, string path)
        {
            var scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray();
            if (scenes.Length == 0)
                return "no scenes in the build list: run StretchBuilder.Build() first";
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = path,
                target = target,
                options = BuildOptions.None,
            });
            var s = report.summary;
            return $"{s.result}: {path}, {s.totalSize / 1048576f:0.0} MB, {s.totalTime.TotalSeconds:0} s, {s.totalErrors} errors, {s.totalWarnings} warnings";
        }
    }
}
