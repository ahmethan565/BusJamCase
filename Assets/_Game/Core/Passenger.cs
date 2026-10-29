namespace Game.Core
{
    // Immutable. The passenger's current position lives in Board (single source of truth).
    public sealed class Passenger
    {
        public Passenger(int id, PassengerColor color, GridPos startPosition)
        {
            Id = id;
            Color = color;
            StartPosition = startPosition;
        }

        public int Id { get; }                  // unique within a level; used to map views
        public PassengerColor Color { get; }
        public GridPos StartPosition { get; }   // initial placement for the view
    }
}
