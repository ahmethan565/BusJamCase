namespace Game.Core
{
    public sealed class WaitingArea
    {
        private readonly Passenger[] slots;   // null = free
        private int count;

        public WaitingArea(int slotCount) => slots = new Passenger[slotCount];

        public int SlotCount => slots.Length;
        public bool IsFull => count == slots.Length;
        public bool IsEmpty => count == 0;

        public Passenger GetAt(int slot) => slots[slot];   // null if free

        // Puts the passenger into the lowest free slot.
        public bool TryAdd(Passenger p, out int slot)
        {
            for (slot = 0; slot < slots.Length; slot++)
            {
                if (slots[slot] != null) continue;
                slots[slot] = p;
                count++;
                return true;
            }
            slot = -1;
            return false;
        }

        // Takes the passenger of the given color from the lowest slot. Others do NOT shift.
        public bool TryTake(PassengerColor color, out Passenger p, out int slot)
        {
            for (slot = 0; slot < slots.Length; slot++)
            {
                p = slots[slot];
                if (p == null || p.Color != color) continue;
                slots[slot] = null;
                count--;
                return true;
            }
            p = null;
            slot = -1;
            return false;
        }

        public WaitingArea Clone()
        {
            var clone = new WaitingArea(slots.Length) { count = count };
            slots.CopyTo(clone.slots, 0);
            return clone;
        }
    }
}
