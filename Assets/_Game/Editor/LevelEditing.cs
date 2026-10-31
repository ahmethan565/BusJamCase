using System;
using System.Collections.Generic;
using System.Text;
using Game.Core;
using Game.Core.Levels;
using Game.Core.Path;
using Game.Runtime.Data;

namespace Game.Editor
{
    // The pure logic behind the level editor, kept apart from the IMGUI code so it can be tested.
    public static class LevelEditing
    {
        public const char Floor = '.';

        private static readonly Dictionary<PassengerColor, char> Letters = new Dictionary<PassengerColor, char>
        {
            { PassengerColor.Red, 'R' }, { PassengerColor.Green, 'G' }, { PassengerColor.Blue, 'B' },
            { PassengerColor.Yellow, 'Y' }, { PassengerColor.Purple, 'P' }, { PassengerColor.Orange, 'O' },
        };

        public static char Letter(PassengerColor color) => Letters[color];

        // Growing adds floor cells, shrinking crops.
        public static string[] Resize(IReadOnlyList<string> rows, int width, int height)
        {
            var result = new string[height];
            for (int y = 0; y < height; y++)
            {
                string old = rows != null && y < rows.Count && rows[y] != null ? rows[y] : "";
                var row = new StringBuilder(width);
                for (int x = 0; x < width; x++) row.Append(x < old.Length ? old[x] : Floor);
                result[y] = row.ToString();
            }
            return result;
        }

        // Returns a new array; the input is not changed.
        public static string[] Paint(IReadOnlyList<string> rows, int x, int y, char symbol)
        {
            var result = new string[rows.Count];
            for (int i = 0; i < rows.Count; i++) result[i] = rows[i];
            char[] row = result[y].ToCharArray();
            row[x] = symbol;
            result[y] = new string(row);
            return result;
        }

        // One bus per `capacity` passengers of each color, in PassengerColor order. The designer reorders by hand.
        public static bool AutoFillBuses(IReadOnlyList<string> rows, int capacity, out string buses, out string warning)
        {
            var counts = new Dictionary<char, int>();
            foreach (string row in rows)
                foreach (char c in row)
                    counts[c] = counts.TryGetValue(c, out int n) ? n + 1 : 1;

            var result = new StringBuilder();
            var problems = new List<string>();
            foreach (PassengerColor color in (PassengerColor[])Enum.GetValues(typeof(PassengerColor)))
            {
                if (!counts.TryGetValue(Letters[color], out int count)) continue;
                if (capacity < 1 || count % capacity != 0)
                    problems.Add($"{color}: {count} passengers cannot be split into buses of {capacity}.");
                if (capacity >= 1) result.Append(Letters[color], count / capacity);
            }

            buses = result.ToString();
            warning = problems.Count == 0 ? null : string.Join("\n", problems);
            return problems.Count == 0;
        }

        // Parse + validation errors of a level asset; empty = valid.
        public static IReadOnlyList<string> Check(LevelAsset asset)
        {
            ParseResult parsed = asset.Parse();
            if (!parsed.Success) return parsed.Errors;
            return new LevelValidator(new BfsPathFinder()).Validate(parsed.Level);
        }
    }
}
