using System;

namespace Game.Core.Progress
{
    // Unlock rules for the linear level chain. Storage details live behind IProgressStorage.
    public sealed class ProgressService : IProgress
    {
        private readonly IProgressStorage storage;
        private readonly SaveData data;

        public ProgressService(IProgressStorage storage, int levelCount)
        {
            this.storage = storage;
            data = storage.Load() ?? new SaveData();
            // Clamp in case an update removed levels.
            data.HighestCompletedIndex = Math.Min(data.HighestCompletedIndex, levelCount - 1);
        }

        public bool IsCompleted(int i) => i <= data.HighestCompletedIndex;
        public bool IsUnlocked(int i)  => i <= data.HighestCompletedIndex + 1;

        public void MarkCompleted(int i)
        {
            if (i <= data.HighestCompletedIndex) return;   // replay: no write
            data.HighestCompletedIndex = i;
            storage.Save(data);
        }
    }
}
