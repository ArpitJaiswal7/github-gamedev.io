using System;

namespace Blockade2D.Core.Model
{
    /// <summary>
    /// Represents a 2-unit long wall placed at an intersection slot on the board.
    /// Intersections range from (0,0) to (7,7) for a standard 9x9 board.
    /// </summary>
    public readonly struct Wall : IEquatable<Wall>
    {
        public BoardPosition Intersection { get; }
        public WallOrientation Orientation { get; }
        public PlayerId PlacedBy { get; }

        public Wall(BoardPosition intersection, WallOrientation orientation, PlayerId placedBy = PlayerId.Player1)
        {
            Intersection = intersection;
            Orientation = orientation;
            PlacedBy = placedBy;
        }

        public Wall(int x, int y, WallOrientation orientation, PlayerId placedBy = PlayerId.Player1)
            : this(new BoardPosition(x, y), orientation, placedBy)
        {
        }

        /// <summary>
        /// Checks if this wall directly conflicts or overlaps with another wall.
        /// Walls cannot cross each other or overlap on the same segment.
        /// </summary>
        public bool ConflictsWith(Wall other)
        {
            // Exact same wall
            if (this == other)
                return true;

            // Two walls cannot cross at the exact same intersection
            if (Intersection == other.Intersection)
                return true;

            // Horizontal walls span 2 units horizontally (X and X+1).
            // They conflict if they share the same Y and overlap by 1 unit (|X1 - X2| == 1).
            if (Orientation == WallOrientation.Horizontal && other.Orientation == WallOrientation.Horizontal)
            {
                if (Intersection.Y == other.Intersection.Y && Math.Abs(Intersection.X - other.Intersection.X) == 1)
                    return true;
            }

            // Vertical walls span 2 units vertically (Y and Y+1).
            // They conflict if they share the same X and overlap by 1 unit (|Y1 - Y2| == 1).
            if (Orientation == WallOrientation.Vertical && other.Orientation == WallOrientation.Vertical)
            {
                if (Intersection.X == other.Intersection.X && Math.Abs(Intersection.Y - other.Intersection.Y) == 1)
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Checks if this wall blocks direct passage between two adjacent cells `from` and `to`.
        /// </summary>
        public bool BlocksPassage(BoardPosition from, BoardPosition to)
        {
            // Must be adjacent
            if (from.ManhattanDistance(to) != 1)
                return false;

            // Vertical movement between (x, y) and (x, y + 1)
            if (from.X == to.X)
            {
                if (Orientation != WallOrientation.Horizontal)
                    return false;

                int lowerY = Math.Min(from.Y, to.Y);
                // A horizontal wall at (x, y) blocks:
                // (x, y) <-> (x, y+1) AND (x+1, y) <-> (x+1, y+1)
                return Intersection.Y == lowerY &&
                       (Intersection.X == from.X || Intersection.X == from.X - 1);
            }
            // Horizontal movement between (x, y) and (x + 1, y)
            else
            {
                if (Orientation != WallOrientation.Vertical)
                    return false;

                int lowerX = Math.Min(from.X, to.X);
                // A vertical wall at (x, y) blocks:
                // (x, y) <-> (x+1, y) AND (x, y+1) <-> (x+1, y+1)
                return Intersection.X == lowerX &&
                       (Intersection.Y == from.Y || Intersection.Y == from.Y - 1);
            }
        }

        public static bool operator ==(Wall a, Wall b) =>
            a.Intersection == b.Intersection && a.Orientation == b.Orientation;

        public static bool operator !=(Wall a, Wall b) => !(a == b);

        public bool Equals(Wall other) =>
            Intersection.Equals(other.Intersection) && Orientation == other.Orientation;

        public override bool Equals(object obj) =>
            obj is Wall other && Equals(other);

        public override int GetHashCode() =>
            HashCode.Combine(Intersection, Orientation);

        public override string ToString() =>
            $"Wall[{Orientation} at {Intersection} by {PlacedBy}]";
    }
}
