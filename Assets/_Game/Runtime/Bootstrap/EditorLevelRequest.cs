#if UNITY_EDITOR
using UnityEngine;

namespace Game.Runtime.Bootstrap
{
    // Reads the "play this level" request written by EditorLevelLauncher (§15.3).
    // The key travels through PlayerPrefs because Game.Runtime cannot reference Game.Editor.
    public static class EditorLevelRequest
    {
        public const string Key = "Editor.RequestedLevel";

        // Read once: the request is deleted, so the next normal Play starts at the menu.
        public static bool TryConsume(out int levelIndex)
        {
            levelIndex = PlayerPrefs.GetInt(Key, -1);
            if (levelIndex < 0) return false;
            PlayerPrefs.DeleteKey(Key);
            PlayerPrefs.Save();
            return true;
        }
    }
}
#endif
