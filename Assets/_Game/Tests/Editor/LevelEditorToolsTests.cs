using System.Collections.Generic;
using Game.Editor;
using Game.Runtime.Bootstrap;
using Game.Runtime.Data;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Game.Editor.Tests
{
    // The logic behind the level editor, the catalog editor and "play this level" (§15).
    public sealed class LevelEditorToolsTests
    {
        private static LevelCatalog LoadCatalog()
        {
            string[] guids = AssetDatabase.FindAssets("t:" + nameof(LevelCatalog));
            Assert.AreEqual(1, guids.Length);
            return AssetDatabase.LoadAssetAtPath<LevelCatalog>(AssetDatabase.GUIDToAssetPath(guids[0]));
        }

        private static LevelAsset NewLevel(string[] rows, string buses, int capacity = 3, int slots = 5)
        {
            var level = ScriptableObject.CreateInstance<LevelAsset>();
            var so = new SerializedObject(level);
            SerializedProperty rowsProperty = so.FindProperty("rows");
            rowsProperty.arraySize = rows.Length;
            for (int i = 0; i < rows.Length; i++) rowsProperty.GetArrayElementAtIndex(i).stringValue = rows[i];
            so.FindProperty("buses").stringValue = buses;
            so.FindProperty("busCapacity").intValue = capacity;
            so.FindProperty("waitingSlots").intValue = slots;
            so.ApplyModifiedPropertiesWithoutUndo();
            return level;
        }

        [TearDown]
        public void TearDown() => PlayerPrefs.DeleteKey(EditorLevelRequest.Key);

        // --- Grid editing (§15.1)

        [Test]
        public void Resize_Grow_KeepsContent_NewCellsAreFloor()
        {
            string[] result = LevelEditing.Resize(new[] { "RB", "G#" }, width: 3, height: 3);

            CollectionAssert.AreEqual(new[] { "RB.", "G#.", "..." }, result);
        }

        [Test]
        public void Resize_Shrink_Crops()
        {
            string[] result = LevelEditing.Resize(new[] { "RBG", "GY-", "..." }, width: 2, height: 2);

            CollectionAssert.AreEqual(new[] { "RB", "GY" }, result);
        }

        [Test]
        public void Paint_ChangesOneCell_LeavesTheInputAlone()
        {
            var rows = new[] { "RB", ".." };

            string[] result = LevelEditing.Paint(rows, x: 1, y: 1, symbol: '#');

            CollectionAssert.AreEqual(new[] { "RB", ".#" }, result);
            CollectionAssert.AreEqual(new[] { "RB", ".." }, rows);
        }

        [Test]
        public void AutoFillBuses_OneBusPerCapacity_InColorOrder()
        {
            bool ok = LevelEditing.AutoFillBuses(new[] { "BBBRRR", "RRRGGG" }, capacity: 3,
                                                 out string buses, out string warning);

            Assert.IsTrue(ok);
            Assert.AreEqual("RRGB", buses, "Red, Green, Blue... (PassengerColor order)");
            Assert.IsNull(warning);
        }

        [Test]
        public void AutoFillBuses_NotDivisible_Warns()
        {
            bool ok = LevelEditing.AutoFillBuses(new[] { "RRRR", "GGG." }, capacity: 3,
                                                 out string buses, out string warning);

            Assert.IsFalse(ok);
            Assert.IsNotEmpty(warning);
            StringAssert.Contains("4", warning, "says how many passengers the color has");
        }

        // --- Live checks (§15.1, §15.2)

        [Test]
        public void Check_EveryCatalogLevel_HasNoErrors()
        {
            LevelCatalog catalog = LoadCatalog();
            for (int i = 0; i < catalog.Count; i++)
                Assert.IsEmpty(LevelEditing.Check(catalog.GetAsset(i)), catalog.GetAsset(i).name);
        }

        [Test]
        public void Check_BrokenLevel_ListsParseAndRuleErrors()
        {
            LevelAsset ragged = NewLevel(new[] { "RRR", "R" }, "R");
            LevelAsset wrongCount = NewLevel(new[] { "RR." }, "R");

            Assert.IsNotEmpty(LevelEditing.Check(ragged));
            Assert.IsNotEmpty(LevelEditing.Check(wrongCount));

            Object.DestroyImmediate(ragged);
            Object.DestroyImmediate(wrongCount);
        }

        // --- "Play this level" (§15.3)

        [Test]
        public void FindIndex_CatalogLevel_ReturnsItsPosition_OtherwiseMinusOne()
        {
            LevelCatalog catalog = LoadCatalog();
            LevelAsset outside = NewLevel(new[] { "RRR" }, "R");

            Assert.AreEqual(2, EditorLevelLauncher.FindIndex(catalog, catalog.GetAsset(2)));
            Assert.AreEqual(-1, EditorLevelLauncher.FindIndex(catalog, outside));

            Object.DestroyImmediate(outside);
        }

        [Test]
        public void Request_IsReadOnce()
        {
            EditorLevelLauncher.WriteRequest(3);

            Assert.IsTrue(EditorLevelRequest.TryConsume(out int index));
            Assert.AreEqual(3, index);
            Assert.IsFalse(EditorLevelRequest.TryConsume(out _), "the request is deleted when read");
        }

        [Test]
        public void NoRequest_TryConsumeIsFalse()
        {
            PlayerPrefs.DeleteKey(EditorLevelRequest.Key);

            Assert.IsFalse(EditorLevelRequest.TryConsume(out _));
        }

        [Test]
        public void RequestKey_IsTheDocumentedOne()
        {
            Assert.AreEqual("Editor.RequestedLevel", EditorLevelRequest.Key);
        }
    }
}
