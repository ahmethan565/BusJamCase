using System.Collections.Generic;

namespace Game.Core
{
    public sealed class BusQueue
    {
        private readonly List<Bus> buses;
        private int currentIndex;

        public BusQueue(IEnumerable<Bus> buses)
        {
            this.buses = new List<Bus>(buses);
            Current = this.buses.Count > 0 ? this.buses[0] : null;
        }

        public Bus Current { get; private set; }   // null when the queue is finished
        public int RemainingCount => buses.Count - currentIndex;   // includes Current

        public void Advance()
        {
            if (currentIndex < buses.Count) currentIndex++;
            Current = currentIndex < buses.Count ? buses[currentIndex] : null;
        }

        public BusQueue Clone()
        {
            var copies = new List<Bus>(buses.Count);
            foreach (Bus bus in buses) copies.Add(bus.Clone());
            var clone = new BusQueue(copies) { currentIndex = currentIndex };
            clone.Current = currentIndex < copies.Count ? copies[currentIndex] : null;
            return clone;
        }
    }
}
