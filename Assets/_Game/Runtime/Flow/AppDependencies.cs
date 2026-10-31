using Game.Core.Levels;
using Game.Core.Path;
using Game.Core.Progress;
using Game.Runtime.Data;
using Game.Runtime.Feedback;
using Game.Runtime.Input;
using Game.Runtime.UI;
using Game.Runtime.View;

namespace Game.Runtime.Flow
{
    // A plain data bundle, NOT a service locator: only GameBootstrap fills it, only the states read it.
    public sealed class AppDependencies
    {
        // Core / services
        public ILevelCatalog Catalog;
        public IProgress Progress;
        public IPathFinder PathFinder;
        public IFeedback Feedback;
        public LayoutSettings LayoutSettings;

        // World
        public BoardView BoardView;
        public WaitingAreaView WaitingAreaView;
        public GameplayPresenter Presenter;
        public CameraFitter CameraFitter;
        public SessionClock Clock;

        // Input
        public InputHandler Input;
        public BackButtonInput BackButton;

        // UI
        public LevelMapScreen MapScreen;
        public Hud Hud;
        public WinScreen WinScreen;
        public LoseScreen LoseScreen;
        public QuitConfirmScreen QuitConfirmScreen;
    }
}
