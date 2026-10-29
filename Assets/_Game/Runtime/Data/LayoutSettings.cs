using UnityEngine;

namespace Game.Runtime.Data
{
    // World placement values (§9.2). All world-coordinate math using them lives in BoardLayout.
    [CreateAssetMenu(menuName = "Game/Layout Settings")]
    public sealed class LayoutSettings : ScriptableObject
    {
        [SerializeField] private float cellSize = 1.0f;
        [Tooltip("Distance of the road from the board's top edge, in cells")]
        [SerializeField] private float roadOffset = 1.0f;
        [Tooltip("Distance of the waiting slot row, in cells")]
        [SerializeField] private float slotsOffset = 2.0f;
        [Tooltip("Bus park point, in cells")]
        [SerializeField] private float busOffset = 3.5f;
        [SerializeField] private float slotSpacing = 1.1f;
        [Tooltip("Distance of the door point in front (-z) of the bus")]
        [SerializeField] private float doorOffsetZ = 0.6f;
        [Tooltip("Off-screen x where buses start and end")]
        [SerializeField] private float busOffscreenX = 10f;
        [Tooltip("How far right of the parked bus the next bus waits (bus length 2.4 + a small gap)")]
        [SerializeField] private float busQueueOffsetX = 2.7f;

        public float CellSize => cellSize;
        public float RoadOffset => roadOffset;
        public float SlotsOffset => slotsOffset;
        public float BusOffset => busOffset;
        public float SlotSpacing => slotSpacing;
        public float DoorOffsetZ => doorOffsetZ;
        public float BusOffscreenX => busOffscreenX;
        public float BusQueueOffsetX => busQueueOffsetX;
    }
}
