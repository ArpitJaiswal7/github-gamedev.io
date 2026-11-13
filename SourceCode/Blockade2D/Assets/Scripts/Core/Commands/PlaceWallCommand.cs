using Blockade2D.Core.Model;
using Blockade2D.Core.State;

namespace Blockade2D.Core.Commands
{
    public class PlaceWallCommand : IGameCommand
    {
        public PlayerId Player { get; }
        public Wall Wall { get; }

        public string Notation => $"{Player}: Wall {Wall.Orientation} at {Wall.Intersection}";

        public PlaceWallCommand(PlayerId player, Wall wall)
        {
            Player = player;
            Wall = wall;
        }

        public void Execute(GameState state)
        {
            state.PlacedWalls.Add(Wall);
            state.DecrementWalls(Player);
            state.SwitchTurn();
        }

        public void Undo(GameState state)
        {
            state.PlacedWalls.Remove(Wall);
            state.IncrementWalls(Player);
            state.ActivePlayer = Player;
        }
    }
}
