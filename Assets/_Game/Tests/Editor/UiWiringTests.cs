using System.Collections.Generic;
using System.Linq;
using Game.Runtime.Input;
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
    // UI wiring in Main.unity (§12, §14.1). Read-only: nothing in the scene is changed.
    public sealed class UiWiringTests
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

        private T[] SceneComponents<T>() where T : Component =>
            scene.GetRootGameObjects().SelectMany(go => go.GetComponentsInChildren<T>(true)).ToArray();

        private T SceneComponent<T>() where T : Component
        {
            T[] found = SceneComponents<T>();
            Assert.AreEqual(1, found.Length, $"exactly one {typeof(T).Name} expected in Main.unity");
            return found[0];
        }

        private ScreenView[] Screens() => new ScreenView[]
        {
            SceneComponent<LevelMapScreen>(), SceneComponent<Hud>(), SceneComponent<WinScreen>(),
            SceneComponent<LoseScreen>(), SceneComponent<QuitConfirmScreen>(),
        };

        private static void AssertAllReferencesAssigned(Object target)
        {
            var missing = new List<string>();
            SerializedProperty property = new SerializedObject(target).GetIterator();
            while (property.NextVisible(true))
            {
                if (property.propertyType == SerializedPropertyType.ObjectReference
                    && property.name != "m_Script" && property.objectReferenceValue == null)
                    missing.Add(property.propertyPath);
            }
            Assert.IsEmpty(missing, $"{target.GetType().Name} on '{target.name}' has unassigned fields: {string.Join(", ", missing)}");
        }

        private static string AllText(Component root) =>
            string.Join("\n", root.GetComponentsInChildren<TMP_Text>(true).Select(t => t.text));

        [Test]
        public void Scene_ScreensAndBackButtonAreWired()
        {
            foreach (ScreenView screen in Screens()) AssertAllReferencesAssigned(screen);
            foreach (LevelNodeView node in SceneComponents<LevelNodeView>()) AssertAllReferencesAssigned(node);
            AssertAllReferencesAssigned(SceneComponent<BackButtonInput>());
        }

        [Test]
        public void Scene_EachScreenHasItsOwnOverlayCanvasWithScaler()
        {
            var canvases = new HashSet<Canvas>();
            foreach (ScreenView screen in Screens())
            {
                Canvas canvas = screen.GetComponentInParent<Canvas>(true);
                Assert.IsNotNull(canvas, screen.name);
                Assert.IsTrue(canvases.Add(canvas), $"{screen.name} shares a canvas with another screen");
                Assert.AreEqual(RenderMode.ScreenSpaceOverlay, canvas.renderMode, screen.name);

                var scaler = canvas.GetComponent<CanvasScaler>();
                Assert.IsNotNull(scaler, screen.name);
                Assert.AreEqual(CanvasScaler.ScaleMode.ScaleWithScreenSize, scaler.uiScaleMode, screen.name);
                Assert.AreEqual(new Vector2(1080f, 1920f), scaler.referenceResolution, screen.name);
                Assert.AreEqual(0.5f, scaler.matchWidthOrHeight, 1e-4f, screen.name);
                Assert.IsNotNull(canvas.GetComponent<GraphicRaycaster>(), screen.name);
            }
        }

        [Test]
        public void Scene_EachScreenHasASafeAreaFitter()
        {
            foreach (ScreenView screen in Screens())
                Assert.IsNotEmpty(screen.GetComponentsInChildren<SafeAreaFitter>(true), screen.name);
        }

        [Test]
        public void Scene_ButtonsHaveNoInspectorOnClick()
        {
            foreach (Button button in SceneComponents<Button>())
                Assert.AreEqual(0, button.onClick.GetPersistentEventCount(), $"{button.name}: buttons are bound in code (§12.1)");
        }

        [Test]
        public void Scene_AllTextsCanShowTurkishCharacters()
        {
            TMP_Text[] texts = SceneComponents<TMP_Text>();
            Assert.IsNotEmpty(texts);
            foreach (TMP_Text text in texts)
            {
                Assert.IsNotNull(text.font, text.name);
                bool all = text.font.HasCharacters("ğĞşŞıİöÖüÜçÇ", out uint[] missing, true, true);
                string missingText = missing == null ? "" : new string(missing.Select(c => (char)c).ToArray());
                Assert.IsTrue(all, $"{text.name} font {text.font.name} misses: {missingText} (§4.4)");
            }
        }

        [Test]
        public void Scene_PlayerTextsAreTurkish()
        {
            StringAssert.Contains("Tebrikler!", AllText(SceneComponent<WinScreen>()));
            StringAssert.Contains("Tüm bölümler tamamlandı!", AllText(SceneComponent<WinScreen>()));
            StringAssert.Contains("Yer kalmadı!", AllText(SceneComponent<LoseScreen>()));
            StringAssert.Contains("Çıkmak istiyor musun?", AllText(SceneComponent<QuitConfirmScreen>()));
        }
    }
}
