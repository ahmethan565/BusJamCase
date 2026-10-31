using System;
using System.Collections.Generic;
using Game.Core.Path;

namespace Game.Core.Levels
{
    // Depth-first search with a visited-state set. Same algorithm as the Python reference simulator.
    public sealed class LevelSolver
    {
        private readonly IPathFinder pathFinder;
        private readonly IGameListener silent = new NullGameListener();

        public LevelSolver(IPathFinder pathFinder) => this.pathFinder = pathFinder;

        public SolveResult Solve(LevelData level, int maxStates = 200_000)
        {
            var search = new Search(maxStates);
            bool solved = Explore(StartSession(level), search);
            IReadOnlyList<GridPos> solution = solved ? search.Moves : new List<GridPos>();
            return new SolveResult(solved, solution, search.Visited.Count, hitLimit: !solved && search.HitLimit);
        }

        // Random-play win rate (0..1) for the editor and tests; only meaningful when comparing levels.
        public float EstimateRandomWinRate(LevelData level, int games, int seed)
        {
            if (games <= 0) throw new ArgumentOutOfRangeException(nameof(games));
            var random = new Random(seed);
            int wins = 0;

            for (int game = 0; game < games; game++)
            {
                GameSession session = StartSession(level);
                while (!session.IsOver)
                {
                    List<GridPos> moves = MovablePassengers(session);
                    if (moves.Count == 0)
                        throw new InvalidOperationException("Softlock: the game is not over but nobody can move (§2.9).");
                    session.Tap(moves[random.Next(moves.Count)]);
                }
                if (session.Result == SessionResult.Won) wins++;
            }
            return (float)wins / games;
        }

        private sealed class Search
        {
            public Search(int maxStates) => MaxStates = maxStates;

            public readonly int MaxStates;
            public readonly HashSet<string> Visited = new HashSet<string>();
            public readonly List<GridPos> Moves = new List<GridPos>();
            public bool HitLimit;
        }

        private bool Explore(GameSession session, Search search)
        {
            if (session.Result == SessionResult.Won) return true;
            if (session.Result == SessionResult.Lost) return false;
            if (search.Visited.Count >= search.MaxStates)
            {
                search.HitLimit = true;
                return false;
            }
            if (!search.Visited.Add(session.StateKey)) return false;

            // Tapping a passenger without a path changes nothing, so only movable passengers are tried.
            foreach (GridPos move in MovablePassengers(session))
            {
                GameSession next = session.Clone(silent);
                next.Tap(move);
                search.Moves.Add(move);
                if (Explore(next, search)) return true;
                search.Moves.RemoveAt(search.Moves.Count - 1);
            }
            return false;
        }

        private GameSession StartSession(LevelData level)
        {
            var session = new GameSession(level, pathFinder, silent);
            session.Start();
            return session;
        }

        private List<GridPos> MovablePassengers(GameSession session)
        {
            Board board = session.Board;
            var moves = new List<GridPos>();
            for (int y = 0; y < board.Height; y++)
                for (int x = 0; x < board.Width; x++)
                {
                    var cell = new GridPos(x, y);
                    if (board.TryGetPassenger(cell, out _) && pathFinder.TryFindPathToExit(board, cell, out _))
                        moves.Add(cell);
                }
            return moves;
        }
    }
}
