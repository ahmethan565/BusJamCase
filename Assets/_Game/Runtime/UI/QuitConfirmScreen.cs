using System;
using DG.Tweening;
using Game.Runtime.Data;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Runtime.UI
{
    // "Quit the game?" overlay on the map, owned by MenuState.
    public sealed class QuitConfirmScreen : ScreenView
    {
        [SerializeField] private AnimationSettings animationSettings;
        [SerializeField] private RectTransform panel;
        [SerializeField] private Button confirmButton;
        [SerializeField] private Button cancelButton;

        public event Action ConfirmClicked, CancelClicked;

        public override void Show()
        {
            base.Show();
            PopIn(panel, animationSettings.PopupTime);
        }

        protected override void BindButtons()
        {
            confirmButton.onClick.AddListener(() => Fire(ConfirmClicked));
            cancelButton.onClick.AddListener(() => Fire(CancelClicked));
        }

        protected override void KillTweens()
        {
            panel.DOKill();
            base.KillTweens();
        }
    }
}
