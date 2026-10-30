using DG.Tweening;
using Game.Core.Path;
using Game.Core.Progress;
using Game.Runtime.Data;
using Game.Runtime.Feedback;
using Game.Runtime.Flow;
using Game.Runtime.Input;
using Game.Runtime.Progress;
using Game.Runtime.UI;
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
        [SerializeField] private SoundLibrary soundLibrary;

        [Header("World")]
        [SerializeField] private BoardView boardView;
        [SerializeField] private WaitingAreaView waitingAreaView;
        [SerializeField] private GameplayPresenter presenter;
        [SerializeField] private CameraFitter cameraFitter;
        [SerializeField] private SessionClock sessionClock;

        [Header("Audio")]
        [SerializeField] private AudioPlayer audioPlayer;
        [SerializeField] private VfxPlayer vfxPlayer;

        [Header("Input")]
        [SerializeField] private InputHandler inputHandler;
        [SerializeField] private BackButtonInput backButton;

        [Header("UI")]
        [SerializeField] private LevelMapScreen mapScreen;
        [SerializeField] private Hud hud;
        [SerializeField] private WinScreen winScreen;
        [SerializeField] private LoseScreen loseScreen;
        [SerializeField] private QuitConfirmScreen quitConfirmScreen;

        private void Awake()
        {
            Application.targetFrameRate = 60;
            DOTween.Init(recycleAllByDefault: true, useSafeMode: true, logBehaviour: LogBehaviour.ErrorsOnly)
                   .SetCapacity(200, 50);

            inputHandler.enabled = false;                     // only GameplayState turns it on
            sessionClock.enabled = false;                     // same for the level time

            // --- Was "play this level" requested from the level editor? (§15.3)
            int requestedLevel = -1;
            bool launchedFromEditor = false;
#if UNITY_EDITOR
            launchedFromEditor = EditorLevelRequest.TryConsume(out requestedLevel)   // the return value decides
                                 && requestedLevel < levelCatalog.Count;
#endif

            // --- Services
            var pathFinder = new BfsPathFinder();
            IProgressStorage storage = launchedFromEditor
                ? new InMemoryProgressStorage()               // an editor try-out must not change the real save
                : new PlayerPrefsProgressStorage();
            var progress = new ProgressService(storage, levelCatalog.Count);
            IFeedback feedback = new FeedbackPlayer(audioPlayer, vfxPlayer, soundLibrary);

            // --- Views and screens
            var screens = new ScreenView[] { mapScreen, hud, winScreen, loseScreen, quitConfirmScreen };
            presenter.Init(colorPalette, animationSettings, layoutSettings, feedback);
            boardView.Init(layoutSettings);
            waitingAreaView.Init(layoutSettings);
            vfxPlayer.Init();
            foreach (ScreenView screen in screens) screen.Init(feedback);
            mapScreen.Build(levelCatalog.Count);
            presenter.Prewarm(); boardView.Prewarm(); waitingAreaView.Prewarm(); vfxPlayer.Prewarm();

            // --- Flow
            var deps = new AppDependencies
            {
                Catalog = levelCatalog, Progress = progress, PathFinder = pathFinder, Feedback = feedback,
                LayoutSettings = layoutSettings,
                BoardView = boardView, WaitingAreaView = waitingAreaView, Presenter = presenter, CameraFitter = cameraFitter, Clock = sessionClock,
                Input = inputHandler, BackButton = backButton,
                MapScreen = mapScreen, Hud = hud, WinScreen = winScreen, LoseScreen = loseScreen,
                QuitConfirmScreen = quitConfirmScreen,
            };
            var machine = new AppStateMachine();
            var states = new AppStates(machine, deps);

            // Every screen starts hidden
            foreach (ScreenView screen in screens) screen.Hide();

            machine.ChangeTo(launchedFromEditor ? states.Gameplay(requestedLevel) : states.Menu());
        }
    }
}
