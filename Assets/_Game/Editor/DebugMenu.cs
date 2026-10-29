using Game.Core.Progress;
using Game.Runtime.Data;
using Game.Runtime.Progress;
using UnityEditor;
using UnityEngine;

namespace Game.Editor
{
    // Tools -> Game (§15.4). Only active outside Play mode: in Play mode ProgressService keeps the data in
    // memory and would overwrite the change with its next save.
    public static class DebugMenu
    {
        private const string ResetItem = "Tools/Game/Reset Progress";
        private const string UnlockItem = "Tools/Game/Unlock All Levels";

        [MenuItem(ResetItem)]
        private static void ResetProgressMenu() => ResetProgress(PlayerPrefsProgressStorage.DefaultKey);

        [MenuItem(UnlockItem)]
        private static void UnlockAllLevelsMenu()
        {
            string[] guids = AssetDatabase.FindAssets("t:" + nameof(LevelCatalog));
            if (guids.Length != 1)
            {
                Debug.LogError($"Expected exactly one LevelCatalog asset, found {guids.Length}.");
                return;
            }
            var catalog = AssetDatabase.LoadAssetAtPath<LevelCatalog>(AssetDatabase.GUIDToAssetPath(guids[0]));
            UnlockAllLevels(PlayerPrefsProgressStorage.DefaultKey, catalog.Count);
        }

        [MenuItem(ResetItem, true)]
        [MenuItem(UnlockItem, true)]
        public static bool CanEditProgress() => !EditorApplication.isPlaying;

        // A build the Unity Profiler can attach to on the phone (§16.5). These are per-user editor settings.
        [MenuItem("Tools/Game/Configure Development Build")]
        private static void ConfigureDevelopmentBuild()
        {
            EditorUserBuildSettings.development = true;
            EditorUserBuildSettings.connectProfiler = true;
            EditorUserBuildSettings.buildWithDeepProfilingSupport = false;   // deep profiling distorts the timings
            Debug.Log("Development Build + Autoconnect Profiler are on. Build and Run to the phone, then open Window > Analysis > Profiler.");
        }

        public static void ResetProgress(string key) => PlayerPrefs.DeleteKey(key);

        // Every level but the last is completed, so every level is open.
        public static void UnlockAllLevels(string key, int levelCount) =>
            new PlayerPrefsProgressStorage(key).Save(new SaveData { HighestCompletedIndex = levelCount - 2 });
    }
}
