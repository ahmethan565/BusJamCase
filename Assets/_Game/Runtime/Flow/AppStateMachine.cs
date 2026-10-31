namespace Game.Runtime.Flow
{
    public sealed class AppStateMachine
    {
        private IAppState current;

        // Synchronous: no frame is drawn between Exit and Enter.
        public void ChangeTo(IAppState next)
        {
            current?.Exit();
            current = next;
            current.Enter();
        }
    }
}
