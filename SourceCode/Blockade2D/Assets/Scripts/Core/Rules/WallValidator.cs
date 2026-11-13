using System.Collections.Generic;
using Blockade2D.Core.Model;
using Blockade2D.Core.State;

namespace Blockade2D.Core.Rules
{
    public enum WallPlacementError
    {
        None = 0,
        NoWallsRemaining,
        OutOfBounds,
        OverlapsExistingWall,
        BlocksPlayerPath
    }

    /// <summary>
    /// Validates wall placement rules, including intersection bounds, overlap prevention,
    /// and the critical BFS non-trapping path check ensuring at least one route to the goal
    /// remains open for all active players in both Classic and Bulls Eye modes.
    /// </summary>
    public static class WallValidator
    {
        /// <summary>
        /// Validates wall placement for a full GameState (supports both Classic 2P and Bulls Eye 4P).
        /// </summary>
        public static WallPlacementError ValidateWallPlacement(
            GameState state,
            PlayerId player,
            Wall wall)
        {
            // 1. Check inventory
            if (state.GetWallsRemaining(player) <= 0)
                return WallPlacementError.NoWallsRemaining;

            // 2. Check bounds (0 to boardSize - 2)
            if (!wall.Intersection.IsValidWallIntersection(state.BoardSize))
                return WallPlacementError.OutOfBounds;

            // 3. Check conflicts/overlaps with existing walls
            foreach (var existing in state.PlacedWalls)
            {
                if (existing.ConflictsWith(wall))
                    return WallPlacementError.OverlapsExistingWall;
            }

            // 4. Simulate placement & run BFS check for ALL active players
            var simulatedWalls = new List<Wall>(state.PlacedWalls.Count + 1);
            simulatedWalls.AddRange(state.PlacedWalls);
            simulatedWalls.Add(wall);

            for (int i = 0; i < state.PlayerCount; i++)
            {
                var p = (PlayerId)i;
                bool canReach = Pathfinding.HasPathToGoal(state, p, simulatedWalls);
                if (!canReach)
                    return WallPlacementError.BlocksPlayerPath;
            }

            return WallPlacementError.None;
        }

        public static WallPlacementError ValidateWallPlacement(
            Wall wall,
            int playerWallsRemaining,
            BoardPosition player1Pos,
            BoardPosition player2Pos,
            IReadOnlyCollection<Wall> existingWalls,
            int boardSize = 9)
        {
            // 1. Check inventory
            if (playerWallsRemaining <= 0)
                return WallPlacementError.NoWallsRemaining;

            // 2. Check bounds (0 to boardSize - 2)
            if (!wall.Intersection.IsValidWallIntersection(boardSize))
                return WallPlacementError.OutOfBounds;

            // 3. Check conflicts/overlaps with existing walls
            foreach (var existing in existingWalls)
            {
                if (existing.ConflictsWith(wall))
                    return WallPlacementError.OverlapsExistingWall;
            }

            // 4. Simulate placement & run BFS check:
            var simulatedWalls = new List<Wall>(existingWalls.Count + 1);
            simulatedWalls.AddRange(existingWalls);
            simulatedWalls.Add(wall);

            bool p1CanReach = Pathfinding.HasPathToGoal(PlayerId.Player1, player1Pos, simulatedWalls, boardSize);
            if (!p1CanReach)
                return WallPlacementError.BlocksPlayerPath;

            bool p2CanReach = Pathfinding.HasPathToGoal(PlayerId.Player2, player2Pos, simulatedWalls, boardSize);
            if (!p2CanReach)
                return WallPlacementError.BlocksPlayerPath;

            return WallPlacementError.None;
        }

        public static bool IsLegalWallPlacement(
            Wall wall,
            int playerWallsRemaining,
            BoardPosition player1Pos,
            BoardPosition player2Pos,
            IReadOnlyCollection<Wall> existingWalls,
            int boardSize = 9)
        {
            return ValidateWallPlacement(wall, playerWallsRemaining, player1Pos, player2Pos, existingWalls, boardSize) 
                   == WallPlacementError.None;
        }

        public static bool IsLegalWallPlacement(GameState state, PlayerId player, Wall wall)
        {
            return ValidateWallPlacement(state, player, wall) == WallPlacementError.None;
        }
    }
}
