using System;
using System.Linq;
using Game.Core.Levels;
using Game.Core.Path;
using NUnit.Framework;

namespace Game.Core.Tests
{
    // Game rules. Expected outcomes cross-checked with the Python reference simulator.
    public sealed class GameSessionTests
    {
        private RecordingGameListener recorder;

        [SetUp]
        public void SetUp() => recorder = new RecordingGameListener();

        [Test]
        public void Start_RaisesBusArrivedForFirstBus()
        {
            GameSession session = TestSessions.From(new[] { "R" }, "R", capacity: 1, slots: 1,
                                                    listener: recorder, autoStart: false);
            Assert.IsEmpty(recorder.Events);

            session.Start();

            CollectionAssert.AreEqual(new[] { "BusArrived bus0" }, recorder.Events);
            Assert.AreEqual(SessionResult.Playing, session.Result);
            Assert.IsFalse(session.IsOver);
        }

        [Test]
        public void Start_Twice_Throws()
        {
            GameSession session = TestSessions.From(new[] { "R" }, "R", capacity: 1, slots: 1);

            Assert.Throws<InvalidOperationException>(() => session.Start());
        }

        [Test]
        public void Tap_BeforeStart_Throws()
        {
            GameSession session = TestSessions.From(new[] { "R" }, "R", capacity: 1, slots: 1, autoStart: false);

            Assert.Throws<InvalidOperationException>(() => session.Tap(new GridPos(0, 0)));
        }

        [Test]
        public void Tap_EmptyCell_NoEvents()
        {
            GameSession session = TestSessions.From(new[] { "R.#-" }, "R", capacity: 1, slots: 1, listener: recorder);
            recorder.Clear();

            session.Tap(new GridPos(1, 0));   // floor
            session.Tap(new GridPos(2, 0));   // wall
            session.Tap(new GridPos(3, 0));   // void
            session.Tap(new GridPos(9, 9));   // outside the board

            Assert.IsEmpty(recorder.Events);
            Assert.AreEqual(1, session.Board.PassengerCount);
            Assert.AreEqual(SessionResult.Playing, session.Result);
        }

        [Test]
        public void Tap_BlockedPassenger_RaisesBlocked_StateUnchanged()
        {
            GameSession session = TestSessions.From(new[] { "R",
                                                            "G" }, "RG", capacity: 1, slots: 1, listener: recorder);
            recorder.Clear();

            session.Tap(new GridPos(0, 1));

            CollectionAssert.AreEqual(new[] { "Blocked 1" }, recorder.Events);
            Assert.AreEqual(2, session.Board.PassengerCount);
            Assert.IsTrue(session.Board.TryGetPassenger(new GridPos(0, 1), out _));
            Assert.IsTrue(session.WaitingArea.IsEmpty);
            Assert.AreEqual(0, session.Buses.Current.Id);
            Assert.AreEqual(0, session.Buses.Current.Count);
            Assert.AreEqual(SessionResult.Playing, session.Result);
        }

        [Test]
        public void Tap_MatchingColor_Boards()
        {
            GameSession session = TestSessions.From(new[] { "RR" }, "R", capacity: 2, slots: 1, listener: recorder);
            recorder.Clear();

            session.Tap(new GridPos(0, 0));

            CollectionAssert.AreEqual(new[] { "Exited 0", "Boarded 0 bus0 seat0" }, recorder.Events);
            CollectionAssert.AreEqual(new[] { new GridPos(0, 0) }, recorder.LastPath);
            Assert.IsFalse(session.Board.TryGetPassenger(new GridPos(0, 0), out _));
            Assert.AreEqual(1, session.Buses.Current.Count);
            Assert.IsTrue(session.WaitingArea.IsEmpty);
            Assert.AreEqual(SessionResult.Playing, session.Result);
        }

        [Test]
        public void Tap_NonMatching_GoesToLowestFreeSlot()
        {
            GameSession session = TestSessions.From(new[] { "GBR" }, "R", capacity: 1, slots: 3, listener: recorder);
            recorder.Clear();

            session.Tap(new GridPos(0, 0));
            session.Tap(new GridPos(1, 0));

            CollectionAssert.AreEqual(new[] { "Exited 0", "Waiting 0 slot0", "Exited 1", "Waiting 1 slot1" }, recorder.Events);
            Assert.AreEqual(0, session.WaitingArea.GetAt(0).Id);
            Assert.AreEqual(1, session.WaitingArea.GetAt(1).Id);
            Assert.IsNull(session.WaitingArea.GetAt(2));
            Assert.AreEqual(0, session.Buses.Current.Count);
        }

        [Test]
        public void BusFull_Departs_NextArrives_WaitingPassengersBoard()
        {
            // Red bus has one free seat, next bus is blue, one blue is waiting.
            GameSession session = TestSessions.From(new[] { "RBR",
                                                            "B.." }, "RB", capacity: 2, slots: 5, listener: recorder);
            session.Tap(new GridPos(0, 0));   // red 0 boards seat 0
            session.Tap(new GridPos(1, 0));   // blue 1 waits in slot 0
            recorder.Clear();

            session.Tap(new GridPos(2, 0));

            CollectionAssert.AreEqual(new[]
            {
                "Exited 2",
                "Boarded 2 bus0 seat1",
                "BusDeparted bus0",
                "BusArrived bus1",
                "LeftWaiting 1 slot0",
                "Boarded 1 bus1 seat0",
            }, recorder.Events);
            Assert.AreEqual(1, session.Buses.Current.Id);
            Assert.AreEqual(1, session.Buses.Current.Count);
            Assert.IsTrue(session.WaitingArea.IsEmpty);
            Assert.AreEqual(SessionResult.Playing, session.Result);
        }

        [Test]
        public void ChainBoarding_WaitingFillsNextBus_ThenNextArrives()
        {
            GameSession session = TestSessions.From(new[] { "GBR" }, "RGB", capacity: 1, slots: 3, listener: recorder);
            session.Tap(new GridPos(0, 0));   // green 0 -> slot 0
            session.Tap(new GridPos(1, 0));   // blue 1 -> slot 1
            recorder.Clear();

            session.Tap(new GridPos(2, 0));   // red 2 fills bus 0 and starts the chain

            CollectionAssert.AreEqual(new[]
            {
                "Exited 2",
                "Boarded 2 bus0 seat0",
                "BusDeparted bus0",
                "BusArrived bus1",
                "LeftWaiting 0 slot0",
                "Boarded 0 bus1 seat0",
                "BusDeparted bus1",
                "BusArrived bus2",
                "LeftWaiting 1 slot1",
                "Boarded 1 bus2 seat0",
                "BusDeparted bus2",
                "Won",
            }, recorder.Events);
            Assert.IsNull(session.Buses.Current);
            Assert.IsTrue(session.WaitingArea.IsEmpty);
            Assert.AreEqual(SessionResult.Won, session.Result);
        }

        [Test]
        public void WaitingOneSlotLeft_TapMatchingPassenger_GameContinues()
        {
            GameSession session = TestSessions.From(new[] { "GRR" }, "R", capacity: 2, slots: 2, listener: recorder);
            session.Tap(new GridPos(0, 0));   // green takes slot 0; slot 1 is still free
            Assert.AreEqual(SessionResult.Playing, session.Result);
            recorder.Clear();

            session.Tap(new GridPos(1, 0));

            CollectionAssert.AreEqual(new[] { "Exited 1", "Boarded 1 bus0 seat0" }, recorder.Events);
            Assert.AreEqual(SessionResult.Playing, session.Result);
            Assert.IsFalse(session.IsOver);
        }

        [Test]
        public void LastFreeSlotTaken_Lost_ExitedWaitingLostOrder()
        {
            GameSession session = TestSessions.From(new[] { "GBR" }, "R", capacity: 1, slots: 2, listener: recorder);
            session.Tap(new GridPos(0, 0));   // green takes slot 0
            recorder.Clear();

            session.Tap(new GridPos(1, 0));   // blue takes the last free slot

            CollectionAssert.AreEqual(new[] { "Exited 1", "Waiting 1 slot1", "Lost" }, recorder.Events);
            Assert.AreEqual(SessionResult.Lost, session.Result);
            Assert.IsTrue(session.IsOver);
            Assert.IsTrue(session.WaitingArea.IsFull);
            Assert.IsFalse(session.Board.TryGetPassenger(new GridPos(1, 0), out _), "the losing passenger has left the board");
        }

        [Test]
        public void LastPassengerBoards_Won_RaisedOnce()
        {
            GameSession session = TestSessions.From(new[] { "RR" }, "R", capacity: 2, slots: 1, listener: recorder);
            recorder.Clear();

            session.Tap(new GridPos(0, 0));
            session.Tap(new GridPos(1, 0));

            CollectionAssert.AreEqual(new[]
            {
                "Exited 0", "Boarded 0 bus0 seat0",
                "Exited 1", "Boarded 1 bus0 seat1", "BusDeparted bus0",
                "Won",
            }, recorder.Events);
            Assert.AreEqual(1, recorder.Events.Count(e => e == "Won"));
            Assert.AreEqual(SessionResult.Won, session.Result);
        }

        [Test]
        public void TapAfterOver_Ignored()
        {
            GameSession session = TestSessions.From(new[] { "GBR" }, "R", capacity: 1, slots: 1, listener: recorder);
            session.Tap(new GridPos(0, 0));   // green takes the only slot: lost
            recorder.Clear();

            session.Tap(new GridPos(2, 0));   // red could board, but the game is over

            Assert.IsEmpty(recorder.Events);
            Assert.IsTrue(session.Board.TryGetPassenger(new GridPos(2, 0), out _));
            Assert.AreEqual(SessionResult.Lost, session.Result);
        }

        [Test]
        public void TappingFrontThenBack_BothLeave()
        {
            GameSession session = TestSessions.From(new[] { "R",
                                                            "R" }, "R", capacity: 2, slots: 1, listener: recorder);
            recorder.Clear();

            session.Tap(new GridPos(0, 0));
            session.Tap(new GridPos(0, 1));   // front cell is already empty in logic

            CollectionAssert.AreEqual(new[]
            {
                "Exited 0", "Boarded 0 bus0 seat0",
                "Exited 1", "Boarded 1 bus0 seat1", "BusDeparted bus0",
                "Won",
            }, recorder.Events);
            CollectionAssert.AreEqual(new[] { new GridPos(0, 1), new GridPos(0, 0) }, recorder.LastPath);
        }

        [Test]
        public void Restart_NewSessionFromSameLevelData_IndependentState()
        {
            LevelData level = TestSessions.Level(new[] { "RG" }, "RG", capacity: 1, slots: 1);
            var first = new GameSession(level, new BfsPathFinder(), new NullGameListener());
            first.Start();
            first.Tap(new GridPos(0, 0));
            first.Tap(new GridPos(1, 0));
            Assert.AreEqual(SessionResult.Won, first.Result);

            var second = new GameSession(level, new BfsPathFinder(), recorder);
            second.Start();

            Assert.AreEqual(SessionResult.Playing, second.Result);
            Assert.AreEqual(2, second.Board.PassengerCount);
            Assert.AreEqual(0, second.Buses.Current.Id);
            Assert.AreEqual(0, second.Buses.Current.Count);
            Assert.IsTrue(second.WaitingArea.IsEmpty);
            Assert.AreEqual(2, level.Passengers.Count, "LevelData is never modified");
        }
    }
}
