using Game.Runtime.Diagnostics;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Game.Editor.Tests
{
    // The device-test FPS readout (§16.5): averaged label, colour by performance, Inspector defaults.
    public sealed class FPSCounterTests
    {
        [Test]
        public void Label_ShowsAverageFpsAndFrameTime()
        {
            Assert.AreEqual("58 FPS (17.2 ms)", FPSCounter.Label(29, 0.5f));
            Assert.AreEqual("60 FPS (16.7 ms)", FPSCounter.Label(30, 0.5f));
        }

        [TestCase(60f, 0.2f, 0.9f, 0.3f)]
        [TestCase(50f, 0.2f, 0.9f, 0.3f)]
        [TestCase(49.9f, 1f, 0.85f, 0.1f)]
        [TestCase(30f, 1f, 0.85f, 0.1f)]
        [TestCase(29.9f, 1f, 0.25f, 0.25f)]
        public void Colour_FollowsPerformance(float fps, float r, float g, float b)
        {
            Assert.AreEqual(new Color(r, g, b), FPSCounter.ColorFor(fps));
        }

        [Test]
        public void Defaults_MatchTheSpec()
        {
            var go = new GameObject("FPSCounter");
            try
            {
                var so = new SerializedObject(go.AddComponent<FPSCounter>());
                Assert.AreEqual(0.5f, so.FindProperty("updateInterval").floatValue);
                Assert.IsTrue(so.FindProperty("setTargetFrameRate").boolValue);
                Assert.AreEqual(60, so.FindProperty("targetFrameRate").intValue);
                Assert.IsTrue(so.FindProperty("developmentBuildOnly").boolValue);
                Assert.AreEqual(0.025f, so.FindProperty("fontScreenShare").floatValue);
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }
    }
}
