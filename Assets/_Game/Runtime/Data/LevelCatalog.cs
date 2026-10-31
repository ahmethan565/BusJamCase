using System;
using System.Collections.Generic;
using Game.Core.Levels;
using UnityEngine;

namespace Game.Runtime.Data
{
    // The ordered level list. Order here = order in the game. Release rule: only append at the end.
    [CreateAssetMenu(menuName = "Game/Level Catalog")]
    public sealed class LevelCatalog : ScriptableObject, ILevelCatalog
    {
        [SerializeField] private List<LevelAsset> levels = new List<LevelAsset>();

        public int Count => levels.Count;

        public bool HasNext(int index) => index + 1 < Count;

        // For editor tooling (catalog inspector, tests): the raw asset at an index.
        public LevelAsset GetAsset(int index) => levels[index];

        // Parses on every call (no cache): cheap, called once per level, and editor edits show up immediately.
        public LevelData Get(int index)
        {
            LevelAsset asset = levels[index];
            ParseResult result = asset != null ? asset.Parse() : null;
            if (result != null && result.Success) return result.Level;

            string reason = result == null ? "empty catalog entry" : string.Join("; ", result.Errors);
            string message = $"Level {index} is broken and cannot be played: {reason}";
            Debug.LogError(message, this);
            throw new InvalidOperationException(message);
        }
    }
}
