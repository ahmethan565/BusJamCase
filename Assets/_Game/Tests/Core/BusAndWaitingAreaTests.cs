using System;
using NUnit.Framework;

namespace Game.Core.Tests
{
    public sealed class BusAndWaitingAreaTests
    {
        private static Passenger P(int id, PassengerColor color) => new Passenger(id, color, new GridPos(0, 0));

        [Test]
        public void Bus_CanBoard_WrongColor_False()
        {
            var bus = new Bus(0, PassengerColor.Red, 3);

            Assert.IsFalse(bus.CanBoard(P(0, PassengerColor.Blue)));
            Assert.IsTrue(bus.CanBoard(P(1, PassengerColor.Red)));
        }

        [Test]
        public void Bus_Add_WhenFull_Throws()
        {
            var bus = new Bus(0, PassengerColor.Red, 1);
            bus.Add(P(0, PassengerColor.Red));

            Assert.IsTrue(bus.IsFull);
            Assert.IsFalse(bus.CanBoard(P(1, PassengerColor.Red)));
            Assert.Throws<InvalidOperationException>(() => bus.Add(P(1, PassengerColor.Red)));
            Assert.AreEqual(1, bus.Count);
        }

        [Test]
        public void Bus_Add_WrongColor_Throws()
        {
            var bus = new Bus(0, PassengerColor.Red, 3);

            Assert.Throws<InvalidOperationException>(() => bus.Add(P(0, PassengerColor.Green)));
            Assert.AreEqual(0, bus.Count);
        }

        [Test]
        public void Bus_Add_ReturnsSeatIndex()
        {
            var bus = new Bus(4, PassengerColor.Red, 3);

            Assert.AreEqual(0, bus.Add(P(0, PassengerColor.Red)));
            Assert.AreEqual(1, bus.Add(P(1, PassengerColor.Red)));
            Assert.IsFalse(bus.IsFull);
            Assert.AreEqual(2, bus.Add(P(2, PassengerColor.Red)));
            Assert.IsTrue(bus.IsFull);
            Assert.AreEqual(4, bus.Id);
            Assert.AreEqual(3, bus.Capacity);
        }

        [Test]
        public void Bus_Clone_IsIndependent()
        {
            var bus = new Bus(0, PassengerColor.Red, 3);
            bus.Add(P(0, PassengerColor.Red));
            Bus clone = bus.Clone();

            clone.Add(P(1, PassengerColor.Red));

            Assert.AreEqual(1, bus.Count);
            Assert.AreEqual(2, clone.Count);
            Assert.AreEqual(bus.Id, clone.Id);
            Assert.AreEqual(bus.Color, clone.Color);
        }

        [Test]
        public void BusQueue_Advance_PastLast_CurrentNull()
        {
            var queue = new BusQueue(new[] { new Bus(0, PassengerColor.Red, 1), new Bus(1, PassengerColor.Blue, 1) });

            Assert.AreEqual(0, queue.Current.Id);
            Assert.AreEqual(2, queue.RemainingCount);

            queue.Advance();
            Assert.AreEqual(1, queue.Current.Id);
            Assert.AreEqual(1, queue.RemainingCount);

            queue.Advance();
            Assert.IsNull(queue.Current);
            Assert.AreEqual(0, queue.RemainingCount);

            queue.Advance();
            Assert.IsNull(queue.Current);
            Assert.AreEqual(0, queue.RemainingCount);
        }

        [Test]
        public void BusQueue_Empty_CurrentNull()
        {
            var queue = new BusQueue(new Bus[0]);

            Assert.IsNull(queue.Current);
            Assert.AreEqual(0, queue.RemainingCount);
        }

        [Test]
        public void BusQueue_Clone_IsIndependent()
        {
            var queue = new BusQueue(new[] { new Bus(0, PassengerColor.Red, 2), new Bus(1, PassengerColor.Blue, 1) });
            BusQueue clone = queue.Clone();

            clone.Current.Add(P(0, PassengerColor.Red));
            clone.Advance();

            Assert.AreEqual(0, queue.Current.Id);
            Assert.AreEqual(0, queue.Current.Count);
            Assert.AreEqual(1, clone.Current.Id);
        }

        [Test]
        public void WaitingArea_TryAdd_UsesLowestFreeSlot()
        {
            var area = new WaitingArea(3);

            Assert.IsTrue(area.IsEmpty);
            Assert.IsTrue(area.TryAdd(P(0, PassengerColor.Red), out int slot0));
            Assert.IsTrue(area.TryAdd(P(1, PassengerColor.Blue), out int slot1));
            Assert.IsTrue(area.TryAdd(P(2, PassengerColor.Green), out int slot2));
            Assert.AreEqual(new[] { 0, 1, 2 }, new[] { slot0, slot1, slot2 });

            Assert.IsTrue(area.TryTake(PassengerColor.Blue, out _, out _));
            Assert.IsTrue(area.TryAdd(P(3, PassengerColor.Yellow), out int reused));

            Assert.AreEqual(1, reused);
            Assert.AreEqual(3, area.GetAt(1).Id);
            Assert.AreEqual(3, area.SlotCount);
        }

        [Test]
        public void WaitingArea_TryTake_LowestMatchingSlot_OthersDoNotShift()
        {
            var area = new WaitingArea(3);
            area.TryAdd(P(0, PassengerColor.Red), out _);
            area.TryAdd(P(1, PassengerColor.Blue), out _);
            area.TryAdd(P(2, PassengerColor.Red), out _);

            Assert.IsTrue(area.TryTake(PassengerColor.Red, out Passenger first, out int firstSlot));
            Assert.AreEqual(0, first.Id);
            Assert.AreEqual(0, firstSlot);
            Assert.IsNull(area.GetAt(0));
            Assert.AreEqual(1, area.GetAt(1).Id);
            Assert.AreEqual(2, area.GetAt(2).Id);

            Assert.IsTrue(area.TryTake(PassengerColor.Red, out Passenger second, out int secondSlot));
            Assert.AreEqual(2, second.Id);
            Assert.AreEqual(2, secondSlot);
            Assert.AreEqual(1, area.GetAt(1).Id);

            Assert.IsFalse(area.TryTake(PassengerColor.Red, out _, out _));
        }

        [Test]
        public void WaitingArea_Full_TryAddFalse()
        {
            var area = new WaitingArea(2);
            area.TryAdd(P(0, PassengerColor.Red), out _);
            Assert.IsFalse(area.IsFull);
            area.TryAdd(P(1, PassengerColor.Blue), out _);

            Assert.IsTrue(area.IsFull);
            Assert.IsFalse(area.IsEmpty);
            Assert.IsFalse(area.TryAdd(P(2, PassengerColor.Green), out _));
        }

        [Test]
        public void WaitingArea_Clone_IsIndependent()
        {
            var area = new WaitingArea(2);
            area.TryAdd(P(0, PassengerColor.Red), out _);
            WaitingArea clone = area.Clone();

            clone.TryTake(PassengerColor.Red, out _, out _);

            Assert.AreEqual(0, area.GetAt(0).Id);
            Assert.IsTrue(clone.IsEmpty);
        }
    }
}
