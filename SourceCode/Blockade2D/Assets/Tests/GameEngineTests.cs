using System.Collections.Generic;
using NUnit.Framework;
using Blockade2D.Core.AI;
using Blockade2D.Core.Commands;
using Blockade2D.Core.Engine;
using Blockade2D.Core.Model;
using Blockade2D.Core.Rules;
using Blockade2D.Core.State;

namespace Blockade2D.Tests
{
    [TestFixture]
    public class GameEngineTests
    {
        private GameEngine _engine;

        [SetUp]
        public void SetUp()
        {
            _engine = new GameEngine();
        }

        [Test]
        public void InitialBoardSetup_IsCorrect()
        {
            Assert.AreEqual(new BoardPosition(4, 0), _engine.State.Player1Position);
            Assert.AreEqual(new BoardPosition(4, 8), _engine.State.Player2Position);
            Assert.AreEqual(10, _engine.State.Player1WallsRemaining);
            Assert.AreEqual(10, _engine.State.Player2WallsRemaining);
            Assert.AreEqual(PlayerId.Player1, _engine.State.ActivePlayer);
            Assert.IsFalse(_engine.State.IsGameOver);
        }

        [Test]
        public void LegalOrthogonalMoves_FromOrigin_AreThree()
        {
            var moves = _engine.GetLegalPawnMoves(PlayerId.Player1);
            Assert.AreEqual(3, moves.Count);
            Assert.IsTrue(moves.Contains(new BoardPosition(4, 1)));
            Assert.IsTrue(moves.Contains(new BoardPosition(3, 0)));
            Assert.IsTrue(moves.Contains(new BoardPosition(5, 0)));
        }

        [Test]
        public void Wall_BlocksPassage_Correctly()
        {
            var wall = new Wall(4, 0, WallOrientation.Horizontal);
            var result = _engine.TryPlaceWall(PlayerId.Player1, wall);
            Assert.IsTrue(result.IsSuccess);

            var p1Moves = MoveValidator.GetLegalPawnMoves(
                _engine.State.Player1Position,
                _engine.State.Player2Position,
                _engine.State.PlacedWalls);

            Assert.IsFalse(p1Moves.Contains(new BoardPosition(4, 1)));
            Assert.IsTrue(p1Moves.Contains(new BoardPosition(3, 0)));
            Assert.IsTrue(p1Moves.Contains(new BoardPosition(5, 0)));
        }

        [Test]
        public void Wall_OverlapAndCrossing_AreRejected()
        {
            var wallH = new Wall(3, 3, WallOrientation.Horizontal);
            _engine.TryPlaceWall(PlayerId.Player1, wallH);

            var errCross = _engine.CheckWallLegality(PlayerId.Player2, new Wall(3, 3, WallOrientation.Vertical));
            Assert.AreEqual(WallPlacementError.OverlapsExistingWall, errCross);

            var errOverlap = _engine.CheckWallLegality(PlayerId.Player2, new Wall(4, 3, WallOrientation.Horizontal));
            Assert.AreEqual(WallPlacementError.OverlapsExistingWall, errOverlap);
        }

        [Test]
        public void StraightJump_WhenAdjacent_IsAllowed()
        {
            _engine.State.Player1Position = new BoardPosition(4, 4);
            _engine.State.Player2Position = new BoardPosition(4, 5);

            var moves = MoveValidator.GetLegalPawnMoves(
                _engine.State.Player1Position,
                _engine.State.Player2Position,
                _engine.State.PlacedWalls);

            Assert.IsTrue(moves.Contains(new BoardPosition(4, 6)));
            Assert.IsFalse(moves.Contains(new BoardPosition(4, 5)));
        }

        [Test]
        public void DiagonalJump_WhenBehindIsBlocked_IsAllowed()
        {
            _engine.State.Player1Position = new BoardPosition(4, 4);
            _engine.State.Player2Position = new BoardPosition(4, 5);

            var wallBehind = new Wall(4, 5, WallOrientation.Horizontal);
            _engine.State.PlacedWalls.Add(wallBehind);

            var moves = MoveValidator.GetLegalPawnMoves(
                _engine.State.Player1Position,
                _engine.State.Player2Position,
                _engine.State.PlacedWalls);

            Assert.IsFalse(moves.Contains(new BoardPosition(4, 6)));
            Assert.IsTrue(moves.Contains(new BoardPosition(3, 5)));
            Assert.IsTrue(moves.Contains(new BoardPosition(5, 5)));
        }

        [Test]
        public void BFS_TrappingPrevention_RejectsEnclosingWall()
        {
            _engine.State.Player1Position = new BoardPosition(4, 4);

            var w1 = new Wall(4, 4, WallOrientation.Horizontal);
            Assert.IsTrue(_engine.TryPlaceWall(PlayerId.Player1, w1).IsSuccess);
            _engine.TryMovePawn(PlayerId.Player2, new BoardPosition(4, 7));

            var w2 = new Wall(3, 3, WallOrientation.Horizontal);
            Assert.IsTrue(_engine.TryPlaceWall(PlayerId.Player1, w2).IsSuccess);
            _engine.TryMovePawn(PlayerId.Player2, new BoardPosition(4, 6));

            var w3 = new Wall(3, 4, WallOrientation.Vertical);
            Assert.IsTrue(_engine.TryPlaceWall(PlayerId.Player1, w3).IsSuccess);
            _engine.TryMovePawn(PlayerId.Player2, new BoardPosition(4, 5));

            var trapWall = new Wall(4, 3, WallOrientation.Vertical);
            var err = _engine.CheckWallLegality(PlayerId.Player1, trapWall);
            Assert.AreEqual(WallPlacementError.BlocksPlayerPath, err);
        }

        [Test]
        public void EdgeRowPlacements_AreAllowed_WithAtLeastOnePathGuaranteed()
        {
            // Vertical blockers on edge rows are now allowed
            var vertRow0 = new Wall(4, 0, WallOrientation.Vertical);
            Assert.AreEqual(WallPlacementError.None, _engine.CheckWallLegality(PlayerId.Player1, vertRow0));

            var vertRow8 = new Wall(4, 7, WallOrientation.Vertical);
            Assert.AreEqual(WallPlacementError.None, _engine.CheckWallLegality(PlayerId.Player1, vertRow8));

            // Horizontal blockers on edge rows are allowed
            var horizRow0 = new Wall(4, 0, WallOrientation.Horizontal);
            Assert.AreEqual(WallPlacementError.None, _engine.CheckWallLegality(PlayerId.Player1, horizRow0));

            var horizRow8 = new Wall(4, 7, WallOrientation.Horizontal);
            Assert.AreEqual(WallPlacementError.None, _engine.CheckWallLegality(PlayerId.Player1, horizRow8));
        }

        [Test]
        public void WallPlacements_RecordDistinctPlayerOwnership()
        {
            var p1Wall = new Wall(2, 2, WallOrientation.Horizontal);
            _engine.TryPlaceWall(PlayerId.Player1, p1Wall);
            Assert.AreEqual(PlayerId.Player1, _engine.State.PlacedWalls[0].PlacedBy);

            var p2Wall = new Wall(5, 5, WallOrientation.Vertical);
            _engine.TryPlaceWall(PlayerId.Player2, p2Wall);
            Assert.AreEqual(PlayerId.Player2, _engine.State.PlacedWalls[1].PlacedBy);
        }

        [Test]
        public void WinCondition_DetectsVictory()
        {
            _engine.State.Player2Position = new BoardPosition(0, 8);
            _engine.State.Player1Position = new BoardPosition(4, 7);

            var res = _engine.TryMovePawn(PlayerId.Player1, new BoardPosition(4, 8));
            Assert.IsTrue(res.IsSuccess);
            Assert.IsTrue(res.IsGameOver);
            Assert.AreEqual(PlayerId.Player1, res.Winner);
        }

        [Test]
        public void CommandPattern_UndoRedo_RestoresStateExactly()
        {
            var startPos = _engine.State.Player1Position;
            var targetPos = new BoardPosition(4, 1);

            _engine.TryMovePawn(PlayerId.Player1, targetPos);
            Assert.AreEqual(targetPos, _engine.State.Player1Position);

            _engine.Undo();
            Assert.AreEqual(startPos, _engine.State.Player1Position);
            Assert.AreEqual(PlayerId.Player1, _engine.State.ActivePlayer);

            _engine.Redo();
            Assert.AreEqual(targetPos, _engine.State.Player1Position);
            Assert.AreEqual(PlayerId.Player2, _engine.State.ActivePlayer);
        }

        [Test]
        public void ResetGame_ClearsGameOverAndRestoresBoardState()
        {
            _engine.State.Player2Position = new BoardPosition(0, 8);
            _engine.State.Player1Position = new BoardPosition(4, 7);
            _engine.TryMovePawn(PlayerId.Player1, new BoardPosition(4, 8));
            Assert.IsTrue(_engine.State.IsGameOver);

            _engine.ResetGame(9, 10);
            Assert.IsFalse(_engine.State.IsGameOver);
            Assert.IsNull(_engine.State.Winner);
            Assert.AreEqual(PlayerId.Player1, _engine.State.ActivePlayer);
            Assert.AreEqual(new BoardPosition(4, 0), _engine.State.Player1Position);
            Assert.AreEqual(new BoardPosition(4, 8), _engine.State.Player2Position);
            Assert.AreEqual(0, _engine.State.PlacedWalls.Count);
        }

        [Test]
        public void BullsEyeMode_Setup_HasFourPlayersAndCenterTarget()
        {
            var engine = new GameEngine(9, null, GameMode.BullsEye);
            Assert.AreEqual(GameMode.BullsEye, engine.State.Mode);
            Assert.AreEqual(4, engine.State.PlayerCount);
            Assert.AreEqual(new BoardPosition(4, 4), engine.State.CenterCell);

            Assert.AreEqual(new BoardPosition(4, 0), engine.State.Player1Position);
            Assert.AreEqual(new BoardPosition(4, 8), engine.State.Player2Position);
            Assert.AreEqual(new BoardPosition(0, 4), engine.State.Player3Position);
            Assert.AreEqual(new BoardPosition(8, 4), engine.State.Player4Position);

            Assert.AreEqual(5, engine.State.Player1WallsRemaining);
            Assert.AreEqual(5, engine.State.Player2WallsRemaining);
            Assert.AreEqual(5, engine.State.Player3WallsRemaining);
            Assert.AreEqual(5, engine.State.Player4WallsRemaining);
            Assert.AreEqual(PlayerId.Player1, engine.State.ActivePlayer);
        }

        [Test]
        public void BullsEyeMode_ReachingCenterCell_TriggersWin()
        {
            var engine = new GameEngine(9, null, GameMode.BullsEye);
            engine.State.Player3Position = new BoardPosition(3, 4);
            engine.State.ActivePlayer = PlayerId.Player3;

            var res = engine.TryMovePawn(PlayerId.Player3, new BoardPosition(4, 4));
            Assert.IsTrue(res.IsSuccess);
            Assert.IsTrue(engine.State.IsGameOver);
            Assert.AreEqual(PlayerId.Player3, engine.State.Winner);
        }

        [Test]
        public void BullsEyeMode_TurnRotation_CyclesThroughAllFourPlayers()
        {
            var engine = new GameEngine(9, null, GameMode.BullsEye);
            engine.TryMovePawn(PlayerId.Player1, new BoardPosition(4, 1));
            Assert.AreEqual(PlayerId.Player2, engine.State.ActivePlayer);

            engine.TryMovePawn(PlayerId.Player2, new BoardPosition(4, 7));
            Assert.AreEqual(PlayerId.Player3, engine.State.ActivePlayer);

            engine.TryMovePawn(PlayerId.Player3, new BoardPosition(1, 4));
            Assert.AreEqual(PlayerId.Player4, engine.State.ActivePlayer);

            engine.TryMovePawn(PlayerId.Player4, new BoardPosition(7, 4));
            Assert.AreEqual(PlayerId.Player1, engine.State.ActivePlayer);
        }

        [Test]
        public void Grid15x15_BullsEyeAndClassic_WorkAccurately()
        {
            var be15 = new GameEngine(15, null, GameMode.BullsEye);
            Assert.AreEqual(15, be15.State.BoardSize);
            Assert.AreEqual(new BoardPosition(7, 7), be15.State.CenterCell);
            Assert.AreEqual(new BoardPosition(7, 0), be15.State.Player1Position);
            Assert.AreEqual(new BoardPosition(7, 14), be15.State.Player2Position);
            Assert.AreEqual(new BoardPosition(0, 7), be15.State.Player3Position);
            Assert.AreEqual(new BoardPosition(14, 7), be15.State.Player4Position);

            be15.State.Player1Position = new BoardPosition(7, 6);
            be15.State.ActivePlayer = PlayerId.Player1;
            var winRes = be15.TryMovePawn(PlayerId.Player1, new BoardPosition(7, 7));
            Assert.IsTrue(winRes.IsSuccess);
            Assert.IsTrue(be15.State.IsGameOver);
            Assert.AreEqual(PlayerId.Player1, be15.State.Winner);
        }

        [Test]
        public void BullsEyeMode_SequentialBotTurnCycle_CalculatesAndExecutesWithoutStalling()
        {
            var engine = new GameEngine(15, null, GameMode.BullsEye);
            var p1Res = engine.TryMovePawn(PlayerId.Player1, new BoardPosition(7, 1));
            Assert.IsTrue(p1Res.IsSuccess);
            Assert.AreEqual(PlayerId.Player2, engine.State.ActivePlayer);

            var p2Action = QuoridorAI.CalculateBestAction(engine.State, PlayerId.Player2, AIDifficulty.Medium);
            var p2Res = p2Action.ActionType == BotActionType.MovePawn
                ? engine.TryMovePawn(PlayerId.Player2, p2Action.TargetPosition)
                : engine.TryPlaceWall(PlayerId.Player2, p2Action.WallToPlace);
            Assert.IsTrue(p2Res.IsSuccess);
            Assert.AreEqual(PlayerId.Player3, engine.State.ActivePlayer);

            var p3Action = QuoridorAI.CalculateBestAction(engine.State, PlayerId.Player3, AIDifficulty.Medium);
            var p3Res = p3Action.ActionType == BotActionType.MovePawn
                ? engine.TryMovePawn(PlayerId.Player3, p3Action.TargetPosition)
                : engine.TryPlaceWall(PlayerId.Player3, p3Action.WallToPlace);
            Assert.IsTrue(p3Res.IsSuccess);
            Assert.AreEqual(PlayerId.Player4, engine.State.ActivePlayer);

            var p4Action = QuoridorAI.CalculateBestAction(engine.State, PlayerId.Player4, AIDifficulty.Medium);
            var p4Res = p4Action.ActionType == BotActionType.MovePawn
                ? engine.TryMovePawn(PlayerId.Player4, p4Action.TargetPosition)
                : engine.TryPlaceWall(PlayerId.Player4, p4Action.WallToPlace);
            Assert.IsTrue(p4Res.IsSuccess);
            Assert.AreEqual(PlayerId.Player1, engine.State.ActivePlayer);
        }
    }
}
