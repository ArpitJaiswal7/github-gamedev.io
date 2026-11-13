using Blockade2D.Core.Model;
using Blockade2D.Core.Rules;

namespace Blockade2D.Core.State
{
    public enum ActionType
    {
        MovePawn,
        PlaceWall
    }

    /// <summary>
    /// Represents the outcome of an attempted turn action.
    /// </summary>
    public readonly struct TurnResult
    {
        public bool IsSuccess { get; }
        public string ErrorMessage { get; }
        public bool IsGameOver { get; }
        public PlayerId? Winner { get; }

        private TurnResult(bool isSuccess, string errorMessage, bool isGameOver, PlayerId? winner)
        {
            IsSuccess = isSuccess;
            ErrorMessage = errorMessage;
            IsGameOver = isGameOver;
            Winner = winner;
        }

        public static TurnResult Success(bool isGameOver = false, PlayerId? winner = null) =>
            new TurnResult(true, null, isGameOver, winner);

        public static TurnResult Failure(string errorMessage) =>
            new TurnResult(false, errorMessage, false, null);

        public static TurnResult FromWallError(WallPlacementError error)
        {
            string message = error switch
            {
                WallPlacementError.NoWallsRemaining => "No walls remaining in inventory.",
                WallPlacementError.OutOfBounds => "Wall placement is outside the board boundary.",
                WallPlacementError.OverlapsExistingWall => "Wall conflicts or overlaps with an existing wall.",
                WallPlacementError.BlocksPlayerPath => "Illegal move: a path to the goal must remain open for both players.",
                _ => "Unknown error."
            };
            return Failure(message);
        }
    }
}
