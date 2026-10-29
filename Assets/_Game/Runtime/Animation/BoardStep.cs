using Game.Runtime.Feedback;
using Game.Runtime.View;

namespace Game.Runtime.Animation
{
    // A passenger gets on the bus. NOT in the BusLane: boardings run in parallel (§10.8). Starts as soon as the bus
    // is parked and the passenger is ready (walk finished, not held back), so nobody waits at the door for anyone.
    // A passenger sitting at the stop runs from its seat; otherwise it is already at the door.
    public sealed class BoardStep : IStep
    {
        private readonly GameplayPresenter presenter;
        private readonly PassengerView passenger;
        private readonly BusView bus;
        private readonly IFeedback feedback;
        private bool started;

        public BoardStep(GameplayPresenter presenter, PassengerView passenger, BusView bus, IFeedback feedback)
        {
            this.presenter = presenter;
            this.passenger = passenger;
            this.bus = bus;
            this.feedback = feedback;
        }

        public bool IsDone { get; private set; }

        public void Tick()
        {
            if (started || !bus.IsParked || passenger.IsWalking || presenter.IsHeldBack(passenger.Id)) return;
            started = true;

            if (presenter.LeaveSeat(passenger.Id)) passenger.MoveFromSlotTo(bus.DoorPoint, Hop);
            else Hop();
        }

        private void Hop() => passenger.HopInto(bus.DoorPoint, OnBoarded);

        private void OnBoarded()
        {
            int boarded = bus.CompleteBoarding();   // out of seat order: count who is in
            bus.SetFilledCount(boarded);
            feedback.PassengerBoarded(boarded - 1);
            presenter.ReleasePassenger(passenger.Id);
            IsDone = true;
        }
    }
}
