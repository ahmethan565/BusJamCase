namespace Game.Runtime.Flow
{
    // State factory (§11.4): holds the shared dependencies and creates a fresh state object for every change.
    public sealed class AppStates
    {
        private readonly AppStateMachine machine;
        private readonly AppDependencies deps;

        public AppStates(AppStateMachine machine, AppDependencies deps)
        {
            this.machine = machine;
            this.deps = deps;
        }

        public IAppState Menu() => new MenuState(machine, this, deps);
        public IAppState Gameplay(int levelIndex) => new GameplayState(machine, this, deps, levelIndex);
        public IAppState Win(int levelIndex) => new WinState(machine, this, deps, levelIndex);
        public IAppState Lose(int levelIndex) => new LoseState(machine, this, deps, levelIndex);
    }
}
