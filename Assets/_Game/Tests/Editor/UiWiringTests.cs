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

        // Every text in the scene and in the level node prefab.
        private TMP_Text[] AllTexts()
        {
            var node = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Game/Content/Prefabs/UI/LevelNode.prefab");
            Assert.IsNotNull(node, "LevelNode prefab");
            return SceneComponents<TMP_Text>().Concat(node.GetComponentsInChildren<TMP_Text>(true)).ToArray();
        }

        [Test]
        public void Scene_EveryTextHasItsGlyphsInTheFont()
        {
            TMP_Text[] texts = AllTexts();
            Assert.IsNotEmpty(texts);
            foreach (TMP_Text text in texts)
            {
                Assert.IsNotNull(text.font, text.name);
                bool all = text.font.HasCharacters(text.text, out uint[] missing, true, true);
                string missingText = missing == null ? "" : new string(missing.Select(c => (char)c).ToArray());
                Assert.IsTrue(all, $"{text.name} font {text.font.name} misses: {missingText} (§4.4)");
            }
        }

        [Test]
        public void Scene_PlayerTextsAreEnglish()
        {
            StringAssert.Contains("Well done!", AllText(SceneComponent<WinScreen>()));
            StringAssert.Contains("All levels completed!", AllText(SceneComponent<WinScreen>()));
            StringAssert.Contains("Next", AllText(SceneComponent<WinScreen>()));
            StringAssert.Contains("Main Menu", AllText(SceneComponent<WinScreen>()));
            StringAssert.Contains("Out of space!", AllText(SceneComponent<LoseScreen>()));
            StringAssert.Contains("Try Again", AllText(SceneComponent<LoseScreen>()));
            StringAssert.Contains("Quit the game?", AllText(SceneComponent<QuitConfirmScreen>()));
            StringAssert.Contains("Yes", AllText(SceneComponent<QuitConfirmScreen>()));
            StringAssert.Contains("No", AllText(SceneComponent<QuitConfirmScreen>()));
            StringAssert.Contains("Restart", AllText(SceneComponent<Hud>()));
            StringAssert.Contains("Levels", AllText(SceneComponent<LevelMapScreen>()));
        }

        [Test]
        public void Scene_NoTurkishCharactersInAnyText()
        {
            const string turkish = "çÇğĞıİöÖşŞüÜ";
            foreach (TMP_Text text in AllTexts())
                Assert.AreEqual(-1, text.text.IndexOfAny(turkish.ToCharArray()), $"{text.name}: '{text.text}' (§1)");
        }
    }
}
