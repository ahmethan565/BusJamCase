using Game.Runtime.Bootstrap;
using Game.Runtime.Data;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Game.Editor
{
    // "Play this level": writes the request, opens Main.unity and enters Play mode.
    // GameBootstrap reads it through EditorLevelRequest and keeps progress in memory for that session.
    public static class EditorLevelLauncher
    {
        private const string MainScenePath = "Assets/_Game/Scenes/Main.unity";

        public static void Play(LevelAsset level)
        {
            LevelCatalog catalog = FindCatalog();
            int index = catalog == null ? -1 : FindIndex(catalog, level);
            if (index < 0)
            {
                EditorUtility.DisplayDialog("Play This Level",
                    $"'{level.name}' is not in the catalog. Append it to the LevelCatalog first.", "OK");
                return;
            }

            if (EditorSceneManager.GetActiveScene().path != MainScenePath)
            {
                if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
                EditorSceneManager.OpenScene(MainScenePath);
            }

            WriteRequest(index);
            EditorApplication.isPlaying = true;
        }

        public static int FindIndex(LevelCatalog catalog, LevelAsset level)
        {
            for (int i = 0; i < catalog.Count; i++)
                if (catalog.GetAsset(i) == level) return i;
            return -1;
        }

        public static void WriteRequest(int levelIndex)
        {
            PlayerPrefs.SetInt(EditorLevelRequest.Key, levelIndex);
            PlayerPrefs.Save();
        }

        public static LevelCatalog FindCatalog()
        {
            string[] guids = AssetDatabase.FindAssets("t:" + nameof(LevelCatalog));
            return guids.Length == 1
                ? AssetDatabase.LoadAssetAtPath<LevelCatalog>(AssetDatabase.GUIDToAssetPath(guids[0]))
                : null;
        }
    }
}
