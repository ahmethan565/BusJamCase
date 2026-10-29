using Game.Runtime.Feedback;
using Game.Runtime.View;

namespace Game.Runtime.Animation
{
    // A passenger gets on the bus. Waits for its turn, for the passenger's walk to end, and for a passenger
    // held back without a seat. A passenger sitting at the stop walks from its seat; otherwise it is at the door.
    public sealed class BoardStep : IStep
    {
        private readonly GameplayPresenter presenter;
        private readonly PassengerView passenger;
        private readonly BusView bus;
        private readonly int seat;
        private readonly IFeedback feedback;
        private bool started;

        public BoardStep(GameplayPresenter presenter, PassengerView passenger, BusView bus, int seat, IFeedback feedback)
        {
            this.presenter = presenter;
            this.passenger = passenger;
            this.bus = bus;
            this.seat = seat;
            this.feedback = feedback;
        }

        public bool IsDone { get; private set; }

        public void Tick()
        {
            if (started || passenger.IsWalking || presenter.IsHeldBack(passenger.Id)) return;
            started = true;

            if (presenter.LeaveSeat(passenger.Id)) passenger.MoveFromSlotTo(bus.DoorPoint, Hop);
            else Hop();
        }

        private void Hop() => passenger.HopInto(bus.DoorPoint, OnBoarded);

        private void OnBoarded()
        {
            bus.SetFilledCount(seat + 1);
            feedback.PassengerBoarded(seat);
            presenter.ReleasePassenger(passenger.Id);
            IsDone = true;
        }
    }
}
