using System;
using ThisIsBlast.Data;
using UnityEngine;

namespace ThisIsBlast.Core
{
    public static class GameEvents
    {
        public static event Action<LevelData> OnLevelLoaded;
        public static event Action<int, int> OnBlockDamaged; // x, y
        public static event Action<int, int, BlockColor, EnemyBlockType> OnBlockDestroyed;
        public static event Action<int, int, int> OnChestProgress; // x, y, currentHits
        public static event Action<int, int, int> OnChestOpened; // x, y, gems
        public static event Action<int, int> OnPiggyBankHit; // x, y
        public static event Action<int, int, int> OnKeyCollected; // x, y, unlockColIndex
        
        public static event Action<int, BlockColor, int> OnArsenalBlockFired; // colIndex, color, remainingBullets
        public static event Action<int> OnArsenalBlockDepleted; // colIndex
        public static event Action<BlockColor, int, int> OnArsenalMergeTriggered; // color, newBulletCount, mergedCount
        public static event Action<int> OnColumnUnlocked; // colIndex
        
        public static event Action<int, int> OnGemsChanged; // totalGems, delta
        public static event Action<int, int> OnScoreChanged; // totalScore, delta
        public static event Action<bool, int, int> OnGameOver; // isWin, stars, gemsEarned
        public static event Action OnComboChain; // Trigger juice banner

        public static void TriggerLevelLoaded(LevelData data) => OnLevelLoaded?.Invoke(data);
        public static void TriggerBlockDamaged(int x, int y) => OnBlockDamaged?.Invoke(x, y);
        public static void TriggerBlockDestroyed(int x, int y, BlockColor color, EnemyBlockType type) => OnBlockDestroyed?.Invoke(x, y, color, type);
        public static void TriggerChestProgress(int x, int y, int hits) => OnChestProgress?.Invoke(x, y, hits);
        public static void TriggerChestOpened(int x, int y, int gems) => OnChestOpened?.Invoke(x, y, gems);
        public static void TriggerPiggyBankHit(int x, int y) => OnPiggyBankHit?.Invoke(x, y);
        public static void TriggerKeyCollected(int x, int y, int col) => OnKeyCollected?.Invoke(x, y, col);
        
        public static void TriggerArsenalBlockFired(int col, BlockColor color, int remaining) => OnArsenalBlockFired?.Invoke(col, color, remaining);
        public static void TriggerArsenalBlockDepleted(int col) => OnArsenalBlockDepleted?.Invoke(col);
        public static void TriggerArsenalMergeTriggered(BlockColor color, int count, int merged) => OnArsenalMergeTriggered?.Invoke(color, count, merged);
        public static void TriggerColumnUnlocked(int col) => OnColumnUnlocked?.Invoke(col);
        
        public static void TriggerGemsChanged(int total, int delta) => OnGemsChanged?.Invoke(total, delta);
        public static void TriggerScoreChanged(int total, int delta) => OnScoreChanged?.Invoke(total, delta);
        public static void TriggerGameOver(bool isWin, int stars, int gems) => OnGameOver?.Invoke(isWin, stars, gems);
        public static void TriggerComboChain() => OnComboChain?.Invoke();
    }
}
