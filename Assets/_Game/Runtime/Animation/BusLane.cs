using System.Collections.Generic;

namespace Game.Runtime.Animation
{
    // The only sequential animation queue: bus arrivals, boardings and departures play in logic order.
    public sealed class BusLane
    {
        private readonly Queue<IStep> steps = new Queue<IStep>();

        public bool IsIdle => steps.Count == 0;
        public void Enqueue(IStep step) => steps.Enqueue(step);
        public void Clear() => steps.Clear();

        public void Tick()
        {
            while (steps.Count > 0)
            {
                IStep step = steps.Peek();
                step.Tick();
                if (!step.IsDone) return;
                steps.Dequeue();
            }
        }
    }
}
