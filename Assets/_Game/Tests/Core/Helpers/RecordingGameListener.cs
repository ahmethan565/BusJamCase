using System.Collections.Generic;

namespace Game.Core.Tests
{
    // Writes every event as a short string, in the order it was raised.
    public sealed class RecordingGameListener : IGameListener
    {
        public List<string> Events { get; } = new List<string>();
        public IReadOnlyList<GridPos> LastPath { get; private set; }

        public void Clear() => Events.Clear();

        public void OnBusArrived(Bus bus) => Events.Add($"BusArrived bus{bus.Id}");
        public void OnPassengerBlocked(Passenger passenger) => Events.Add($"Blocked {passenger.Id}");

        public void OnPassengerExited(Passenger passenger, IReadOnlyList<GridPos> path)
        {
            LastPath = path;
            Events.Add($"Exited {passenger.Id}");
        }

        public void OnPassengerWaiting(Passenger passenger, int slot) => Events.Add($"Waiting {passenger.Id} slot{slot}");
        public void OnPassengerLeftWaitingArea(Passenger passenger, int slot) => Events.Add($"LeftWaiting {passenger.Id} slot{slot}");
        public void OnPassengerBoarded(Passenger passenger, Bus bus, int seat) => Events.Add($"Boarded {passenger.Id} bus{bus.Id} seat{seat}");
        public void OnBusDeparted(Bus bus) => Events.Add($"BusDeparted bus{bus.Id}");
        public void OnWon() => Events.Add("Won");
        public void OnLost() => Events.Add("Lost");
    }
}
