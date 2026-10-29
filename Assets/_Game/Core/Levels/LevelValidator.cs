using System.Collections.Generic;
using Game.Core.Path;

namespace Game.Core.Levels
{
    // Checks the rules of §7.3. Every rule is checked; it does not stop at the first error.
    public sealed class LevelValidator
    {
        public const int SupportedVersion = 1;

        private readonly IPathFinder pathFinder;

        public LevelValidator(IPathFinder pathFinder) => this.pathFinder = pathFinder;

        // Empty list = valid. Messages are Turkish and shown as-is in the level editor.
        public IReadOnlyList<string> Validate(LevelData level)
        {
            var errors = new List<string>();

            if (level.Version != SupportedVersion)
                errors.Add($"Desteklenmeyen sürüm: {level.Version} (desteklenen: {SupportedVersion}).");
            if (level.BusCapacity < 1)
                errors.Add("Otobüs kapasitesi en az 1 olmalı.");
            if (level.WaitingSlots < 1)
                errors.Add("Bekleme alanında en az 1 koltuk olmalı.");
            if (level.Passengers.Count == 0)
                errors.Add("Bölümde hiç yolcu yok.");
            if (level.BusColors.Count == 0)
                errors.Add("Bölümde hiç otobüs yok.");

            CheckColorCounts(level, errors);

            Board board = Board.FromLevel(level);
            if (board.ExitCells.Count == 0)
                errors.Add("Hiç çıkış hücresi yok.");

            HashSet<GridPos> reachable = pathFinder.ReachableFromExits(board, ignorePassengers: true);
            foreach (PassengerSpawn spawn in level.Passengers)
                if (!reachable.Contains(spawn.Position))
                    errors.Add($"{spawn.Position} konumundaki yolcu çıkışa hiç ulaşamaz.");

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
                    errors.Add($"{TurkishName(color)}: {passengers} yolcu var, {buses} otobüs × {level.BusCapacity} = {needed} gerekiyor.");
            }
        }

        private static void Increment(Dictionary<PassengerColor, int> counts, PassengerColor color)
        {
            counts.TryGetValue(color, out int count);
            counts[color] = count + 1;
        }

        private static string TurkishName(PassengerColor color) => color switch
        {
            PassengerColor.Red => "Kırmızı",
            PassengerColor.Green => "Yeşil",
            PassengerColor.Blue => "Mavi",
            PassengerColor.Yellow => "Sarı",
            PassengerColor.Purple => "Mor",
            PassengerColor.Orange => "Turuncu",
            _ => color.ToString(),
        };
    }
}
