using System.Collections.Generic;
using Game.Core.Levels;
using Game.Runtime.Data;
using Game.Runtime.View;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Game.Editor.Tests
{
    // With the fixed tilt/FOV and margin 0.3, every catalog level fits on portrait phone screens,
    // including objects up to one unit tall standing on the outermost cells.
    public sealed class CameraFitterTests
    {
        private GameObject cameraObject;
        private Camera cam;
        private CameraFitter fitter;
        private LayoutSettings settings;

        [SetUp]
        public void SetUp()
        {
            cameraObject = new GameObject("TestCamera");
            cam = cameraObject.AddComponent<Camera>();
            fitter = cameraObject.AddComponent<CameraFitter>();
            var so = new SerializedObject(fitter);
            so.FindProperty("cam").objectReferenceValue = cam;
            so.ApplyModifiedPropertiesWithoutUndo();
            settings = ScriptableObject.CreateInstance<LayoutSettings>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(cameraObject);
            Object.DestroyImmediate(settings);
        }

        [Test]
        public void Defaults_TiltFovMargin()
        {
            var so = new SerializedObject(fitter);
            Assert.AreEqual(55f, so.FindProperty("tilt").floatValue);
            Assert.AreEqual(40f, so.FindProperty("verticalFov").floatValue);
            Assert.AreEqual(0.3f, so.FindProperty("margin").floatValue);
        }

        [Test]
        public void Fit_AppliesTiltAndFov()
        {
            var layout = new BoardLayout(LevelParser.Parse(new[] { "..." }, "R", 1, 5, 1).Level, settings);

            fitter.Fit(layout.ContentBounds);

            Assert.AreEqual(40f, cam.fieldOfView, 1e-3f);
            Assert.AreEqual(55f, cam.transform.eulerAngles.x, 1e-3f);
            Assert.AreEqual(0f, cam.transform.eulerAngles.y, 1e-3f);
        }

        [TestCase(1080f / 1920f)]
        [TestCase(1080f / 2340f)]
        public void Fit_EveryCatalogLevelFitsOnPortraitScreen(float aspect)
        {
            string[] guids = AssetDatabase.FindAssets("t:" + nameof(LevelCatalog));
            Assert.AreEqual(1, guids.Length);
            var catalog = AssetDatabase.LoadAssetAtPath<LevelCatalog>(AssetDatabase.GUIDToAssetPath(guids[0]));
            cam.aspect = aspect;
            var problems = new List<string>();

            for (int i = 0; i < catalog.Count; i++)
            {
                LevelData level = catalog.Get(i);
                var layout = new BoardLayout(level, settings);
                fitter.Fit(layout.ContentBounds);

                Bounds b = layout.ContentBounds;
                foreach (float x in new[] { b.min.x, b.max.x })
                    foreach (float z in new[] { b.min.z, b.max.z })
                        foreach (float y in new[] { 0f, 1f })
                        {
                            Vector3 v = cam.WorldToViewportPoint(new Vector3(x, y, z));
                            if (v.z <= 0f || v.x < 0f || v.x > 1f || v.y < 0f || v.y > 1f)
                                problems.Add($"level #{i}: corner ({x},{y},{z}) -> viewport {v}");
                        }
            }

            Assert.IsEmpty(problems, string.Join("\n", problems));
        }
    }
}
