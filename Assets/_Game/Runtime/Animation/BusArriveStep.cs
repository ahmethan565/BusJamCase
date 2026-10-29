using Game.Runtime.Feedback;
using Game.Runtime.View;
using UnityEngine;

namespace Game.Runtime.Animation
{
    // Bus drives in from the right and parks.
    public sealed class BusArriveStep : IStep
    {
        private readonly BusView bus;
        private readonly Vector3 parkPosition;
        private readonly IFeedback feedback;
        private bool started;

        public BusArriveStep(BusView bus, Vector3 parkPosition, IFeedback feedback)
        {
            this.bus = bus;
            this.parkPosition = parkPosition;
            this.feedback = feedback;
        }

        public bool IsDone { get; private set; }

        public void Tick()
        {
            if (started) return;
            started = true;
            bus.Arrive(parkPosition, OnArrived);
        }

        private void OnArrived()
        {
            feedback.BusArrived();
            IsDone = true;
        }
    }
}
