using System.Collections.Generic;
using System.Linq;
using Game.Core;
using Game.Runtime.Bootstrap;
using Game.Runtime.Data;
using Game.Runtime.Input;
using Game.Runtime.View;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

namespace Game.Editor.Tests
{
    // Catches broken prefab / settings / scene wiring that would otherwise only show up in Play mode.
    public sealed class ContentWiringTests
    {
        private const string ScenePath = "Assets/_Game/Scenes/Main.unity";
        private const string PrefabFolder = "Assets/_Game/Content/Prefabs/";

        private Scene scene;
        private bool openedByTest;

        private static T LoadSingle<T>() where T : Object
        {
            string[] guids = AssetDatabase.FindAssets("t:" + typeof(T).Name);
            Assert.AreEqual(1, guids.Length, $"exactly one {typeof(T).Name} asset expected");
            return AssetDatabase.LoadAssetAtPath<T>(AssetDatabase.GUIDToAssetPath(guids[0]));
        }

        private static GameObject LoadPrefab(string name)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabFolder + name + ".prefab");
            Assert.IsNotNull(prefab, $"missing prefab {name}");
            return prefab;
        }

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

        private T SceneComponent<T>() where T : Component
        {
            T[] found = scene.GetRootGameObjects().SelectMany(go => go.GetComponentsInChildren<T>(true)).ToArray();
            Assert.AreEqual(1, found.Length, $"exactly one {typeof(T).Name} expected in Main.unity");
            return found[0];
        }

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
        public void Settings_EachAssetExistsOnce()
        {
            Assert.IsNotNull(LoadSingle<LayoutSettings>());
            Assert.IsNotNull(LoadSingle<AnimationSettings>());
            Assert.IsNotNull(LoadSingle<ColorPalette>());
        }

        [Test]
        public void ColorPalette_HasOneSimpleLitMaterialPerColor()
        {
            var palette = LoadSingle<ColorPalette>();
            var seen = new HashSet<Material>();

            foreach (PassengerColor color in (PassengerColor[])System.Enum.GetValues(typeof(PassengerColor)))
            {
                Material material = palette.GetMaterial(color);
                Assert.IsNotNull(material, color.ToString());
                Assert.AreEqual("Universal Render Pipeline/Simple Lit", material.shader.name, color.ToString());
                Assert.IsTrue(seen.Add(material), $"{color} shares a material with another color");
                Assert.AreEqual(1f, palette.GetColor(color).a, 1e-3f, $"{color} UI color must be opaque");
            }
        }

        [Test]
        public void PassengerPrefab_HasLayeredHierarchy()
        {
            GameObject prefab = LoadPrefab("Passenger");

            Assert.IsNotNull(prefab.GetComponent<PassengerView>());
            Transform mesh = prefab.transform.Find("Bounce/Body/Mesh");
            Assert.IsNotNull(mesh, "Passenger -> Bounce -> Body -> Mesh");
            Assert.IsNotNull(mesh.GetComponentInChildren<MeshRenderer>(), "Mesh layer needs a renderer");
            AssertAllReferencesAssigned(prefab.GetComponent<PassengerView>());
        }

        [Test]
        public void BusPrefab_HasSeatRoot()
        {
            GameObject prefab = LoadPrefab("Bus");

            Assert.IsNotNull(prefab.GetComponent<BusView>());
            Assert.IsNotNull(prefab.transform.Find("SeatRoot"));
            AssertAllReferencesAssigned(prefab.GetComponent<BusView>());
        }

        [Test]
        public void AllPrefabs_ExistWithoutColliders()
        {
            foreach (string name in new[] { "Passenger", "Bus", "SeatedPassenger", "FloorTile", "WallBlock", "EdgePiece", "SlotMarker" })
            {
                GameObject prefab = LoadPrefab(name);
                Assert.IsEmpty(prefab.GetComponentsInChildren<Collider>(true), $"{name}: no physics is used (§19 #23)");
            }
        }

        [Test]
        public void Scene_WorldComponentsAreWired()
        {
            AssertAllReferencesAssigned(SceneComponent<GameBootstrap>());
            AssertAllReferencesAssigned(SceneComponent<BoardView>());
            AssertAllReferencesAssigned(SceneComponent<WaitingAreaView>());
            AssertAllReferencesAssigned(SceneComponent<GameplayPresenter>());
            AssertAllReferencesAssigned(SceneComponent<CameraFitter>());
            AssertAllReferencesAssigned(SceneComponent<InputHandler>());
        }

        [Test]
        public void Scene_InputHandlerStartsDisabled()
        {
            Assert.IsFalse(SceneComponent<InputHandler>().enabled, "only gameplay enables input (§9.5)");
        }

        [Test]
        public void Scene_CameraFitterSitsOnTheCamera()
        {
            CameraFitter fitter = SceneComponent<CameraFitter>();
            Assert.IsNotNull(fitter.GetComponent<Camera>());
        }

        [Test]
        public void Scene_EventSystemUsesInputSystemModule()
        {
            EventSystem eventSystem = SceneComponent<EventSystem>();
            Assert.IsNotNull(eventSystem.GetComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>());
            Assert.IsNull(eventSystem.GetComponent<StandaloneInputModule>());
        }

        [Test]
        public void Scene_HasNoColliders()
        {
            Collider[] colliders = scene.GetRootGameObjects().SelectMany(go => go.GetComponentsInChildren<Collider>(true)).ToArray();
            Assert.IsEmpty(colliders);
        }
    }
}
