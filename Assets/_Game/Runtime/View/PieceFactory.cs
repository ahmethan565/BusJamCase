using UnityEngine;
using UnityEngine.Pool;

namespace Game.Runtime.View
{
    // Pools for static pieces that have no animation (board tiles, slot markers).
    internal static class PieceFactory
    {
        public static ObjectPool<GameObject> CreatePool(GameObject prefab, Transform parent) =>
            new ObjectPool<GameObject>(
                createFunc: () => Object.Instantiate(prefab, parent),
                actionOnGet: piece => piece.SetActive(true),
                actionOnRelease: piece => piece.SetActive(false),
                collectionCheck: false);

        // A backdrop (a unit cube whose top face is at the root's origin) stretched over an area, just under the tiles.
        public static void PlaceBackdrop(Transform backdrop, Bounds area, float cellSize)
        {
            const float tileBottom = -0.1f;   // FloorTile model spans y -0.1..0 (per cell size)
            const float thickness = 0.05f;
            backdrop.position = new Vector3(area.center.x, tileBottom * cellSize - 0.01f, area.center.z);
            backdrop.localScale = new Vector3(area.size.x, thickness, area.size.z);
            backdrop.gameObject.SetActive(true);
        }

        // Takes `count` objects and gives them back, so the first level does not Instantiate.
        public static void Prewarm(ObjectPool<GameObject> pool, int count)
        {
            var taken = new GameObject[count];
            for (int i = 0; i < count; i++) taken[i] = pool.Get();
            foreach (GameObject piece in taken) pool.Release(piece);
        }
    }
}
