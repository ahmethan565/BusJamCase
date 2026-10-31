using System.Linq;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Game.Editor.Tests
{
    // UI texts are light so they read on the dark blue panels and the colored buttons.
    public sealed class TextColorTests
    {
        private const string ScenePath = "Assets/_Game/Scenes/Main.unity";

        private Scene scene;
        private bool openedByTest;

        [OneTimeSetUp]
        public void OpenScene()
        {
            scene = SceneManager.GetSceneByPath(ScenePath);
            if (scene.isLoaded) return;
            scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
            openedByTest = true;
        }

        [OneTimeTearDown]
        public void CloseScene()
        {
            if (openedByTest) EditorSceneManager.CloseScene(scene, true);
        }

        [Test]
        public void EveryText_IsWhite()
        {
            var node = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Game/Content/Prefabs/UI/LevelNode.prefab");
            TMP_Text[] texts = scene.GetRootGameObjects().SelectMany(go => go.GetComponentsInChildren<TMP_Text>(true))
                                    .Concat(node.GetComponentsInChildren<TMP_Text>(true)).ToArray();
            Assert.IsNotEmpty(texts);
            foreach (TMP_Text text in texts)
            {
                Color c = text.color;
                Assert.IsTrue(c.r > 0.99f && c.g > 0.99f && c.b > 0.99f, $"{text.name} is {c}");
            }
        }
    }
}
