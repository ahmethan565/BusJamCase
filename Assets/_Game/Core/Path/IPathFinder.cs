using System.Collections.Generic;

namespace Game.Core.Path
{
    public interface IPathFinder
    {
        // If there is a path, returns a new list from start to exit (both included).
        bool TryFindPathToExit(Board board, GridPos from, out List<GridPos> path);

        // Floor cells reachable backwards from all exit cells.
        // ignorePassengers = true: passengers do not block (used by level validation).
        HashSet<GridPos> ReachableFromExits(Board board, bool ignorePassengers);
    }
}
