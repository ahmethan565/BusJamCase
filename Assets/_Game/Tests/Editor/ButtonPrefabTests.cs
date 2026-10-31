using System.Linq;
using DG.Tweening;
using Game.Runtime.UI;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Game.Editor.Tests
{
    // Button prefabs: the button art and its label; pressing shrinks the button and tints it.
    public sealed class ButtonPrefabTests
    {
        private const string ScenePath = "Assets/_Game/Scenes/Main.unity";
        private const string NarrowPrefab = "Assets/_Game/Content/Prefabs/UI/Button.prefab";
        private const string WidePrefab = "Assets/_Game/Content/Prefabs/UI/WideButton.prefab";
        private const string NarrowSprite = "Assets/_Game/Content/Images/UI/Button.png";
        private const string WideSprite = "Assets/_Game/Content/Images/UI/WideButton.png";

        private Scene scene;
        private bool openedByTest;
        private Scene preview;

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

        [SetUp]
        public void SetUp() => preview = EditorSceneManager.NewPreviewScene();

        [TearDown]
        public void TearDown() => EditorSceneManager.ClosePreviewScene(preview);

        private static GameObject Load(string path)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            Assert.IsNotNull(prefab, path);
            return prefab;
        }

        [TestCase(NarrowPrefab, NarrowSprite)]
        [TestCase(WidePrefab, WideSprite)]
        public void Prefab_IsTheButtonArtWithALabel(string prefabPath, string spritePath)
        {
            GameObject root = Load(prefabPath);
            var image = root.GetComponent<Image>();
            var button = root.GetComponent<Button>();

            Assert.AreSame(AssetDatabase.LoadAssetAtPath<Sprite>(spritePath), image.sprite);
            Assert.AreSame(image, button.targetGraphic, "the press colour goes on the button art");
            Assert.AreEqual(Selectable.Transition.ColorTint, button.transition);
            Assert.IsNotNull(root.GetComponent<ButtonPress>());
            Assert.IsNotNull(root.transform.Find("Label").GetComponent<TMP_Text>());
            Assert.AreEqual(1, root.transform.childCount, "no frame: only the label inside");
        }

        [Test]
        public void Press_ShrinksTheButton_ReleaseRestoresIt()
        {
            GameObject button = (GameObject)PrefabUtility.InstantiatePrefab(Load(WidePrefab), preview);
            var press = button.GetComponent<ButtonPress>();

            press.Press();
            DOTween.Complete(button.transform);
            Assert.Less(button.transform.localScale.x, 1f, "pressed: smaller");

            press.Release();
            DOTween.Complete(button.transform);
            Assert.AreEqual(Vector3.one, button.transform.localScale, "released: full size");
        }

        [Test]
        public void SceneButtons_AreThePrefabs()
        {
            GameObject narrow = Load(NarrowPrefab), wide = Load(WidePrefab);
            Button[] buttons = scene.GetRootGameObjects().SelectMany(go => go.GetComponentsInChildren<Button>(true)).ToArray();
            Assert.AreEqual(7, buttons.Length);
            foreach (Button button in buttons)
            {
                GameObject source = PrefabUtility.GetCorrespondingObjectFromSource(button.gameObject);
                bool isWide = ((Image)button.targetGraphic).sprite.name == "WideButton";
                Assert.AreSame(isWide ? wide : narrow, source, button.name);
            }
        }
    }
}
