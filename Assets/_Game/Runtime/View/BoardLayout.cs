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
            ComputeBackdrops();
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

        // The next bus waits here, right behind the parked one; only part of it is on screen (it is not in ContentBounds).
        public Vector3 BusQueuePosition => BusParkPosition + new Vector3(s.BusQueueOffsetX, 0f, 0f);
        // A follower drives in one queue offset behind its leader.
        public Vector3 BusQueueEnterPosition => BusEnterPosition + new Vector3(s.BusQueueOffsetX, 0f, 0f);

        // World -> cell (a single division per axis)
        public GridPos WorldToCell(Vector3 world) => new GridPos(
            Mathf.RoundToInt((world.x - Origin.x) / s.CellSize),
            Mathf.RoundToInt((Origin.z - world.z) / s.CellSize));

        // The backdrops under the board (and the road) and under the seat row; they touch at the line between
        // the road and the seats, so board and seats look joined. The bus lane stays outside them.
        public Bounds BoardBackdrop { get; private set; }
        public Bounds StopBackdrop { get; private set; }

        // The road under the bus lane: from the stop backdrop's top edge, centered on the park point; it reaches past
        // where buses spawn and vanish, so it spans the whole screen width. Not in ContentBounds.
        public Bounds BusRoad { get; private set; }

        // The bus stop sign: on the road's seat-side edge, ahead of the parked bus's nose with a visible gap between its plate
        // and the bus (offset / 2 + 0.5 = half a bus + a 0.3 gap + half the 0.62 plate + a little); clear of the seat -> door walks.
        public Vector3 BusStopSignPosition =>
            new Vector3(BusParkPosition.x - (s.BusQueueOffsetX / 2f + 0.5f), 0f, BusRoad.min.z);

        // Board + waiting slots + backdrops + bus area (for the camera)
        public Bounds ContentBounds { get; }

        private void ComputeBackdrops()
        {
            float half = s.CellSize / 2f;
            float pad = s.BackdropPadding * s.CellSize;
            Vector3 topLeft = CellToWorld(new GridPos(0, 0));
            Vector3 bottomRight = CellToWorld(new GridPos(width - 1, height - 1));
            float joinZ = (s.RoadOffset + s.SlotsOffset) / 2f * s.CellSize;

            BoardBackdrop = FromEdges(topLeft.x - half - pad, bottomRight.x + half + pad, bottomRight.z - half - pad, joinZ);

            float slotsHalfWidth = Mathf.Abs(SlotPosition(0).x) + s.SlotSpacing / 2f;
            float slotsTop = s.SlotsOffset * s.CellSize + s.SlotSpacing / 2f + pad;
            StopBackdrop = FromEdges(-slotsHalfWidth - pad, slotsHalfWidth + pad, joinZ, slotsTop);

            float roadHalfWidth = BusParkPosition.z - slotsTop;
            BusRoad = FromEdges(BusExitPosition.x - s.BusQueueOffsetX, BusQueueEnterPosition.x + s.BusQueueOffsetX,
                slotsTop, BusParkPosition.z + roadHalfWidth);
        }

        private static Bounds FromEdges(float minX, float maxX, float minZ, float maxZ) =>
            new Bounds(new Vector3((minX + maxX) / 2f, 0f, (minZ + maxZ) / 2f), new Vector3(maxX - minX, 0f, maxZ - minZ));

        private Bounds ComputeContentBounds()
        {
            float half = s.CellSize / 2f;
            float minX = Mathf.Min(BoardBackdrop.min.x, StopBackdrop.min.x);
            float maxX = Mathf.Max(BoardBackdrop.max.x, StopBackdrop.max.x);
            float minZ = BoardBackdrop.min.z;
            float maxZ = BusParkPosition.z + half;

            var center = new Vector3((minX + maxX) / 2f, 0f, (minZ + maxZ) / 2f);
            return new Bounds(center, new Vector3(maxX - minX, 0f, maxZ - minZ));
        }
    }
}
