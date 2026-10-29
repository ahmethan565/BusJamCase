namespace Game.Runtime.UI
{
    // Code counts levels from 0, the player sees numbers from 1. The conversion happens only here (§12.5).
    public static class UiText
    {
        public static string LevelLabel(int index) => $"Bölüm {index + 1}";
    }
}
