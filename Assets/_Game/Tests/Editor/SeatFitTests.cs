using System.Linq;
using DG.Tweening;
using Game.Runtime.Data;
using Game.Runtime.View;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Game.Editor.Tests
{
    // Roof seats fit any capacity: when they would not fit, the whole seat (chair + passenger) shrinks.
    public sealed class SeatFitTests
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

        private void Fill(int capacity)
        {
            bus.Init(0, AssetDatabase.LoadAssetAtPath<Material>("Assets/_Game/Content/Materials/Passenger_Red.mat"), capacity, Vector3.zero, Vector3.zero);
            bus.SetFilledCount(capacity);
            DOTween.CompleteAll();
        }

        private Renderer[] Visible(string part) => bus.transform.Find("SeatRoot").Cast<Transform>()
            .Where(seat => seat.gameObject.activeSelf)
            .Select(seat => seat.Find(part).GetComponent<Renderer>())
            .OrderBy(r => r.bounds.center.x).ToArray();

        private Bounds Body() => bus.transform.Find("Body").GetComponent<Renderer>().bounds;

        // How much of a seated passenger shows above the roof.
        private float ShareAboveRoof()
        {
            Bounds p = Visible("Passenger").First().bounds;
            return (p.max.y - Body().max.y) / p.size.y;
        }

        [TestCase(3)]
        [TestCase(5)]
        [TestCase(8)]
        public void EveryCapacity_FitsOnTheRoofWithoutOverlap(int capacity)
        {
            Fill(capacity);
            Bounds body = Body();

            foreach (string part in new[] { "Chair", "Passenger" })
            {
                Renderer[] row = Visible(part);
                Assert.AreEqual(capacity, row.Length, part);
                Assert.GreaterOrEqual(row.First().bounds.min.x, body.min.x - 1e-3f, $"{part}: front end on the bus");
                Assert.LessOrEqual(row.Last().bounds.max.x, body.max.x + 1e-3f, $"{part}: back end on the bus");
                for (int i = 1; i < row.Length; i++)
                    Assert.Less(row[i - 1].bounds.max.x, row[i].bounds.min.x, $"{part}: {i - 1} and {i} do not overlap");
            }
        }

        [Test]
        public void ThreeSeats_KeepTheirFullSize()
        {
            Fill(3);
            foreach (Transform seat in bus.transform.Find("SeatRoot").Cast<Transform>().Where(s => s.gameObject.activeSelf))
                Assert.AreEqual(Vector3.one, seat.localScale, seat.name);
        }

        [TestCase(5)]
        [TestCase(8)]
        public void ShrunkSeats_ShowAsMuchOfThePassengerAboveTheRoof(int capacity)
        {
            Fill(3);
            float full = ShareAboveRoof();
            Fill(capacity);
            Assert.AreEqual(full, ShareAboveRoof(), 0.02f, "not just a head: the sink shrinks with the passenger");
        }
    }
}
