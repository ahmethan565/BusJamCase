using System.Linq;
using Game.Runtime.UI;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Game.Editor.Tests
{
    // Popup look: content centered with fixed spacing, panel 1 background, wide buttons in the sprite's ratio.
    public sealed class PopupLayoutTests
    {
        private const string ScenePath = "Assets/_Game/Scenes/Main.unity";
        private const string PanelSprite = "Assets/_Game/Content/Images/UI/panel 1.png";
        private const string WideSprite = "Assets/_Game/Content/Images/UI/wideButton.png";

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

        private T SceneComponent<T>() where T : Component =>
            scene.GetRootGameObjects().SelectMany(go => go.GetComponentsInChildren<T>(true)).Single();

        private RectTransform[] Panels() => new[]
        {
            (RectTransform)SceneComponent<WinScreen>().transform.Find("SafeArea/Panel"),
            (RectTransform)SceneComponent<LoseScreen>().transform.Find("SafeArea/Panel"),
            (RectTransform)SceneComponent<QuitConfirmScreen>().transform.Find("SafeArea/Panel"),
        };

        [Test]
        public void Popups_UsePanel1Background()
        {
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(PanelSprite);
            Assert.IsNotNull(sprite);
            foreach (RectTransform panel in Panels())
                Assert.AreSame(sprite, panel.GetComponent<Image>().sprite, panel.parent.parent.name);
        }

        [Test]
        public void Popups_StackContentInTheCenter_WithOneSpacing()
        {
            float spacing = -1f;
            foreach (RectTransform panel in Panels())
            {
                var layout = panel.GetComponent<VerticalLayoutGroup>();
                Assert.IsNotNull(layout, panel.parent.parent.name);
                Assert.AreEqual(TextAnchor.MiddleCenter, layout.childAlignment, panel.parent.parent.name);
                Assert.IsFalse(layout.childControlWidth || layout.childControlHeight, "children keep their own sizes");
                Assert.IsFalse(layout.childForceExpandWidth || layout.childForceExpandHeight);
                if (spacing < 0f) spacing = layout.spacing;
                Assert.AreEqual(spacing, layout.spacing, 1e-3f, "the same spacing in every popup");
            }
            Assert.Greater(spacing, 0f);
        }

        [Test]
        public void WideButtons_FollowTheSpriteRatio()
        {
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(WideSprite);
            float ratio = sprite.rect.width / sprite.rect.height;
            int found = 0;
            foreach (Button button in scene.GetRootGameObjects().SelectMany(go => go.GetComponentsInChildren<Button>(true)))
            {
                var image = (Image)button.targetGraphic;
                if (image.sprite != sprite) continue;
                found++;
                Vector2 size = image.rectTransform.sizeDelta;
                Assert.AreEqual(ratio, size.x / size.y, 0.02f, button.name);
            }
            Assert.AreEqual(6, found, "Next, two Main Menu, Try Again, Yes, No");
        }
    }
}
