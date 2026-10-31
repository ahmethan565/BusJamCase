using System.Linq;
using Game.Core;
using Game.Core.Levels;
using Game.Runtime.Data;
using NUnit.Framework;
using UnityEditor;

namespace Game.Editor.Tests
{
    // Levels 7-16 each push one rule or limit to its edge. CatalogTests already checks that every
    // catalog level is valid and solvable; these tests make sure each level keeps the edge it is there for.
    public sealed class EdgeCaseLevelTests
    {
        private static LevelData Level(string name)
        {
            string[] guids = AssetDatabase.FindAssets("t:" + nameof(LevelCatalog));
            var catalog = AssetDatabase.LoadAssetAtPath<LevelCatalog>(AssetDatabase.GUIDToAssetPath(guids.Single()));
            for (int i = 0; i < catalog.Count; i++)
                if (catalog.GetAsset(i).name == name) return catalog.Get(i);
            Assert.Fail($"{name} is not in the catalog");
            return null;
        }

        private static int Count(LevelData level, CellType type) => level.Cells.Count(c => c == type);

        [Test]
        public void SingleTap_IsTheSmallestPossibleLevel()
        {
            LevelData level = Level("Level_07_SingleTap");
            Assert.AreEqual((1, 1), (level.Width, level.Height));
            Assert.AreEqual(1, level.Passengers.Count);
            Assert.AreEqual(1, level.BusColors.Count);
            Assert.AreEqual(1, level.BusCapacity);
            Assert.AreEqual(1, level.WaitingSlots);
            Assert.AreEqual(0f, level.TimeLimitSeconds, "no limit: the timer is hidden");
        }

        [Test]
        public void OneSlot_AnyWrongTapLoses()
        {
            LevelData level = Level("Level_08_OneSlot");
            Assert.AreEqual(1, level.WaitingSlots);
            Assert.That(level.Passengers.Any(p => p.Position.Y == 0 && p.Color != level.BusColors[0]),
                "a wrong colour can be tapped right away");
        }

        [Test]
        public void SameColorQueue_SixOneSeatBusesOfOneColour()
        {
            LevelData level = Level("Level_09_SameColorQueue");
            Assert.AreEqual(1, level.BusCapacity);
            Assert.GreaterOrEqual(level.BusColors.Count, 6);
            Assert.AreEqual(1, level.BusColors.Distinct().Count());
        }

        [Test]
        public void AllColors_UsesEveryPassengerColour()
        {
            LevelData level = Level("Level_10_AllColors");
            var all = (PassengerColor[])System.Enum.GetValues(typeof(PassengerColor));
            CollectionAssert.AreEquivalent(all, level.BusColors.Distinct());
        }

        [Test]
        public void WideBoard_IsWiderThanAnyOtherShape()
        {
            LevelData level = Level("Level_11_WideBoard");
            Assert.GreaterOrEqual(level.Width, 9);
            Assert.Less(level.Height, level.Width / 2);
        }

        [Test]
        public void TallBoard_IsMuchTallerThanWide()
        {
            LevelData level = Level("Level_12_TallBoard");
            Assert.GreaterOrEqual(level.Height, 10);
            Assert.LessOrEqual(level.Width, 3);
        }

        [Test]
        public void Maze_UsesTheWallLimitForALongCorridor()
        {
            LevelData level = Level("Level_13_Maze");
            Assert.AreEqual(8, Count(level, CellType.Wall), "as many walls as the pool holds");
            Assert.That(level.Passengers.Any(p => p.Position.Y == level.Height - 1), "someone starts at the far end");
        }

        [Test]
        public void NearlyFull_NeedsTheWaitingAreaButOneSlot()
        {
            LevelData level = Level("Level_14_NearlyFull");
            int wrongOnTop = level.Passengers.Count(p => p.Position.Y == 0 && p.Color != level.BusColors[0]);
            Assert.AreEqual(level.WaitingSlots + 1, wrongOnTop, "parking every wrong top passenger would fill the last slot");
        }

        [Test]
        public void ShortTimer_StartsJustAboveTheWarning()
        {
            LevelData level = Level("Level_15_ShortTimer");
            Assert.That(level.TimeLimitSeconds, Is.InRange(11f, 15f), "the last-10-seconds warning comes almost at once");
        }

        [Test]
        public void DeepExits_HaveExitsBelowTheTopRow()
        {
            LevelData level = Level("Level_16_DeepExits");
            var board = new Board(level.Width, level.Height, level.Cells.ToArray(), System.Array.Empty<Passenger>());
            Assert.That(board.ExitCells.Any(c => c.Y > 0), "some exits are below the top row (void above them)");
            Assert.That(board.ExitCells.Any(c => c.Y == 0));
        }
    }
}
