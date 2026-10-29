namespace Game.Runtime.Animation
{
    // A step in BusLane.
    public interface IStep
    {
        void Tick();          // starts its animation when ready (once)
        bool IsDone { get; }  // its own flag, set in OnComplete
    }
}
