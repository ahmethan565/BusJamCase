namespace Game.Runtime.Flow
{
    // One app screen state (§11.3). Exactly one is active at a time.
    public interface IAppState
    {
        void Enter();
        void Exit();
    }
}
