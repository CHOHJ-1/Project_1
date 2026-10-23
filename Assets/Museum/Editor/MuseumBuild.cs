#if UNITY_EDITOR
using System;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.Build;
using UnityEngine;

namespace Museum.Editor
{
    public static class MuseumBuild
    {
        [MenuItem("Museum/Build Windows x64")]
        public static void Windows()
        {
            string output = "Builds/Windows/MuseumPlatform2.exe";
            string[] args = Environment.GetCommandLineArgs();
            int index = Array.IndexOf(args, "-museumOutput");
            if (index >= 0 && index + 1 < args.Length) output = args[index + 1];
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Standalone, ScriptingImplementation.Mono2x);
            PlayerSettings.defaultScreenWidth = 1920;
            PlayerSettings.defaultScreenHeight = 1080;
            PlayerSettings.fullScreenMode = FullScreenMode.FullScreenWindow;
            PlayerSettings.resizableWindow = false;
            PlayerSettings.runInBackground = true;
            PlayerSettings.companyName = "Museum";
            PlayerSettings.productName = "Museum Platform 2";
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions {
                scenes = new[] { "Assets/Museum/Scenes/MainScene.unity" },
                locationPathName = output,
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.None
            });
            if (report.summary.result != BuildResult.Succeeded)
                throw new Exception("Windows build failed: " + report.summary.result);
        }
    }
}
#endif
