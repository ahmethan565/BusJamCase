using System.Collections.Generic;
using System.Linq;
using Game.Core.Progress;
using Game.Runtime.Feedback;
using Game.Runtime.UI;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

namespace Game.Editor.Tests
{
    // Screen behaviour (§11.9, §12). Each test works on a COPY of the scene's screen inside a preview scene,
    // so Main.unity is never modified.
    public sealed class UiScreenTests
    {
        private const string ScenePath = "Assets/_Game/Scenes/Main.unity";

        private sealed class CountingFeedback : IFeedback
        {
            public int UiClicks;
            public void PassengerTapped(Vector3 at) { }
            public void PassengerBlocked(Vector3 at) { }
            public void PassengerBoarded(int seat) { }
            public void PassengerWaiting() { }
            public void BusDeparted(Vector3 at) { }
            public void BusArrived() { }
            public void Won() { }
            public void Lost() { }
            public void UiClick() => UiClicks++;
        }

        // Levels 0..HighestCompleted are completed; the next one is unlocked.
        private sealed class FixedProgress : IProgress
        {
            public int HighestCompleted = -1;
            public bool IsUnlocked(int levelIndex) => levelIndex <= HighestCompleted + 1;
            public bool IsCompleted(int levelIndex) => levelIndex <= HighestCompleted;
            public void MarkCompleted(int levelIndex) { }
        }

        private Scene mainScene;
        private bool openedByTest;
        private Scene preview;
        private CountingFeedback feedback;

        [OneTimeSetUp]
        public void OpenScene()
        {
            mainScene = SceneManager.GetSceneByPath(ScenePath);
            if (mainScene.isLoaded) return;
            mainScene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
            openedByTest = true;
        }

        [OneTimeTearDown]
        public void CloseScene()
        {
            if (openedByTest) EditorSceneManager.CloseScene(mainScene, true);
        }

        [SetUp]
        public void SetUp()
        {
            preview = EditorSceneManager.NewPreviewScene();
            feedback = new CountingFeedback();
        }

        [TearDown]
        public void TearDown() => EditorSceneManager.ClosePreviewScene(preview);

        // A copy of the scene screen's whole canvas, initialised like the bootstrap does.
        private T Copy<T>() where T : ScreenView
        {
            T[] found = mainScene.GetRootGameObjects().SelectMany(go => go.GetComponentsInChildren<T>(true)).ToArray();
            Assert.AreEqual(1, found.Length, $"exactly one {typeof(T).Name} expected in Main.unity");
            GameObject canvas = found[0].GetComponentInParent<Canvas>(true).gameObject;

            GameObject copy = Object.Instantiate(canvas);
            SceneManager.MoveGameObjectToScene(copy, preview);
            T screen = copy.GetComponentInChildren<T>(true);
            screen.Init(feedback);
            return screen;
        }

        private static GameObject Child(Component root, string name)
        {
            Transform found = root.GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name == name);
            Assert.IsNotNull(found, $"{root.name} has no child '{name}'");
            return found.gameObject;
        }

        // A real pointer click: Button ignores it when the button or its CanvasGroup is not interactable.
        private static void Click(GameObject button) =>
            ExecuteEvents.Execute(button, new PointerEventData(null), ExecuteEvents.pointerClickHandler);

        private static Canvas CanvasOf(ScreenView screen) =>
            (Canvas)new SerializedObject(screen).FindProperty("canvas").objectReferenceValue;

        private static CanvasGroup GroupOf(ScreenView screen) =>
            (CanvasGroup)new SerializedObject(screen).FindProperty("group").objectReferenceValue;

        // --- ScreenView

        [Test]
        public void Hide_DisablesCanvasAndStopsBlockingTouches()
        {
            LoseScreen screen = Copy<LoseScreen>();
            screen.Show();

            screen.Hide();

            Assert.IsFalse(CanvasOf(screen).enabled);
            Assert.IsFalse(GroupOf(screen).interactable);
            Assert.IsFalse(GroupOf(screen).blocksRaycasts, "a hidden popup must not block the board (§12.2)");
        }

        [Test]
        public void Show_EnablesCanvasAndInteraction()
        {
            LoseScreen screen = Copy<LoseScreen>();
            screen.Hide();

            screen.Show();

            Assert.IsTrue(CanvasOf(screen).enabled);
            Assert.IsTrue(GroupOf(screen).interactable);
            Assert.IsTrue(GroupOf(screen).blocksRaycasts);
        }

        // --- Hud

        [Test]
        public void Hud_SetLevel_ShowsOneBasedLabel()
        {
            Hud hud = Copy<Hud>();

            hud.SetLevel(2);

            Assert.AreEqual("Bölüm 3", Child(hud, "LevelLabel").GetComponent<TMP_Text>().text);
        }

        [Test]
        public void Hud_Restart_CanBePressedAgainAndAgain()
        {
            Hud hud = Copy<Hud>();
            hud.Show();
            int restarts = 0;
            hud.RestartClicked += () => restarts++;

            Click(Child(hud, "RestartButton"));
            Click(Child(hud, "RestartButton"));

            Assert.AreEqual(2, restarts, "the HUD does not use Fire (§12.2)");
            Assert.AreEqual(2, feedback.UiClicks);
        }

        // --- WinScreen

        [Test]
        public void Win_WithNextLevel_ShowsNextButton()
        {
            WinScreen win = Copy<WinScreen>();

            win.Show(showNextButton: true);

            Assert.IsTrue(Child(win, "NextButton").activeSelf);
            Assert.IsFalse(Child(win, "AllDoneText").activeSelf);
        }

        [Test]
        public void Win_LastLevel_HidesNextAndShowsAllDone()
        {
            WinScreen win = Copy<WinScreen>();

            win.Show(showNextButton: false);

            Assert.IsFalse(Child(win, "NextButton").activeSelf);
            Assert.IsTrue(Child(win, "AllDoneText").activeSelf);
            Assert.IsTrue(Child(win, "MenuButton").activeSelf);
        }

        [Test]
        public void Win_DoubleClickNext_RaisesOnce()
        {
            WinScreen win = Copy<WinScreen>();
            win.Show(showNextButton: true);
            int next = 0, menu = 0;
            win.NextClicked += () => next++;
            win.MenuClicked += () => menu++;

            Click(Child(win, "NextButton"));
            Click(Child(win, "NextButton"));
            Click(Child(win, "MenuButton"));

            Assert.AreEqual(1, next, "Fire makes the group non-interactable after the first click (§11.9)");
            Assert.AreEqual(0, menu);
            Assert.AreEqual(1, feedback.UiClicks);
        }

        [Test]
        public void Win_ShowAgainAfterClick_IsInteractable()
        {
            WinScreen win = Copy<WinScreen>();
            win.Show(showNextButton: true);
            int menu = 0;
            win.MenuClicked += () => menu++;
            Click(Child(win, "MenuButton"));
            win.Hide();

            win.Show(showNextButton: true);
            Click(Child(win, "MenuButton"));

            Assert.AreEqual(2, menu);
        }

        // --- LoseScreen and QuitConfirmScreen

        [Test]
        public void Lose_RetryAndMenu_RaiseTheirEvents()
        {
            LoseScreen lose = Copy<LoseScreen>();
            int retry = 0, menu = 0;
            lose.RetryClicked += () => retry++;
            lose.MenuClicked += () => menu++;

            lose.Show();
            Click(Child(lose, "RetryButton"));
            lose.Show();
            Click(Child(lose, "MenuButton"));

            Assert.AreEqual(1, retry);
            Assert.AreEqual(1, menu);
        }

        [Test]
        public void QuitConfirm_ConfirmAndCancel_RaiseTheirEvents()
        {
            QuitConfirmScreen quit = Copy<QuitConfirmScreen>();
            int confirm = 0, cancel = 0;
            quit.ConfirmClicked += () => confirm++;
            quit.CancelClicked += () => cancel++;

            quit.Show();
            Click(Child(quit, "CancelButton"));
            quit.Show();
            Click(Child(quit, "ConfirmButton"));

            Assert.AreEqual(1, confirm);
            Assert.AreEqual(1, cancel);
        }

        // --- LevelMapScreen

        private static LevelNodeView[] Nodes(LevelMapScreen map) =>
            map.GetComponentsInChildren<LevelNodeView>(true).OrderBy(n => n.Index).ToArray();

        [Test]
        public void Map_Build_OneNodePerLevel_FirstAtTheBottom_Zigzag()
        {
            LevelMapScreen map = Copy<LevelMapScreen>();

            map.Build(5);

            LevelNodeView[] nodes = Nodes(map);
            Assert.AreEqual(5, nodes.Length);
            CollectionAssert.AreEqual(new[] { 0, 1, 2, 3, 4 }, nodes.Select(n => n.Index).ToArray());

            float[] zigzag = { 0f, 150f, 0f, -150f, 0f };
            for (int i = 0; i < nodes.Length; i++)
            {
                Vector2 p = ((RectTransform)nodes[i].transform).anchoredPosition;
                Assert.AreEqual(zigzag[i], p.x, 1e-3f, $"node {i} x (§12.4)");
                if (i > 0)
                {
                    Vector2 below = ((RectTransform)nodes[i - 1].transform).anchoredPosition;
                    Assert.AreEqual(250f, p.y - below.y, 1e-3f, $"node {i} is one NodeSpacing above node {i - 1}");
                }
            }
        }

        [Test]
        public void Map_Show_SetsCompletedCurrentLocked()
        {
            LevelMapScreen map = Copy<LevelMapScreen>();
            map.Build(5);

            map.Show(new FixedProgress { HighestCompleted = 1 });

            CollectionAssert.AreEqual(
                new[] { LevelNodeState.Completed, LevelNodeState.Completed, LevelNodeState.Current,
                        LevelNodeState.Locked, LevelNodeState.Locked },
                Nodes(map).Select(n => n.State).ToArray());
        }

        [Test]
        public void Map_NodeClicks_OnlyUnlockedSelectALevel_AndMapStaysClickable()
        {
            LevelMapScreen map = Copy<LevelMapScreen>();
            map.Build(5);
            map.Show(new FixedProgress { HighestCompleted = 0 });
            var selected = new List<int>();
            map.LevelSelected += selected.Add;
            LevelNodeView[] nodes = Nodes(map);

            Click(nodes[3].gameObject);   // locked: shakes, no selection
            Click(nodes[0].gameObject);   // completed: replay
            Click(nodes[1].gameObject);   // current

            CollectionAssert.AreEqual(new[] { 0, 1 }, selected, "a locked node must not block the map (§12.2)");
        }

        [Test]
        public void Map_ShowAgain_RefreshesStatesWithoutRebuilding()
        {
            LevelMapScreen map = Copy<LevelMapScreen>();
            map.Build(5);
            map.Show(new FixedProgress { HighestCompleted = -1 });
            map.Hide();

            map.Show(new FixedProgress { HighestCompleted = 4 });

            LevelNodeView[] nodes = Nodes(map);
            Assert.AreEqual(5, nodes.Length, "Show never creates nodes (§12.4)");
            Assert.IsTrue(nodes.All(n => n.State == LevelNodeState.Completed));
        }
    }
}
