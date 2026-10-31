using Game.Runtime.Data;
using NUnit.Framework;
using UnityEngine;

namespace Game.Editor.Tests
{
    // Documented default values of the settings assets.
    public sealed class SettingsDefaultsTests
    {
        [Test]
        public void LayoutSettings_Defaults()
        {
            var s = ScriptableObject.CreateInstance<LayoutSettings>();

            Assert.AreEqual(1.0f, s.CellSize);
            Assert.AreEqual(1.0f, s.RoadOffset);
            Assert.AreEqual(2.0f, s.SlotsOffset);
            Assert.AreEqual(3.5f, s.BusOffset);
            Assert.AreEqual(1.1f, s.SlotSpacing);
            Assert.AreEqual(0.6f, s.DoorOffsetZ);
            Assert.AreEqual(10f, s.BusOffscreenX);
            Object.DestroyImmediate(s);
        }

        [Test]
        public void AnimationSettings_Defaults()
        {
            var a = ScriptableObject.CreateInstance<AnimationSettings>();

            Assert.AreEqual(4.0f, a.WalkSpeed);
            Assert.AreEqual(0.15f, a.StepHeight);
            Assert.AreEqual(0.12f, a.StepTime);
            Assert.AreEqual(new Vector3(0.2f, -0.2f, 0.2f), a.TapSquash);
            Assert.AreEqual(0.2f, a.TapSquashTime);
            Assert.AreEqual(0.1f, a.BlockedShakeStrength);
            Assert.AreEqual(0.3f, a.BlockedShakeTime);
            Assert.AreEqual(0.5f, a.BoardHopHeight);
            Assert.AreEqual(0.2f, a.BoardHopTime);
            Assert.AreEqual(0.25f, a.BoardFromSlotTime);
            Assert.AreEqual(0.5f, a.BusArriveTime);
            Assert.AreEqual(0.4f, a.BusDepartTime);
            Assert.AreEqual(0.25f, a.IntroPopTime);
            Assert.AreEqual(0.02f, a.IntroStagger);
            Assert.AreEqual(0.03f, a.IdleBreathScale);
            Assert.AreEqual(1.2f, a.IdleBreathTime);
            Assert.AreEqual(0.7f, a.SeatSpacing);
            Assert.AreEqual(0.25f, a.PopupTime);
            Assert.AreEqual(1.1f, a.NodePulseScale);
            Assert.AreEqual(0.6f, a.NodePulseTime);
            Object.DestroyImmediate(a);
        }
    }
}
