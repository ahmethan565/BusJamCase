using Game.Core.Progress;

namespace Game.Core.Tests
{
    // Fake storage that counts writes and can return any value (including null) from Load.
    public sealed class CountingProgressStorage : IProgressStorage
    {
        private readonly SaveData loaded;

        public CountingProgressStorage(SaveData loaded) => this.loaded = loaded;

        public int SaveCount { get; private set; }

        public SaveData Load() => loaded;
        public void Save(SaveData data) => SaveCount++;
    }
}
