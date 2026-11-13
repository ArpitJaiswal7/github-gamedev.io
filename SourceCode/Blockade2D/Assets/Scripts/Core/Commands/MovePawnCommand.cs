using Blockade2D.Core.Model;
using Blockade2D.Core.State;

namespace Blockade2D.Core.Commands
{
    public class MovePawnCommand : IGameCommand
    {
        public PlayerId Player { get; }
        public BoardPosition From { get; }
        public BoardPosition To { get; }

        public string Notation => $"{Player}: Pawn {From} -> {To}";

        public MovePawnCommand(PlayerId player, BoardPosition from, BoardPosition to)
        {
            Player = player;
            From = from;
            To = to;
        }

        public void Execute(GameState state)
        {
            state.SetPlayerPosition(Player, To);
            state.SwitchTurn();
        }

        public void Undo(GameState state)
        {
            state.SetPlayerPosition(Player, From);
            state.ActivePlayer = Player;
        }
    }
}
