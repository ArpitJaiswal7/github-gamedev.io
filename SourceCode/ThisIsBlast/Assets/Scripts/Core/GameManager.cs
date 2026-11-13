using System.Collections;
using ThisIsBlast.Arsenal;
using ThisIsBlast.Combat;
using ThisIsBlast.Data;
using ThisIsBlast.Effects;
using ThisIsBlast.EnemyGrid;
using ThisIsBlast.UI;
using UnityEngine;

namespace ThisIsBlast.Core
{
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        public GameState CurrentState { get; private set; } = GameState.Loading;
        public int CurrentLevelIndex { get; private set; } = 1;
        public int TotalGems { get; private set; } = 0;
        public int CurrentScore { get; private set; } = 0;

        private int _shotsUsedInLevel = 0;
        private int _mergesInLevel = 0;
        private int _gemsEarnedInLevel = 0;
        private int _chestsOpenedInLevel = 0;
        private LevelData _currentLevelData;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            // Load saved data
            CurrentLevelIndex = PlayerPrefs.GetInt("CurrentLevel", 1);
            TotalGems = PlayerPrefs.GetInt("TotalGems", 0);
        }

        private void Start()
        {
            GameEvents.OnBlockDestroyed += HandleBlockDestroyed;
            GameEvents.OnChestOpened += HandleChestOpened;
            GameEvents.OnPiggyBankHit += HandlePiggyBankHit;
            GameEvents.OnArsenalBlockFired += HandleBlockFired;
            GameEvents.OnArsenalMergeTriggered += HandleMergeTriggered;

            // Start first level!
            StartCoroutine(InitializeGame());
        }

        private void OnDestroy()
        {
            GameEvents.OnBlockDestroyed -= HandleBlockDestroyed;
            GameEvents.OnChestOpened -= HandleChestOpened;
            GameEvents.OnPiggyBankHit -= HandlePiggyBankHit;
            GameEvents.OnArsenalBlockFired -= HandleBlockFired;
            GameEvents.OnArsenalMergeTriggered -= HandleMergeTriggered;
        }

        private IEnumerator InitializeGame()
        {
            yield return null; // Wait for all singletons to initialize
            LoadLevel(CurrentLevelIndex);
        }

        public void LoadLevel(int levelNumber)
        {
            StartCoroutine(LoadLevelRoutine(levelNumber));
        }

        private IEnumerator LoadLevelRoutine(int levelNumber)
        {
            CurrentLevelIndex = levelNumber;
            PlayerPrefs.SetInt("CurrentLevel", CurrentLevelIndex);
            PlayerPrefs.Save();

            CurrentState = GameState.Loading;
            _shotsUsedInLevel = 0;
            _mergesInLevel = 0;
            _gemsEarnedInLevel = 0;
            _chestsOpenedInLevel = 0;

            LevelData loadedLevel = null;
            if (LiveOpsManager.Instance != null)
            {
                yield return StartCoroutine(LiveOpsManager.Instance.FetchProgressionLevelRoutine(levelNumber, data => loadedLevel = data));
            }

            // Fallback to LevelDecoder 3-tier loading (tracks_manifest -> persistent cache -> resources -> procedural)
            if (loadedLevel == null)
            {
                loadedLevel = LevelDecoder.LoadLevel(levelNumber);
            }

            _currentLevelData = loadedLevel;

            // Build enemy grid & arsenal
            EnemyGridManager.Instance?.BuildGrid(_currentLevelData);
            ArsenalManager.Instance?.BuildArsenal(_currentLevelData);

            HUDController.Instance?.UpdateLevel(CurrentLevelIndex);
            HUDController.Instance?.UpdateGems(TotalGems, 0);
            HUDController.Instance?.UpdateScore(CurrentScore, 0);

            AnalyticsManager.Instance?.LogLevelStart(CurrentLevelIndex, _currentLevelData.tier);
            GameEvents.TriggerLevelLoaded(_currentLevelData);

            CurrentState = GameState.Playing;
            Debug.Log($"<color=cyan>[GameManager] Level {levelNumber} (Tier {_currentLevelData.tier}) Loaded with Archetype '{LiveOpsManager.Instance?.ActiveArchetype}' and Seed '{LiveOpsManager.Instance?.ActiveSeed}'!</color>");
        }

        public void CheckWinCondition()
        {
            if (CurrentState != GameState.Playing) return;
            if (_currentLevelData == null) return;

            int remaining = EnemyGridManager.Instance ? EnemyGridManager.Instance.RemainingBlocksCount : 0;
            int totalInitial = _currentLevelData.enemyBlocks != null ? _currentLevelData.enemyBlocks.Count : 1;

            bool isWon = false;

            switch (_currentLevelData.winCondition)
            {
                case WinConditionType.ClearPercentage:
                    float percent = ((float)(totalInitial - remaining) / Mathf.Max(1, totalInitial)) * 100f;
                    if (percent >= _currentLevelData.clearPercentageRequired || remaining <= 0)
                    {
                        isWon = true;
                    }
                    break;

                case WinConditionType.OpenChests:
                    if (_chestsOpenedInLevel >= _currentLevelData.targetChestsRequired || remaining <= 0)
                    {
                        isWon = true;
                    }
                    break;

                case WinConditionType.ClearAll:
                default:
                    if (remaining <= 0)
                    {
                        isWon = true;
                    }
                    break;
            }

            if (isWon)
            {
                TriggerVictory();
            }
        }

        public void TriggerVictory()
        {
            if (CurrentState != GameState.Playing) return;
            CurrentState = GameState.Victory;

            LiveOpsManager.Instance?.RecordVictory();

            SoundManager.Instance?.PlayWin();
            CameraShaker.Instance?.Shake(0.25f, 0.25f);

            // Calculate stars (3 stars = optimal, 2 stars = moderate, 1 star = barely)
            int stars = _shotsUsedInLevel < 15 ? 3 : (_shotsUsedInLevel < 35 ? 2 : 1);
            int levelBonusGems = 5 * _currentLevelData.tier;
            AddGems(levelBonusGems);
            _gemsEarnedInLevel += levelBonusGems;

            AnalyticsManager.Instance?.LogLevelComplete(CurrentLevelIndex, _shotsUsedInLevel, _gemsEarnedInLevel, _mergesInLevel);
            GameEvents.TriggerGameOver(true, stars, _gemsEarnedInLevel);

            UIManager.Instance?.ShowWinPopup(stars, _gemsEarnedInLevel);
        }

        public void TriggerDefeat(string reason = "Out of Ammo!")
        {
            if (CurrentState != GameState.Playing) return;

            // Safety Check: If enemies are already dead, it's a victory, not defeat!
            int remaining = EnemyGridManager.Instance ? EnemyGridManager.Instance.RemainingBlocksCount : 0;
            if (remaining <= 0)
            {
                Debug.Log("<color=green>[GameManager] Defeat prevented because all enemies are cleared -> Triggering VICTORY instead!</color>");
                TriggerVictory();
                return;
            }

            CurrentState = GameState.Defeat;

            LiveOpsManager.Instance?.RecordDefeat();

            SoundManager.Instance?.PlayFail();
            CameraShaker.Instance?.Shake(0.2f, 0.2f);

            AnalyticsManager.Instance?.LogLevelFail(CurrentLevelIndex, reason, remaining);
            GameEvents.TriggerGameOver(false, 0, 0);

            UIManager.Instance?.ShowFailPopup(reason);
        }

        public void ReviveWithAd()
        {
            // Revive: find first column and add 10 bullets to its active peek block!
            var blocks = ArsenalManager.Instance ? ArsenalManager.Instance.GetAllArsenalBlocks() : null;
            if (blocks != null && blocks.Count > 0)
            {
                blocks[0].AddBullets(10);
                FloatingText.Create(blocks[0].transform.position, "+10 BULLETS (AD)! 📺", Color.cyan, 0.45f);
            }
            CurrentState = GameState.Playing;
        }

        public void LoadNextLevel()
        {
            LoadLevel(CurrentLevelIndex + 1);
        }

        public void RestartLevel()
        {
            LoadLevel(CurrentLevelIndex);
        }

        public void AddGems(int delta)
        {
            TotalGems += delta;
            _gemsEarnedInLevel += delta;
            PlayerPrefs.SetInt("TotalGems", TotalGems);
            PlayerPrefs.Save();
            GameEvents.TriggerGemsChanged(TotalGems, delta);
        }

        public void AddScore(int delta)
        {
            CurrentScore += delta;
            GameEvents.TriggerScoreChanged(CurrentScore, delta);
        }

        private void HandleBlockDestroyed(int x, int y, BlockColor color, EnemyBlockType type)
        {
            AddScore(10);
            CheckWinCondition();
        }

        private void HandleChestOpened(int x, int y, int gems)
        {
            AddGems(gems);
            AddScore(100);
            _chestsOpenedInLevel++;
            CheckWinCondition();
        }

        private void HandlePiggyBankHit(int x, int y)
        {
            int gems = RemoteConfigService.Instance ? RemoteConfigService.Instance.GetInt("piggy_gem_per_hit", 1) : 1;
            AddGems(gems);
            AddScore(15);
        }

        private void HandleBlockFired(int col, BlockColor color, int remaining)
        {
            _shotsUsedInLevel++;
        }

        private void HandleMergeTriggered(BlockColor color, int bulletCount, int mergedCount)
        {
            _mergesInLevel++;
            AddScore(50 * mergedCount);
        }
    }
}
