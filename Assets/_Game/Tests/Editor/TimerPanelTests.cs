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
    // The time sits in the dark slot of the timer art.
    public sealed class TimerPanelTests
    {
        private const string ScenePath = "Assets/_Game/Scenes/Main.unity";
        private const string SpritePath = "Assets/_Game/Content/Images/UI/Timer.png";

        // The dark slot inside Timer.png (202x90 px): x 79..180, y 26..68 from the top; normalized, y up.
        private static readonly Rect Slot = Rect.MinMaxRect(79f / 202f, 1f - 69f / 90f, 181f / 202f, 1f - 26f / 90f);

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

        private Hud SceneHud()
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                var hud = root.GetComponentInChildren<Hud>(true);
                if (hud != null) return hud;
            }
            Assert.Fail("no Hud in the scene");
            return null;
        }

        private static TMP_Text TimeLabel(Hud hud) =>
            (TMP_Text)new SerializedObject(hud).FindProperty("timeLabel").objectReferenceValue;

        [Test]
        public void TimerArt_IsASprite()
        {
            Assert.IsNotNull(AssetDatabase.LoadAssetAtPath<Sprite>(SpritePath));
        }

        [Test]
        public void TimeLabel_SitsInTheDarkSlotOfTheTimerArt()
        {
            TMP_Text label = TimeLabel(SceneHud());
            var panel = (RectTransform)label.transform.parent;
            var image = panel.GetComponent<Image>();
            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(SpritePath);

            Assert.AreSame(sprite, image.sprite, "the timer art is right behind the time");
            Assert.IsFalse(image.raycastTarget, "the art does not catch taps");
            Assert.AreEqual(sprite.rect.width / sprite.rect.height, panel.sizeDelta.x / panel.sizeDelta.y, 0.02f, "not stretched");

            var rect = (RectTransform)label.transform;
            Assert.AreEqual(Vector2.zero, rect.offsetMin, "the label is exactly the slot");
            Assert.AreEqual(Vector2.zero, rect.offsetMax, "the label is exactly the slot");
            Assert.AreEqual(Slot.xMin, rect.anchorMin.x, 0.01f);
            Assert.AreEqual(Slot.yMin, rect.anchorMin.y, 0.01f);
            Assert.AreEqual(Slot.xMax, rect.anchorMax.x, 0.01f);
            Assert.AreEqual(Slot.yMax, rect.anchorMax.y, 0.01f);
        }

        [Test]
        public void TimerArt_IsHidden_WhenTheLevelHasNoLimit()
        {
            GameObject copy = Object.Instantiate(SceneHud().gameObject);
            SceneManager.MoveGameObjectToScene(copy, preview);
            var hud = copy.GetComponent<Hud>();
            GameObject panel = TimeLabel(hud).transform.parent.gameObject;

            hud.SetTimeLimit(0);
            Assert.IsFalse(panel.activeSelf, "no limit: no timer at all");

            hud.SetTimeLimit(60);
            Assert.IsTrue(panel.activeSelf);
            Assert.AreEqual("1:00", TimeLabel(hud).text);
        }
    }
}
