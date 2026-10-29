using UnityEngine;

namespace Game.Runtime.Feedback
{
    // Sound/effects, triggered at animation moments (not at Core events, §13.1).
    public interface IFeedback
    {
        void PassengerTapped(Vector3 at);
        void PassengerBlocked(Vector3 at);
        void PassengerBoarded(int seat);
        void PassengerWaiting();
        void BusDeparted(Vector3 at);
        void BusArrived();
        void Won();
        void Lost();
        void UiClick();
    }
}
