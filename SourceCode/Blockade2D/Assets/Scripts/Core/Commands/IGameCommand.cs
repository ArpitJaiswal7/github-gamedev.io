using Blockade2D.Core.Model;
using Blockade2D.Core.State;

namespace Blockade2D.Core.Commands
{
    /// <summary>
    /// Command interface supporting reversible actions for Undo/Redo and Replays.
    /// </summary>
    public interface IGameCommand
    {
        PlayerId Player { get; }
        string Notation { get; }
        void Execute(GameState state);
        void Undo(GameState state);
    }
}
