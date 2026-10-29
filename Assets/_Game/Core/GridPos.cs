using System;

namespace Game.Core
{
    public readonly struct GridPos : IEquatable<GridPos>
    {
        public readonly int X;
        public readonly int Y;

        public GridPos(int x, int y) { X = x; Y = y; }

        public static readonly GridPos Up    = new GridPos(0, -1);   // towards the road
        public static readonly GridPos Down  = new GridPos(0, 1);
        public static readonly GridPos Left  = new GridPos(-1, 0);
        public static readonly GridPos Right = new GridPos(1, 0);

        // BFS neighbour order: up first (for natural-looking paths)
        public static readonly GridPos[] Directions = { Up, Left, Right, Down };

        public static GridPos operator +(GridPos a, GridPos b) => new GridPos(a.X + b.X, a.Y + b.Y);
        public static bool operator ==(GridPos a, GridPos b) => a.Equals(b);
        public static bool operator !=(GridPos a, GridPos b) => !a.Equals(b);

        public bool Equals(GridPos other) => X == other.X && Y == other.Y;
        public override bool Equals(object obj) => obj is GridPos other && Equals(other);
        public override int GetHashCode() => (X * 397) ^ Y;
        public override string ToString() => $"({X},{Y})";
    }
}
