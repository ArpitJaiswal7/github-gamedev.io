using System;
using System.Collections;
using UnityEngine;
using Blockade2D.Core.AI;
using Blockade2D.Core.Engine;
using Blockade2D.Core.Model;
using Blockade2D.Core.State;

namespace Blockade2D.Presentation.Controllers
{
    /// <summary>
    /// Executes AI decisions for bot players (Player 2 in Classic; Players 2, 3, 4 in Bulls Eye)
    /// with a natural human-like thinking delay and sequential turn chaining.
    /// </summary>
    public class AIController : MonoBehaviour
    {
        [Header("Configuration")]
        [SerializeField] private bool isAIEnabled = true;
        [SerializeField] private AIDifficulty difficulty = AIDifficulty.Medium;
        [SerializeField] private float thinkingDelay = 0.4f;

        public bool IsAIEnabled
        {
            get => isAIEnabled;
            set => isAIEnabled = value;
        }

        public AIDifficulty Difficulty
        {
            get => difficulty;
            set => difficulty = value;
        }

        public bool IsThinking { get; private set; }
        public PlayerId CurrentBotPlayer { get; private set; } = PlayerId.Player2;

        public event Action OnAIBeganThinking;
        public event Action OnAIFinishedTurn;

        private Coroutine _turnRoutine;

        public void CancelPendingTurn()
        {
            if (_turnRoutine != null)
            {
                StopCoroutine(_turnRoutine);
                _turnRoutine = null;
            }
            IsThinking = false;
        }

        public void QueueTurn(GameEngine engine, PlayerId botPlayer)
        {
            if (!isAIEnabled || engine == null || engine.State.IsGameOver)
                return;

            CancelPendingTurn();
            _turnRoutine = StartCoroutine(ExecuteAITurnRoutine(engine, botPlayer));
        }

        public void TryTriggerAITurn(GameEngine engine, PlayerId botPlayer = PlayerId.Player2)
        {
            QueueTurn(engine, botPlayer);
        }

        private IEnumerator ExecuteAITurnRoutine(GameEngine engine, PlayerId botPlayer)
        {
            CurrentBotPlayer = botPlayer;
            IsThinking = true;
            OnAIBeganThinking?.Invoke();

            // Yield one frame so previous move/turn invocation stack completely unwinds
            yield return null;

            // Natural thinking delay
            if (thinkingDelay > 0f)
            {
                yield return new WaitForSeconds(thinkingDelay);
            }

            if (!engine.State.IsGameOver && engine.State.ActivePlayer == botPlayer)
            {
                var action = QuoridorAI.CalculateBestAction(engine.State, botPlayer, difficulty);

                TurnResult result;
                if (action.ActionType == BotActionType.MovePawn)
                {
                    result = engine.TryMovePawn(botPlayer, action.TargetPosition);
                }
                else
                {
                    result = engine.TryPlaceWall(botPlayer, action.WallToPlace);
                }

                // Safety fallback: if action was rejected for any edge case, execute first legal pawn move
                if (!result.IsSuccess && !engine.State.IsGameOver && engine.State.ActivePlayer == botPlayer)
                {
                    var legalMoves = engine.GetLegalPawnMoves(botPlayer);
                    if (legalMoves.Count > 0)
                    {
                        engine.TryMovePawn(botPlayer, legalMoves[0]);
                    }
                }
            }

            // Note: If the move triggered another AI player, QueueTurn will have stopped this coroutine.
            // If we reached here (e.g. game over or human player's turn), clean up state.
            IsThinking = false;
            _turnRoutine = null;
            OnAIFinishedTurn?.Invoke();
        }
    }
}
