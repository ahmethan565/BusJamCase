using System.Linq;
using Game.Core.Levels;
using Game.Runtime.Data;
using Game.Runtime.View;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace Game.Editor.Tests
{
    // The road under the bus lane and the bus stop sign at the park point (§9.3, §10.4).
    public sealed class BusRoadTests
    {
        private const string ScenePath = "Assets/_Game/Scenes/Main.unity";
        private const float BusHalfLength = 1.2f;   // bus body is 2.4 long and 1 deep
        private const float BusHalfDepth = 0.5f;
        private static readonly float[] PortraitAspects = { 9f / 16f, 9f / 19.5f, 3f / 4f };

        private LayoutSettings settings;
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
        public void SetUp()
        {
            settings = ScriptableObject.CreateInstance<LayoutSettings>();
            preview = EditorSceneManager.NewPreviewScene();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(settings);
            EditorSceneManager.ClosePreviewScene(preview);
        }

        private static LevelCatalog Catalog()
        {
            string[] guids = AssetDatabase.FindAssets("t:" + nameof(LevelCatalog));
            return AssetDatabase.LoadAssetAtPath<LevelCatalog>(AssetDatabase.GUIDToAssetPath(guids.Single()));
        }

        private static GameObject Prefab(string name)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>($"Assets/_Game/Content/Prefabs/{name}.prefab");
            Assert.IsNotNull(prefab, name);
            return prefab;
        }

        [Test]
        public void EveryCatalogLevel_RoadCarriesTheBusesAndTouchesTheStop()
        {
            LevelCatalog catalog = Catalog();
            for (int i = 0; i < catalog.Count; i++)
            {
                var layout = new BoardLayout(catalog.Get(i), settings);
                Bounds road = layout.BusRoad;
                Vector3 park = layout.BusParkPosition;
                string at = $"level {i}";

                Assert.AreEqual(layout.StopBackdrop.max.z, road.min.z, 1e-4f, $"{at}: road and stop touch, no gap");
                Assert.AreEqual(park.z, road.center.z, 1e-4f, $"{at}: the bus drives in the middle of the road");
                Assert.LessOrEqual(road.min.z, park.z - BusHalfDepth, at);
                Assert.GreaterOrEqual(road.max.z, park.z + BusHalfDepth, at);

                Assert.LessOrEqual(road.min.x, layout.BusExitPosition.x - BusHalfLength, $"{at}: a leaving bus stays on the road");
                Assert.GreaterOrEqual(road.max.x, layout.BusQueueEnterPosition.x + BusHalfLength, $"{at}: an arriving follower is on the road");
            }
        }

        [Test]
        public void EveryCatalogLevel_RoadSpansTheWholeScreenWidth()
        {
            var camGo = new GameObject("TestCamera", typeof(Camera), typeof(CameraFitter));
            SceneManager.MoveGameObjectToScene(camGo, preview);
            var cam = camGo.GetComponent<Camera>();
            var fitter = camGo.GetComponent<CameraFitter>();
            var so = new SerializedObject(fitter);
            so.FindProperty("cam").objectReferenceValue = cam;
            so.ApplyModifiedPropertiesWithoutUndo();
            var ground = new Plane(Vector3.up, Vector3.zero);

            LevelCatalog catalog = Catalog();
            for (int i = 0; i < catalog.Count; i++)
            {
                var layout = new BoardLayout(catalog.Get(i), settings);
                Bounds road = layout.BusRoad;
                foreach (float aspect in PortraitAspects)
                {
                    cam.aspect = aspect;
                    fitter.Fit(layout.ContentBounds);
                    int rows = 0;
                    for (float v = 0f; v <= 1f; v += 0.01f)
                    {
                        foreach (float u in new[] { 0f, 1f })
                        {
                            Ray ray = cam.ViewportPointToRay(new Vector3(u, v, 0f));
                            if (!ground.Raycast(ray, out float d)) continue;
                            Vector3 p = ray.GetPoint(d);
                            if (p.z < road.min.z || p.z > road.max.z) continue;
                            rows++;
                            Assert.That(p.x, Is.InRange(road.min.x, road.max.x), $"level {i}, aspect {aspect}: screen edge at z {p.z}");
                        }
                    }
                    Assert.Greater(rows, 0, $"level {i}, aspect {aspect}: the road is on screen");
                }
            }
        }

        [Test]
        public void EveryCatalogLevel_SignStandsOnTheCurbInFrontOfTheParkedBus()
        {
            LevelCatalog catalog = Catalog();
            for (int i = 0; i < catalog.Count; i++)
            {
                LevelData level = catalog.Get(i);
                var layout = new BoardLayout(level, settings);
                Vector3 sign = layout.BusStopSignPosition;
                Vector3 park = layout.BusParkPosition;
                string at = $"level {i}";

                Assert.AreEqual(layout.BusRoad.min.z, sign.z, 1e-4f, $"{at}: on the seat side edge of the road");
                Assert.AreEqual(0f, sign.y, 1e-4f, at);
                Assert.Less(sign.x, park.x - BusHalfLength, $"{at}: ahead of the parked bus's nose");
                Assert.Greater(sign.x, park.x - BusHalfLength - 0.5f, $"{at}: right at the bus");

                // Not on any seat -> door walk
                Vector3 door = layout.DoorPoint;
                for (int s = 0; s < level.WaitingSlots; s++)
                {
                    Vector3 seat = layout.SlotPosition(s);
                    Vector3 closest = seat + Vector3.Project(sign - seat, door - seat);
                    float t = Vector3.Dot(sign - seat, door - seat) / (door - seat).sqrMagnitude;
                    if (t < 0f || t > 1f) continue;
                    Assert.Greater(Vector3.Distance(sign, closest), 0.3f, $"{at}: seat {s} walks past the sign");
                }
            }
        }

        [TestCase("Road")]
        [TestCase("BusStopSign")]
        public void Prefab_IsSimpleLitWithoutShadowsOrColliders(string name)
        {
            GameObject prefab = Prefab(name);
            Assert.IsEmpty(prefab.GetComponentsInChildren<Collider>(true));
            MeshRenderer[] renderers = prefab.GetComponentsInChildren<MeshRenderer>(true)
                .Where(r => r.GetComponent<TMPro.TMP_Text>() == null).ToArray();
            Assert.IsNotEmpty(renderers);
            foreach (MeshRenderer renderer in renderers)
            {
                Assert.AreEqual("Universal Render Pipeline/Simple Lit", renderer.sharedMaterial.shader.name, renderer.name);
                Assert.AreEqual(ShadowCastingMode.Off, renderer.shadowCastingMode, renderer.name);
            }
        }

        [Test]
        public void SignModel_StandsOnTheGroundAndIsAboutAPassengerAndAHalfTall()
        {
            GameObject sign = Object.Instantiate(Prefab("BusStopSign"));
            SceneManager.MoveGameObjectToScene(sign, preview);
            Renderer[] renderers = sign.GetComponentsInChildren<MeshRenderer>(true)
                .Where(r => r.GetComponent<TMPro.TMP_Text>() == null).ToArray();
            Bounds b = renderers[0].bounds;
            foreach (Renderer r in renderers) b.Encapsulate(r.bounds);

            Assert.AreEqual(0f, b.min.y, 0.02f, "stands on the ground");
            Assert.That(b.size.y, Is.InRange(1.1f, 1.6f));
            Assert.Less(b.size.z, 0.3f, "a thin sign");
        }

        [Test]
        public void Build_PlacesRoadAndSign_ClearHidesThem()
        {
            WaitingAreaView source = scene.GetRootGameObjects()
                .Select(go => go.GetComponentInChildren<WaitingAreaView>(true)).First(v => v != null);
            GameObject copy = Object.Instantiate(source.gameObject);
            SceneManager.MoveGameObjectToScene(copy, preview);
            var view = copy.GetComponent<WaitingAreaView>();
            var so = new SerializedObject(view);
            so.FindProperty("poolRoot").objectReferenceValue = copy.transform;
            so.ApplyModifiedPropertiesWithoutUndo();

            LevelData level = Catalog().Get(0);
            var layout = new BoardLayout(level, settings);
            view.Init(settings);
            view.Build(level.WaitingSlots, layout);

            Transform road = copy.transform.Cast<Transform>().Single(t => t.name.StartsWith("Road"));
            Transform sign = copy.transform.Cast<Transform>().Single(t => t.name.StartsWith("BusStopSign"));
            Assert.IsTrue(road.gameObject.activeSelf && sign.gameObject.activeSelf);
            Assert.AreEqual(layout.BusRoad.center.x, road.position.x, 1e-4f);
            Assert.AreEqual(layout.BusRoad.center.z, road.position.z, 1e-4f);
            Assert.AreEqual(layout.BusRoad.size.x, road.localScale.x, 1e-4f);
            Assert.AreEqual(layout.BusRoad.size.z, road.localScale.z, 1e-4f);
            Assert.AreEqual(layout.BusStopSignPosition, sign.position);

            view.Clear();
            Assert.IsFalse(road.gameObject.activeSelf || sign.gameObject.activeSelf);
        }
    }
}
