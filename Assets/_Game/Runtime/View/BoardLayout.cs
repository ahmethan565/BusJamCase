using Game.Core;
using Game.Core.Levels;
using Game.Runtime.Data;
using UnityEngine;

namespace Game.Runtime.View
{
    // Pure calculation class (§9.3). ALL world-coordinate math lives here and only here.
    public sealed class BoardLayout
    {
        private readonly LayoutSettings s;
        private readonly int width;
        private readonly int height;
        private readonly int slotCount;

        public BoardLayout(LevelData level, LayoutSettings settings)
        {
            s = settings;
            width = level.Width;
            height = level.Height;
            slotCount = level.WaitingSlots;
            ContentBounds = ComputeContentBounds();
        }

        // The board is centered in x, its top row sits at z = 0.
        public Vector3 Origin => new Vector3(-(width - 1) / 2f * s.CellSize, 0f, 0f);

        public Vector3 CellToWorld(GridPos p) =>
            Origin + new Vector3(p.X * s.CellSize, 0f, -p.Y * s.CellSize);

        public Vector3 RoadPointAbove(GridPos exitCell) =>
            new Vector3(CellToWorld(exitCell).x, 0f, s.RoadOffset * s.CellSize);

        public Vector3 SlotPosition(int slot) =>
            new Vector3((slot - (slotCount - 1) / 2f) * s.SlotSpacing, 0f, s.SlotsOffset * s.CellSize);

        public Vector3 BusParkPosition => new Vector3(0f, 0f, s.BusOffset * s.CellSize);
        public Vector3 DoorPoint => BusParkPosition + new Vector3(0f, 0f, -s.DoorOffsetZ);
        public Vector3 BusEnterPosition => BusParkPosition + new Vector3(s.BusOffscreenX, 0f, 0f);
        public Vector3 BusExitPosition => BusParkPosition + new Vector3(-s.BusOffscreenX, 0f, 0f);

        // World -> cell (a single division per axis)
        public GridPos WorldToCell(Vector3 world) => new GridPos(
            Mathf.RoundToInt((world.x - Origin.x) / s.CellSize),
            Mathf.RoundToInt((Origin.z - world.z) / s.CellSize));

        // Board + waiting slots + bus area (for the camera)
        public Bounds ContentBounds { get; }

        private Bounds ComputeContentBounds()
        {
            float half = s.CellSize / 2f;
            Vector3 topLeft = CellToWorld(new GridPos(0, 0));
            Vector3 bottomRight = CellToWorld(new GridPos(width - 1, height - 1));
            float slotsHalfWidth = Mathf.Abs(SlotPosition(0).x) + s.SlotSpacing / 2f;

            float minX = Mathf.Min(topLeft.x - half, -slotsHalfWidth);
            float maxX = Mathf.Max(bottomRight.x + half, slotsHalfWidth);
            float minZ = bottomRight.z - half;
            float maxZ = BusParkPosition.z + half;

            var center = new Vector3((minX + maxX) / 2f, 0f, (minZ + maxZ) / 2f);
            return new Bounds(center, new Vector3(maxX - minX, 0f, maxZ - minZ));
        }
    }
}
