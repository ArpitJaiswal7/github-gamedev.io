using System;

namespace Blockade2D.Core.Model
{
    /// <summary>
    /// Immutable 2D integer coordinate on the board grid.
    /// Standard board is 9x9 (X: 0 to 8, Y: 0 to 8).
    /// </summary>
    public readonly struct BoardPosition : IEquatable<BoardPosition>
    {
        public int X { get; }
        public int Y { get; }

        public BoardPosition(int x, int y)
        {
            X = x;
            Y = y;
        }

        public static readonly BoardPosition Zero = new BoardPosition(0, 0);
        public static readonly BoardPosition Up = new BoardPosition(0, 1);
        public static readonly BoardPosition Down = new BoardPosition(0, -1);
        public static readonly BoardPosition Left = new BoardPosition(-1, 0);
        public static readonly BoardPosition Right = new BoardPosition(1, 0);

        public static readonly BoardPosition[] OrthogonalDirections = { Up, Down, Left, Right };

        public bool IsWithinBoard(int boardSize = 9)
        {
            return X >= 0 && X < boardSize && Y >= 0 && Y < boardSize;
        }

        public bool IsValidWallIntersection(int boardSize = 9)
        {
            // For a 9x9 grid, wall intersections range from (0,0) to (7,7)
            return X >= 0 && X < boardSize - 1 && Y >= 0 && Y < boardSize - 1;
        }

        public int ManhattanDistance(BoardPosition other)
        {
            return Math.Abs(X - other.X) + Math.Abs(Y - other.Y);
        }

        public static BoardPosition operator +(BoardPosition a, BoardPosition b) =>
            new BoardPosition(a.X + b.X, a.Y + b.Y);

        public static BoardPosition operator -(BoardPosition a, BoardPosition b) =>
            new BoardPosition(a.X - b.X, a.Y - b.Y);

        public static bool operator ==(BoardPosition a, BoardPosition b) =>
            a.X == b.X && a.Y == b.Y;

        public static bool operator !=(BoardPosition a, BoardPosition b) =>
            !(a == b);

        public bool Equals(BoardPosition other) => X == other.X && Y == other.Y;

        public override bool Equals(object obj) =>
            obj is BoardPosition other && Equals(other);

        public override int GetHashCode() => HashCode.Combine(X, Y);

        public override string ToString() => $"({X}, {Y})";
    }
}
