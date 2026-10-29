using System;

namespace Game.Core
{
    public sealed class Bus
    {
        public Bus(int id, PassengerColor color, int capacity)
        {
            Id = id;
            Color = color;
            Capacity = capacity;
        }

        public int Id { get; }                 // index in the queue; used to map views
        public PassengerColor Color { get; }
        public int Capacity { get; }
        public int Count { get; private set; }
        public bool IsFull => Count == Capacity;

        public bool CanBoard(Passenger p) => !IsFull && p.Color == Color;

        // Returns the seat index (0..Capacity-1).
        public int Add(Passenger p)
        {
            if (!CanBoard(p))
                throw new InvalidOperationException($"Passenger {p.Id} cannot board bus {Id}.");
            return Count++;
        }

        public Bus Clone() => new Bus(Id, Color, Capacity) { Count = Count };
    }
}
