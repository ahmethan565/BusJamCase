using System.Globalization;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Game.Editor
{
    // One-click development APK for testing on a phone (§4.2, §16.5). The Android settings are applied in code,
    // so every build uses the same ones. Signed with the debug key; release signing comes with publishing.
    public static class AndroidBuild
    {
        public const string CompanyName = "Ahmethan";
        public const string PackageName = "com.ahmethan.busjam";
        public const string ApkPath = "Builds/Android/BusJamCase-dev.apk";   // Builds/ is in .gitignore

        [MenuItem("Tools/Game/Build Android APK (Development)")]
        public static void BuildDevelopmentApk()
        {
            ApplySettings();
            Directory.CreateDirectory(Path.GetDirectoryName(ApkPath));

            BuildReport report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = Scenes(),
                locationPathName = ApkPath,
                target = BuildTarget.Android,
                targetGroup = BuildTargetGroup.Android,
                options = BuildOptions.Development | BuildOptions.ConnectWithProfiler,
            });

            BuildSummary summary = report.summary;
            if (summary.result != BuildResult.Succeeded)
                throw new BuildFailedException($"Android build {summary.result} with {summary.totalErrors} error(s).");
            float megabytes = new FileInfo(ApkPath).Length / (1024f * 1024f);   // the file, not summary.totalSize (uncompressed)
            Debug.Log(string.Format(CultureInfo.InvariantCulture, "Built {0} ({1:0.0} MB). Install with: adb install -r {0}", ApkPath, megabytes));
        }

        // §4.2. Also run once by hand so the values are saved in ProjectSettings.
        public static void ApplySettings()
        {
            PlayerSettings.companyName = CompanyName;
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, PackageName);
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
            EditorUserBuildSettings.buildAppBundle = false;                     // an .apk to install, not a Play bundle
            EditorUserBuildSettings.development = true;
            EditorUserBuildSettings.connectProfiler = true;
            EditorUserBuildSettings.buildWithDeepProfilingSupport = false;     // deep profiling distorts the timings
        }

        public static string[] Scenes() =>
            EditorBuildSettings.scenes.Where(scene => scene.enabled).Select(scene => scene.path).ToArray();
    }
}
