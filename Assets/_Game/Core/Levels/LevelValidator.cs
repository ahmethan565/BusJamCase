using System.Collections.Generic;
using Game.Core.Path;

namespace Game.Core.Levels
{
    // Checks every level rule and does not stop at the first error.
    public sealed class LevelValidator
    {
        public const int SupportedVersion = 1;

        private readonly IPathFinder pathFinder;

        public LevelValidator(IPathFinder pathFinder) => this.pathFinder = pathFinder;

        // Empty list = valid. Messages are shown as-is in the level editor.
        public IReadOnlyList<string> Validate(LevelData level)
        {
            var errors = new List<string>();

            if (level.Version != SupportedVersion)
                errors.Add($"Unsupported version: {level.Version} (supported: {SupportedVersion}).");
            if (level.BusCapacity < 1)
                errors.Add("Bus capacity must be at least 1.");
            if (level.WaitingSlots < 1)
                errors.Add("The waiting area needs at least 1 slot.");
            if (level.TimeLimitSeconds < 0f)
                errors.Add($"The time limit cannot be negative ({level.TimeLimitSeconds}); use 0 for no limit.");
            if (level.Passengers.Count == 0)
                errors.Add("The level has no passengers.");
            if (level.BusColors.Count == 0)
                errors.Add("The level has no buses.");

            CheckColorCounts(level, errors);

            Board board = Board.FromLevel(level);
            if (board.ExitCells.Count == 0)
                errors.Add("There is no exit cell.");

            HashSet<GridPos> reachable = pathFinder.ReachableFromExits(board, ignorePassengers: true);
            foreach (PassengerSpawn spawn in level.Passengers)
                if (!reachable.Contains(spawn.Position))
                    errors.Add($"The passenger at {spawn.Position} can never reach an exit.");

            return errors;
        }

        private static void CheckColorCounts(LevelData level, List<string> errors)
        {
            var passengerCounts = new Dictionary<PassengerColor, int>();
            var busCounts = new Dictionary<PassengerColor, int>();
            foreach (PassengerSpawn spawn in level.Passengers) Increment(passengerCounts, spawn.Color);
            foreach (PassengerColor color in level.BusColors) Increment(busCounts, color);

            foreach (PassengerColor color in (PassengerColor[])System.Enum.GetValues(typeof(PassengerColor)))
            {
                passengerCounts.TryGetValue(color, out int passengers);
                busCounts.TryGetValue(color, out int buses);
                int needed = buses * level.BusCapacity;
                if (passengers != needed)
                    errors.Add($"{color}: {passengers} passengers, but {buses} buses × {level.BusCapacity} = {needed} are needed.");
            }
        }

        private static void Increment(Dictionary<PassengerColor, int> counts, PassengerColor color)
        {
            counts.TryGetValue(color, out int count);
            counts[color] = count + 1;
        }
    }
}
