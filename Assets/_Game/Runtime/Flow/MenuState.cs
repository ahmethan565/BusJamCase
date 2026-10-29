using UnityEngine;

namespace Game.Runtime.Flow
{
    // The level map. The quit confirmation is an overlay owned by this state, not a state of its own (§11.7).
    public sealed class MenuState : IAppState
    {
        private readonly AppStateMachine machine;
        private readonly AppStates states;
        private readonly AppDependencies deps;
        private bool quitOpen;

        public MenuState(AppStateMachine machine, AppStates states, AppDependencies deps)
        {
            this.machine = machine;
            this.states = states;
            this.deps = deps;
        }

        public void Enter()
        {
            // The previous level's board stayed visible behind the popup; it is cleared here (§11.6).
            deps.Presenter.Clear();
            deps.BoardView.Clear();
            deps.WaitingAreaView.Clear();

            deps.MapScreen.Show(deps.Progress);
            deps.MapScreen.LevelSelected += OnLevelSelected;
            deps.BackButton.BackPressed += OnBackPressed;
            deps.QuitConfirmScreen.ConfirmClicked += OnQuitConfirmed;
            deps.QuitConfirmScreen.CancelClicked += CloseQuitConfirm;
        }

        public void Exit()
        {
            deps.MapScreen.LevelSelected -= OnLevelSelected;
            deps.BackButton.BackPressed -= OnBackPressed;
            deps.QuitConfirmScreen.ConfirmClicked -= OnQuitConfirmed;
            deps.QuitConfirmScreen.CancelClicked -= CloseQuitConfirm;

            deps.MapScreen.Hide();
            if (quitOpen) CloseQuitConfirm();
        }

        private void OnLevelSelected(int levelIndex)
        {
            // Checked again: a UI bug must not be able to open a locked level.
            if (deps.Progress.IsUnlocked(levelIndex))
                machine.ChangeTo(states.Gameplay(levelIndex));
        }

        private void OnBackPressed()
        {
            if (quitOpen)
            {
                CloseQuitConfirm();
                return;
            }
            quitOpen = true;
            deps.QuitConfirmScreen.Show();
        }

        private void CloseQuitConfirm()
        {
            quitOpen = false;
            deps.QuitConfirmScreen.Hide();
        }

        private static void OnQuitConfirmed() => Application.Quit();
    }
}
