using System;
using DG.Tweening;
using Game.Runtime.Feedback;
using UnityEngine;

namespace Game.Runtime.UI
{
    // Base of every screen. Each screen has its own Canvas; hiding disables the canvas, never the GameObject.
    public abstract class ScreenView : MonoBehaviour
    {
        [SerializeField] private Canvas canvas;
        [SerializeField] private CanvasGroup group;
        protected IFeedback Feedback { get; private set; }

        // Once, in the bootstrap. Buttons are bound here in code, never in the Inspector.
        public void Init(IFeedback feedback)
        {
            Feedback = feedback;
            BindButtons();
        }

        public virtual void Show()
        {
            canvas.enabled = true;
            group.interactable = true;
            group.blocksRaycasts = true;
        }

        public virtual void Hide()
        {
            group.interactable = false;
            group.blocksRaycasts = false;   // a hidden popup's background must not block touches
            KillTweens();                   // the screen's own tweens (popup panel, fade, ...)
            canvas.enabled = false;
        }

        protected abstract void BindButtons();

        // Subclasses kill their own tween targets too, e.g. a popup: panel.DOKill(); base.KillTweens();
        protected virtual void KillTweens() { group.DOKill(); }

        // ONLY for buttons that cause a definite state change (popup buttons):
        // double-click guard + click sound. The group is enabled again by the next Show().
        protected void Fire(Action action)
        {
            group.interactable = false;
            Feedback?.UiClick();
            action?.Invoke();
        }

        // Popups open their panel with scale 0.8 -> 1 and a fade. Unscaled time, so it works while paused.
        protected void PopIn(Transform panel, float time)
        {
            panel.localScale = Vector3.one * 0.8f;
            group.alpha = 0f;
            panel.DOScale(1f, time).SetEase(Ease.OutBack).SetUpdate(true);
            group.DOFade(1f, time).SetUpdate(true);
        }
    }
}
