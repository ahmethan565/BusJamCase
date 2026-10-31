using System.Collections.Generic;

namespace Game.Runtime.View
{
    // Who sits on which waiting slot ON SCREEN. This can differ from the logical slot:
    // a passenger keeps its seat until it really walks to the bus, and passengers of a bus that
    // has not parked yet also wait here. Keeping one owner per slot means two passengers never overlap.
    public sealed class StopSeats
    {
        private const int Free = -1;
        private readonly List<int> occupants = new List<int>();   // passenger id per slot, or Free

        public void Reset(int slotCount)
        {
            occupants.Clear();
            for (int i = 0; i < slotCount; i++) occupants.Add(Free);
        }

        // The preferred slot if it is free, otherwise the lowest free one; -1 if every slot is taken.
        public int Take(int passengerId, int preferred)
        {
            int slot = preferred >= 0 && preferred < occupants.Count && occupants[preferred] == Free
                ? preferred
                : occupants.IndexOf(Free);
            if (slot >= 0) occupants[slot] = passengerId;
            return slot;
        }

        public bool TryRelease(int passengerId, out int slot)
        {
            slot = occupants.IndexOf(passengerId);
            if (slot < 0) return false;
            occupants[slot] = Free;
            return true;
        }

        public bool IsSeated(int passengerId) => occupants.Contains(passengerId);
    }
}
