using Game.Core;
using Game.Runtime.Data;
using Game.Runtime.View;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Game.Editor.Tests
{
    // Parallel boarding: the bus counts who still has to get on, so it leaves after the last one,
    // and its indicators fill by how many are in (passengers can get on out of seat order).
    public sealed class ParallelBoardingTests
    {
        private Scene preview;
        private BusView bus;

        [SetUp]
        public void SetUp()
        {
            preview = EditorSceneManager.NewPreviewScene();
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Game/Content/Prefabs/Bus.prefab");
            var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, preview);
            bus = go.GetComponent<BusView>();
            bus.Configure(AssetDatabase.LoadAssetAtPath<AnimationSettings>("Assets/_Game/Content/Settings/AnimationSettings.asset"));
            Material red = AssetDatabase.LoadAssetAtPath<ColorPalette>("Assets/_Game/Content/Settings/ColorPalette.asset")
                                        .GetMaterial(PassengerColor.Red);
            bus.Init(0, red, 3, Vector3.zero, Vector3.zero);
        }

        [TearDown]
        public void TearDown() => EditorSceneManager.ClosePreviewScene(preview);

        [Test]
        public void NewBus_HasNobodyToWaitFor()
        {
            Assert.AreEqual(0, bus.PendingBoardings);
            Assert.IsFalse(bus.IsParked, "a bus is parked only after its arrival");
        }

        [Test]
        public void Boardings_AreCountedInAndOut()
        {
            bus.ExpectBoarding();
            bus.ExpectBoarding();
            bus.ExpectBoarding();
            Assert.AreEqual(3, bus.PendingBoardings);

            Assert.AreEqual(1, bus.CompleteBoarding(), "first one in, whatever its seat");
            Assert.AreEqual(2, bus.CompleteBoarding());
            Assert.AreEqual(1, bus.PendingBoardings);
            Assert.AreEqual(3, bus.CompleteBoarding());
            Assert.AreEqual(0, bus.PendingBoardings, "now it may leave");
        }

        [Test]
        public void Init_ResetsTheCounters()
        {
            bus.ExpectBoarding();
            bus.CompleteBoarding();
            bus.ExpectBoarding();

            bus.Init(1, null, 3, Vector3.zero, Vector3.zero);

            Assert.AreEqual(0, bus.PendingBoardings);
            bus.ExpectBoarding();
            Assert.AreEqual(1, bus.CompleteBoarding(), "the count starts again for the reused bus");
        }
    }
}
