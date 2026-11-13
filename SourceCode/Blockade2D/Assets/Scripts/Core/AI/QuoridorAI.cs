using System;
using System.Collections.Generic;
using Blockade2D.Core.Model;
using Blockade2D.Core.Rules;
using Blockade2D.Core.State;

namespace Blockade2D.Core.AI
{
    /// <summary>
    /// Heuristic AI engine for Quoridor supporting both Classic 2-player and Bulls Eye 4-player modes.
    /// Utilizes BFS path delta analysis to optimize bot progress and obstruct opponents.
    /// </summary>
    public static class QuoridorAI
    {
        private static readonly Random _rng = new Random();

        public static BotAction CalculateBestAction(
            GameState state,
            PlayerId botPlayer,
            AIDifficulty difficulty = AIDifficulty.Medium)
        {
            var botPos = state.GetPlayerPosition(botPlayer);
            var otherPositions = state.GetAllOtherPlayerPositions(botPlayer);
            int botWalls = state.GetWallsRemaining(botPlayer);

            int currentBotDist = Pathfinding.GetShortestPathDistance(state, botPlayer, state.PlacedWalls);

            // 1. Find Best Pawn Move
            var legalPawnMoves = MoveValidator.GetLegalPawnMoves(botPos, otherPositions, state.PlacedWalls, state.BoardSize);
            BoardPosition bestPawnMove = botPos;
            int bestPawnDist = int.MaxValue;

            foreach (var move in legalPawnMoves)
            {
                // Immediate win check!
                if (state.Mode == GameMode.BullsEye && move == state.CenterCell)
                {
                    return BotAction.Move(move);
                }
                if (state.Mode == GameMode.Classic && move.Y == botPlayer.GoalRow(state.BoardSize))
                {
                    return BotAction.Move(move);
                }

                // Simulate bot at 'move'
                int dist = state.Mode == GameMode.BullsEye
                    ? Pathfinding.GetShortestPathDistanceToPosition(move, state.CenterCell, state.PlacedWalls, state.BoardSize)
                    : Pathfinding.GetShortestPathDistance(botPlayer, move, state.PlacedWalls, state.BoardSize);

                if (dist != -1 && dist < bestPawnDist)
                {
                    bestPawnDist = dist;
                    bestPawnMove = move;
                }
            }

            // Fallback: If no move reduced distance, choose the first legal move rather than staying stationary
            if (bestPawnMove == botPos && legalPawnMoves.Count > 0)
            {
                bestPawnMove = legalPawnMoves[0];
            }

            // Easy difficulty: predominantly moves pawn forward
            if (difficulty == AIDifficulty.Easy)
            {
                if (botWalls > 0 && _rng.NextDouble() < 0.25)
                {
                    var randomWall = FindRandomLegalWall(state, botPlayer);
                    if (randomWall.HasValue)
                        return BotAction.Place(randomWall.Value);
                }
                return BotAction.Move(bestPawnMove);
            }

            // If bot is 1 step from victory, take the win immediately!
            if (bestPawnDist == 0)
            {
                return BotAction.Move(bestPawnMove);
            }

            // 2. Identify Primary Opponent Threat
            PlayerId primaryOpponent = PlayerId.Player1;
            int minOppDist = int.MaxValue;

            for (int i = 0; i < state.PlayerCount; i++)
            {
                var p = (PlayerId)i;
                if (p == botPlayer) continue;

                int oppDist = Pathfinding.GetShortestPathDistance(state, p, state.PlacedWalls);
                if (oppDist != -1 && oppDist < minOppDist)
                {
                    minOppDist = oppDist;
                    primaryOpponent = p;
                }
            }

            // 3. Evaluate Potential Strategic Walls
            Wall? bestWall = null;
            int bestWallScore = int.MinValue;

            if (botWalls > 0 && minOppDist < int.MaxValue)
            {
                var candidateWalls = GenerateStrategicCandidateWalls(state, primaryOpponent, botPlayer);

                foreach (var wall in candidateWalls)
                {
                    var err = WallValidator.ValidateWallPlacement(state, botPlayer, wall);
                    if (err != WallPlacementError.None)
                        continue;

                    // Simulate placing this wall
                    var simulatedWalls = new List<Wall>(state.PlacedWalls.Count + 1);
                    simulatedWalls.AddRange(state.PlacedWalls);
                    simulatedWalls.Add(wall);

                    int newOppDist = Pathfinding.GetShortestPathDistance(state, primaryOpponent, simulatedWalls);
                    int newBotDist = Pathfinding.GetShortestPathDistance(state, botPlayer, simulatedWalls);

                    if (newOppDist == -1 || newBotDist == -1)
                        continue;

                    int oppPathIncrease = newOppDist - minOppDist;
                    int botPathIncrease = newBotDist - currentBotDist;

                    // Score: reward slowing down opponent, penalize slowing down self
                    int wallScore = (oppPathIncrease * 3) - (botPathIncrease * 2);

                    if (wallScore > bestWallScore)
                    {
                        bestWallScore = wallScore;
                        bestWall = wall;
                    }
                }
            }

            // 4. Decision Matrix: Move vs Place Wall
            int moveScore = (currentBotDist - bestPawnDist) * 2;

            // Opponent is about to win (within 1-2 steps) -> Urgent blocking needed!
            if (minOppDist <= 2 && bestWall.HasValue && bestWallScore > 0)
            {
                return BotAction.Place(bestWall.Value);
            }

            // Difficulty threshold for placing walls
            int wallThreshold = difficulty == AIDifficulty.Hard ? 3 : 4;

            if (bestWall.HasValue && bestWallScore >= wallThreshold && bestWallScore > moveScore)
            {
                return BotAction.Place(bestWall.Value);
            }

            // Default: advance pawn
            return BotAction.Move(bestPawnMove);
        }

        private static List<Wall> GenerateStrategicCandidateWalls(
            GameState state,
            PlayerId targetOpponent,
            PlayerId botPlayer)
        {
            var candidates = new HashSet<Wall>();
            var oppPath = Pathfinding.GetShortestPath(state, targetOpponent, state.PlacedWalls);

            if (oppPath == null || oppPath.Count < 2)
                return new List<Wall>();

            // Generate walls directly along the opponent's planned path (first 3-4 steps)
            int checkSteps = Math.Min(oppPath.Count - 1, 4);

            for (int i = 0; i < checkSteps; i++)
            {
                var a = oppPath[i];
                var b = oppPath[i + 1];

                // If vertical movement between a and b
                if (a.X == b.X)
                {
                    int lowerY = Math.Min(a.Y, b.Y);
                    if (a.X < state.BoardSize - 1 && lowerY < state.BoardSize - 1)
                        candidates.Add(new Wall(a.X, lowerY, WallOrientation.Horizontal, botPlayer));

                    if (a.X - 1 >= 0 && lowerY < state.BoardSize - 1)
                        candidates.Add(new Wall(a.X - 1, lowerY, WallOrientation.Horizontal, botPlayer));
                }
                // If horizontal movement between a and b
                else
                {
                    int lowerX = Math.Min(a.X, b.X);
                    if (lowerX < state.BoardSize - 1 && a.Y < state.BoardSize - 1)
                        candidates.Add(new Wall(lowerX, a.Y, WallOrientation.Vertical, botPlayer));

                    if (lowerX < state.BoardSize - 1 && a.Y - 1 >= 0)
                        candidates.Add(new Wall(lowerX, a.Y - 1, WallOrientation.Vertical, botPlayer));
                }
            }

            return new List<Wall>(candidates);
        }

        private static Wall? FindRandomLegalWall(GameState state, PlayerId player)
        {
            int maxIntersection = state.BoardSize - 2;
            for (int attempts = 0; attempts < 15; attempts++)
            {
                int x = _rng.Next(0, maxIntersection + 1);
                int y = _rng.Next(0, maxIntersection + 1);
                var orientation = _rng.Next(2) == 0 ? WallOrientation.Horizontal : WallOrientation.Vertical;
                var wall = new Wall(x, y, orientation, player);

                var err = WallValidator.ValidateWallPlacement(state, player, wall);
                if (err == WallPlacementError.None)
                    return wall;
            }

            return null;
        }
    }
}
