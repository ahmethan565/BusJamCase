using System.Linq;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Game.Editor.Tests
{
    // Every UI text uses the Zany font.
    public sealed class FontTests
    {
        private const string ScenePath = "Assets/_Game/Scenes/Main.unity";
        private const string FontAssetPath = "Assets/_Game/Content/Fonts/Zany_SDF.asset";

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
        public void EveryText_UsesZany()
        {
            var zany = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontAssetPath);
            Assert.IsNotNull(zany, FontAssetPath);

            var node = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Game/Content/Prefabs/UI/LevelNode.prefab");
            TMP_Text[] texts = scene.GetRootGameObjects().SelectMany(go => go.GetComponentsInChildren<TMP_Text>(true))
                                    .Concat(node.GetComponentsInChildren<TMP_Text>(true)).ToArray();
            Assert.IsNotEmpty(texts);
            foreach (TMP_Text text in texts)
            {
                Assert.AreSame(zany, text.font, text.name);
                Assert.AreSame(zany.material, text.fontSharedMaterial, $"{text.name}: the font's own material");
            }
        }

        [Test]
        public void NewTexts_DefaultToZany()
        {
            Assert.AreEqual(FontAssetPath, AssetDatabase.GetAssetPath(TMP_Settings.defaultFontAsset));
        }
    }
}
