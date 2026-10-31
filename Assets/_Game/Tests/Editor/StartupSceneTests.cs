using Game.Editor;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;

namespace Game.Editor.Tests
{
    // The editor opens and plays the game scene.
    public sealed class StartupSceneTests
    {
        [Test]
        public void MainScene_IsTheFirstBuildScene()
        {
            Assert.IsNotEmpty(EditorBuildSettings.scenes);
            Assert.AreEqual(MainSceneOnStartup.MainScenePath, EditorBuildSettings.scenes[0].path);
            Assert.IsTrue(EditorBuildSettings.scenes[0].enabled);
        }

        [Test]
        public void PlayMode_AlwaysStartsFromTheMainScene()
        {
            SceneAsset start = EditorSceneManager.playModeStartScene;

            Assert.IsNotNull(start, "Play from any open scene starts the game scene");
            Assert.AreEqual(MainSceneOnStartup.MainScenePath, AssetDatabase.GetAssetPath(start));
        }
    }
}
