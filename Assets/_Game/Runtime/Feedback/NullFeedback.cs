using UnityEngine;

namespace Game.Runtime.Feedback
{
    // Silent IFeedback for Phases 3-5 and tests.
    public sealed class NullFeedback : IFeedback
    {
        public void PassengerTapped(Vector3 at) { }
        public void PassengerBlocked(Vector3 at) { }
        public void PassengerBoarded(int seat) { }
        public void PassengerWaiting() { }
        public void BusDeparted(Vector3 at) { }
        public void BusArrived() { }
        public void Won() { }
        public void Lost() { }
        public void UiClick() { }
    }
}
