using System;
using Game.Core.Levels;
using Game.Runtime.Data;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace Game.Editor.Tests
{
    // Behaviour of LevelAsset / LevelCatalog using in-memory instances (no project assets touched).
    public sealed class LevelCatalogTests
    {
        private LevelCatalog catalog;
        private LevelAsset first;
        private LevelAsset second;

        private static LevelAsset CreateLevel(string[] rows, string buses, int capacity = 1, int slots = 5)
        {
            var asset = ScriptableObject.CreateInstance<LevelAsset>();
            var so = new SerializedObject(asset);
            SerializedProperty rowsProperty = so.FindProperty("rows");
            rowsProperty.arraySize = rows.Length;
            for (int i = 0; i < rows.Length; i++)
                rowsProperty.GetArrayElementAtIndex(i).stringValue = rows[i];
            so.FindProperty("buses").stringValue = buses;
            so.FindProperty("busCapacity").intValue = capacity;
            so.FindProperty("waitingSlots").intValue = slots;
            so.ApplyModifiedPropertiesWithoutUndo();
            return asset;
        }

        private static LevelCatalog CreateCatalog(params LevelAsset[] levels)
        {
            var created = ScriptableObject.CreateInstance<LevelCatalog>();
            var so = new SerializedObject(created);
            SerializedProperty list = so.FindProperty("levels");
            list.arraySize = levels.Length;
            for (int i = 0; i < levels.Length; i++)
                list.GetArrayElementAtIndex(i).objectReferenceValue = levels[i];
            so.ApplyModifiedPropertiesWithoutUndo();
            return created;
        }

        [SetUp]
        public void SetUp()
        {
            first = CreateLevel(new[] { "R" }, "R");
            second = CreateLevel(new[] { "GG" }, "G", capacity: 2);
            catalog = CreateCatalog(first, second);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(catalog);
            Object.DestroyImmediate(first);
            Object.DestroyImmediate(second);
        }

        [Test]
        public void LevelAsset_DefaultValues()
        {
            var asset = ScriptableObject.CreateInstance<LevelAsset>();
            var so = new SerializedObject(asset);

            Assert.AreEqual(1, so.FindProperty("version").intValue);
            Assert.AreEqual(3, so.FindProperty("busCapacity").intValue);
            Assert.AreEqual(5, so.FindProperty("waitingSlots").intValue);
            Object.DestroyImmediate(asset);
        }

        [Test]
        public void LevelAsset_Parse_UsesSerializedFields()
        {
            ParseResult result = second.Parse();

            Assert.IsTrue(result.Success);
            Assert.AreEqual(2, result.Level.Width);
            Assert.AreEqual(2, result.Level.BusCapacity);
            Assert.AreEqual(1, result.Level.Version);
        }

        [Test]
        public void Catalog_CountAndHasNext()
        {
            Assert.AreEqual(2, catalog.Count);
            Assert.IsTrue(catalog.HasNext(0));
            Assert.IsFalse(catalog.HasNext(1), "the last level has no next");
            Assert.AreSame(second, catalog.GetAsset(1));
        }

        [Test]
        public void Catalog_Get_ReturnsParsedLevelInCatalogOrder()
        {
            Assert.AreEqual(1, catalog.Get(0).Width);
            Assert.AreEqual(2, catalog.Get(1).Width);
        }

        [Test]
        public void Catalog_Get_NoCache_ReflectsEdits()
        {
            Assert.AreEqual(1, catalog.Get(0).Width);
            var so = new SerializedObject(first);
            so.FindProperty("rows").GetArrayElementAtIndex(0).stringValue = "R..";
            so.ApplyModifiedPropertiesWithoutUndo();

            Assert.AreEqual(3, catalog.Get(0).Width);
        }

        [Test]
        public void Catalog_Get_BrokenLevel_LogsErrorAndThrows()
        {
            LevelAsset broken = CreateLevel(new[] { "RX" }, "R");
            LevelCatalog brokenCatalog = CreateCatalog(broken);

            LogAssert.Expect(LogType.Error, new System.Text.RegularExpressions.Regex(".*"));
            Assert.Throws<InvalidOperationException>(() => brokenCatalog.Get(0));

            Object.DestroyImmediate(brokenCatalog);
            Object.DestroyImmediate(broken);
        }
    }
}
