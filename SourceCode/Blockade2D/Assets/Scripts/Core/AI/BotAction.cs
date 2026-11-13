using Blockade2D.Core.Model;

namespace Blockade2D.Core.AI
{
    public enum BotActionType
    {
        MovePawn,
        PlaceWall
    }

    /// <summary>
    /// Represents an action chosen by the AI.
    /// </summary>
    public readonly struct BotAction
    {
        public BotActionType ActionType { get; }
        public BoardPosition TargetPosition { get; }
        public Wall WallToPlace { get; }

        private BotAction(BotActionType actionType, BoardPosition targetPosition, Wall wallToPlace)
        {
            ActionType = actionType;
            TargetPosition = targetPosition;
            WallToPlace = wallToPlace;
        }

        public static BotAction Move(BoardPosition destination) =>
            new BotAction(BotActionType.MovePawn, destination, default);

        public static BotAction Place(Wall wall) =>
            new BotAction(BotActionType.PlaceWall, default, wall);

        public override string ToString() =>
            ActionType == BotActionType.MovePawn 
                ? $"BotMove -> {TargetPosition}" 
                : $"BotWall -> {WallToPlace}";
    }
}
