using System;
using System.Collections.Generic;
using Blockade2D.Core.Model;
using Blockade2D.Core.State;

namespace Blockade2D.Core.Rules
{
    /// <summary>
    /// BFS-based pathfinding engine for connectivity checks and shortest-path calculation.
    /// Supports both standard baseline goals and target position goals (Bulls Eye center).
    /// </summary>
    public static class Pathfinding
    {
        public const int BoardSize = 9;

        /// <summary>
        /// Checks if direct passage between two adjacent cells is blocked by any wall in the collection.
        /// </summary>
        public static bool IsPassageBlocked(BoardPosition from, BoardPosition to, IEnumerable<Wall> walls)
        {
            foreach (var wall in walls)
            {
                if (wall.BlocksPassage(from, to))
                    return true;
            }
            return false;
        }

        #region Standard Baseline Goal Pathfinding

        /// <summary>
        /// Determines whether a path exists for the given player to reach their goal row.
        /// </summary>
        public static bool HasPathToGoal(
            PlayerId player,
            BoardPosition startPos,
            IEnumerable<Wall> walls,
            int boardSize = BoardSize)
        {
            return GetShortestPathDistance(player, startPos, walls, boardSize) != -1;
        }

        /// <summary>
        /// Computes the shortest distance (in steps) from startPos to the player's goal row using BFS.
        /// Returns -1 if no path exists.
        /// </summary>
        public static int GetShortestPathDistance(
            PlayerId player,
            BoardPosition startPos,
            IEnumerable<Wall> walls,
            int boardSize = BoardSize)
        {
            int goalRow = player.GoalRow(boardSize);
            if (startPos.Y == goalRow)
                return 0;

            var queue = new Queue<(BoardPosition pos, int dist)>();
            var visited = new bool[boardSize, boardSize];

            queue.Enqueue((startPos, 0));
            visited[startPos.X, startPos.Y] = true;

            while (queue.Count > 0)
            {
                var (curr, dist) = queue.Dequeue();

                if (curr.Y == goalRow)
                    return dist;

                foreach (var dir in BoardPosition.OrthogonalDirections)
                {
                    BoardPosition next = curr + dir;

                    if (!next.IsWithinBoard(boardSize))
                        continue;

                    if (visited[next.X, next.Y])
                        continue;

                    if (IsPassageBlocked(curr, next, walls))
                        continue;

                    visited[next.X, next.Y] = true;
                    queue.Enqueue((next, dist + 1));
                }
            }

            return -1; // Unreachable
        }

        /// <summary>
        /// Computes and returns the full shortest path coordinates to the goal row.
        /// Returns null if no path exists.
        /// </summary>
        public static List<BoardPosition> GetShortestPath(
            PlayerId player,
            BoardPosition startPos,
            IEnumerable<Wall> walls,
            int boardSize = BoardSize)
        {
            int goalRow = player.GoalRow(boardSize);
            var queue = new Queue<BoardPosition>();
            var visited = new bool[boardSize, boardSize];
            var parent = new Dictionary<BoardPosition, BoardPosition>();

            queue.Enqueue(startPos);
            visited[startPos.X, startPos.Y] = true;

            BoardPosition? destination = null;

            while (queue.Count > 0)
            {
                var curr = queue.Dequeue();

                if (curr.Y == goalRow)
                {
                    destination = curr;
                    break;
                }

                foreach (var dir in BoardPosition.OrthogonalDirections)
                {
                    BoardPosition next = curr + dir;

                    if (!next.IsWithinBoard(boardSize))
                        continue;

                    if (visited[next.X, next.Y])
                        continue;

                    if (IsPassageBlocked(curr, next, walls))
                        continue;

                    visited[next.X, next.Y] = true;
                    parent[next] = curr;
                    queue.Enqueue(next);
                }
            }

            if (!destination.HasValue)
                return null;

            var path = new List<BoardPosition>();
            BoardPosition backtrack = destination.Value;

            while (backtrack != startPos)
            {
                path.Add(backtrack);
                backtrack = parent[backtrack];
            }

            path.Add(startPos);
            path.Reverse();
            return path;
        }

        #endregion

        #region Target Position (Bulls Eye) Goal Pathfinding

        /// <summary>
        /// Determines whether a path exists from startPos to targetPos.
        /// </summary>
        public static bool HasPathToPosition(
            BoardPosition startPos,
            BoardPosition targetPos,
            IEnumerable<Wall> walls,
            int boardSize = BoardSize)
        {
            return GetShortestPathDistanceToPosition(startPos, targetPos, walls, boardSize) != -1;
        }

        /// <summary>
        /// Computes shortest distance in steps from startPos to targetPos using BFS.
        /// Returns -1 if target is unreachable.
        /// </summary>
        public static int GetShortestPathDistanceToPosition(
            BoardPosition startPos,
            BoardPosition targetPos,
            IEnumerable<Wall> walls,
            int boardSize = BoardSize)
        {
            if (startPos == targetPos)
                return 0;

            var queue = new Queue<(BoardPosition pos, int dist)>();
            var visited = new bool[boardSize, boardSize];

            queue.Enqueue((startPos, 0));
            visited[startPos.X, startPos.Y] = true;

            while (queue.Count > 0)
            {
                var (curr, dist) = queue.Dequeue();

                if (curr == targetPos)
                    return dist;

                foreach (var dir in BoardPosition.OrthogonalDirections)
                {
                    BoardPosition next = curr + dir;

                    if (!next.IsWithinBoard(boardSize))
                        continue;

                    if (visited[next.X, next.Y])
                        continue;

                    if (IsPassageBlocked(curr, next, walls))
                        continue;

                    visited[next.X, next.Y] = true;
                    queue.Enqueue((next, dist + 1));
                }
            }

            return -1;
        }

        /// <summary>
        /// Computes and returns the full shortest path coordinates to a target position.
        /// Returns null if unreachable.
        /// </summary>
        public static List<BoardPosition> GetShortestPathToPosition(
            BoardPosition startPos,
            BoardPosition targetPos,
            IEnumerable<Wall> walls,
            int boardSize = BoardSize)
        {
            if (startPos == targetPos)
                return new List<BoardPosition> { startPos };

            var queue = new Queue<BoardPosition>();
            var visited = new bool[boardSize, boardSize];
            var parent = new Dictionary<BoardPosition, BoardPosition>();

            queue.Enqueue(startPos);
            visited[startPos.X, startPos.Y] = true;

            bool reached = false;

            while (queue.Count > 0)
            {
                var curr = queue.Dequeue();

                if (curr == targetPos)
                {
                    reached = true;
                    break;
                }

                foreach (var dir in BoardPosition.OrthogonalDirections)
                {
                    BoardPosition next = curr + dir;

                    if (!next.IsWithinBoard(boardSize))
                        continue;

                    if (visited[next.X, next.Y])
                        continue;

                    if (IsPassageBlocked(curr, next, walls))
                        continue;

                    visited[next.X, next.Y] = true;
                    parent[next] = curr;
                    queue.Enqueue(next);
                }
            }

            if (!reached)
                return null;

            var path = new List<BoardPosition>();
            BoardPosition backtrack = targetPos;

            while (backtrack != startPos)
            {
                path.Add(backtrack);
                backtrack = parent[backtrack];
            }

            path.Add(startPos);
            path.Reverse();
            return path;
        }

        #endregion

        #region Unified Mode-Aware Pathfinding

        public static bool HasPathToGoal(
            GameState state,
            PlayerId player,
            IEnumerable<Wall> walls)
        {
            var pos = state.GetPlayerPosition(player);
            if (state.Mode == GameMode.BullsEye)
            {
                return HasPathToPosition(pos, state.CenterCell, walls, state.BoardSize);
            }
            return HasPathToGoal(player, pos, walls, state.BoardSize);
        }

        public static int GetShortestPathDistance(
            GameState state,
            PlayerId player,
            IEnumerable<Wall> walls)
        {
            var pos = state.GetPlayerPosition(player);
            if (state.Mode == GameMode.BullsEye)
            {
                return GetShortestPathDistanceToPosition(pos, state.CenterCell, walls, state.BoardSize);
            }
            return GetShortestPathDistance(player, pos, walls, state.BoardSize);
        }

        public static List<BoardPosition> GetShortestPath(
            GameState state,
            PlayerId player,
            IEnumerable<Wall> walls)
        {
            var pos = state.GetPlayerPosition(player);
            if (state.Mode == GameMode.BullsEye)
            {
                return GetShortestPathToPosition(pos, state.CenterCell, walls, state.BoardSize);
            }
            return GetShortestPath(player, pos, walls, state.BoardSize);
        }

        #endregion
    }
}
