using System.Collections.Generic;

namespace Game.Core
{
    // Does nothing. Used by the solver and by tests that do not care about events.
    public sealed class NullGameListener : IGameListener
    {
        public void OnBusArrived(Bus bus) { }
        public void OnPassengerBlocked(Passenger passenger) { }
        public void OnPassengerExited(Passenger passenger, IReadOnlyList<GridPos> path) { }
        public void OnPassengerWaiting(Passenger passenger, int slot) { }
        public void OnPassengerLeftWaitingArea(Passenger passenger, int slot) { }
        public void OnPassengerBoarded(Passenger passenger, Bus bus, int seat) { }
        public void OnBusDeparted(Bus bus) { }
        public void OnTimerStarted() { }
        public void OnTimeChanged(int secondsLeft) { }
        public void OnWon() { }
        public void OnLost(LoseReason reason) { }
    }
}
