namespace Game.Runtime.Flow
{
    public sealed class LoseState : IAppState
    {
        private readonly AppStateMachine machine;
        private readonly AppStates states;
        private readonly AppDependencies deps;
        private readonly int levelIndex;

        public LoseState(AppStateMachine machine, AppStates states, AppDependencies deps, int levelIndex)
        {
            this.machine = machine;
            this.states = states;
            this.deps = deps;
            this.levelIndex = levelIndex;
        }

        public void Enter()
        {
            deps.LoseScreen.Show();
            deps.Feedback.Lost();

            deps.LoseScreen.RetryClicked += OnRetryClicked;
            deps.LoseScreen.MenuClicked += GoToMenu;
            deps.BackButton.BackPressed += GoToMenu;
        }

        public void Exit()
        {
            deps.LoseScreen.RetryClicked -= OnRetryClicked;
            deps.LoseScreen.MenuClicked -= GoToMenu;
            deps.BackButton.BackPressed -= GoToMenu;
            deps.LoseScreen.Hide();
        }

        private void OnRetryClicked() => machine.ChangeTo(states.Gameplay(levelIndex));

        private void GoToMenu() => machine.ChangeTo(states.Menu());
    }
}
