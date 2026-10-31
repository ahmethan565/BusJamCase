using System;
using DG.Tweening;
using Game.Runtime.Data;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Runtime.UI
{
    // Level number + time + restart. Nothing else (§12.3).
    public sealed class Hud : ScreenView
    {
        private const int WarningSeconds = 10;

        [SerializeField] private AnimationSettings animationSettings;
        [SerializeField] private TMP_Text levelLabel;
        [SerializeField] private TMP_Text timeLabel;
        [SerializeField] private GameObject timerPanel;   // the timer art; the time sits in its dark slot
        [SerializeField] private Button restartButton;
        [SerializeField] private Color timeColor = Color.white;
        [SerializeField] private Color warningColor = new Color(1f, 0.32f, 0.27f);

        private string[] timeTexts = new string[0];   // built once per level: no string per second
        private bool warning;

        public event Action RestartClicked;

        public void SetLevel(int levelIndex) => levelLabel.text = UiText.LevelLabel(levelIndex);

        // At the start of a level: the full time, frozen until the first tap. A level without a limit hides it.
        public void SetTimeLimit(int seconds)
        {
            StopWarning();
            timerPanel.SetActive(seconds > 0);
            if (seconds <= 0) return;

            if (timeTexts.Length < seconds + 1) timeTexts = new string[seconds + 1];
            for (int s = 0; s <= seconds; s++) timeTexts[s] = UiText.FormatTime(s);
            SetTime(seconds);
        }

        public void SetTime(int seconds)
        {
            timeLabel.text = timeTexts[Mathf.Clamp(seconds, 0, timeTexts.Length - 1)];
            if (seconds <= WarningSeconds && !warning) StartWarning();
        }

        // The last 10 seconds: red and a gentle pulse.
        private void StartWarning()
        {
            warning = true;
            timeLabel.color = warningColor;
            timeLabel.transform.DOScale(animationSettings.TimerPulseScale, animationSettings.TimerPulseTime / 2f)
                .SetEase(Ease.InOutSine)
                .SetLoops(-1, LoopType.Yoyo);
        }

        private void StopWarning()
        {
            warning = false;
            timeLabel.transform.DOKill();
            timeLabel.transform.localScale = Vector3.one;
            timeLabel.color = timeColor;
        }

        protected override void KillTweens()
        {
            StopWarning();
            base.KillTweens();
        }

        // No Fire: restart can be pressed again and again.
        protected override void BindButtons() => restartButton.onClick.AddListener(() =>
        {
            Feedback?.UiClick();
            RestartClicked?.Invoke();
        });
    }
}
