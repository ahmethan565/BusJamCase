using System;
using System.Collections.Generic;
using System.Linq;
using Game.Core.Levels;
using Game.Core.Path;

namespace Game.Core.Tests
{
    // Builds levels from text WITHOUT validation, so rule tests can use incomplete levels on purpose.
    public static class TestSessions
    {
        public static LevelData Level(string[] rows, string buses, int capacity, int slots)
        {
            ParseResult result = LevelParser.Parse(rows, buses, capacity, slots, 1);
            if (!result.Success)
                throw new ArgumentException("Test level does not parse: " + string.Join("; ", result.Errors));
            return result.Level;
        }

        public static GameSession From(string[] rows, string buses, int capacity, int slots,
                                       IGameListener listener = null, bool autoStart = true)
        {
            var session = new GameSession(Level(rows, buses, capacity, slots), new BfsPathFinder(),
                                          listener ?? new NullGameListener());
            if (autoStart) session.Start();
            return session;
        }
    }

    public static class TestBoards
    {
        public static Board From(params string[] rows)
        {
            LevelData level = TestSessions.Level(rows, "", 1, 1);
            IEnumerable<Passenger> passengers = level.Passengers
                .Select(spawn => new Passenger(spawn.Id, spawn.Color, spawn.Position));
            return new Board(level.Width, level.Height, level.Cells.ToArray(), passengers);
        }
    }
}
