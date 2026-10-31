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
    // Boarding shows up as passengers sitting on the bus roof (§10.7): they are already in their seats,
    // hidden, and each boarding reveals the next one.
    public sealed class SeatedPassengerTests
    {
        private const string Prefabs = "Assets/_Game/Content/Prefabs/";
        private const float BusHalfLength = 1.22f;

        private Scene preview;
        private BusView bus;
        private Material material;

        [SetUp]
        public void SetUp()
        {
            preview = EditorSceneManager.NewPreviewScene();
            var go = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Prefabs + "Bus.prefab"), preview);
            bus = go.GetComponent<BusView>();
            bus.Configure(AssetDatabase.LoadAssetAtPath<AnimationSettings>("Assets/_Game/Content/Settings/AnimationSettings.asset"));
            material = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Game/Content/Materials/Passenger_Red.mat");
            bus.Init(0, material, 3, Vector3.zero, Vector3.zero);
        }

        [TearDown]
        public void TearDown()
        {
            DOTween.KillAll();
            EditorSceneManager.ClosePreviewScene(preview);
        }

        // The passenger of every seat, front to back.
        private Renderer[] Seats() => bus.transform.Find("SeatRoot").Cast<Transform>()
            .OrderBy(seat => seat.position.x)
            .Select(seat => seat.Find("Passenger").GetComponent<Renderer>()).ToArray();

        private static Bounds BodyBounds(BusView view) =>
            view.transform.Find("Body").GetComponent<Renderer>().bounds;

        [Test]
        public void SeatPrefab_PassengerIsThePassengerModel_NoScriptsOrShadows()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(Prefabs + "BusSeat.prefab");
            Assert.IsNotNull(prefab);
            var passengerMesh = AssetDatabase.LoadAssetAtPath<Mesh>("Assets/_Game/Content/Models/Passenger.asset");

            Transform passenger = prefab.transform.Find("Passenger");
            Assert.IsNotNull(passenger, "BusSeat -> Passenger");
            Assert.AreSame(passengerMesh, passenger.GetComponent<MeshFilter>().sharedMesh);
            Assert.AreEqual(ShadowCastingMode.Off, passenger.GetComponent<MeshRenderer>().shadowCastingMode);
            Assert.AreEqual(2, prefab.GetComponentsInChildren<MeshFilter>(true).Length, "a chair and a passenger, no blob shadow");
            Assert.IsEmpty(prefab.GetComponentsInChildren<MonoBehaviour>(true), "plain models: the bus owns them");
        }

        [Test]
        public void Init_SeatsEveryoneHidden_InTheBusColour()
        {
            Renderer[] seats = Seats();
            Assert.AreEqual(3, seats.Length);
            foreach (Renderer seat in seats)
            {
                Assert.IsFalse(seat.gameObject.activeInHierarchy, "nobody is in yet");
                Assert.AreSame(material, seat.sharedMaterial);
            }
        }

        [Test]
        public void Seats_SitInARowOnTheRoof()
        {
            Bounds body = BodyBounds(bus);
            bus.SetFilledCount(3);
            DOTween.CompleteAll();   // the reveal punch ends at the resting size
            foreach (Renderer seat in Seats())
            {
                Bounds b = seat.bounds;
                Assert.Less(b.min.y, body.max.y, $"{seat.name}: sunk into the bus, it sits");
                Assert.Greater(b.max.y, body.max.y + 0.3f, $"{seat.name}: head and shoulders show above the roof");
                Assert.That(b.min.x, Is.GreaterThanOrEqualTo(-BusHalfLength), $"{seat.name}: on the bus");
                Assert.That(b.max.x, Is.LessThanOrEqualTo(BusHalfLength), $"{seat.name}: on the bus");
                Assert.AreEqual(body.center.z, b.center.z, 0.05f, $"{seat.name}: on the middle line");
            }

            Renderer[] row = Seats();
            for (int i = 1; i < row.Length; i++)
                Assert.Less(row[i - 1].bounds.max.x, row[i].bounds.min.x, "they do not overlap");
        }

        [Test]
        public void EachBoarding_RevealsTheNextSeat()
        {
            bus.SetFilledCount(1);
            Assert.AreEqual(1, Seats().Count(s => s.gameObject.activeInHierarchy));
            bus.SetFilledCount(2);
            Assert.AreEqual(2, Seats().Count(s => s.gameObject.activeInHierarchy));
        }

        [Test]
        public void ResetForPool_HidesEveryone()
        {
            bus.SetFilledCount(3);
            bus.ResetForPool();
            Assert.IsTrue(Seats().All(s => !s.gameObject.activeInHierarchy));
        }
    }
}
