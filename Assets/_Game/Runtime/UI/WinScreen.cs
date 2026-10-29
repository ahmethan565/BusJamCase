using System;
using DG.Tweening;
using Game.Runtime.Data;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Runtime.UI
{
    // "Tebrikler!" popup. On the last level the Next button is hidden and "all levels completed" is shown.
    public sealed class WinScreen : ScreenView
    {
        [SerializeField] private AnimationSettings animationSettings;
        [SerializeField] private RectTransform panel;
        [SerializeField] private Button nextButton;
        [SerializeField] private Button menuButton;
        [SerializeField] private GameObject allDoneText;

        public event Action NextClicked, MenuClicked;

        public void Show(bool showNextButton)
        {
            nextButton.gameObject.SetActive(showNextButton);
            allDoneText.SetActive(!showNextButton);
            Show();
            PopIn(panel, animationSettings.PopupTime);
        }

        protected override void BindButtons()
        {
            nextButton.onClick.AddListener(() => Fire(NextClicked));
            menuButton.onClick.AddListener(() => Fire(MenuClicked));
        }

        protected override void KillTweens()
        {
            panel.DOKill();
            base.KillTweens();
        }
    }
}
