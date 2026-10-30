using System.Collections.Generic;

namespace Game.Core
{
    // Core -> outside world. Events are synchronous and raised in the order they happen in logic.
    public interface IGameListener
    {
        void OnBusArrived(Bus bus);
        void OnPassengerBlocked(Passenger passenger);
        void OnPassengerExited(Passenger passenger, IReadOnlyList<GridPos> path);
        void OnPassengerWaiting(Passenger passenger, int slot);
        void OnPassengerLeftWaitingArea(Passenger passenger, int slot);
        void OnPassengerBoarded(Passenger passenger, Bus bus, int seat);
        void OnBusDeparted(Bus bus);
        void OnTimerStarted();
        void OnTimeChanged(int secondsLeft);   // only when the whole second shown on screen changes
        void OnWon();
        void OnLost(LoseReason reason);
    }
}
