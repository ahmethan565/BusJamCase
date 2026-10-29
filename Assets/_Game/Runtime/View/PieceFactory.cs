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

        // Takes `count` objects and gives them back, so the first level does not Instantiate.
        public static void Prewarm(ObjectPool<GameObject> pool, int count)
        {
            var taken = new GameObject[count];
            for (int i = 0; i < count; i++) taken[i] = pool.Get();
            foreach (GameObject piece in taken) pool.Release(piece);
        }
    }
}
