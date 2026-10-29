using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Runtime.UI
{
    // Level number + restart. Nothing else (§12.3).
    public sealed class Hud : ScreenView
    {
        [SerializeField] private TMP_Text levelLabel;
        [SerializeField] private Button restartButton;

        public event Action RestartClicked;

        public void SetLevel(int levelIndex) => levelLabel.text = UiText.LevelLabel(levelIndex);

        // No Fire: restart can be pressed again and again.
        protected override void BindButtons() => restartButton.onClick.AddListener(() =>
        {
            Feedback?.UiClick();
            RestartClicked?.Invoke();
        });
    }
}
