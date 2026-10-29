using System;

namespace Game.Core.Progress
{
    [Serializable]
    public sealed class SaveData
    {
        public int Version = 1;
        public int HighestCompletedIndex = -1;   // -1 = no level completed yet
    }
}
