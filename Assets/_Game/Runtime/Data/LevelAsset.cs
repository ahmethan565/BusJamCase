using Game.Core.Levels;
using UnityEngine;

namespace Game.Runtime.Data
{
    // One level in the text format. Kept 1:1 convertible to JSON (string array + a few numbers).
    [CreateAssetMenu(menuName = "Game/Level")]
    public sealed class LevelAsset : ScriptableObject
    {
        [SerializeField] private int version = 1;
        [SerializeField] private string[] rows;
        [SerializeField] private string buses;
        [SerializeField] private int busCapacity = 3;
        [SerializeField] private int waitingSlots = 5;
        [Tooltip("Seconds to finish the level, counted from the first tap. 0 = no time limit")]
        [SerializeField] private float timeLimitSeconds = 0f;

        public ParseResult Parse() => LevelParser.Parse(rows, buses, busCapacity, waitingSlots, version, timeLimitSeconds);
    }
}
