namespace Game.Runtime.Flow
{
    public sealed class WinState : IAppState
    {
        private readonly AppStateMachine machine;
        private readonly AppStates states;
        private readonly AppDependencies deps;
        private readonly int levelIndex;

        public WinState(AppStateMachine machine, AppStates states, AppDependencies deps, int levelIndex)
        {
            this.machine = machine;
            this.states = states;
            this.deps = deps;
            this.levelIndex = levelIndex;
        }

        public void Enter()
        {
            deps.Progress.MarkCompleted(levelIndex);
            // On the last level "Next" is hidden and "all levels completed" is shown.
            deps.WinScreen.Show(showNextButton: deps.Catalog.HasNext(levelIndex));
            deps.Feedback.Won();

            deps.WinScreen.NextClicked += OnNextClicked;
            deps.WinScreen.MenuClicked += GoToMenu;
            deps.BackButton.BackPressed += GoToMenu;
        }

        public void Exit()
        {
            deps.WinScreen.NextClicked -= OnNextClicked;
            deps.WinScreen.MenuClicked -= GoToMenu;
            deps.BackButton.BackPressed -= GoToMenu;
            deps.WinScreen.Hide();
        }

        private void OnNextClicked() => machine.ChangeTo(states.Gameplay(levelIndex + 1));

        private void GoToMenu() => machine.ChangeTo(states.Menu());
    }
}
