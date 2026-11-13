using System;
using System.Collections.Generic;
using Blockade2D.Core.Model;

namespace Blockade2D.Core.State
{
    /// <summary>
    /// Encapsulates the complete snapshot of a match at any turn.
    /// Supports both Classic 2-player and Bulls Eye 4-player game modes.
    /// Supports cloning for lookahead AI simulation.
    /// </summary>
    public class GameState
    {
        public const int DefaultBoardSize = 9;
        public const int DefaultWallsPerPlayerClassic = 10;
        public const int DefaultWallsPerPlayerBullsEye = 5;

        public GameMode Mode { get; }
        public int BoardSize { get; }
        public int PlayerCount => Mode == GameMode.BullsEye ? 4 : 2;
        public BoardPosition CenterCell => new BoardPosition(BoardSize / 2, BoardSize / 2);

        private readonly BoardPosition[] _playerPositions = new BoardPosition[4];
        private readonly int[] _wallsRemaining = new int[4];

        public BoardPosition Player1Position
        {
            get => _playerPositions[0];
            set => _playerPositions[0] = value;
        }

        public BoardPosition Player2Position
        {
            get => _playerPositions[1];
            set => _playerPositions[1] = value;
        }

        public BoardPosition Player3Position
        {
            get => _playerPositions[2];
            set => _playerPositions[2] = value;
        }

        public BoardPosition Player4Position
        {
            get => _playerPositions[3];
            set => _playerPositions[3] = value;
        }

        public int Player1WallsRemaining
        {
            get => _wallsRemaining[0];
            set => _wallsRemaining[0] = value;
        }

        public int Player2WallsRemaining
        {
            get => _wallsRemaining[1];
            set => _wallsRemaining[1] = value;
        }

        public int Player3WallsRemaining
        {
            get => _wallsRemaining[2];
            set => _wallsRemaining[2] = value;
        }

        public int Player4WallsRemaining
        {
            get => _wallsRemaining[3];
            set => _wallsRemaining[3] = value;
        }

        public List<Wall> PlacedWalls { get; }
        public PlayerId ActivePlayer { get; set; }
        public bool IsGameOver { get; private set; }
        public PlayerId? Winner { get; private set; }

        public static int GetDefaultWalls(GameMode mode, int boardSize)
        {
            if (boardSize >= 15)
            {
                return mode == GameMode.BullsEye ? 10 : 15;
            }
            return mode == GameMode.BullsEye
                ? DefaultWallsPerPlayerBullsEye
                : DefaultWallsPerPlayerClassic;
        }

        public GameState(
            int boardSize = DefaultBoardSize,
            int? wallsPerPlayer = null,
            GameMode mode = GameMode.Classic)
        {
            Mode = mode;
            BoardSize = boardSize;

            int defaultWalls = GetDefaultWalls(mode, boardSize);
            int walls = wallsPerPlayer ?? defaultWalls;

            for (int i = 0; i < 4; i++)
            {
                var player = (PlayerId)i;
                _playerPositions[i] = player.StartingPosition(boardSize);
                _wallsRemaining[i] = walls;
            }

            PlacedWalls = new List<Wall>();
            ActivePlayer = PlayerId.Player1;
            IsGameOver = false;
            Winner = null;
        }

        public BoardPosition GetPlayerPosition(PlayerId player)
        {
            int idx = (int)player;
            if (idx >= 0 && idx < 4)
                return _playerPositions[idx];
            return BoardPosition.Zero;
        }

        public void SetPlayerPosition(PlayerId player, BoardPosition position)
        {
            int idx = (int)player;
            if (idx >= 0 && idx < 4)
                _playerPositions[idx] = position;

            CheckWinCondition();
        }

        public List<BoardPosition> GetAllOtherPlayerPositions(PlayerId player)
        {
            var others = new List<BoardPosition>(PlayerCount - 1);
            for (int i = 0; i < PlayerCount; i++)
            {
                var p = (PlayerId)i;
                if (p != player)
                {
                    others.Add(_playerPositions[i]);
                }
            }
            return others;
        }

        public int GetWallsRemaining(PlayerId player)
        {
            int idx = (int)player;
            if (idx >= 0 && idx < 4)
                return _wallsRemaining[idx];
            return 0;
        }

        public void DecrementWalls(PlayerId player)
        {
            int idx = (int)player;
            if (idx >= 0 && idx < 4)
                _wallsRemaining[idx]--;
        }

        public void IncrementWalls(PlayerId player)
        {
            int idx = (int)player;
            if (idx >= 0 && idx < 4)
                _wallsRemaining[idx]++;
        }

        public void SwitchTurn()
        {
            ActivePlayer = ActivePlayer.Next(PlayerCount);
        }

        public void CheckWinCondition()
        {
            if (Mode == GameMode.BullsEye)
            {
                var target = CenterCell;
                for (int i = 0; i < PlayerCount; i++)
                {
                    if (_playerPositions[i] == target)
                    {
                        IsGameOver = true;
                        Winner = (PlayerId)i;
                        return;
                    }
                }
                IsGameOver = false;
                Winner = null;
            }
            else
            {
                if (Player1Position.Y == PlayerId.Player1.GoalRow(BoardSize))
                {
                    IsGameOver = true;
                    Winner = PlayerId.Player1;
                }
                else if (Player2Position.Y == PlayerId.Player2.GoalRow(BoardSize))
                {
                    IsGameOver = true;
                    Winner = PlayerId.Player2;
                }
                else
                {
                    IsGameOver = false;
                    Winner = null;
                }
            }
        }

        /// <summary>
        /// Deep copy of the state for AI lookahead evaluations.
        /// </summary>
        public GameState Clone()
        {
            var clone = new GameState(BoardSize, Player1WallsRemaining, Mode)
            {
                Player1Position = this.Player1Position,
                Player2Position = this.Player2Position,
                Player3Position = this.Player3Position,
                Player4Position = this.Player4Position,
                Player1WallsRemaining = this.Player1WallsRemaining,
                Player2WallsRemaining = this.Player2WallsRemaining,
                Player3WallsRemaining = this.Player3WallsRemaining,
                Player4WallsRemaining = this.Player4WallsRemaining,
                ActivePlayer = this.ActivePlayer,
                IsGameOver = this.IsGameOver,
                Winner = this.Winner
            };

            clone.PlacedWalls.AddRange(this.PlacedWalls);
            return clone;
        }
    }
}
