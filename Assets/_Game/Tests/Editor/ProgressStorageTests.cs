using Game.Core.Progress;
using Game.Editor;
using Game.Runtime.Progress;
using NUnit.Framework;
using UnityEngine;

namespace Game.Editor.Tests
{
    // §8.3, §8.4, §15.4. Every test uses its own key; the real save ("progress") is never touched.
    public sealed class ProgressStorageTests
    {
        private const string TestKey = "progress_test";

        [SetUp]
        public void SetUp() => PlayerPrefs.DeleteKey(TestKey);

        [TearDown]
        public void TearDown() => PlayerPrefs.DeleteKey(TestKey);

        [Test]
        public void DefaultKey_IsProgress()
        {
            Assert.AreEqual("progress", PlayerPrefsProgressStorage.DefaultKey);
        }

        [Test]
        public void PlayerPrefsStorage_Missing_ReturnsDefault()
        {
            SaveData data = new PlayerPrefsProgressStorage(TestKey).Load();

            Assert.IsNotNull(data);
            Assert.AreEqual(1, data.Version);
            Assert.AreEqual(-1, data.HighestCompletedIndex);
        }

        [Test]
        public void PlayerPrefsStorage_Corrupt_ReturnsDefault()
        {
            PlayerPrefs.SetString(TestKey, "{ this is not json");

            SaveData data = null;
            Assert.DoesNotThrow(() => data = new PlayerPrefsProgressStorage(TestKey).Load());

            Assert.IsNotNull(data);
            Assert.AreEqual(-1, data.HighestCompletedIndex);
        }

        [Test]
        public void PlayerPrefsStorage_SaveThenLoad_RoundTrips()
        {
            var storage = new PlayerPrefsProgressStorage(TestKey);

            storage.Save(new SaveData { HighestCompletedIndex = 3 });

            Assert.AreEqual(3, new PlayerPrefsProgressStorage(TestKey).Load().HighestCompletedIndex);
        }

        [Test]
        public void PlayerPrefsStorage_TestKey_DoesNotTouchRealSave()
        {
            bool hadReal = PlayerPrefs.HasKey(PlayerPrefsProgressStorage.DefaultKey);
            string real = PlayerPrefs.GetString(PlayerPrefsProgressStorage.DefaultKey, "");

            new PlayerPrefsProgressStorage(TestKey).Save(new SaveData { HighestCompletedIndex = 2 });

            Assert.AreEqual(hadReal, PlayerPrefs.HasKey(PlayerPrefsProgressStorage.DefaultKey));
            Assert.AreEqual(real, PlayerPrefs.GetString(PlayerPrefsProgressStorage.DefaultKey, ""));
        }

        [Test]
        public void ProgressService_Restart_KeepsProgress()
        {
            // "Close and reopen the app": a new service over the same storage key.
            var first = new ProgressService(new PlayerPrefsProgressStorage(TestKey), levelCount: 5);
            first.MarkCompleted(0);
            first.MarkCompleted(1);

            var reopened = new ProgressService(new PlayerPrefsProgressStorage(TestKey), levelCount: 5);

            Assert.IsTrue(reopened.IsCompleted(1));
            Assert.IsTrue(reopened.IsUnlocked(2));
            Assert.IsFalse(reopened.IsUnlocked(3));
        }

        [Test]
        public void ProgressService_ReplayOldLevel_DoesNotLowerProgress()
        {
            var service = new ProgressService(new PlayerPrefsProgressStorage(TestKey), levelCount: 5);
            service.MarkCompleted(0);
            service.MarkCompleted(1);
            service.MarkCompleted(2);

            service.MarkCompleted(0);   // replay

            Assert.AreEqual(2, new PlayerPrefsProgressStorage(TestKey).Load().HighestCompletedIndex);
        }

        [Test]
        public void DebugMenu_UnlockAll_CompletesAllButTheLast()
        {
            DebugMenu.UnlockAllLevels(TestKey, levelCount: 5);

            var service = new ProgressService(new PlayerPrefsProgressStorage(TestKey), levelCount: 5);
            Assert.IsTrue(service.IsCompleted(3));
            Assert.IsFalse(service.IsCompleted(4));
            Assert.IsTrue(service.IsUnlocked(4), "every level is open");
        }

        [Test]
        public void DebugMenu_Reset_DeletesTheSave()
        {
            new PlayerPrefsProgressStorage(TestKey).Save(new SaveData { HighestCompletedIndex = 4 });

            DebugMenu.ResetProgress(TestKey);

            Assert.IsFalse(PlayerPrefs.HasKey(TestKey));
        }

        [Test]
        public void DebugMenu_EnabledOnlyOutsidePlayMode()
        {
            Assert.IsTrue(DebugMenu.CanEditProgress(), "tests run in edit mode");
        }
    }
}
