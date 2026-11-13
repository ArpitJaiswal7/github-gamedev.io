using System;
using System.Collections.Generic;
using Blockade2D.Core.Commands;
using Blockade2D.Core.Model;
using Blockade2D.Core.Rules;
using Blockade2D.Core.State;

namespace Blockade2D.Core.Engine
{
    /// <summary>
    /// Core game loop coordinator. Enforces rules, executes reversible commands,
    /// and fires game events for UI and sound systems to subscribe to.
    /// Supports Classic 2-player and Bulls Eye 4-player modes.
    /// </summary>
    public class GameEngine
    {
        public GameState State { get; private set; }
        public CommandHistory History { get; }

        public event Action<PlayerId> OnTurnChanged;
        public event Action<PlayerId, BoardPosition, BoardPosition> OnPawnMoved; // (player, from, to)
        public event Action<PlayerId, Wall> OnWallPlaced;
        public event Action<PlayerId> OnGameOver;

        public GameEngine(
            int boardSize = GameState.DefaultBoardSize,
            int? wallsPerPlayer = null,
            GameMode mode = GameMode.Classic)
        {
            State = new GameState(boardSize, wallsPerPlayer, mode);
            History = new CommandHistory();
        }

        public GameEngine(int boardSize, int wallsPerPlayer)
            : this(boardSize, (int?)wallsPerPlayer, GameMode.Classic)
        {
        }

        public void ResetGame(
            int boardSize = GameState.DefaultBoardSize,
            int? wallsPerPlayer = null,
            GameMode mode = GameMode.Classic)
        {
            State = new GameState(boardSize, wallsPerPlayer, mode);
            History.Clear();
            OnTurnChanged?.Invoke(State.ActivePlayer);
        }

        public void ResetGame(int boardSize, int wallsPerPlayer)
        {
            ResetGame(boardSize, (int?)wallsPerPlayer, GameMode.Classic);
        }

        public List<BoardPosition> GetLegalPawnMoves(PlayerId player)
        {
            if (State.IsGameOver)
                return new List<BoardPosition>();

            var currentPos = State.GetPlayerPosition(player);
            var otherPositions = State.GetAllOtherPlayerPositions(player);
            return MoveValidator.GetLegalPawnMoves(currentPos, otherPositions, State.PlacedWalls, State.BoardSize);
        }

        public WallPlacementError CheckWallLegality(PlayerId player, Wall wall)
        {
            if (State.IsGameOver)
                return WallPlacementError.OutOfBounds;

            return WallValidator.ValidateWallPlacement(State, player, wall);
        }

        public TurnResult TryMovePawn(PlayerId player, BoardPosition destination)
        {
            if (State.IsGameOver)
                return TurnResult.Failure("Game is already finished.");

            if (State.ActivePlayer != player)
                return TurnResult.Failure($"It is {State.ActivePlayer}'s turn.");

            var currentPos = State.GetPlayerPosition(player);
            var otherPositions = State.GetAllOtherPlayerPositions(player);

            if (!MoveValidator.IsLegalPawnMove(currentPos, destination, otherPositions, State.PlacedWalls, State.BoardSize))
            {
                return TurnResult.Failure($"Illegal pawn move to {destination}.");
            }

            var command = new MovePawnCommand(player, currentPos, destination);
            History.ExecuteCommand(command, State);

            OnPawnMoved?.Invoke(player, currentPos, destination);

            if (State.IsGameOver)
            {
                OnGameOver?.Invoke(State.Winner.Value);
                return TurnResult.Success(true, State.Winner);
            }

            OnTurnChanged?.Invoke(State.ActivePlayer);
            return TurnResult.Success();
        }

        public TurnResult TryPlaceWall(PlayerId player, Wall wall)
        {
            if (State.IsGameOver)
                return TurnResult.Failure("Game is already finished.");

            if (State.ActivePlayer != player)
                return TurnResult.Failure($"It is {State.ActivePlayer}'s turn.");

            var error = CheckWallLegality(player, wall);
            if (error != WallPlacementError.None)
            {
                return TurnResult.FromWallError(error);
            }

            var placedWall = new Wall(wall.Intersection, wall.Orientation, player);
            var command = new PlaceWallCommand(player, placedWall);
            History.ExecuteCommand(command, State);

            OnWallPlaced?.Invoke(player, placedWall);
            OnTurnChanged?.Invoke(State.ActivePlayer);

            return TurnResult.Success();
        }

        public bool Undo()
        {
            if (!History.CanUndo)
                return false;

            bool undone = History.Undo(State);
            if (undone)
            {
                OnTurnChanged?.Invoke(State.ActivePlayer);
            }
            return undone;
        }

        public bool Redo()
        {
            if (!History.CanRedo)
                return false;

            bool redone = History.Redo(State);
            if (redone)
            {
                OnTurnChanged?.Invoke(State.ActivePlayer);
            }
            return redone;
        }
    }
}
