using System;
using Game.Core;
using Game.Core.Levels;
using Game.Runtime.View;
using NUnit.Framework;

namespace Game.Editor.Tests
{
    // Grey board edges (§10.4): no edge on the top side of an exit cell, where passengers walk out to the road.
    public sealed class BoardEdgeTests
    {
        private static readonly GridPos Up = new GridPos(0, -1);
        private static readonly GridPos Down = new GridPos(0, 1);
        private static readonly GridPos Left = new GridPos(-1, 0);
        private static readonly GridPos Right = new GridPos(1, 0);

        private static Board BoardOf(params string[] rows)
        {
            LevelData level = LevelParser.Parse(rows, "", 1, 1, 1).Level;
            var cells = new CellType[level.Cells.Count];
            for (int i = 0; i < cells.Length; i++) cells[i] = level.Cells[i];
            return new Board(level.Width, level.Height, cells, Array.Empty<Passenger>());
        }

        [Test]
        public void TopRowFloor_NoTopEdge_SidesStay()
        {
            Board board = BoardOf(".");

            Assert.IsFalse(BoardView.HasEdge(board, new GridPos(0, 0), Up));
            Assert.IsTrue(BoardView.HasEdge(board, new GridPos(0, 0), Left));
            Assert.IsTrue(BoardView.HasEdge(board, new GridPos(0, 0), Right));
            Assert.IsTrue(BoardView.HasEdge(board, new GridPos(0, 0), Down));
        }

        [Test]
        public void ExitUnderVoidColumn_NoTopEdge()
        {
            Board board = BoardOf("-.-",
                                  "...");

            Assert.IsFalse(BoardView.HasEdge(board, new GridPos(0, 1), Up), "nothing above it: an exit");
            Assert.IsFalse(BoardView.HasEdge(board, new GridPos(1, 0), Up));
        }

        [Test]
        public void CellUnderMiddleHole_KeepsTopEdge()
        {
            Board board = BoardOf("...",
                                  ".-.",
                                  "...");

            Assert.IsTrue(BoardView.HasEdge(board, new GridPos(1, 2), Up), "a cell exists above the hole: not an exit");
        }

        [Test]
        public void TopRowWall_KeepsTopEdge()
        {
            Board board = BoardOf("#.");

            Assert.IsTrue(BoardView.HasEdge(board, new GridPos(0, 0), Up));
            Assert.IsFalse(BoardView.HasEdge(board, new GridPos(1, 0), Up));
        }

        [Test]
        public void ExitWithAPassenger_NoTopEdge()
        {
            Board board = BoardOf("R.");

            Assert.IsFalse(BoardView.HasEdge(board, new GridPos(0, 0), Up), "a passenger does not change the exit");
        }

        [Test]
        public void InsideNeighbour_NoEdge()
        {
            Board board = BoardOf("..");

            Assert.IsFalse(BoardView.HasEdge(board, new GridPos(0, 0), Right));
        }
    }
}
