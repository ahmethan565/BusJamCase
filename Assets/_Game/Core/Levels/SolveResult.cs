using System.Collections.Generic;

namespace Game.Core.Levels
{
    public sealed class SolveResult
    {
        public SolveResult(bool isSolvable, IReadOnlyList<GridPos> solution, int statesExplored, bool hitLimit)
        {
            IsSolvable = isSolvable;
            Solution = solution;
            StatesExplored = statesExplored;
            HitLimit = hitLimit;
        }

        public bool IsSolvable { get; }
        public IReadOnlyList<GridPos> Solution { get; }   // tap sequence; empty when not solvable
        public int StatesExplored { get; }
        public bool HitLimit { get; }                     // true = search stopped early, answer unknown
    }
}
