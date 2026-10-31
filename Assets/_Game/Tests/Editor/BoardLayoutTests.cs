using Game.Core;
using Game.Core.Levels;
using Game.Runtime.Data;
using Game.Runtime.View;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Game.Editor.Tests
{
    // World layout math with the default LayoutSettings.
    public sealed class BoardLayoutTests
    {
        private LayoutSettings settings;

        [SetUp]
        public void SetUp() => settings = ScriptableObject.CreateInstance<LayoutSettings>();

        [TearDown]
        public void TearDown() => Object.DestroyImmediate(settings);

        private static LevelData Level(string[] rows, int slots = 5) =>
            LevelParser.Parse(rows, "R", 1, slots, 1).Level;

        private static void AssertNear(Vector3 expected, Vector3 actual, string message = null)
        {
            Assert.Less(Vector3.Distance(expected, actual), 1e-4f, $"expected {expected} but was {actual}. {message}");
        }

        [Test]
        public void Board_IsCenteredInX_TopRowAtZeroZ_GrowsTowardsMinusZ()
        {
            var layout = new BoardLayout(Level(new[] { "...", "..." }), settings);

            AssertNear(new Vector3(-1f, 0f, 0f), layout.Origin);
            AssertNear(new Vector3(-1f, 0f, 0f), layout.CellToWorld(new GridPos(0, 0)));
            AssertNear(new Vector3(0f, 0f, 0f), layout.CellToWorld(new GridPos(1, 0)));
            AssertNear(new Vector3(1f, 0f, -1f), layout.CellToWorld(new GridPos(2, 1)));
        }

        [Test]
        public void EvenWidth_IsCenteredBetweenCells()
        {
            var layout = new BoardLayout(Level(new[] { "...." }), settings);

            AssertNear(new Vector3(-1.5f, 0f, 0f), layout.CellToWorld(new GridPos(0, 0)));
            AssertNear(new Vector3(1.5f, 0f, 0f), layout.CellToWorld(new GridPos(3, 0)));
        }

        [Test]
        public void WorldToCell_IsInverseOfCellToWorld_AndRoundsToNearestCell()
        {
            var layout = new BoardLayout(Level(new[] { "....", "....", "...." }), settings);

            for (int y = 0; y < 3; y++)
                for (int x = 0; x < 4; x++)
                {
                    var cell = new GridPos(x, y);
                    Vector3 center = layout.CellToWorld(cell);
                    Assert.AreEqual(cell, layout.WorldToCell(center));
                    Assert.AreEqual(cell, layout.WorldToCell(center + new Vector3(0.4f, 0f, -0.4f)));
                    Assert.AreEqual(cell, layout.WorldToCell(center + new Vector3(-0.4f, 0f, 0.4f)));
                }

            Assert.AreEqual(new GridPos(-1, 0), layout.WorldToCell(layout.CellToWorld(new GridPos(0, 0)) + Vector3.left));
            Assert.AreEqual(new GridPos(0, -1), layout.WorldToCell(layout.CellToWorld(new GridPos(0, 0)) + Vector3.forward));
        }

        [Test]
        public void RoadAndBusPoints()
        {
            var layout = new BoardLayout(Level(new[] { "...", "..." }), settings);

            AssertNear(new Vector3(1f, 0f, 1f), layout.RoadPointAbove(new GridPos(2, 0)));
            AssertNear(new Vector3(0f, 0f, 3.5f), layout.BusParkPosition);
            AssertNear(new Vector3(0f, 0f, 2.9f), layout.DoorPoint);
            AssertNear(new Vector3(10f, 0f, 3.5f), layout.BusEnterPosition, "bus comes from the right");
            AssertNear(new Vector3(-10f, 0f, 3.5f), layout.BusExitPosition, "bus leaves to the left");
        }

        [Test]
        public void Slots_AreCenteredInARow()
        {
            var layout = new BoardLayout(Level(new[] { "..." }, slots: 5), settings);

            AssertNear(new Vector3(-2.2f, 0f, 2f), layout.SlotPosition(0));
            AssertNear(new Vector3(0f, 0f, 2f), layout.SlotPosition(2));
            AssertNear(new Vector3(2.2f, 0f, 2f), layout.SlotPosition(4));

            var six = new BoardLayout(Level(new[] { "..." }, slots: 6), settings);
            AssertNear(new Vector3(-2.75f, 0f, 2f), six.SlotPosition(0));
            AssertNear(new Vector3(2.75f, 0f, 2f), six.SlotPosition(5));
        }

        [Test]
        public void CellSize_ScalesBoardAndOffsets()
        {
            var so = new SerializedObject(settings);
            so.FindProperty("cellSize").floatValue = 2f;
            so.ApplyModifiedPropertiesWithoutUndo();
            var layout = new BoardLayout(Level(new[] { "...", "..." }), settings);

            AssertNear(new Vector3(0f, 0f, -2f), layout.CellToWorld(new GridPos(1, 1)));
            Assert.AreEqual(new GridPos(1, 1), layout.WorldToCell(new Vector3(0.9f, 0f, -2.9f)));
            AssertNear(new Vector3(-2f, 0f, 2f), layout.RoadPointAbove(new GridPos(0, 0)));
            AssertNear(new Vector3(0f, 0f, 7f), layout.BusParkPosition);
            AssertNear(new Vector3(0f, 0f, 4f), layout.SlotPosition(2));
        }

        [Test]
        public void ContentBounds_ContainBoardSlotsAndBus()
        {
            var layout = new BoardLayout(Level(new[] { "......", "......", "......", "......", "......" }), settings);
            Bounds b = layout.ContentBounds;

            void AssertInside(Vector3 p, string what)
            {
                Assert.IsTrue(p.x >= b.min.x - 1e-4f && p.x <= b.max.x + 1e-4f &&
                              p.z >= b.min.z - 1e-4f && p.z <= b.max.z + 1e-4f, $"{what} {p} outside {b}");
            }

            // Whole cells (not only centers) are inside.
            AssertInside(layout.CellToWorld(new GridPos(0, 0)) + new Vector3(-0.5f, 0f, 0.5f), "top-left cell corner");
            AssertInside(layout.CellToWorld(new GridPos(5, 4)) + new Vector3(0.5f, 0f, -0.5f), "bottom-right cell corner");
            for (int slot = 0; slot < 5; slot++) AssertInside(layout.SlotPosition(slot), $"slot {slot}");
            AssertInside(layout.BusParkPosition, "bus park");
            AssertInside(layout.DoorPoint, "door");
        }
    }
}
