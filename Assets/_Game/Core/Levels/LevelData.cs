using System.Collections.Generic;

namespace Game.Core.Levels
{
    // Immutable. Any number of new GameSessions can be built from the same LevelData.
    public sealed class LevelData
    {
        public LevelData(int version, int width, int height, IReadOnlyList<CellType> cells,
                         IReadOnlyList<PassengerSpawn> passengers, IReadOnlyList<PassengerColor> busColors,
                         int busCapacity, int waitingSlots)
        {
            Version = version;
            Width = width;
            Height = height;
            Cells = new List<CellType>(cells).AsReadOnly();
            Passengers = new List<PassengerSpawn>(passengers).AsReadOnly();
            BusColors = new List<PassengerColor>(busColors).AsReadOnly();
            BusCapacity = busCapacity;
            WaitingSlots = waitingSlots;
        }

        public int Version { get; }
        public int Width { get; }
        public int Height { get; }
        public IReadOnlyList<CellType> Cells { get; }              // Width * Height, index = y * Width + x
        public IReadOnlyList<PassengerSpawn> Passengers { get; }   // ids in reading order
        public IReadOnlyList<PassengerColor> BusColors { get; }    // arrival order
        public int BusCapacity { get; }
        public int WaitingSlots { get; }
    }
}
