using System;
using System.Collections.Generic;
using UnityEngine;

namespace Meowsweeper.Core
{
    public class LeaderboardRowView
    {
        public bool isDivider;
        public int skippedCount;
        public int rank;
        public string name;
        public string archetype;
        public int score;
        public bool isPlayer;
        public string rewardLabel;
        public string formattedScore;
    }

    public class LeaderboardSyncResult
    {
        public string modeId;
        public string seasonId;
        public string status;
        public bool rewardClaimed;
        public bool isClamped;
        public string phaseText;
        public string countdownText;
        public int playerRank;
        public int totalEntries;
        public int rankDelta;
        public string playerScoreFormatted;
        public string rewardTierName;
        public int rewardTierCoins;
        public List<LeaderboardRowView> windowedRows = new List<LeaderboardRowView>();
    }

    public static class OfflineLeaderboardService
    {
        private static readonly string[] BotNames = new string[]
        {
            "Whiskers_99", "ZenSolver", "TileMaster", "PawsAndClaws", "CalicoQueen",
            "JigsawNinja", "MochiCat", "PixelPuzzler", "ShadowTail", "CozyCrafter",
            "SolverPrime", "NekoSweeper", "VelvetPaw", "GridWalker", "BiscuitMaker",
            "PurrfectLine", "CornerPiece", "EdgeHunter", "TabAndBlank", "CatnapKing",
            "MineSniffer", "TuxedoCat", "AmberEyes", "SilentPounce", "LuckyClover",
            "StarlightCat", "SnapMaster", "SolitaireAce", "GingerSnap"
        };

        private class BotState
        {
            public string botId;
            public string name;
            public string archetype;
            public int score;
            public int targetEndScore;
        }

        private class ModeSeasonState
        {
            public int seasonNumber = 1;
            public string seasonId;
            public string modeId;
            public bool isCumulative;
            public double startTimeSec;
            public double endTimeSec;
            public double lastSimulatedSec;
            public double lastSeenSec;
            public string status = "active";
            public bool rewardClaimed = false;
            public int playerScore = 0;
            public int runsCompleted = 0;
            public int previousRank = 30;
            public int currentRank = 30;
            public List<BotState> bots = new List<BotState>();
        }

        private static Dictionary<string, ModeSeasonState> seasons = new Dictionary<string, ModeSeasonState>();
        private static double simOffsetSec = 0;
        public static string ActiveMode = "meowsweeper"; // "meowsweeper" or "unified_league"

        private const string PREF_WALLET = "MeowWebGL_WalletCoins";
        private const string PREF_BEST_MS = "MeowWebGL_BestTimeMs";
        private const string PREF_LEAGUE_PTS = "MeowWebGL_LeaguePts";

        public static int GetWalletCoins()
        {
            return PlayerPrefs.GetInt(PREF_WALLET, 150);
        }

        public static void SetWalletCoins(int coins)
        {
            PlayerPrefs.SetInt(PREF_WALLET, coins);
            PlayerPrefs.Save();
        }

        private static double NowSec()
        {
            return Time.realtimeSinceStartupAsDouble + 1700000000.0 + simOffsetSec;
        }

        private static ModeSeasonState GetOrCreateSeason(string modeId, int seasonNum = 1)
        {
            if (seasons.TryGetValue(modeId, out var existing) && existing != null)
            {
                return existing;
            }

            bool isCumulative = modeId == "unified_league";
            double now = NowSec();
            double durationSec = isCumulative ? (7 * 24 * 3600) : (3 * 24 * 3600);
            int baseMin = isCumulative ? 900 : 14000;
            int baseMax = isCumulative ? 4800 : 135000;
            int botCount = 29;
            int seed = seasonNum * 101 + modeId.Length * 37;

            var st = new ModeSeasonState
            {
                seasonNumber = seasonNum,
                seasonId = $"S0{seasonNum}_{modeId.ToUpper()}",
                modeId = modeId,
                isCumulative = isCumulative,
                startTimeSec = now,
                endTimeSec = now + durationSec,
                lastSimulatedSec = now,
                lastSeenSec = now,
                status = "active",
                rewardClaimed = false,
                playerScore = isCumulative
                    ? PlayerPrefs.GetInt(PREF_LEAGUE_PTS, 0)
                    : PlayerPrefs.GetInt(PREF_BEST_MS, 0),
                previousRank = 30,
                currentRank = 30
            };

            for (int i = 0; i < botCount; i++)
            {
                float ratio = i / (float)Math.Max(1, botCount - 1);
                string archetype = ratio < 0.15f ? "elite" : (ratio < 0.45f ? "rival" : (ratio < 0.75f ? "steady" : "casual"));
                float jitter = ((seed * (i + 17) * 7919) % 100) / 100f;
                int span = baseMax - baseMin;

                if (isCumulative)
                {
                    int targetEnd = Mathf.RoundToInt(baseMax - ratio * span + (jitter - 0.5f) * 180f);
                    int initial = Mathf.RoundToInt(targetEnd * (0.04f + jitter * 0.08f));
                    st.bots.Add(new BotState
                    {
                        botId = $"bot_{i + 1}",
                        name = BotNames[i % BotNames.Length],
                        archetype = archetype,
                        score = Mathf.Max(20, initial),
                        targetEndScore = Mathf.Max(250, targetEnd)
                    });
                }
                else
                {
                    int targetBestMs = Mathf.RoundToInt(baseMin + Mathf.Pow(ratio, 1.15f) * span + (jitter - 0.5f) * 3500f);
                    int initialMs = Mathf.RoundToInt(targetBestMs * (1.15f + jitter * 0.15f));
                    st.bots.Add(new BotState
                    {
                        botId = $"bot_{i + 1}",
                        name = BotNames[i % BotNames.Length],
                        archetype = archetype,
                        score = initialMs,
                        targetEndScore = Mathf.Max(baseMin, targetBestMs)
                    });
                }
            }

            seasons[modeId] = st;
            return st;
        }

        public static LeaderboardSyncResult SyncAndCalculate(string modeId)
        {
            var st = GetOrCreateSeason(modeId);
            double rawNow = NowSec();
            bool clamped = rawNow < st.lastSeenSec;
            double effectiveNow = Math.Max(rawNow, st.lastSeenSec);
            st.lastSeenSec = effectiveNow;

            double simTarget = Math.Min(effectiveNow, st.endTimeSec);
            double dtSec = Math.Max(0, simTarget - st.lastSimulatedSec);

            if (dtSec > 0)
            {
                double totalDur = Math.Max(1.0, st.endTimeSec - st.startTimeSec);
                float prevProg = Mathf.Clamp01((float)((st.lastSimulatedSec - st.startTimeSec) / totalDur));
                float newProg = Mathf.Clamp01((float)((simTarget - st.startTimeSec) / totalDur));
                float deltaProg = Mathf.Max(0f, newProg - prevProg);

                if (deltaProg > 0f)
                {
                    bool isLate = newProg >= 0.65f;
                    int pScore = st.playerScore;

                    for (int idx = 0; idx < st.bots.Count; idx++)
                    {
                        var bot = st.bots[idx];
                        if (isLate && pScore > 0 && (bot.archetype == "rival" || bot.archetype == "elite"))
                        {
                            if (st.isCumulative)
                            {
                                float proj = pScore / Mathf.Max(0.35f, newProg);
                                float diff = proj - bot.targetEndScore;
                                if (Mathf.Abs(diff) < 1200f)
                                    bot.targetEndScore = Mathf.RoundToInt(bot.targetEndScore + diff * 0.18f);
                            }
                            else
                            {
                                float diff = pScore - bot.targetEndScore;
                                if (Mathf.Abs(diff) < 25000f)
                                    bot.targetEndScore = Mathf.RoundToInt(bot.targetEndScore + diff * 0.15f);
                            }
                        }

                        float remProg = Mathf.Max(0.01f, 1f - prevProg);
                        float stepFrac = Mathf.Min(1f, deltaProg / remProg);
                        float variance = 0.85f + (((idx * 31 + Mathf.FloorToInt(newProg * 100f)) % 30) / 100f);

                        if (st.isCumulative)
                        {
                            int remPts = Mathf.Max(0, bot.targetEndScore - bot.score);
                            bot.score += Mathf.RoundToInt(remPts * stepFrac * variance);
                        }
                        else
                        {
                            int remRoom = Mathf.Max(0, bot.score - bot.targetEndScore);
                            bot.score = Mathf.Max(bot.targetEndScore, bot.score - Mathf.RoundToInt(remRoom * stepFrac * variance * 0.75f));
                        }
                    }
                }
                st.lastSimulatedSec = simTarget;
            }

            if (effectiveNow >= st.endTimeSec && st.status == "active")
            {
                st.status = "expired_unclaimed";
            }

            return BuildResult(st, clamped, effectiveNow);
        }

        private static LeaderboardSyncResult BuildResult(ModeSeasonState st, bool clamped, double effectiveNow)
        {
            var all = new List<LeaderboardRowView>();
            foreach (var b in st.bots)
            {
                all.Add(new LeaderboardRowView
                {
                    name = b.name,
                    archetype = b.archetype,
                    score = b.score,
                    isPlayer = false,
                    formattedScore = FormatScore(b.score, st.isCumulative)
                });
            }
            all.Add(new LeaderboardRowView
            {
                name = "You (Player)",
                archetype = "YOU",
                score = st.playerScore,
                isPlayer = true,
                formattedScore = FormatScore(st.playerScore, st.isCumulative)
            });

            all.Sort((a, b) =>
            {
                if (st.isCumulative)
                {
                    if (b.score != a.score) return b.score.CompareTo(a.score);
                    return a.isPlayer ? -1 : 1;
                }
                else
                {
                    int aVal = a.score > 0 ? a.score : int.MaxValue;
                    int bVal = b.score > 0 ? b.score : int.MaxValue;
                    if (aVal != bVal) return aVal.CompareTo(bVal);
                    return a.isPlayer ? -1 : 1;
                }
            });

            int playerRank = all.Count;
            for (int i = 0; i < all.Count; i++)
            {
                all[i].rank = i + 1;
                int r = i + 1;
                all[i].rewardLabel = r == 1 ? "500c" : (r <= 3 ? "250c" : (r <= 10 ? "100c" : "25c"));
                if (all[i].isPlayer) playerRank = r;
            }

            st.currentRank = playerRank;

            // Windowed rows: Top 3 + player neighborhood (-2..+2)
            var selected = new SortedSet<int> { 0, 1, 2 };
            int pIdx = playerRank - 1;
            for (int i = Mathf.Max(0, pIdx - 2); i <= Mathf.Min(all.Count - 1, pIdx + 2); i++)
            {
                selected.Add(i);
            }
            int padIdx = 3;
            while (selected.Count < 8 && padIdx < all.Count) selected.Add(padIdx++);

            var windowed = new List<LeaderboardRowView>();
            int prev = -1;
            foreach (int idx in selected)
            {
                if (prev != -1 && idx - prev > 1)
                {
                    windowed.Add(new LeaderboardRowView
                    {
                        isDivider = true,
                        skippedCount = idx - prev - 1
                    });
                }
                windowed.Add(all[idx]);
                prev = idx;
            }

            double remSec = Math.Max(0, st.endTimeSec - effectiveNow);
            int totalMins = (int)(remSec / 60.0);
            int d = totalMins / 1440;
            int h = (totalMins % 1440) / 60;
            int m = totalMins % 60;
            string cdText = remSec <= 0 ? "ENDED" : $"{d}d {h:D2}h {m:D2}m";

            double totalDur = Math.Max(1.0, st.endTimeSec - st.startTimeSec);
            float pct = Mathf.Clamp01((float)((effectiveNow - st.startTimeSec) / totalDur));
            string phaseText = clamped ? "Clamped (Anti-Rollback)" : (pct >= 1f ? "Locked" : (pct >= 0.65f ? "Convergence" : "Live Sync"));

            string tierName = playerRank == 1 ? "Gold Crown" : (playerRank <= 3 ? "Silver Chest" : (playerRank <= 10 ? "Bronze Pack" : "Participation"));
            int tierCoins = playerRank == 1 ? 500 : (playerRank <= 3 ? 250 : (playerRank <= 10 ? 100 : 25));

            return new LeaderboardSyncResult
            {
                modeId = st.modeId,
                seasonId = st.seasonId,
                status = st.status,
                rewardClaimed = st.rewardClaimed,
                isClamped = clamped,
                phaseText = phaseText,
                countdownText = cdText,
                playerRank = playerRank,
                totalEntries = all.Count,
                rankDelta = st.previousRank - playerRank,
                playerScoreFormatted = FormatScore(st.playerScore, st.isCumulative),
                rewardTierName = tierName,
                rewardTierCoins = tierCoins,
                windowedRows = windowed
            };
        }

        public static string FormatScore(int score, bool isCumulative)
        {
            if (isCumulative) return $"{score:N0} pts";
            if (score <= 0) return "Unranked";
            return $"{(score / 1000f):F2}s";
        }

        public static (int prevRank, int newRank, int rankDelta) SubmitRun(string modeId, int rawScore)
        {
            var pre = SyncAndCalculate(modeId);
            var st = GetOrCreateSeason(modeId);
            if (st.status != "active") return (pre.playerRank, pre.playerRank, 0);

            int prevRank = pre.playerRank;
            st.previousRank = prevRank;
            st.runsCompleted++;

            if (st.isCumulative)
            {
                st.playerScore += rawScore;
                PlayerPrefs.SetInt(PREF_LEAGUE_PTS, st.playerScore);
            }
            else
            {
                if (st.playerScore == 0 || rawScore < st.playerScore)
                {
                    st.playerScore = rawScore;
                    PlayerPrefs.SetInt(PREF_BEST_MS, st.playerScore);
                }
            }
            PlayerPrefs.Save();

            var post = SyncAndCalculate(modeId);
            return (prevRank, post.playerRank, prevRank - post.playerRank);
        }

        public static void AdvanceTimeHours(float hours)
        {
            simOffsetSec += hours * 3600.0;
        }

        public static void ExpireSeason(string modeId)
        {
            var st = GetOrCreateSeason(modeId);
            double rem = Math.Max(0, st.endTimeSec - NowSec());
            simOffsetSec += rem + 1.0;
        }

        public static bool ClaimSeasonReward(string modeId, out string rewardMsg)
        {
            var res = SyncAndCalculate(modeId);
            var st = GetOrCreateSeason(modeId);
            if (st.status == "active")
            {
                rewardMsg = "Season is still active!";
                return false;
            }
            if (st.rewardClaimed)
            {
                rewardMsg = "Idempotent Guard: Season reward already claimed!";
                return false;
            }
            st.rewardClaimed = true;
            st.status = "archived";
            SetWalletCoins(GetWalletCoins() + res.rewardTierCoins);
            rewardMsg = $"Claimed {res.rewardTierName} (+{res.rewardTierCoins} Coins)!";
            return true;
        }

        public static void StartNextSeason(string modeId)
        {
            var st = GetOrCreateSeason(modeId);
            int nextNum = st.seasonNumber + 1;
            seasons.Remove(modeId);
            simOffsetSec = 0;
            GetOrCreateSeason(modeId, nextNum);
        }
    }
}
