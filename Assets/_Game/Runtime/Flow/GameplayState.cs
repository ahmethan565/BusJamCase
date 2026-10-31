using Game.Core;
using Game.Core.Levels;
using Game.Runtime.View;
using UnityEngine;

namespace Game.Runtime.Flow
{
    // One level being played. Restart and retry build a NEW GameplayState: nothing is reset in place.
    public sealed class GameplayState : IAppState
    {
        private readonly AppStateMachine machine;
        private readonly AppStates states;
        private readonly AppDependencies deps;
        private readonly int levelIndex;
        private GameSession session;

        public GameplayState(AppStateMachine machine, AppStates states, AppDependencies deps, int levelIndex)
        {
            this.machine = machine;
            this.states = states;
            this.deps = deps;
            this.levelIndex = levelIndex;
        }

        public void Enter()
        {
            // 1) Clear the previous level's visuals (they stayed behind the popup)
            deps.Presenter.Clear();
            deps.BoardView.Clear();
            deps.WaitingAreaView.Clear();

            // 2) Level and layout
            LevelData level = deps.Catalog.Get(levelIndex);
            var layout = new BoardLayout(level, deps.LayoutSettings);

            // 3) Static board and camera
            deps.BoardView.Build(level, layout);
            deps.WaitingAreaView.Build(level.WaitingSlots, layout);
            deps.CameraFitter.Fit(layout.ContentBounds);

            // 4-5) Passenger visuals, then the rules
            deps.Presenter.Begin(level, layout);
            session = new GameSession(level, deps.PathFinder, deps.Presenter);

            // 6) Input and time (the time waits for the first tap on the board)
            deps.Input.SetLayout(layout);
            deps.Input.CellTapped += session.Tap;
            deps.Input.enabled = true;
            deps.Clock.Bind(session);
            deps.Clock.enabled = true;

            // 7) HUD
            deps.Hud.Show();
            deps.Hud.SetLevel(levelIndex);
            deps.Hud.SetTimeLimit(Mathf.CeilToInt(level.TimeLimitSeconds));

            // 8) Subscriptions
            deps.Hud.RestartClicked += OnRestartClicked;
            deps.BackButton.BackPressed += OnBackPressed;
            deps.Presenter.PresentationFinished += OnPresentationFinished;
            deps.Presenter.TimeChanged += deps.Hud.SetTime;

            // 9) First bus arrives
            session.Start();
        }

        // Board visuals and their tweens are NOT cleared here: the popup opens over the finished board.
        public void Exit()
        {
            deps.Input.enabled = false;
            deps.Input.CellTapped -= session.Tap;
            deps.Clock.enabled = false;
            deps.Clock.Unbind();
            deps.Presenter.TimeChanged -= deps.Hud.SetTime;
            deps.Hud.RestartClicked -= OnRestartClicked;
            deps.BackButton.BackPressed -= OnBackPressed;
            deps.Presenter.PresentationFinished -= OnPresentationFinished;
            deps.Hud.Hide();
        }

        private void OnPresentationFinished(SessionResult result, LoseReason reason) =>
            machine.ChangeTo(result == SessionResult.Won ? states.Win(levelIndex) : states.Lose(levelIndex, reason));

        private void OnRestartClicked() => machine.ChangeTo(states.Gameplay(levelIndex));

        private void OnBackPressed() => machine.ChangeTo(states.Menu());
    }
}
