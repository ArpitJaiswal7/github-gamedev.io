using System;
using System.Collections.Generic;
using UnityEngine;

namespace ThisIsBlast.Data
{
    public enum WinConditionType
    {
        ClearAll = 0,
        ClearPercentage = 1,
        OpenChests = 2
    }

    [Serializable]
    public class EnemyBlockData
    {
        public int x; // Column in enemy grid
        public int y; // Row in enemy grid (0 = lowest/front)
        public int z; // Layer/depth in enemy grid (0 = main front layer)
        public EnemyBlockType type = EnemyBlockType.Standard;
        public BlockColor color = BlockColor.Red;
        public int hp = 1;
        public int hitThreshold = 10; // For Chest
        public int gemReward = 5;     // For Chest / Piggy
        public int partnerX = -1;     // For Interlocked
        public int partnerY = -1;     // For Interlocked
        public int unlockColIndex = -1;// Which arsenal column this Key unlocks
    }

    [Serializable]
    public class ArsenalBlockData
    {
        public BlockColor color = BlockColor.Red;
        public int bulletCount = 20;
        public int tier = 1;
        public bool isHidden = false; // If true, mystery covered with "?" until it moves to peek/active position
        public bool isInterlocked = false; // If true, interlocked with partner block
        public int partnerCol = -1; // Column index of interlocked partner

        public ArsenalBlockData() { }

        public ArsenalBlockData(BlockColor color, int bulletCount, int tier = 1, bool isHidden = false, bool isInterlocked = false, int partnerCol = -1)
        {
            this.color = color;
            this.bulletCount = bulletCount;
            this.tier = tier;
            this.isHidden = isHidden;
            this.isInterlocked = isInterlocked;
            this.partnerCol = partnerCol;
        }

        public ArsenalBlockData Clone()
        {
            return new ArsenalBlockData(color, bulletCount, tier, isHidden, isInterlocked, partnerCol);
        }
    }

    [Serializable]
    public class ArsenalColumnData
    {
        public int columnIndex;
        public bool isLockedInitially = false;
        public int unlockKeyId = 0; // Key required to unlock
        // Ordered from active (index 0) down to bottom of stack
        public List<ArsenalBlockData> blocks = new List<ArsenalBlockData>();
    }

    [Serializable]
    public class LevelData
    {
        public int levelId = 1;
        public int tier = 1; // 1 = Tutorial, 2 = Easy, 3 = Medium, 4 = Hard, 5 = Expert
        public int gridWidth = 7;
        public int gridHeight = 8;
        public int gridDepth = 1;
        public int arsenalColumnsCount = 5;

        public WinConditionType winCondition = WinConditionType.ClearAll;
        public float clearPercentageRequired = 100f; // If ClearPercentage
        public int targetChestsRequired = 1;         // If OpenChests

        public List<EnemyBlockData> enemyBlocks = new List<EnemyBlockData>();
        public List<ArsenalColumnData> arsenalColumns = new List<ArsenalColumnData>();
    }
}
