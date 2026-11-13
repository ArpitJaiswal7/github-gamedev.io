using System.Collections.Generic;
using Blockade2D.Core.State;

namespace Blockade2D.Core.Commands
{
    /// <summary>
    /// Manages the stack of executed commands to provide Undo/Redo and match replay step-through.
    /// </summary>
    public class CommandHistory
    {
        private readonly List<IGameCommand> _history = new List<IGameCommand>();
        private int _currentIndex = -1;

        public IReadOnlyList<IGameCommand> AllRecordedMoves => _history.AsReadOnly();
        public int CurrentIndex => _currentIndex;
        public bool CanUndo => _currentIndex >= 0;
        public bool CanRedo => _currentIndex < _history.Count - 1;

        public void ExecuteCommand(IGameCommand command, GameState state)
        {
            // If we are executing after an undo, truncate the redo branch
            if (_currentIndex < _history.Count - 1)
            {
                _history.RemoveRange(_currentIndex + 1, _history.Count - (_currentIndex + 1));
            }

            command.Execute(state);
            _history.Add(command);
            _currentIndex++;
        }

        public bool Undo(GameState state)
        {
            if (!CanUndo)
                return false;

            var command = _history[_currentIndex];
            command.Undo(state);
            _currentIndex--;
            return true;
        }

        public bool Redo(GameState state)
        {
            if (!CanRedo)
                return false;

            _currentIndex++;
            var command = _history[_currentIndex];
            command.Execute(state);
            return true;
        }

        public void Clear()
        {
            _history.Clear();
            _currentIndex = -1;
        }
    }
}
