using System.Linq;
using Game.Core.Levels;
using Game.Core.Path;
using NUnit.Framework;

namespace Game.Core.Tests
{
    // Level time limit. Time is handed in with Tick; no real waiting.
    public sealed class LevelTimerTests
    {
        private RecordingGameListener recorder;

        [SetUp]
        public void SetUp() => recorder = new RecordingGameListener();

        // Two reds on the top row, a red bus of 2 and two slots: tapping both wins.
        private GameSession Session(float timeLimit, string[] rows = null, string buses = "R", int capacity = 2, int slots = 2)
        {
            LevelData level = LevelParser.Parse(rows ?? new[] { "RR.", "..." }, buses, capacity, slots, 1, timeLimit).Level;
            var session = new GameSession(level, new BfsPathFinder(), recorder);
            session.Start();
            recorder.Clear();
            return session;
        }

        private int Count(string e) => recorder.Events.Count(x => x == e);

        // --- LevelTimer

        [Test]
        public void Timer_ShowsCeilingOfRemaining_AndExpiresOnce()
        {
            var timer = new LevelTimer(2f);
            Assert.IsTrue(timer.HasLimit);
            Assert.AreEqual(2, timer.RemainingWholeSeconds);
            Assert.IsFalse(timer.Tick(1f), "not running yet");

            Assert.IsTrue(timer.StartIfNotRunning());
            Assert.IsFalse(timer.StartIfNotRunning(), "already running");
            Assert.IsFalse(timer.Tick(0.4f));
            Assert.AreEqual(2, timer.RemainingWholeSeconds, "1.6 s shows as 2");
            Assert.IsTrue(timer.Tick(5f), "ran out in this tick");
            Assert.AreEqual(0f, timer.Remaining);
            Assert.IsFalse(timer.IsRunning);
            Assert.IsFalse(timer.Tick(1f));
        }

        [Test]
        public void Timer_NoLimit_NeverStarts()
        {
            var timer = new LevelTimer(0f);
            Assert.IsFalse(timer.HasLimit);
            Assert.IsFalse(timer.StartIfNotRunning());
        }

        // --- GameSession

        [Test]
        public void Tick_BeforeFirstTap_DoesNotDecreaseTime()
        {
            GameSession session = Session(30f);

            session.Tick(10f);

            Assert.AreEqual(30f, session.RemainingTime);
            Assert.IsEmpty(recorder.Events);
        }

        [Test]
        public void FirstTap_EmptyCell_StartsTimer_RaisesTimerStartedOnce()
        {
            GameSession session = Session(30f);

            session.Tap(new GridPos(2, 1));   // floor
            session.Tap(new GridPos(9, 9));   // outside the board

            Assert.AreEqual(1, Count("TimerStarted"));
            session.Tick(1f);
            Assert.AreEqual(29f, session.RemainingTime, 1e-4f);
        }

        [Test]
        public void FirstTap_BlockedPassenger_StartsTimer()
        {
            GameSession session = Session(30f, rows: new[] { "R#.", "R#." }, buses: "R");

            session.Tap(new GridPos(0, 1));   // blocked: the red in front, a wall beside

            CollectionAssert.AreEqual(new[] { "TimerStarted", "Blocked 1" }, recorder.Events);
        }

        [Test]
        public void Tick_RaisesTimeChanged_OnlyOnWholeSecondChange()
        {
            GameSession session = Session(30f);
            session.Tap(new GridPos(2, 1));
            recorder.Clear();

            session.Tick(0.3f);   // 29.7 -> shows 30
            session.Tick(0.3f);   // 29.4 -> 30
            session.Tick(0.5f);   // 28.9 -> 29

            CollectionAssert.AreEqual(new[] { "Time 29" }, recorder.Events);
        }

        [Test]
        public void TimeRunsOut_LostWithTimeUp_RaisedOnce()
        {
            GameSession session = Session(3f);
            session.Tap(new GridPos(2, 1));
            recorder.Clear();

            session.Tick(2f);
            session.Tick(2f);
            session.Tick(2f);

            CollectionAssert.AreEqual(new[] { "Time 1", "Time 0", "Lost TimeUp" }, recorder.Events);
            Assert.AreEqual(SessionResult.Lost, session.Result);
            Assert.AreEqual(LoseReason.TimeUp, recorder.LastLoseReason);
        }

        [Test]
        public void Win_StopsTimer_LaterTickDoesNotLose()
        {
            GameSession session = Session(5f);
            session.Tap(new GridPos(0, 0));
            session.Tap(new GridPos(1, 0));   // second red fills the bus: won
            Assert.AreEqual(SessionResult.Won, session.Result);
            recorder.Clear();

            session.Tick(10f);

            Assert.IsEmpty(recorder.Events);
            Assert.AreEqual(SessionResult.Won, session.Result);
        }

        [Test]
        public void TapAfterTimeUp_Ignored()
        {
            GameSession session = Session(1f);
            session.Tap(new GridPos(2, 1));
            session.Tick(2f);
            recorder.Clear();

            session.Tap(new GridPos(0, 0));

            Assert.IsEmpty(recorder.Events);
            Assert.IsTrue(session.Board.TryGetPassenger(new GridPos(0, 0), out _));
        }

        [Test]
        public void NoLimitLevel_NeverLosesByTime_NoTimerEvents()
        {
            GameSession session = Session(0f);

            session.Tap(new GridPos(2, 1));
            session.Tick(1000f);

            Assert.IsEmpty(recorder.Events);
            Assert.AreEqual(SessionResult.Playing, session.Result);
        }

        [Test]
        public void Restart_NewSession_TimerWaitsForFirstTapAgain()
        {
            LevelData level = LevelParser.Parse(new[] { "RR.", "..." }, "R", 2, 2, 1, 10f).Level;
            var first = new GameSession(level, new BfsPathFinder(), new NullGameListener());
            first.Start();
            first.Tap(new GridPos(2, 1));
            first.Tick(4f);

            var second = new GameSession(level, new BfsPathFinder(), recorder);
            second.Start();
            second.Tick(4f);

            Assert.AreEqual(10f, second.RemainingTime, "fresh timer, still waiting for the first tap");
            Assert.AreEqual(6f, first.RemainingTime, 1e-4f);
        }

        [Test]
        public void NoSpaceLoss_ReportsNoSpace()
        {
            GameSession session = Session(30f, rows: new[] { "GB.", "R.." }, buses: "R", capacity: 1, slots: 2);
            session.Tap(new GridPos(0, 0));   // green -> slot 0
            session.Tap(new GridPos(1, 0));   // blue takes the last slot: lost

            Assert.AreEqual(SessionResult.Lost, session.Result);
            Assert.AreEqual(LoseReason.NoSpace, recorder.LastLoseReason);
            session.Tick(100f);
            Assert.AreEqual(0, Count("Lost TimeUp"), "the timer stopped with the loss");
        }

        // --- Level data

        [Test]
        public void Parse_TimeLimit_DefaultsToZero()
        {
            Assert.AreEqual(0f, LevelParser.Parse(new[] { "R" }, "R", 1, 1, 1).Level.TimeLimitSeconds);
            Assert.AreEqual(45f, LevelParser.Parse(new[] { "R" }, "R", 1, 1, 1, 45f).Level.TimeLimitSeconds);
        }

        [Test]
        public void Validate_NegativeTimeLimit_Error()
        {
            LevelData level = LevelParser.Parse(new[] { "R" }, "R", 1, 1, 1, -5f).Level;

            Assert.IsNotEmpty(new LevelValidator(new BfsPathFinder()).Validate(level));
        }

        [Test]
        public void Solver_IgnoresTime()
        {
            LevelData level = LevelParser.Parse(new[] { "RR.", "..." }, "R", 2, 2, 1, 1f).Level;

            Assert.IsTrue(new LevelSolver(new BfsPathFinder()).Solve(level).IsSolvable);
        }
    }
}
