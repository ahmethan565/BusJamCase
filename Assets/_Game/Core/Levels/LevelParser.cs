using System.Collections.Generic;

namespace Game.Core.Levels
{
    // Turns the text format into LevelData. Does not validate rules; that is LevelValidator's job.
    public static class LevelParser
    {
        private readonly struct Symbol
        {
            public readonly CellType Cell;
            public readonly bool HasPassenger;
            public readonly PassengerColor Color;

            public Symbol(CellType cell) { Cell = cell; HasPassenger = false; Color = default; }
            public Symbol(PassengerColor color) { Cell = CellType.Floor; HasPassenger = true; Color = color; }
        }

        // The single character table. A new cell type = one more row here.
        private static readonly Dictionary<char, Symbol> Symbols = new Dictionary<char, Symbol>
        {
            { '.', new Symbol(CellType.Floor) },
            { '#', new Symbol(CellType.Wall) },
            { '-', new Symbol(CellType.Void) },
            { 'R', new Symbol(PassengerColor.Red) },
            { 'G', new Symbol(PassengerColor.Green) },
            { 'B', new Symbol(PassengerColor.Blue) },
            { 'Y', new Symbol(PassengerColor.Yellow) },
            { 'P', new Symbol(PassengerColor.Purple) },
            { 'O', new Symbol(PassengerColor.Orange) },
        };

        public static ParseResult Parse(IReadOnlyList<string> rows, string buses,
                                        int busCapacity, int waitingSlots, int version, float timeLimitSeconds = 0f)
        {
            var errors = new List<string>();
            if (rows == null || rows.Count == 0)
            {
                errors.Add("The level has no rows.");
                return ParseResult.Failed(errors);
            }

            int height = rows.Count;
            int width = rows[0]?.Length ?? 0;
            var cells = new CellType[width * height];
            var passengers = new List<PassengerSpawn>();

            for (int y = 0; y < height; y++)
            {
                string row = rows[y] ?? "";
                if (row.Length != width)
                {
                    errors.Add($"Row {y} has length {row.Length}, expected {width}.");
                    continue;
                }

                for (int x = 0; x < width; x++)
                {
                    if (!Symbols.TryGetValue(row[x], out Symbol symbol))
                    {
                        errors.Add($"Unknown character '{row[x]}' at ({x},{y}).");
                        continue;
                    }
                    cells[y * width + x] = symbol.Cell;
                    if (symbol.HasPassenger)
                        passengers.Add(new PassengerSpawn(passengers.Count, symbol.Color, new GridPos(x, y)));
                }
            }

            var busColors = new List<PassengerColor>();
            foreach (char letter in buses ?? "")
            {
                if (letter == ' ') continue;
                if (Symbols.TryGetValue(letter, out Symbol symbol) && symbol.HasPassenger)
                    busColors.Add(symbol.Color);
                else
                    errors.Add($"Unknown letter '{letter}' in the bus list.");
            }

            if (errors.Count > 0) return ParseResult.Failed(errors);
            return ParseResult.Ok(new LevelData(version, width, height, cells, passengers, busColors,
                                                busCapacity, waitingSlots, timeLimitSeconds));
        }
    }
}
