using System;
using DG.Tweening;
using Game.Runtime.Data;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Runtime.UI
{
    public enum LevelNodeState { Completed, Current, Locked }

    // One level on the map. Layers, one owner each:
    //   Node (root) -> anchored position: the locked shake
    //   └── Body    -> scale:             the "current" pulse
    public sealed class LevelNodeView : MonoBehaviour
    {
        [SerializeField] private Button button;
        [SerializeField] private RectTransform body;
        [SerializeField] private Image background;
        [SerializeField] private TMP_Text label;
        [SerializeField] private GameObject completedIcon;
        [SerializeField] private GameObject lockIcon;
        [SerializeField] private Color completedColor = new Color(0.30f, 0.69f, 0.31f);
        [SerializeField] private Color currentColor = new Color(1.00f, 0.76f, 0.03f);
        [SerializeField] private Color lockedColor = new Color(0.62f, 0.62f, 0.62f);

        private AnimationSettings anim;

        public int Index { get; private set; }
        public LevelNodeState State { get; private set; }
        public event Action<LevelNodeView> Clicked;

        // Once, when the map builds its nodes.
        public void Init(int index, AnimationSettings animationSettings)
        {
            Index = index;
            anim = animationSettings;
            label.text = UiText.LevelLabel(index);
            button.onClick.AddListener(() => Clicked?.Invoke(this));
        }

        public void SetState(LevelNodeState state)
        {
            State = state;
            background.color = state == LevelNodeState.Completed ? completedColor
                             : state == LevelNodeState.Current ? currentColor
                             : lockedColor;
            completedIcon.SetActive(state == LevelNodeState.Completed);
            lockIcon.SetActive(state == LevelNodeState.Locked);
        }

        public void StartPulse()
        {
            body.DOScale(anim.NodePulseScale, anim.NodePulseTime / 2f)
                .SetEase(Ease.InOutSine)
                .SetLoops(-1, LoopType.Yoyo);
        }

        public void StopTweens()
        {
            body.DOKill();
            body.localScale = Vector3.one;
            ((RectTransform)transform).DOKill(complete: true);   // a running shake returns to its place
        }

        public void PlayLockedShake()
        {
            var rect = (RectTransform)transform;
            rect.DOKill(complete: true);
            rect.DOShakeAnchorPos(anim.BlockedShakeTime, 12f);
        }
    }
}
