using System.Collections.Generic;
using Game.Runtime.Data;
using UnityEngine;
using UnityEngine.Pool;

namespace Game.Runtime.View
{
    // Waiting slot markers (§10.4). Owns the SlotMarker pool.
    public sealed class WaitingAreaView : MonoBehaviour
    {
        [SerializeField] private GameObject slotMarkerPrefab;
        [SerializeField] private Transform poolRoot;
        [SerializeField] private int slotMarkerPrewarm = 8;

        private readonly List<GameObject> active = new List<GameObject>();
        private LayoutSettings settings;
        private ObjectPool<GameObject> markers;

        // Once, in the bootstrap.
        public void Init(LayoutSettings layoutSettings)
        {
            settings = layoutSettings;
            markers = PieceFactory.CreatePool(slotMarkerPrefab, poolRoot);
        }

        public void Prewarm() => PieceFactory.Prewarm(markers, slotMarkerPrewarm);

        public void Build(int slotCount, BoardLayout layout)
        {
            for (int slot = 0; slot < slotCount; slot++)
            {
                GameObject marker = markers.Get();
                marker.transform.SetPositionAndRotation(layout.SlotPosition(slot), Quaternion.identity);
                marker.transform.localScale = Vector3.one * settings.CellSize;
                active.Add(marker);
            }
        }

        public void Clear()
        {
            foreach (GameObject marker in active) markers.Release(marker);
            active.Clear();
        }
    }
}
