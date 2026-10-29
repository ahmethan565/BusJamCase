using DG.Tweening;
using Game.Core;
using Game.Core.Levels;
using Game.Core.Path;
using Game.Runtime.Data;
using Game.Runtime.Feedback;
using Game.Runtime.Input;
using Game.Runtime.View;
using UnityEngine;

namespace Game.Runtime.Bootstrap
{
    // The ONLY place that wires everything together (§14.3). No singletons, no FindObjectOfType, no DI framework.
    public sealed class GameBootstrap : MonoBehaviour
    {
        [Header("Content")]
        [SerializeField] private LevelCatalog levelCatalog;
        [SerializeField] private LayoutSettings layoutSettings;
        [SerializeField] private AnimationSettings animationSettings;
        [SerializeField] private ColorPalette colorPalette;

        [Header("World")]
        [SerializeField] private BoardView boardView;
        [SerializeField] private WaitingAreaView waitingAreaView;
        [SerializeField] private GameplayPresenter presenter;
        [SerializeField] private CameraFitter cameraFitter;

        [Header("Input")]
        [SerializeField] private InputHandler inputHandler;

        [Header("Phase 3 only")]
        [Tooltip("0-based catalog index of the level to start (level 1 cannot be lost; try 3 to test losing)")]
        [SerializeField] private int phase3LevelIndex;

        private void Awake()
        {
            Application.targetFrameRate = 60;
            DOTween.Init(recycleAllByDefault: true, useSafeMode: true, logBehaviour: LogBehaviour.ErrorsOnly)
                   .SetCapacity(200, 50);

            inputHandler.enabled = false;                     // only gameplay turns it on

            // --- Services
            var pathFinder = new BfsPathFinder();
            IFeedback feedback = new NullFeedback();          // Phase 6: FeedbackPlayer

            // --- Views
            presenter.Init(colorPalette, animationSettings, layoutSettings, feedback);
            boardView.Init(layoutSettings);
            waitingAreaView.Init(layoutSettings);
            presenter.Prewarm(); boardView.Prewarm(); waitingAreaView.Prewarm();

            StartFirstLevel(pathFinder);
        }

        // Phase 3 only: starts a level directly, in the same order as GameplayState.Enter (§11.5).
        // Replaced by the app state machine in Phase 4.
        private void StartFirstLevel(IPathFinder pathFinder)
        {
            LevelData level = levelCatalog.Get(phase3LevelIndex);
            var layout = new BoardLayout(level, layoutSettings);

            boardView.Build(level, layout);
            waitingAreaView.Build(level.WaitingSlots, layout);
            cameraFitter.Fit(layout.ContentBounds);

            presenter.Begin(level, layout);
            var session = new GameSession(level, pathFinder, presenter);

            inputHandler.SetLayout(layout);
            inputHandler.CellTapped += session.Tap;
            inputHandler.enabled = true;

            presenter.PresentationFinished += result => Debug.Log($"[Phase 3] Presentation finished: {result}");
            session.Start();
        }
    }
}
