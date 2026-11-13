using System.Collections.Generic;
using System.Linq;
using Blockade2D.Core.Model;

namespace Blockade2D.Core.Rules
{
    /// <summary>
    /// Validates and generates legal moves for pawns adhering to Quoridor rules,
    /// including straight jumps and diagonal jumps when blocked.
    /// Supports both 2-player and 4-player rulesets.
    /// </summary>
    public static class MoveValidator
    {
        public static List<BoardPosition> GetLegalPawnMoves(
            BoardPosition currentPos,
            IReadOnlyCollection<BoardPosition> otherPlayerPositions,
            IEnumerable<Wall> walls,
            int boardSize = 9)
        {
            var legalMoves = new List<BoardPosition>();
            var occupiedSet = otherPlayerPositions != null
                ? new HashSet<BoardPosition>(otherPlayerPositions)
                : new HashSet<BoardPosition>();

            foreach (var dir in BoardPosition.OrthogonalDirections)
            {
                BoardPosition adj = currentPos + dir;

                if (!adj.IsWithinBoard(boardSize))
                    continue;

                // Check if passage from current to adjacent is blocked by a wall
                if (Pathfinding.IsPassageBlocked(currentPos, adj, walls))
                    continue;

                // Case 1: Adjacent tile is empty -> Standard Move
                if (!occupiedSet.Contains(adj))
                {
                    legalMoves.Add(adj);
                    continue;
                }

                // Case 2: Another player is on adjacent tile -> Jump scenario
                BoardPosition behind = adj + dir;
                bool canJumpStraight = behind.IsWithinBoard(boardSize) &&
                                       !Pathfinding.IsPassageBlocked(adj, behind, walls) &&
                                       !occupiedSet.Contains(behind);

                if (canJumpStraight)
                {
                    // Mandatory straight jump if space behind opponent is open and unblocked
                    legalMoves.Add(behind);
                }
                else
                {
                    // Space behind is blocked by a wall, board edge, or another pawn -> Diagonal jumps permitted
                    var perpendiculars = GetPerpendicularDirections(dir);
                    foreach (var perp in perpendiculars)
                    {
                        BoardPosition diag = adj + perp;

                        if (diag.IsWithinBoard(boardSize) &&
                            !Pathfinding.IsPassageBlocked(adj, diag, walls) &&
                            !occupiedSet.Contains(diag))
                        {
                            legalMoves.Add(diag);
                        }
                    }
                }
            }

            return legalMoves;
        }

        public static List<BoardPosition> GetLegalPawnMoves(
            BoardPosition currentPos,
            BoardPosition opponentPos,
            IEnumerable<Wall> walls,
            int boardSize = 9)
        {
            return GetLegalPawnMoves(currentPos, new[] { opponentPos }, walls, boardSize);
        }

        public static bool IsLegalPawnMove(
            BoardPosition from,
            BoardPosition to,
            IReadOnlyCollection<BoardPosition> otherPlayerPositions,
            IEnumerable<Wall> walls,
            int boardSize = 9)
        {
            var moves = GetLegalPawnMoves(from, otherPlayerPositions, walls, boardSize);
            return moves.Contains(to);
        }

        public static bool IsLegalPawnMove(
            BoardPosition from,
            BoardPosition to,
            BoardPosition opponentPos,
            IEnumerable<Wall> walls,
            int boardSize = 9)
        {
            var moves = GetLegalPawnMoves(from, opponentPos, walls, boardSize);
            return moves.Contains(to);
        }

        private static BoardPosition[] GetPerpendicularDirections(BoardPosition dir)
        {
            // If moving vertically (Up/Down), perpendiculars are Left and Right
            if (dir.X == 0)
            {
                return new[] { BoardPosition.Left, BoardPosition.Right };
            }
            // If moving horizontally (Left/Right), perpendiculars are Up and Down
            return new[] { BoardPosition.Up, BoardPosition.Down };
        }
    }
}
