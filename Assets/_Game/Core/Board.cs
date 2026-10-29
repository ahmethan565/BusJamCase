using System;
using System.Collections.Generic;
using Game.Core.Levels;

namespace Game.Core
{
    // Knows which cell has which type and which passenger. Knows no rules.
    public sealed class Board
    {
        private readonly CellType[] cells;      // length Width * Height
        private readonly Passenger[] occupants; // null = empty
        private readonly bool[] exits;
        private readonly List<GridPos> exitCells;

        public Board(int width, int height, CellType[] cells, IEnumerable<Passenger> passengers)
        {
            if (cells.Length != width * height)
                throw new ArgumentException("Cell count must be width * height.", nameof(cells));

            Width = width;
            Height = height;
            this.cells = (CellType[])cells.Clone();
            occupants = new Passenger[cells.Length];

            foreach (Passenger passenger in passengers)
                Place(passenger);

            exits = new bool[cells.Length];
            exitCells = new List<GridPos>();
            for (int y = 0; y < height; y++)
                for (int x = 0; x < width; x++)
                {
                    var p = new GridPos(x, y);
                    if (!ComputeIsExit(p)) continue;
                    exits[Index(p)] = true;
                    exitCells.Add(p);
                }
        }

        private Board(Board source)
        {
            Width = source.Width;
            Height = source.Height;
            cells = source.cells;               // never modified, safe to share
            exits = source.exits;
            exitCells = source.exitCells;
            occupants = (Passenger[])source.occupants.Clone();
            PassengerCount = source.PassengerCount;
        }

        public int Width { get; }
        public int Height { get; }
        public int PassengerCount { get; private set; }
        public bool IsEmpty => PassengerCount == 0;
        public IReadOnlyList<GridPos> ExitCells => exitCells;

        public bool InBounds(GridPos p) => p.X >= 0 && p.X < Width && p.Y >= 0 && p.Y < Height;

        public CellType GetCell(GridPos p) => InBounds(p) ? cells[Index(p)] : CellType.Void;

        public bool Exists(GridPos p) => GetCell(p) != CellType.Void;

        public bool IsWalkable(GridPos p) => GetCell(p) == CellType.Floor && occupants[Index(p)] == null;

        public bool IsExit(GridPos p) => InBounds(p) && exits[Index(p)];

        public bool TryGetPassenger(GridPos p, out Passenger passenger)
        {
            passenger = InBounds(p) ? occupants[Index(p)] : null;
            return passenger != null;
        }

        public void RemovePassenger(GridPos p)
        {
            if (!TryGetPassenger(p, out _))
                throw new InvalidOperationException($"No passenger at {p}.");
            occupants[Index(p)] = null;
            PassengerCount--;
        }

        public Board Clone() => new Board(this);

        // Used by GameSession and LevelValidator so both build the board the same way.
        internal static Board FromLevel(LevelData level)
        {
            var cells = new CellType[level.Cells.Count];
            for (int i = 0; i < cells.Length; i++) cells[i] = level.Cells[i];
            var passengers = new List<Passenger>(level.Passengers.Count);
            foreach (PassengerSpawn spawn in level.Passengers)
                passengers.Add(new Passenger(spawn.Id, spawn.Color, spawn.Position));
            return new Board(level.Width, level.Height, cells, passengers);
        }

        private void Place(Passenger passenger)
        {
            GridPos p = passenger.StartPosition;
            if (GetCell(p) != CellType.Floor)
                throw new ArgumentException($"Passenger {passenger.Id} is not on a floor cell {p}.");
            if (occupants[Index(p)] != null)
                throw new ArgumentException($"Two passengers on {p}.");
            occupants[Index(p)] = passenger;
            PassengerCount++;
        }

        // §2.4: a floor cell is an exit if every cell above it in its column is Void.
        private bool ComputeIsExit(GridPos p)
        {
            if (GetCell(p) != CellType.Floor) return false;
            for (int y = p.Y - 1; y >= 0; y--)
                if (Exists(new GridPos(p.X, y))) return false;
            return true;
        }

        private int Index(GridPos p) => p.Y * Width + p.X;
    }
}
