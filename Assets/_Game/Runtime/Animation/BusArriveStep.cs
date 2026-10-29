using Game.Runtime.Feedback;
using Game.Runtime.View;
using UnityEngine;

namespace Game.Runtime.Animation
{
    // The bus moves up from the queue (or drives in from the right) and parks; the next bus, if any,
    // moves up into the queue behind it at the same time.
    public sealed class BusArriveStep : IStep
    {
        private readonly BusView bus;
        private readonly Vector3 parkPosition;
        private readonly BusView follower;
        private readonly Vector3 queuePosition;
        private readonly IFeedback feedback;
        private bool started;

        public BusArriveStep(BusView bus, Vector3 parkPosition, BusView follower, Vector3 queuePosition, IFeedback feedback)
        {
            this.bus = bus;
            this.parkPosition = parkPosition;
            this.follower = follower;
            this.queuePosition = queuePosition;
            this.feedback = feedback;
        }

        public bool IsDone { get; private set; }

        public void Tick()
        {
            if (started) return;
            started = true;
            bus.Arrive(parkPosition, OnArrived);
            if (follower != null) follower.DriveTo(queuePosition);
        }

        private void OnArrived()
        {
            feedback.BusArrived();
            IsDone = true;
        }
    }
}
