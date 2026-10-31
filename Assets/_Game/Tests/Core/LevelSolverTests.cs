using System.Collections.Generic;
using Game.Core.Levels;
using Game.Core.Path;
using NUnit.Framework;

namespace Game.Core.Tests
{
    public sealed class LevelSolverTests
    {
        private readonly LevelSolver solver = new LevelSolver(new BfsPathFinder());

        private static GameSession Replay(LevelData level, IEnumerable<GridPos> taps, RecordingGameListener recorder)
        {
            var session = new GameSession(level, new BfsPathFinder(), recorder);
            session.Start();
            foreach (GridPos tap in taps) session.Tap(tap);
            return session;
        }

        [Test]
        public void Solve_TrivialLevel_Solvable()
        {
            LevelData level = TestSessions.Level(new[] { "RR",
                                                         "R." }, "R", capacity: 3, slots: 5);

            SolveResult result = solver.Solve(level);

            Assert.IsTrue(result.IsSolvable);
            Assert.IsFalse(result.HitLimit);
            Assert.Greater(result.StatesExplored, 0);
            Assert.AreEqual(3, result.Solution.Count);
            Assert.AreEqual(SessionResult.Won, Replay(level, result.Solution, new RecordingGameListener()).Result);
        }

        [Test]
        public void Solve_UnsolvableLevel_NotSolvable()
        {
            // Single column, R in front. Passes validation, but the bus order makes it impossible.
            var rows = new[] { "R",
                               "G",
                               "B" };
            LevelData unsolvable = TestSessions.Level(rows, "BGR", capacity: 1, slots: 1);
            Assert.IsEmpty(new LevelValidator(new BfsPathFinder()).Validate(unsolvable));

            SolveResult result = solver.Solve(unsolvable);

            Assert.IsFalse(result.IsSolvable);
            Assert.IsFalse(result.HitLimit, "the search space is tiny, the answer is a real 'no'");
            Assert.IsEmpty(result.Solution);

            Assert.IsTrue(solver.Solve(TestSessions.Level(rows, "RGB", capacity: 1, slots: 1)).IsSolvable);
        }

        [Test]
        public void Solve_TooManyStates_HitLimit()
        {
            DocumentedLevels.Entry level4 = new List<DocumentedLevels.Entry>(DocumentedLevels.All())[3];

            SolveResult result = solver.Solve(level4.Level, maxStates: 5);

            Assert.IsTrue(result.HitLimit);
            Assert.IsFalse(result.IsSolvable);
        }

        [TestCaseSource(typeof(DocumentedLevels), nameof(DocumentedLevels.All))]
        public void Solve_KnownSolutions_Replay(DocumentedLevels.Entry entry)
        {
            var recorder = new RecordingGameListener();

            GameSession session = Replay(entry.Level, entry.KnownSolution, recorder);

            Assert.AreEqual(SessionResult.Won, session.Result);
            CollectionAssert.DoesNotContain(recorder.Events, "Lost");
            Assert.IsFalse(recorder.Events.Exists(e => e.StartsWith("Blocked")), "every known tap has a path");
        }

        [TestCaseSource(typeof(DocumentedLevels), nameof(DocumentedLevels.All))]
        public void Solve_DocumentedLevels_SolverFindsWinningSolution(DocumentedLevels.Entry entry)
        {
            SolveResult result = solver.Solve(entry.Level);

            Assert.IsTrue(result.IsSolvable);
            Assert.IsFalse(result.HitLimit);
            Assert.AreEqual(SessionResult.Won, Replay(entry.Level, result.Solution, new RecordingGameListener()).Result);
        }

        [Test]
        public void EstimateRandomWinRate_SameSeed_SameResult()
        {
            LevelData level = new List<DocumentedLevels.Entry>(DocumentedLevels.All())[2].Level;

            float first = solver.EstimateRandomWinRate(level, games: 200, seed: 42);
            float second = solver.EstimateRandomWinRate(level, games: 200, seed: 42);

            Assert.AreEqual(first, second);
        }

        [Test]
        public void EstimateRandomWinRate_FirstLevel_AlwaysWon()
        {
            LevelData level1 = new List<DocumentedLevels.Entry>(DocumentedLevels.All())[0].Level;

            Assert.AreEqual(1f, solver.EstimateRandomWinRate(level1, games: 300, seed: 1));
        }

        [TestCaseSource(typeof(DocumentedLevels), nameof(DocumentedLevels.All))]
        public void EstimateRandomWinRate_DocumentedLevels_NoSoftlock(DocumentedLevels.Entry entry)
        {
            // A softlock (game not over, nobody can move) would throw.
            float rate = 0f;
            Assert.DoesNotThrow(() => rate = solver.EstimateRandomWinRate(entry.Level, games: 300, seed: 7));
            Assert.That(rate, Is.InRange(0f, 1f));
        }
    }
}
