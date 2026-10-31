using Game.Core.Progress;
using NUnit.Framework;

namespace Game.Core.Tests
{
    public sealed class ProgressServiceTests
    {
        [Test]
        public void Fresh_OnlyFirstUnlocked()
        {
            var progress = new ProgressService(new InMemoryProgressStorage(), levelCount: 5);

            Assert.IsTrue(progress.IsUnlocked(0));
            Assert.IsFalse(progress.IsUnlocked(1));
            Assert.IsFalse(progress.IsCompleted(0));
        }

        [Test]
        public void MarkCompleted_UnlocksNext()
        {
            var storage = new InMemoryProgressStorage();
            var progress = new ProgressService(storage, levelCount: 5);

            progress.MarkCompleted(0);

            Assert.IsTrue(progress.IsCompleted(0));
            Assert.IsTrue(progress.IsUnlocked(1));
            Assert.IsFalse(progress.IsCompleted(1));
            Assert.IsFalse(progress.IsUnlocked(2));

            var reloaded = new ProgressService(storage, levelCount: 5);
            Assert.IsTrue(reloaded.IsCompleted(0), "progress is saved immediately");
            Assert.IsTrue(reloaded.IsUnlocked(1));
        }

        [Test]
        public void MarkCompleted_Replay_DoesNotDecreaseOrWrite()
        {
            var storage = new CountingProgressStorage(new SaveData());
            var progress = new ProgressService(storage, levelCount: 5);
            progress.MarkCompleted(0);
            progress.MarkCompleted(1);
            Assert.AreEqual(2, storage.SaveCount);

            progress.MarkCompleted(0);

            Assert.AreEqual(2, storage.SaveCount);
            Assert.IsTrue(progress.IsCompleted(1));
            Assert.IsTrue(progress.IsUnlocked(2));
        }

        [Test]
        public void Load_ClampsToLevelCount()
        {
            var storage = new InMemoryProgressStorage();
            storage.Save(new SaveData { HighestCompletedIndex = 10 });

            var progress = new ProgressService(storage, levelCount: 3);

            Assert.IsTrue(progress.IsCompleted(2));
            Assert.IsFalse(progress.IsCompleted(3));
        }

        [Test]
        public void Load_Null_TreatedAsFresh()
        {
            var progress = new ProgressService(new CountingProgressStorage(null), levelCount: 3);

            Assert.IsTrue(progress.IsUnlocked(0));
            Assert.IsFalse(progress.IsUnlocked(1));
        }

        [Test]
        public void InMemoryStorage_StoresCopies()
        {
            var storage = new InMemoryProgressStorage();
            Assert.AreEqual(-1, storage.Load().HighestCompletedIndex);
            Assert.AreEqual(1, storage.Load().Version);

            var data = new SaveData { HighestCompletedIndex = 2 };
            storage.Save(data);
            data.HighestCompletedIndex = 4;           // mutate the caller's copy after saving
            storage.Load().HighestCompletedIndex = 7; // mutate a loaded copy without saving

            Assert.AreEqual(2, storage.Load().HighestCompletedIndex);
        }
    }
}
