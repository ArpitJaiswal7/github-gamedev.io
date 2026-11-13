namespace Blockade2D.Core.Model
{
    /// <summary>
    /// Identifies the players in a match.
    /// Supports up to 4 players for Bulls Eye 4-player mode.
    /// </summary>
    public enum PlayerId
    {
        Player1 = 0,
        Player2 = 1,
        Player3 = 2,
        Player4 = 3
    }

    public static class PlayerExtensions
    {
        public static PlayerId Other(this PlayerId player)
        {
            return player == PlayerId.Player1 ? PlayerId.Player2 : PlayerId.Player1;
        }

        public static PlayerId Next(this PlayerId player, int totalPlayers = 2)
        {
            if (totalPlayers <= 1) return player;
            return (PlayerId)(((int)player + 1) % totalPlayers);
        }

        public static int GoalRow(this PlayerId player, int boardSize = 9)
        {
            return player switch
            {
                PlayerId.Player1 => boardSize - 1,
                PlayerId.Player2 => 0,
                PlayerId.Player3 => boardSize - 1,
                PlayerId.Player4 => 0,
                _ => boardSize - 1
            };
        }

        public static int GoalRow(this PlayerId player) => player.GoalRow(9);

        public static int StartRow(this PlayerId player)
        {
            return player switch
            {
                PlayerId.Player1 => 0,
                PlayerId.Player2 => 8,
                PlayerId.Player3 => 4,
                PlayerId.Player4 => 4,
                _ => 0
            };
        }

        /// <summary>
        /// Starting positions for 9x9 board:
        /// Player 1: (4, 0) - South / Bottom
        /// Player 2: (4, 8) - North / Top
        /// Player 3: (0, 4) - West / Left
        /// Player 4: (8, 4) - East / Right
        /// </summary>
        public static BoardPosition StartingPosition(this PlayerId player, int boardSize = 9)
        {
            int mid = boardSize / 2;
            int max = boardSize - 1;

            return player switch
            {
                PlayerId.Player1 => new BoardPosition(mid, 0),
                PlayerId.Player2 => new BoardPosition(mid, max),
                PlayerId.Player3 => new BoardPosition(0, mid),
                PlayerId.Player4 => new BoardPosition(max, mid),
                _ => new BoardPosition(mid, 0)
            };
        }

        public static string DisplayName(this PlayerId player)
        {
            return player switch
            {
                PlayerId.Player1 => "Player 1 (Blue)",
                PlayerId.Player2 => "Player 2 (Orange)",
                PlayerId.Player3 => "Player 3 (Green)",
                PlayerId.Player4 => "Player 4 (Purple)",
                _ => player.ToString()
            };
        }
    }
}
