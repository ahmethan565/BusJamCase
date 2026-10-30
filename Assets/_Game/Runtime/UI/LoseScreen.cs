using System;
using DG.Tweening;
using Game.Core;
using Game.Runtime.Data;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Runtime.UI
{
    // "Out of space!" / "Time's up!" popup.
    public sealed class LoseScreen : ScreenView
    {
        [SerializeField] private AnimationSettings animationSettings;
        [SerializeField] private RectTransform panel;
        [SerializeField] private TMP_Text titleLabel;
        [SerializeField] private Button retryButton;
        [SerializeField] private Button menuButton;

        public event Action RetryClicked, MenuClicked;

        public void Show(LoseReason reason)
        {
            titleLabel.text = UiText.LoseTitle(reason);
            Show();
        }

        public override void Show()
        {
            base.Show();
            PopIn(panel, animationSettings.PopupTime);
        }

        protected override void BindButtons()
        {
            retryButton.onClick.AddListener(() => Fire(RetryClicked));
            menuButton.onClick.AddListener(() => Fire(MenuClicked));
        }

        protected override void KillTweens()
        {
            panel.DOKill();
            base.KillTweens();
        }
    }
}
