using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;
using System;
using System.IO;

namespace TexasHoldem.Editor
{
    public class BuildScript
    {
        private static readonly string[] Scenes = new string[]
        {
            "Assets/Scenes/LoadingScene.unity",
            "Assets/Scenes/MainScene.unity",
            "Assets/Scenes/GameScene.unity"
        };

        private static string GetBuildPath(string platform, string extension)
        {
            string buildDir = Path.Combine(Directory.GetParent(Application.dataPath).FullName, "Builds");
            if (!Directory.Exists(buildDir))
            {
                Directory.CreateDirectory(buildDir);
            }
            
            string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            return Path.Combine(buildDir, $"TexasHoldem_{platform}_{timestamp}{extension}");
        }

        [MenuItem("Texas Holdem/Build/Android APK")]
        public static void BuildAndroid()
        {
            BuildAndroidInternal(false);
        }

        [MenuItem("Texas Holdem/Build/Android APK (Development)")]
        public static void BuildAndroidDev()
        {
            BuildAndroidInternal(true);
        }

        private static void BuildAndroidInternal(bool isDevelopment)
        {
            string path = GetBuildPath("Android", ".apk");
            
            PlayerSettings.Android.bundleVersionCode++;
            PlayerSettings.SetScriptingBackend(BuildTargetGroup.Android, ScriptingImplementation.IL2CPP);
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64 | AndroidArchitecture.ARMv7;
            
            BuildPlayerOptions options = new BuildPlayerOptions
            {
                scenes = Scenes,
                locationPathName = path,
                target = BuildTarget.Android,
                options = isDevelopment 
                    ? BuildOptions.Development | BuildOptions.AllowDebugging 
                    : BuildOptions.None
            };

            Build(options, "Android");
        }

        [MenuItem("Texas Holdem/Build/iOS")]
        public static void BuildiOS()
        {
            BuildiOSInternal(false);
        }

        [MenuItem("Texas Holdem/Build/iOS (Development)")]
        public static void BuildiOSDev()
        {
            BuildiOSInternal(true);
        }

        private static void BuildiOSInternal(bool isDevelopment)
        {
            string path = GetBuildPath("iOS", "");
            
            PlayerSettings.SetScriptingBackend(BuildTargetGroup.iOS, ScriptingImplementation.IL2CPP);
            
            BuildPlayerOptions options = new BuildPlayerOptions
            {
                scenes = Scenes,
                locationPathName = path,
                target = BuildTarget.iOS,
                options = isDevelopment 
                    ? BuildOptions.Development | BuildOptions.AllowDebugging 
                    : BuildOptions.None
            };

            Build(options, "iOS");
        }

        [MenuItem("Texas Holdem/Build/Windows")]
        public static void BuildWindows()
        {
            string path = GetBuildPath("Windows", "/TexasHoldem.exe");
            
            BuildPlayerOptions options = new BuildPlayerOptions
            {
                scenes = Scenes,
                locationPathName = path,
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.None
            };

            Build(options, "Windows");
        }

        [MenuItem("Texas Holdem/Build/macOS")]
        public static void BuildMacOS()
        {
            string path = GetBuildPath("macOS", ".app");
            
            BuildPlayerOptions options = new BuildPlayerOptions
            {
                scenes = Scenes,
                locationPathName = path,
                target = BuildTarget.StandaloneOSX,
                options = BuildOptions.None
            };

            Build(options, "macOS");
        }

        private static void Build(BuildPlayerOptions options, string platformName)
        {
            Debug.Log($"Starting {platformName} build...");
            Debug.Log($"Output path: {options.locationPathName}");

            BuildReport report = BuildPipeline.BuildPlayer(options);
            BuildSummary summary = report.summary;

            if (summary.result == BuildResult.Succeeded)
            {
                Debug.Log($"{platformName} build succeeded!");
                Debug.Log($"Total size: {summary.totalSize / (1024 * 1024):F2} MB");
                Debug.Log($"Total time: {summary.totalTime.TotalSeconds:F1} seconds");
                Debug.Log($"Output: {options.locationPathName}");
                
                EditorUtility.RevealInFinder(options.locationPathName);
            }
            else
            {
                Debug.LogError($"{platformName} build failed!");
                Debug.LogError($"Errors: {summary.totalErrors}");
                Debug.LogError($"Warnings: {summary.totalWarnings}");
            }
        }

        // Command line build methods
        public static void BuildAndroidCLI()
        {
            string path = GetCommandLineArg("-buildPath") ?? GetBuildPath("Android", ".apk");
            bool isDev = HasCommandLineArg("-development");
            
            PlayerSettings.Android.bundleVersionCode++;
            PlayerSettings.SetScriptingBackend(BuildTargetGroup.Android, ScriptingImplementation.IL2CPP);
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64 | AndroidArchitecture.ARMv7;
            
            BuildPlayerOptions options = new BuildPlayerOptions
            {
                scenes = Scenes,
                locationPathName = path,
                target = BuildTarget.Android,
                options = isDev 
                    ? BuildOptions.Development | BuildOptions.AllowDebugging 
                    : BuildOptions.None
            };

            BuildReport report = BuildPipeline.BuildPlayer(options);
            
            if (report.summary.result != BuildResult.Succeeded)
            {
                EditorApplication.Exit(1);
            }
        }

        public static void BuildiOSCLI()
        {
            string path = GetCommandLineArg("-buildPath") ?? GetBuildPath("iOS", "");
            bool isDev = HasCommandLineArg("-development");
            
            PlayerSettings.SetScriptingBackend(BuildTargetGroup.iOS, ScriptingImplementation.IL2CPP);
            
            BuildPlayerOptions options = new BuildPlayerOptions
            {
                scenes = Scenes,
                locationPathName = path,
                target = BuildTarget.iOS,
                options = isDev 
                    ? BuildOptions.Development | BuildOptions.AllowDebugging 
                    : BuildOptions.None
            };

            BuildReport report = BuildPipeline.BuildPlayer(options);
            
            if (report.summary.result != BuildResult.Succeeded)
            {
                EditorApplication.Exit(1);
            }
        }

        private static string GetCommandLineArg(string name)
        {
            string[] args = Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length - 1; i++)
            {
                if (args[i] == name)
                {
                    return args[i + 1];
                }
            }
            return null;
        }

        private static bool HasCommandLineArg(string name)
        {
            string[] args = Environment.GetCommandLineArgs();
            foreach (string arg in args)
            {
                if (arg == name) return true;
            }
            return false;
        }
    }
}
