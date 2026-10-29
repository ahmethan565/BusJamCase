namespace Game.Core.Progress
{
    // Keeps progress only in memory: tests, Phase 4, and levels launched from the editor.
    public sealed class InMemoryProgressStorage : IProgressStorage
    {
        private SaveData stored = new SaveData();

        public SaveData Load() => Copy(stored);
        public void Save(SaveData data) => stored = Copy(data);

        private static SaveData Copy(SaveData data) => new SaveData
        {
            Version = data.Version,
            HighestCompletedIndex = data.HighestCompletedIndex,
        };
    }
}
