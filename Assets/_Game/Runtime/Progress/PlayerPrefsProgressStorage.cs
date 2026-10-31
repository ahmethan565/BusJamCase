using Game.Core.Progress;
using UnityEngine;

namespace Game.Runtime.Progress
{
    // The chosen storage: one JSON string in PlayerPrefs.
    public sealed class PlayerPrefsProgressStorage : IProgressStorage
    {
        public const string DefaultKey = "progress";
        private readonly string key;

        // The key can be injected so tests never overwrite the real save.
        public PlayerPrefsProgressStorage(string key = DefaultKey) => this.key = key;

        public SaveData Load()
        {
            string json = PlayerPrefs.GetString(key, "");
            if (string.IsNullOrEmpty(json)) return new SaveData();
            try { return JsonUtility.FromJson<SaveData>(json) ?? new SaveData(); }
            catch { return new SaveData(); }   // corrupt save: start over, never crash
        }

        public void Save(SaveData data)
        {
            PlayerPrefs.SetString(key, JsonUtility.ToJson(data));
            PlayerPrefs.Save();                // OnApplicationQuit is NOT reliable on mobile
        }
    }
}
