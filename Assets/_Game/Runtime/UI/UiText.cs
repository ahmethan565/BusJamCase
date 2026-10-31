using Game.Core;

namespace Game.Runtime.UI
{
    // Code counts levels from 0, the player sees numbers from 1. The conversion happens only here.
    public static class UiText
    {
        public static string LevelLabel(int index) => $"Level {index + 1}";

        // 65 -> "1:05". Called once per second at most; the HUD builds its labels once per level.
        public static string FormatTime(int seconds) => $"{seconds / 60}:{seconds % 60:00}";

        public static string LoseTitle(LoseReason reason) => reason == LoseReason.TimeUp ? "Time's up!" : "Out of space!";
    }
}
