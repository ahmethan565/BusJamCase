namespace Game.Core.Levels
{
    public readonly struct PassengerSpawn
    {
        public readonly int Id;
        public readonly PassengerColor Color;
        public readonly GridPos Position;

        public PassengerSpawn(int id, PassengerColor color, GridPos position)
        {
            Id = id;
            Color = color;
            Position = position;
        }
    }
}
