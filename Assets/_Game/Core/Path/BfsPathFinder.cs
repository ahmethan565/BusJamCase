using System.Collections.Generic;

namespace Game.Core.Path
{
    public sealed class BfsPathFinder : IPathFinder
    {
        // Reused between searches; reallocated only when the board size changes.
        private bool[] visited = new bool[0];
        private int[] parent = new int[0];
        private int[] queue = new int[0];

        public bool TryFindPathToExit(Board board, GridPos from, out List<GridPos> path)
        {
            path = null;
            if (!board.InBounds(from)) return false;
            if (board.IsExit(from))
            {
                path = new List<GridPos> { from };
                return true;
            }

            PrepareBuffers(board.Width * board.Height);
            int width = board.Width;
            int start = from.Y * width + from.X;
            int head = 0, tail = 0;
            visited[start] = true;
            queue[tail++] = start;

            while (head < tail)
            {
                int current = queue[head++];
                var currentPos = new GridPos(current % width, current / width);

                foreach (GridPos direction in GridPos.Directions)
                {
                    GridPos next = currentPos + direction;
                    if (!board.IsWalkable(next)) continue;

                    int nextIndex = next.Y * width + next.X;
                    if (visited[nextIndex]) continue;
                    visited[nextIndex] = true;
                    parent[nextIndex] = current;

                    if (board.IsExit(next))
                    {
                        path = BuildPath(start, nextIndex, width);
                        return true;
                    }
                    queue[tail++] = nextIndex;
                }
            }
            return false;
        }

        // Multi-source BFS from every exit cell. Not used in the game loop, so allocations are fine.
        public HashSet<GridPos> ReachableFromExits(Board board, bool ignorePassengers)
        {
            var reachable = new HashSet<GridPos>();
            var pending = new Queue<GridPos>();

            foreach (GridPos exit in board.ExitCells)
            {
                if (!IsPassable(board, exit, ignorePassengers)) continue;
                reachable.Add(exit);
                pending.Enqueue(exit);
            }

            while (pending.Count > 0)
            {
                GridPos current = pending.Dequeue();
                foreach (GridPos direction in GridPos.Directions)
                {
                    GridPos next = current + direction;
                    if (reachable.Contains(next) || !IsPassable(board, next, ignorePassengers)) continue;
                    reachable.Add(next);
                    pending.Enqueue(next);
                }
            }
            return reachable;
        }

        private static bool IsPassable(Board board, GridPos p, bool ignorePassengers) =>
            ignorePassengers ? board.GetCell(p) == CellType.Floor : board.IsWalkable(p);

        private void PrepareBuffers(int size)
        {
            if (visited.Length != size)
            {
                visited = new bool[size];
                parent = new int[size];
                queue = new int[size];
            }
            else
            {
                System.Array.Clear(visited, 0, size);
            }
        }

        private List<GridPos> BuildPath(int start, int end, int width)
        {
            var path = new List<GridPos>();
            for (int index = end; index != start; index = parent[index])
                path.Add(new GridPos(index % width, index / width));
            path.Add(new GridPos(start % width, start / width));
            path.Reverse();
            return path;
        }
    }
}
