using Game.Runtime.Feedback;
using Game.Runtime.View;

namespace Game.Runtime.Animation
{
    // A passenger gets on the bus. Waits for its turn AND for the passenger's walk to end.
    public sealed class BoardStep : IStep
    {
        private readonly GameplayPresenter presenter;
        private readonly PassengerView passenger;
        private readonly BusView bus;
        private readonly int seat;
        private readonly bool fromSlot;
        private readonly IFeedback feedback;
        private bool started;

        public BoardStep(GameplayPresenter presenter, PassengerView passenger, BusView bus,
                         int seat, bool fromSlot, IFeedback feedback)
        {
            this.presenter = presenter;
            this.passenger = passenger;
            this.bus = bus;
            this.seat = seat;
            this.fromSlot = fromSlot;
            this.feedback = feedback;
        }

        public bool IsDone { get; private set; }

        public void Tick()
        {
            if (started || passenger.IsWalking) return;   // do not start before the walk ends
            started = true;

            if (fromSlot) passenger.MoveFromSlotTo(bus.DoorPoint, Hop);
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
