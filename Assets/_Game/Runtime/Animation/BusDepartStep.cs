using Game.Runtime.Feedback;
using Game.Runtime.View;
using UnityEngine;

namespace Game.Runtime.Animation
{
    // A full bus drives away to the left and goes back to the pool, once everyone boarding it is in.
    public sealed class BusDepartStep : IStep
    {
        private readonly GameplayPresenter presenter;
        private readonly BusView bus;
        private readonly Vector3 exitPosition;
        private readonly IFeedback feedback;
        private bool started;

        public BusDepartStep(GameplayPresenter presenter, BusView bus, Vector3 exitPosition, IFeedback feedback)
        {
            this.presenter = presenter;
            this.bus = bus;
            this.exitPosition = exitPosition;
            this.feedback = feedback;
        }

        public bool IsDone { get; private set; }

        public void Tick()
        {
            if (started || bus.PendingBoardings > 0) return;
            started = true;
            feedback.BusDeparted(bus.transform.position);
            bus.Depart(exitPosition, OnDeparted);
        }

        private void OnDeparted()
        {
            presenter.ReleaseBus(bus.Id);
            IsDone = true;
        }
    }
}
