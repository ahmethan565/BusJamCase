using NUnit.Framework;

namespace Game.Core.Tests
{
    public sealed class BoardTests
    {
        [Test]
        public void IsExit_TopRowFloor_True()
        {
            Board board = TestBoards.From("..#",
                                          "...");

            Assert.IsTrue(board.IsExit(new GridPos(0, 0)));
            Assert.IsTrue(board.IsExit(new GridPos(1, 0)));
            Assert.IsFalse(board.IsExit(new GridPos(2, 0)), "a wall is never an exit");
            Assert.IsFalse(board.IsExit(new GridPos(0, 1)), "a cell under a floor is not an exit");
        }

        [Test]
        public void IsExit_UnderWall_False()
        {
            Board board = TestBoards.From("#.",
                                          "..");

            Assert.IsFalse(board.IsExit(new GridPos(0, 1)));
        }

        [Test]
        public void IsExit_UnderVoidColumn_True()
        {
            Board board = TestBoards.From("--.",
                                          "-..",
                                          "...");

            Assert.IsTrue(board.IsExit(new GridPos(2, 0)));
            Assert.IsTrue(board.IsExit(new GridPos(1, 1)));
            Assert.IsTrue(board.IsExit(new GridPos(0, 2)));
            Assert.IsFalse(board.IsExit(new GridPos(2, 1)));
            Assert.IsFalse(board.IsExit(new GridPos(1, 2)));
            CollectionAssert.AreEquivalent(
                new[] { new GridPos(2, 0), new GridPos(1, 1), new GridPos(0, 2) }, board.ExitCells);
        }

        [Test]
        public void IsExit_UnderMiddleHole_False()
        {
            Board board = TestBoards.From("...",
                                          ".-.",
                                          "...");

            Assert.IsFalse(board.IsExit(new GridPos(1, 2)));
        }

        [Test]
        public void GetCell_OutOfBounds_ReturnsVoid()
        {
            Board board = TestBoards.From("..",
                                          "..");

            foreach (GridPos p in new[] { new GridPos(-1, 0), new GridPos(0, -1), new GridPos(2, 0), new GridPos(0, 2) })
            {
                Assert.AreEqual(CellType.Void, board.GetCell(p), p.ToString());
                Assert.IsFalse(board.Exists(p), p.ToString());
                Assert.IsFalse(board.InBounds(p), p.ToString());
                Assert.IsFalse(board.IsWalkable(p), p.ToString());
                Assert.IsFalse(board.TryGetPassenger(p, out _), p.ToString());
            }
        }

        [Test]
        public void ParsedCellTypes_AreStored()
        {
            Board board = TestBoards.From(".#-R");

            Assert.AreEqual(4, board.Width);
            Assert.AreEqual(1, board.Height);
            Assert.AreEqual(CellType.Floor, board.GetCell(new GridPos(0, 0)));
            Assert.AreEqual(CellType.Wall, board.GetCell(new GridPos(1, 0)));
            Assert.AreEqual(CellType.Void, board.GetCell(new GridPos(2, 0)));
            Assert.AreEqual(CellType.Floor, board.GetCell(new GridPos(3, 0)));
            Assert.IsTrue(board.Exists(new GridPos(1, 0)), "walls exist");
            Assert.IsFalse(board.Exists(new GridPos(2, 0)), "voids do not exist");
            Assert.IsFalse(board.IsWalkable(new GridPos(1, 0)));
        }

        [Test]
        public void RemovePassenger_MakesCellWalkable()
        {
            Board board = TestBoards.From("R.");
            var cell = new GridPos(0, 0);

            Assert.IsFalse(board.IsWalkable(cell));
            Assert.IsTrue(board.TryGetPassenger(cell, out Passenger passenger));
            Assert.AreEqual(PassengerColor.Red, passenger.Color);
            Assert.AreEqual(1, board.PassengerCount);

            board.RemovePassenger(cell);

            Assert.IsTrue(board.IsWalkable(cell));
            Assert.IsFalse(board.TryGetPassenger(cell, out _));
            Assert.AreEqual(0, board.PassengerCount);
            Assert.IsTrue(board.IsEmpty);
        }

        [Test]
        public void Clone_IsIndependent()
        {
            Board board = TestBoards.From("RG");
            Board clone = board.Clone();

            clone.RemovePassenger(new GridPos(0, 0));

            Assert.IsTrue(board.TryGetPassenger(new GridPos(0, 0), out _));
            Assert.AreEqual(2, board.PassengerCount);
            Assert.AreEqual(1, clone.PassengerCount);
        }

        [Test]
        public void GridPos_EqualityAndToString()
        {
            Assert.AreEqual(new GridPos(1, 2), new GridPos(1, 2));
            Assert.IsTrue(new GridPos(1, 2) == new GridPos(1, 2));
            Assert.IsTrue(new GridPos(1, 2) != new GridPos(2, 1));
            Assert.AreEqual(new GridPos(1, 1), new GridPos(1, 2) + GridPos.Up);
            Assert.AreEqual("(1,2)", new GridPos(1, 2).ToString());
            CollectionAssert.AreEqual(new[] { GridPos.Up, GridPos.Left, GridPos.Right, GridPos.Down }, GridPos.Directions);
        }
    }
}
