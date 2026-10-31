using System.Linq;
using Game.Core;
using Game.Core.Levels;
using Game.Runtime.Animation;
using Game.Runtime.Data;
using Game.Runtime.View;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools.Constraints;
using Is = NUnit.Framework.Is;
using NoAlloc = UnityEngine.TestTools.Constraints.Is;

namespace Game.Editor.Tests
{
    // Phase 8: render settings (§16.2), model budgets (§16.2, §16.4), SRP batching, pool prewarm, zero per-frame GC.
    public sealed class PolishTests
    {
        private const string ScenePath = "Assets/_Game/Scenes/Main.unity";
        private const string PrefabFolder = "Assets/_Game/Content/Prefabs/";
        private const string SimpleLit = "Universal Render Pipeline/Simple Lit";
        private const int ModelTriangleLimit = 1500;
        private const int SceneTriangleLimit = 100_000;

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

        private T SceneComponent<T>() where T : Component
        {
            T[] found = scene.GetRootGameObjects().SelectMany(go => go.GetComponentsInChildren<T>(true)).ToArray();
            Assert.AreEqual(1, found.Length, $"exactly one {typeof(T).Name} expected in Main.unity");
            return found[0];
        }

        private static GameObject Prefab(string name)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabFolder + name + ".prefab");
            Assert.IsNotNull(prefab, name);
            return prefab;
        }

        private static int Triangles(GameObject root, params string[] skipNames) =>
            root.GetComponentsInChildren<MeshFilter>(true)
                .Where(f => !skipNames.Contains(f.name) && f.sharedMesh != null)
                .Sum(f => MeshTriangles(f.sharedMesh));

        private static int MeshTriangles(Mesh mesh)
        {
            int triangles = 0;
            for (int i = 0; i < mesh.subMeshCount; i++) triangles += (int)mesh.GetIndexCount(i) / 3;
            return triangles;
        }

        private static bool IsBuiltIn(Mesh mesh) =>
            AssetDatabase.GetAssetPath(mesh).StartsWith("Library/") || AssetDatabase.GetAssetPath(mesh) == "";

        private static int Prewarm(Object component, string field) =>
            new SerializedObject(component).FindProperty(field).intValue;

        private static LevelCatalog Catalog()
        {
            string[] guids = AssetDatabase.FindAssets("t:" + nameof(LevelCatalog));
            Assert.AreEqual(1, guids.Length);
            return AssetDatabase.LoadAssetAtPath<LevelCatalog>(AssetDatabase.GUIDToAssetPath(guids[0]));
        }

        // --- Render settings (§16.2)

        [Test]
        public void Urp_MobileRenderSettings()
        {
            var urp = GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
            Assert.IsNotNull(urp, "URP is the active pipeline");

            Assert.IsTrue(urp.useSRPBatcher, "SRP Batcher on");
            Assert.IsFalse(urp.supportsHDR, "HDR off");
            Assert.AreEqual(2, urp.msaaSampleCount, "MSAA 2x");
            Assert.IsFalse(urp.supportsMainLightShadows, "no real-time shadows; blob shadows instead");
            Assert.AreEqual(LightRenderingMode.Disabled, urp.additionalLightsRenderingMode, "no additional lights");
        }

        [Test]
        public void Scene_LightHasNoShadows_CameraHasNoPostProcessing()
        {
            Light light = SceneComponent<Light>();
            Assert.AreEqual(LightShadows.None, light.shadows);

            Camera cam = SceneComponent<Camera>();
            var data = cam.GetComponent<UniversalAdditionalCameraData>();
            Assert.IsTrue(data == null || !data.renderPostProcessing, "no post-processing");
            Assert.IsFalse(cam.allowHDR && ((UniversalRenderPipelineAsset)GraphicsSettings.currentRenderPipeline).supportsHDR);
        }

        // --- Models (§16.2: low-poly, ~500-1500 triangles)

        [Test]
        public void Passenger_UsesALowPolyModel()
        {
            GameObject passenger = Prefab("Passenger");
            MeshFilter model = passenger.transform.Find("Bounce/Body/Mesh/Model").GetComponent<MeshFilter>();

            Assert.IsFalse(IsBuiltIn(model.sharedMesh), "not a Unity primitive any more");
            int triangles = Triangles(passenger, "BlobShadow");
            Assert.That(triangles, Is.InRange(200, ModelTriangleLimit));
        }

        [Test]
        public void Bus_UsesALowPolyModel()
        {
            GameObject bus = Prefab("Bus");
            MeshFilter body = bus.transform.Find("Body").GetComponent<MeshFilter>();

            Assert.IsFalse(IsBuiltIn(body.sharedMesh), "not a Unity primitive any more");
            Assert.That(Triangles(bus), Is.InRange(200, ModelTriangleLimit));
        }

        [Test]
        public void Models_StandOnTheGround()
        {
            Bounds passenger = Prefab("Passenger").transform.Find("Bounce/Body/Mesh/Model").GetComponent<MeshFilter>().sharedMesh.bounds;
            Assert.AreEqual(0f, passenger.min.y, 0.02f, "passenger feet at the pivot");
            Assert.That(passenger.size.y, Is.InRange(0.7f, 1.1f), "about one cell tall");
        }

        [Test]
        public void HardestLevel_StaysUnderTheTriangleBudget()
        {
            int passenger = Triangles(Prefab("Passenger"));
            int bus = Triangles(Prefab("Bus")) + 3 * Triangles(Prefab("BusSeat"));
            int floor = Triangles(Prefab("FloorTile"));
            int wall = Triangles(Prefab("WallBlock"));
            int edge = Triangles(Prefab("EdgePiece"));
            int slot = Triangles(Prefab("SlotMarker"));

            LevelCatalog catalog = Catalog();
            for (int i = 0; i < catalog.Count; i++)
            {
                LevelData level = catalog.Get(i);
                int cells = level.Cells.Count(c => c != CellType.Void);
                int walls = level.Cells.Count(c => c == CellType.Wall);
                // Upper bound: every cell with four edges, two buses on screen at once.
                int total = level.Passengers.Count * passenger + cells * (floor + 4 * edge) + walls * wall
                            + level.WaitingSlots * slot + 2 * bus;
                Assert.Less(total, SceneTriangleLimit, $"level {i} (§16.4)");
            }
        }

        // --- Draw calls: every opaque world material uses one shader, so the SRP Batcher can batch them

        [Test]
        public void WorldPrefabs_UseSimpleLitExceptTheBlobShadow()
        {
            foreach (string name in new[] { "Passenger", "Bus", "BusSeat", "FloorTile", "WallBlock", "EdgePiece", "SlotMarker" })
            {
                foreach (MeshRenderer renderer in Prefab(name).GetComponentsInChildren<MeshRenderer>(true))
                {
                    if (renderer.name == "BlobShadow") continue;
                    foreach (Material material in renderer.sharedMaterials)
                    {
                        Assert.IsNotNull(material, $"{name}/{renderer.name}");
                        Assert.AreEqual(SimpleLit, material.shader.name, $"{name}/{renderer.name}");
                    }
                    Assert.AreEqual(ShadowCastingMode.Off, renderer.shadowCastingMode, $"{name}/{renderer.name}");
                }
            }
        }

        // --- Pool prewarm (§10.2): the first frame of any catalog level does not Instantiate

        [Test]
        public void Prewarm_CoversEveryCatalogLevel()
        {
            GameplayPresenter presenter = SceneComponent<GameplayPresenter>();
            BoardView board = SceneComponent<BoardView>();
            WaitingAreaView waiting = SceneComponent<WaitingAreaView>();

            LevelCatalog catalog = Catalog();
            for (int i = 0; i < catalog.Count; i++)
            {
                LevelData level = catalog.Get(i);
                Assert.LessOrEqual(level.Passengers.Count, Prewarm(presenter, "passengerPrewarm"), $"level {i} passengers");
                Assert.LessOrEqual(level.Cells.Count(c => c != CellType.Void), Prewarm(board, "floorTilePrewarm"), $"level {i} floor tiles");
                Assert.LessOrEqual(level.Cells.Count(c => c == CellType.Wall), Prewarm(board, "wallBlockPrewarm"), $"level {i} walls");
                Assert.LessOrEqual(level.WaitingSlots, Prewarm(waiting, "slotMarkerPrewarm"), $"level {i} slots");
            }
            Assert.GreaterOrEqual(Prewarm(presenter, "busPrewarm"), 3, "arriving, parked and departing bus");
        }

        // --- Zero GC per frame (§16.3)

        private sealed class RunningStep : IStep
        {
            public bool IsDone => false;
            public void Tick() { }
        }

        [Test]
        public void BusLane_Tick_AllocatesNothing()
        {
            var idle = new BusLane();
            idle.Tick();
            Assert.That(() => idle.Tick(), NoAlloc.Not.AllocatingGCMemory(), "idle lane");

            var busy = new BusLane();
            busy.Enqueue(new RunningStep());
            busy.Enqueue(new RunningStep());
            busy.Tick();
            Assert.That(() => busy.Tick(), NoAlloc.Not.AllocatingGCMemory(), "lane with a running step");
        }
    }
}
