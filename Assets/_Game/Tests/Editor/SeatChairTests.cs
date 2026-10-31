using System.Linq;
using DG.Tweening;
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
    // A dark chair under every roof seat (§10.7): the empty chairs show how many passengers the bus takes.
    public sealed class SeatChairTests
    {
        private const string Prefabs = "Assets/_Game/Content/Prefabs/";

        private Scene preview;
        private BusView bus;

        [SetUp]
        public void SetUp()
        {
            preview = EditorSceneManager.NewPreviewScene();
            var go = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Prefabs + "Bus.prefab"), preview);
            bus = go.GetComponent<BusView>();
            bus.Configure(AssetDatabase.LoadAssetAtPath<AnimationSettings>("Assets/_Game/Content/Settings/AnimationSettings.asset"));
        }

        [TearDown]
        public void TearDown()
        {
            DOTween.KillAll();
            EditorSceneManager.ClosePreviewScene(preview);
        }

        private void Init(int capacity) =>
            bus.Init(0, AssetDatabase.LoadAssetAtPath<Material>("Assets/_Game/Content/Materials/Passenger_Red.mat"), capacity, Vector3.zero, Vector3.zero);

        private Renderer[] Chairs(bool visibleOnly) => bus.transform.Find("ChairRoot").GetComponentsInChildren<Renderer>(true)
            .Where(r => !visibleOnly || r.gameObject.activeInHierarchy).OrderBy(r => r.bounds.center.x).ToArray();

        private Renderer[] Passengers() => bus.transform.Find("SeatRoot").GetComponentsInChildren<Renderer>(true)
            .OrderBy(r => r.transform.position.x).ToArray();

        [Test]
        public void ChairPrefab_IsADarkSimpleLitBoxWithoutShadowsOrColliders()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(Prefabs + "SeatChair.prefab");
            Assert.IsNotNull(prefab);
            Assert.IsEmpty(prefab.GetComponentsInChildren<Collider>(true));
            MeshRenderer renderer = prefab.GetComponentsInChildren<MeshRenderer>(true).Single();
            Assert.AreEqual("Universal Render Pipeline/Simple Lit", renderer.sharedMaterial.shader.name);
            Assert.AreEqual(ShadowCastingMode.Off, renderer.shadowCastingMode);
            Color c = renderer.sharedMaterial.GetColor("_BaseColor");
            Assert.Less(c.grayscale, 0.3f, "dark, so it reads as an empty seat on any bus colour");
        }

        [Test]
        public void Init_ShowsOneEmptyChairPerSeat()
        {
            Init(3);
            Assert.AreEqual(3, Chairs(true).Length, "the empty chairs show the capacity");
            Assert.IsTrue(Passengers().All(p => !p.gameObject.activeInHierarchy));

            Init(2);   // a pooled bus coming back with a smaller capacity
            Assert.AreEqual(2, Chairs(true).Length);
        }

        [Test]
        public void Chairs_StayDuringBoarding_UnderEachPassenger_OnTheRoof()
        {
            Init(3);
            bus.SetFilledCount(2);
            DOTween.CompleteAll();

            Bounds body = bus.transform.Find("Body").GetComponent<Renderer>().bounds;
            Renderer[] chairs = Chairs(true);
            Renderer[] passengers = Passengers();
            Assert.AreEqual(3, chairs.Length, "chairs stay while passengers sit down");
            for (int i = 0; i < chairs.Length; i++)
            {
                Bounds chair = chairs[i].bounds;
                Assert.AreEqual(passengers[i].transform.position.x, chair.center.x, 0.01f, $"chair {i} under its passenger");
                Assert.AreEqual(body.max.y, chair.min.y, 0.02f, $"chair {i} sits on the roof");
                Assert.Less(chair.size.y, 0.2f, $"chair {i} is low: it never hides its passenger");
                Assert.Greater(chair.size.x, 0.5f, $"chair {i} is wider than a passenger's body");
            }
            for (int i = 1; i < chairs.Length; i++)
                Assert.Less(chairs[i - 1].bounds.max.x, chairs[i].bounds.min.x, "chairs do not touch");
        }

        [Test]
        public void ResetForPool_HidesTheChairs()
        {
            Init(3);
            bus.ResetForPool();
            Assert.AreEqual(0, Chairs(true).Length);
        }
    }
}
