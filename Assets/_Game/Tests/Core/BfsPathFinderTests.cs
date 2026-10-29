using System;
using System.Collections.Generic;
using Game.Core.Path;
using NUnit.Framework;

namespace Game.Core.Tests
{
    public sealed class BfsPathFinderTests
    {
        private readonly BfsPathFinder pathFinder = new BfsPathFinder();

        [Test]
        public void PassengerOnExit_PathIsSingleCell()
        {
            Board board = TestBoards.From("R.",
                                          "..");

            Assert.IsTrue(pathFinder.TryFindPathToExit(board, new GridPos(0, 0), out List<GridPos> path));
            CollectionAssert.AreEqual(new[] { new GridPos(0, 0) }, path);
        }

        [Test]
        public void BlockedByPassengers_NoPath()
        {
            Board board = TestBoards.From("GGG",
                                          "GRG");

            Assert.IsFalse(pathFinder.TryFindPathToExit(board, new GridPos(1, 1), out _));
        }

        [Test]
        public void RoutesAroundWall()
        {
            Board board = TestBoards.From("..",
                                          "#.",
                                          "R.");

            Assert.IsTrue(pathFinder.TryFindPathToExit(board, new GridPos(0, 2), out List<GridPos> path));
            CollectionAssert.AreEqual(
                new[] { new GridPos(0, 2), new GridPos(1, 2), new GridPos(1, 1), new GridPos(1, 0) }, path);
        }

        [Test]
        public void CannotPassThroughVoid()
        {
            Board board = TestBoards.From("#-.",
                                          "R-.");

            Assert.IsFalse(pathFinder.TryFindPathToExit(board, new GridPos(0, 1), out _));
        }

        [Test]
        public void PathStartsAtPassengerAndEndsAtExit()
        {
            Board board = TestBoards.From("#..#",
                                          "...G",
                                          ".#..",
                                          "R...");
            var start = new GridPos(0, 3);

            Assert.IsTrue(pathFinder.TryFindPathToExit(board, start, out List<GridPos> path));

            Assert.AreEqual(start, path[0]);
            Assert.IsTrue(board.IsExit(path[path.Count - 1]));
            for (int i = 1; i < path.Count; i++)
            {
                GridPos a = path[i - 1], b = path[i];
                Assert.AreEqual(1, Math.Abs(a.X - b.X) + Math.Abs(a.Y - b.Y), "steps must be 4-neighbours");
                Assert.IsTrue(board.IsWalkable(b), $"{b} must be walkable");
            }
        }

        [Test]
        public void ShortestPath_IsFound()
        {
            Board board = TestBoards.From("....",
                                          "....",
                                          "R...");

            Assert.IsTrue(pathFinder.TryFindPathToExit(board, new GridPos(0, 2), out List<GridPos> path));
            CollectionAssert.AreEqual(new[] { new GridPos(0, 2), new GridPos(0, 1), new GridPos(0, 0) }, path);
        }

        [Test]
        public void EachSearch_ReturnsNewList()
        {
            Board board = TestBoards.From(".", "R");
            pathFinder.TryFindPathToExit(board, new GridPos(0, 1), out List<GridPos> first);
            pathFinder.TryFindPathToExit(board, new GridPos(0, 1), out List<GridPos> second);

            Assert.AreNotSame(first, second);
            CollectionAssert.AreEqual(first, second);
        }

        [Test]
        public void BoardSizeChange_StillWorks()
        {
            Assert.IsTrue(pathFinder.TryFindPathToExit(TestBoards.From(".", "R"), new GridPos(0, 1), out _));
            Assert.IsTrue(pathFinder.TryFindPathToExit(TestBoards.From("#...", "....", "#..R"), new GridPos(3, 2), out List<GridPos> path));
            Assert.AreEqual(3, path.Count);
        }

        [Test]
        public void ReachableFromExits_IgnorePassengers_FindsIsland()
        {
            Board board = TestBoards.From("R.#",
                                          "##.",
                                          "-#B");

            HashSet<GridPos> reachable = pathFinder.ReachableFromExits(board, ignorePassengers: true);

            Assert.IsTrue(reachable.Contains(new GridPos(0, 0)), "passenger on exit is reachable");
            Assert.IsTrue(reachable.Contains(new GridPos(1, 0)));
            Assert.IsFalse(reachable.Contains(new GridPos(2, 1)), "island cell");
            Assert.IsFalse(reachable.Contains(new GridPos(2, 2)), "island passenger");
            Assert.AreEqual(2, reachable.Count);
        }

        [Test]
        public void ReachableFromExits_WithPassengers_TreatsThemAsBlocking()
        {
            Board board = TestBoards.From("R",
                                          ".");

            Assert.IsTrue(pathFinder.ReachableFromExits(board, ignorePassengers: true).Contains(new GridPos(0, 1)));
            Assert.IsFalse(pathFinder.ReachableFromExits(board, ignorePassengers: false).Contains(new GridPos(0, 1)));
        }
    }
}
