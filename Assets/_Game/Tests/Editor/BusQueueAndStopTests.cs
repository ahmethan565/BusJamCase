using Game.Core;
using Game.Core.Levels;
using Game.Runtime.Data;
using Game.Runtime.View;
using NUnit.Framework;
using UnityEngine;

namespace Game.Editor.Tests
{
    // The visible bus queue and the on-screen seats at the stop.
    public sealed class BusQueueAndStopTests
    {
        private const float BusLength = 2.4f;

        // --- Bus queue: the next bus waits right behind the parked one

        [Test]
        public void LayoutSettings_QueueOffsetDefault()
        {
            var s = ScriptableObject.CreateInstance<LayoutSettings>();
            Assert.AreEqual(2.7f, s.BusQueueOffsetX);
            Object.DestroyImmediate(s);
        }

        [Test]
        public void QueuedBus_WaitsBehindTheParkedBus_WithoutTouchingIt()
        {
            var settings = ScriptableObject.CreateInstance<LayoutSettings>();
            LevelData level = LevelParser.Parse(new[] { "RRR" }, "R", 3, 5, 1).Level;
            var layout = new BoardLayout(level, settings);

            Vector3 park = layout.BusParkPosition;
            Vector3 queue = layout.BusQueuePosition;
            Assert.AreEqual(park.z, queue.z, 1e-4f, "same lane");
            Assert.Greater(queue.x - park.x, BusLength, "behind it (to the right) with a gap");
            Assert.Less(queue.x, layout.BusEnterPosition.x, "nearer than the off-screen start");
            Assert.AreEqual(layout.BusEnterPosition.x + (queue.x - park.x), layout.BusQueueEnterPosition.x, 1e-4f,
                            "a follower enters one queue offset behind the leader");
            Object.DestroyImmediate(settings);
        }

        // --- Stop seats: who sits where on screen

        [Test]
        public void Seats_PreferredSlotIsUsedWhenFree()
        {
            var seats = new StopSeats();
            seats.Reset(5);

            Assert.AreEqual(3, seats.Take(passengerId: 7, preferred: 3));
            Assert.IsTrue(seats.IsSeated(7));
        }

        [Test]
        public void Seats_TakenPreferredSlot_FallsBackToLowestFree()
        {
            var seats = new StopSeats();
            seats.Reset(5);
            seats.Take(1, preferred: 0);
            seats.Take(2, preferred: 1);

            Assert.AreEqual(2, seats.Take(3, preferred: 0), "slot 0 is still occupied on screen");
            Assert.AreEqual(3, seats.Take(4, preferred: -1), "no preference: lowest free");
        }

        [Test]
        public void Seats_Full_ReturnsMinusOne()
        {
            var seats = new StopSeats();
            seats.Reset(2);
            seats.Take(1, -1);
            seats.Take(2, -1);

            Assert.AreEqual(-1, seats.Take(3, -1));
            Assert.IsFalse(seats.IsSeated(3));
        }

        [Test]
        public void Seats_Release_FreesTheSlotForTheNextPassenger()
        {
            var seats = new StopSeats();
            seats.Reset(2);
            seats.Take(1, -1);
            seats.Take(2, -1);

            Assert.IsTrue(seats.TryRelease(1, out int freed));
            Assert.AreEqual(0, freed);
            Assert.IsFalse(seats.IsSeated(1));
            Assert.IsFalse(seats.TryRelease(1, out _), "only once");
            Assert.AreEqual(0, seats.Take(3, -1));
        }

        [Test]
        public void Seats_Reset_EmptiesEverySlot()
        {
            var seats = new StopSeats();
            seats.Reset(3);
            seats.Take(1, -1);

            seats.Reset(3);

            Assert.IsFalse(seats.IsSeated(1));
            Assert.AreEqual(0, seats.Take(2, -1));
        }
    }
}
