using System;
using System.Collections.Generic;
using Game.Core.Progress;
using Game.Runtime.Data;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Runtime.UI
{
    // The level chain, bottom to top. Nodes are built once; Show only refreshes their states.
    public sealed class LevelMapScreen : ScreenView
    {
        private const float BottomPadding = 300f;
        private const float NodeSpacing = 250f;
        private const float LineThickness = 12f;
        private static readonly float[] Zigzag = { 0f, 150f, 0f, -150f };

        [SerializeField] private AnimationSettings animationSettings;
        [SerializeField] private ScrollRect scrollRect;
        [SerializeField] private RectTransform content;
        [SerializeField] private RectTransform lineRoot;
        [SerializeField] private RectTransform nodeRoot;
        [SerializeField] private LevelNodeView nodePrefab;
        [SerializeField] private RectTransform linePrefab;

        private readonly List<LevelNodeView> nodes = new List<LevelNodeView>();

        // Only for unlocked nodes. Locked nodes shake instead.
        public event Action<int> LevelSelected;

        // Once, in the bootstrap.
        public void Build(int levelCount)
        {
            content.sizeDelta = new Vector2(content.sizeDelta.x, 2f * BottomPadding + (levelCount - 1) * NodeSpacing);

            for (int i = 0; i < levelCount; i++)
            {
                LevelNodeView node = Instantiate(nodePrefab, nodeRoot);
                ((RectTransform)node.transform).anchoredPosition = NodePosition(i);
                node.Init(i, animationSettings);
                node.Clicked += OnNodeClicked;
                nodes.Add(node);

                if (i > 0) AddLine(NodePosition(i - 1), NodePosition(i));
            }
        }

        public void Show(IProgress progress)
        {
            foreach (LevelNodeView node in nodes)
            {
                node.StopTweens();
                node.SetState(progress.IsCompleted(node.Index) ? LevelNodeState.Completed
                            : progress.IsUnlocked(node.Index) ? LevelNodeState.Current
                            : LevelNodeState.Locked);
                if (node.State == LevelNodeState.Current) node.StartPulse();
            }
            Show();

            int currentIndex = FirstNotCompleted(progress, nodes.Count);
            Canvas.ForceUpdateCanvases();   // without a built layout the scroll position is ignored
            // 0 is the bottom of the map.
            scrollRect.verticalNormalizedPosition = nodes.Count > 1 ? currentIndex / (float)(nodes.Count - 1) : 0f;
        }

        // A node tap does not always change state (locked nodes shake), so no Fire.
        protected override void BindButtons() { }

        protected override void KillTweens()
        {
            foreach (LevelNodeView node in nodes) node.StopTweens();
            base.KillTweens();
        }

        private void OnNodeClicked(LevelNodeView node)
        {
            Feedback?.UiClick();
            if (node.State == LevelNodeState.Locked)
            {
                node.PlayLockedShake();
                return;
            }
            LevelSelected?.Invoke(node.Index);
        }

        private void AddLine(Vector2 from, Vector2 to)
        {
            RectTransform line = Instantiate(linePrefab, lineRoot);
            Vector2 delta = to - from;
            line.anchoredPosition = (from + to) / 2f;
            line.sizeDelta = new Vector2(delta.magnitude, LineThickness);
            line.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg);
        }

        private static Vector2 NodePosition(int index) =>
            new Vector2(Zigzag[index % Zigzag.Length], BottomPadding + index * NodeSpacing);

        // The first level not completed yet; if all are completed, the last one.
        private static int FirstNotCompleted(IProgress progress, int count)
        {
            for (int i = 0; i < count; i++)
                if (!progress.IsCompleted(i)) return i;
            return count - 1;
        }
    }
}
