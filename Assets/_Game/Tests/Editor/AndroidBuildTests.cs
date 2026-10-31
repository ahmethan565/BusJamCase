using Game.Editor;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.Build;

namespace Game.Editor.Tests
{
    // The Android build setup (§4.2, §16.5): the project settings the APK is built with.
    public sealed class AndroidBuildTests
    {
        [Test]
        public void PlayerSettings_AreTheAndroidSetup()
        {
            Assert.AreEqual(AndroidBuild.CompanyName, PlayerSettings.companyName);
            Assert.AreEqual(AndroidBuild.PackageName, PlayerSettings.GetApplicationIdentifier(NamedBuildTarget.Android));
            Assert.AreEqual(ScriptingImplementation.IL2CPP, PlayerSettings.GetScriptingBackend(NamedBuildTarget.Android));
            Assert.AreEqual(AndroidArchitecture.ARM64, PlayerSettings.Android.targetArchitectures);
            Assert.AreEqual(UIOrientation.Portrait, PlayerSettings.defaultInterfaceOrientation);
            Assert.GreaterOrEqual((int)PlayerSettings.Android.minSdkVersion, 22);
        }

        [Test]
        public void Package_IsAValidAndroidId()
        {
            StringAssert.IsMatch(@"^[a-z][a-z0-9_]*(\.[a-z][a-z0-9_]*)+$", AndroidBuild.PackageName);
        }

        [Test]
        public void Apk_GoesToTheIgnoredBuildsFolder()
        {
            StringAssert.StartsWith("Builds/Android/", AndroidBuild.ApkPath);
            StringAssert.EndsWith(".apk", AndroidBuild.ApkPath);
            StringAssert.Contains("[Bb]uilds/", System.IO.File.ReadAllText(".gitignore"));
        }

        [Test]
        public void Scenes_AreTheEnabledBuildScenes()
        {
            CollectionAssert.AreEqual(new[] { MainSceneOnStartup.MainScenePath }, AndroidBuild.Scenes());
        }
    }
}
