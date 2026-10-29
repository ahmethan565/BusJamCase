using System.Collections.Generic;
using Game.Core.Levels;

namespace Game.Core.Tests
{
    // The 5 levels and their known solutions from ARCHITECTURE.md §21.1 (also Docs/reference/levels.py).
    public static class DocumentedLevels
    {
        public sealed class Entry
        {
            public string Name;
            public string[] Rows;
            public string Buses;
            public int Capacity;
            public int Slots;
            public GridPos[] KnownSolution;

            public LevelData Level => TestSessions.Level(Rows, Buses, Capacity, Slots);
            public override string ToString() => Name;
        }

        private static GridPos[] Taps(params int[] xy)
        {
            var taps = new GridPos[xy.Length / 2];
            for (int i = 0; i < taps.Length; i++) taps[i] = new GridPos(xy[2 * i], xy[2 * i + 1]);
            return taps;
        }

        public static IEnumerable<Entry> All()
        {
            yield return new Entry
            {
                Name = "Level_01_FirstTap",
                Rows = new[] { "RBR",
                               "B.B",
                               "R.." },
                Buses = "RB", Capacity = 3, Slots = 5,
                KnownSolution = Taps(0,0, 1,0, 2,0, 0,1, 2,1, 0,2),
            };
            yield return new Entry
            {
                Name = "Level_02_Walls",
                Rows = new[] { "GRRB",
                               "R#.G",
                               "BG#B",
                               "...." },
                Buses = "RGB", Capacity = 3, Slots = 5,
                KnownSolution = Taps(0,0, 1,0, 2,0, 3,0, 0,1, 3,1, 0,2, 1,2, 3,2),
            };
            yield return new Entry
            {
                Name = "Level_03_TriangleDown",
                Rows = new[] { "RGBGR",
                               "YBRBY",
                               "-YG.-",
                               "--.--" },
                Buses = "RBGY", Capacity = 3, Slots = 5,
                KnownSolution = Taps(0,0, 1,0, 2,0, 3,0, 4,0, 0,1, 1,1, 2,1, 3,1, 4,1, 1,2, 2,2),
            };
            yield return new Entry
            {
                Name = "Level_04_Holes",
                Rows = new[] { "RRBBGG",
                               "Y-RG-B",
                               "GYB..R",
                               "-BR#G-",
                               "-GYRB-" },
                Buses = "GRBYGRB", Capacity = 3, Slots = 5,
                KnownSolution = Taps(0,0, 1,0, 2,0, 3,0, 4,0, 5,0, 0,1, 3,1, 2,1, 5,1, 0,2, 1,2, 2,2, 5,2,
                                     1,3, 2,3, 2,4, 4,3, 1,4, 3,4, 4,4),
            };
            yield return new Entry
            {
                Name = "Level_05_Peak",
                Rows = new[] { "--RG--",
                               "-BYPG-",
                               "RP#.#B",
                               "GRBRBY",
                               ".R-GBG",
                               "YB.RPG" },
                Buses = "GRPBYRGB", Capacity = 3, Slots = 6,
                KnownSolution = Taps(2,0, 3,0, 1,1, 2,1, 3,1, 4,1, 0,2, 1,2, 0,3, 5,2, 1,3, 3,3, 4,3, 4,4,
                                     4,5, 2,3, 5,3, 1,4, 3,4, 5,4, 0,5, 1,5, 3,5, 5,5),
            };
        }
    }
}
