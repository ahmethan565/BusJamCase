using System.Collections.Generic;
using Game.Core;
using Game.Core.Levels;
using Game.Runtime.Data;
using UnityEngine;
using UnityEngine.Pool;

namespace Game.Runtime.View
{
    // Static board pieces (§10.4). Owns the FloorTile, WallBlock and EdgePiece pools.
    public sealed class BoardView : MonoBehaviour
    {
        [SerializeField] private GameObject floorTilePrefab;
        [SerializeField] private GameObject wallBlockPrefab;
        [SerializeField] private GameObject edgePiecePrefab;
        [SerializeField] private Transform poolRoot;
        [SerializeField] private int floorTilePrewarm = 64;
        [SerializeField] private int wallBlockPrewarm = 8;
        [SerializeField] private int edgePiecePrewarm = 64;

        private readonly List<(ObjectPool<GameObject> pool, GameObject piece)> active =
            new List<(ObjectPool<GameObject>, GameObject)>();
        private LayoutSettings settings;
        private ObjectPool<GameObject> floorTiles;
        private ObjectPool<GameObject> wallBlocks;
        private ObjectPool<GameObject> edgePieces;

        // Once, in the bootstrap.
        public void Init(LayoutSettings layoutSettings)
        {
            settings = layoutSettings;
            floorTiles = PieceFactory.CreatePool(floorTilePrefab, poolRoot);
            wallBlocks = PieceFactory.CreatePool(wallBlockPrefab, poolRoot);
            edgePieces = PieceFactory.CreatePool(edgePiecePrefab, poolRoot);
        }

        public void Prewarm()
        {
            PieceFactory.Prewarm(floorTiles, floorTilePrewarm);
            PieceFactory.Prewarm(wallBlocks, wallBlockPrewarm);
            PieceFactory.Prewarm(edgePieces, edgePiecePrewarm);
        }

        public void Build(LevelData level, BoardLayout layout)
        {
            for (int y = 0; y < level.Height; y++)
                for (int x = 0; x < level.Width; x++)
                {
                    CellType cell = CellAt(level, x, y);
                    if (cell == CellType.Void) continue;   // nothing is drawn

                    var p = new GridPos(x, y);
                    Vector3 center = layout.CellToWorld(p);
                    Place(floorTiles, center, Quaternion.identity);
                    if (cell == CellType.Wall) Place(wallBlocks, center, Quaternion.identity);

                    // An edge on every side whose neighbour does not exist. No auto-tiling.
                    foreach (GridPos direction in GridPos.Directions)
                    {
                        GridPos neighbour = p + direction;
                        if (CellAt(level, neighbour.X, neighbour.Y) != CellType.Void) continue;

                        Vector3 offset = new Vector3(direction.X, 0f, -direction.Y) * (settings.CellSize / 2f);
                        Quaternion rotation = direction.X == 0 ? Quaternion.identity : Quaternion.Euler(0f, 90f, 0f);
                        Place(edgePieces, center + offset, rotation);
                    }
                }
        }

        public void Clear()
        {
            foreach (var (pool, piece) in active) pool.Release(piece);
            active.Clear();
        }

        private void Place(ObjectPool<GameObject> pool, Vector3 position, Quaternion rotation)
        {
            GameObject piece = pool.Get();
            piece.transform.SetPositionAndRotation(position, rotation);
            piece.transform.localScale = Vector3.one * settings.CellSize;
            active.Add((pool, piece));
        }

        // Outside the board counts as Void (§2.2).
        private static CellType CellAt(LevelData level, int x, int y)
        {
            if (x < 0 || y < 0 || x >= level.Width || y >= level.Height) return CellType.Void;
            return level.Cells[y * level.Width + x];
        }
    }
}
