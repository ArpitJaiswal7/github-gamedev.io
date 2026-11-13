using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace JigsawSolitaire.Core
{
    public enum SolitaireScene
    {
        Sunset,
        Flamingo,
        Forest,
        Coast
    }

    public enum RightPanelTab
    {
        Leaderboard,
        PuzzlesCatalog
    }

    public enum LeaderboardMode
    {
        SolitaireLeague,
        UnifiedLeague
    }

    public class SolitaireCard
    {
        public string id;
        public int row;       // Target row in complete image (0..rows-1)
        public int col;       // Target col in complete image (0..cols-1)
        public int boardRow;  // Current board row (0..rows-1)
        public int boardCol;  // Current board col (0..cols-1)
        public int groupId;
        public bool faceDown;

        public Texture2D cardTexture; // UV slice of fullBoardTexture

        public float pulseScale = 1f;
        public float pulseTimer = 0f;
    }

    public class CardFlightAnim
    {
        public SolitaireCard card;
        public int fromRow;
        public int fromCol;
        public int toRow;
        public int toCol;
        public float progress;
        public float duration;
        public Action onComplete;
    }

    public class VisualEffectItem
    {
        public enum EffectType { Ring, Sparkle }
        public EffectType type;
        public Vector2 pos;
        public Vector2 velocity;
        public float radius;
        public float startRadius;
        public float targetRadius;
        public Color color;
        public float alpha;
        public float age;
        public float duration;
        public float size;
    }

    [Serializable]
    public class LeaderboardBotEntry
    {
        public string botId;
        public string name;
        public string archetype;
        public long score;
        public long targetEndScore;
    }

    [Serializable]
    public class LeaderboardSeasonState
    {
        public int schemaVersion = 1;
        public int seasonNumber = 1;
        public string seasonId;
        public string gameMode;
        public string scoringRule;
        public long startTimeMs;
        public long endTimeMs;
        public long lastSimulatedAtMs;
        public long lastSeenTimestampMs;
        public string status = "active";
        public bool rewardClaimed = false;
        public long playerScore = 0;
        public int runsCompleted = 0;
        public int previousRank = 30;
        public int currentRank = 30;
        public List<LeaderboardBotEntry> bots = new List<LeaderboardBotEntry>();
    }

    public class LeaderboardRowView
    {
        public bool isDivider;
        public int skippedCount;
        public int rank;
        public string name;
        public string archetype;
        public long score;
        public bool isPlayer;
        public string rewardCoinText;
        public string formattedScore;
    }

    public class LeaderboardComputedView
    {
        public int totalEntries = 30;
        public int playerRank = 30;
        public int netDelta = 0;
        public string countdownText = "06d 23h";
        public string phaseText = "Live Sync";
        public bool isClamped = false;
        public bool isExpired = false;
        public bool rewardClaimed = false;
        public string rewardTierName = "Participation";
        public int rewardTierCoins = 25;
        public string playerFormattedScore = "Unranked";
        public string scoreLabel = "Best:";
        public List<LeaderboardRowView> windowedRows = new List<LeaderboardRowView>();
    }

    public class JigsawSolitaireGameManager : MonoBehaviour
    {
        public static JigsawSolitaireGameManager Instance { get; private set; }

        public int rows = 4;
        public int cols = 4;
        public SolitaireScene currentScene = SolitaireScene.Sunset;
        public int coins = 150;
        public int hintsLeft = 3;
        public int magnetsLeft = 3;

        public bool showPreview = false;
        public bool mysteryMode = false;
        public bool rightPanelExpanded = true;
        public RightPanelTab rightTab = RightPanelTab.Leaderboard;
        public LeaderboardMode lbActiveMode = LeaderboardMode.SolitaireLeague;
        public bool isPuzzleComplete = false;
        public bool isAnimatingPowerup = false;

        // Run Timer & Win Summary
        public float runElapsedSeconds = 0f;
        public bool runSubmitted = false;
        public string winSummaryText = "All solitaire cards connected into a seamless picture! +25 Coins earned.";
        public string winRankBadgeText = "League Rank Updated!";

        // Toast feedback
        public string activeToastMessage = "";
        public float toastTimer = 0f;
        public bool toastIsError = false;

        // Offline Leaderboard State
        public long lbSimOffsetMs = 0L;
        private LeaderboardSeasonState lbSolitaireState;
        private LeaderboardSeasonState lbUnifiedState;
        public LeaderboardComputedView lbCurrentView = new LeaderboardComputedView();
        public int topHeaderRank = 30;
        private float lbSyncTimer = 0f;

        private static readonly string[] LB_BOT_NAMES = new string[]
        {
            "Whiskers_99", "ZenSolver", "TileMaster", "PawsAndClaws", "CalicoQueen",
            "JigsawNinja", "MochiCat", "PixelPuzzler", "ShadowTail", "CozyCrafter",
            "SolverPrime", "NekoSweeper", "VelvetPaw", "GridWalker", "BiscuitMaker",
            "PurrfectLine", "CornerPiece", "EdgeHunter", "CardFlipper", "CatnapKing",
            "MineSniffer", "TuxedoCat", "AmberEyes", "SilentPounce", "LuckyClover",
            "StarlightCat", "SnapMaster", "SolitaireAce", "GingerSnap"
        };

        // Board & Card Metrics (Vertical Rectangle Cards, Min Gap)
        public float boardWidth = 400f;
        public float boardHeight = 500f;
        public const float CARD_GAP = 1.5f;
        public const float BOARD_ASPECT = 0.80f;

        public List<SolitaireCard> cards = new List<SolitaireCard>();
        public int nextGroupId = 1;

        // Active Full Board Texture
        public Texture2D fullBoardTexture;
        public Texture2D cardBackTexture;

        // Server Puzzles & Native Image Bridge
        public List<ServerPuzzleItem> serverPuzzles = new List<ServerPuzzleItem>();
        public int activeServerPuzzleIndex = -1;
        public Texture2D activeServerTexture;
        public bool isDownloadingServerImage = false;

        // Dragging & Multi-tile Swap State
        public SolitaireCard draggingLead = null;
        public List<SolitaireCard> draggingCluster = new List<SolitaireCard>();
        public Vector2 dragScreenOffset = Vector2.zero;
        public int targetLeadRow = 0;
        public int targetLeadCol = 0;

        // Flight & Visual Effects
        public List<CardFlightAnim> activeFlights = new List<CardFlightAnim>();
        public List<VisualEffectItem> activeEffects = new List<VisualEffectItem>();

        // Audio synthesizer
        private AudioSource audioSource;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else if (Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
        }

        private void Start()
        {
            cardBackTexture = GenerateCardBackTexture();
            LbInitSeasons();
            LbRefreshCurrentView();

            // Auto-fetch server puzzle catalog
            StartCoroutine(AutoFetchServerPuzzlesCoroutine());

            InitPuzzle(false);
        }

        private void Update()
        {
            if (!isPuzzleComplete && !isAnimatingPowerup)
            {
                runElapsedSeconds += Time.deltaTime;
            }

            if (toastTimer > 0f)
            {
                toastTimer -= Time.deltaTime;
                if (toastTimer <= 0f) activeToastMessage = "";
            }

            // Sync Leaderboard every 1.5s
            lbSyncTimer += Time.deltaTime;
            if (lbSyncTimer >= 1.5f)
            {
                lbSyncTimer = 0f;
                LbRefreshCurrentView();
            }

            // Update flight animations
            for (int i = activeFlights.Count - 1; i >= 0; i--)
            {
                var fl = activeFlights[i];
                fl.progress += Time.deltaTime / Mathf.Max(0.01f, fl.duration);
                if (fl.progress >= 1f)
                {
                    fl.progress = 1f;
                    activeFlights.RemoveAt(i);
                    fl.onComplete?.Invoke();
                }
            }

            // Update visual effects
            for (int i = activeEffects.Count - 1; i >= 0; i--)
            {
                var fx = activeEffects[i];
                fx.age += Time.deltaTime;
                float t = fx.age / Mathf.Max(0.01f, fx.duration);
                if (t >= 1f)
                {
                    activeEffects.RemoveAt(i);
                    continue;
                }

                if (fx.type == VisualEffectItem.EffectType.Ring)
                {
                    fx.radius = Mathf.Lerp(fx.startRadius, fx.targetRadius, Mathf.SmoothStep(0f, 1f, t));
                    fx.alpha = 1f - t;
                }
                else
                {
                    fx.pos += fx.velocity * Time.deltaTime;
                    fx.velocity *= 0.94f;
                    fx.alpha = 1f - t;
                }
            }
        }

        public void ShowToast(string message, bool isError = false)
        {
            activeToastMessage = message;
            toastIsError = isError;
            toastTimer = 3.6f;
        }

        // --- PUZZLE SETUP & GENERATION ---
        public void InitPuzzle(bool preserveState = false)
        {
            isPuzzleComplete = false;
            runElapsedSeconds = 0f;
            runSubmitted = false;
            activeFlights.Clear();
            activeEffects.Clear();
            isAnimatingPowerup = false;
            draggingLead = null;
            draggingCluster.Clear();

            int total = rows * cols;

            if (activeServerPuzzleIndex >= 0 && activeServerTexture != null)
            {
                fullBoardTexture = activeServerTexture;
            }
            else
            {
                fullBoardTexture = GenerateFullBoardTexture(512, (int)(512 / BOARD_ASPECT), currentScene);
            }

            if (!preserveState || cards.Count != total)
            {
                cards.Clear();
                nextGroupId = 1;

                for (int r = 0; r < rows; r++)
                {
                    for (int c = 0; c < cols; c++)
                    {
                        var card = new SolitaireCard
                        {
                            id = $"card-{r}-{c}",
                            row = r,
                            col = c,
                            boardRow = r,
                            boardCol = c,
                            groupId = nextGroupId++,
                            faceDown = false
                        };
                        BakeCardTexture(card, fullBoardTexture);
                        cards.Add(card);
                    }
                }

                ScrambleCardsWithoutInitialMatches();

                if (mysteryMode)
                {
                    ApplyMysteryFaceDownCards();
                }
            }

            EvaluateBoardMerges(true);
        }

        private void ScrambleCardsWithoutInitialMatches()
        {
            var slots = new List<Vector2Int>();
            for (int r = 0; r < rows; r++)
                for (int c = 0; c < cols; c++)
                    slots.Add(new Vector2Int(r, c));

            var rng = new System.Random();
            for (int attempt = 0; attempt < 50; attempt++)
            {
                // Shuffle slots
                int n = slots.Count;
                while (n > 1)
                {
                    n--;
                    int k = rng.Next(n + 1);
                    var tmp = slots[k];
                    slots[k] = slots[n];
                    slots[n] = tmp;
                }

                for (int i = 0; i < cards.Count; i++)
                {
                    cards[i].boardRow = slots[i].x;
                    cards[i].boardCol = slots[i].y;
                    cards[i].groupId = nextGroupId++;
                }

                bool hasPreMatch = false;
                for (int i = 0; i < cards.Count; i++)
                {
                    for (int j = i + 1; j < cards.Count; j++)
                    {
                        var a = cards[i];
                        var b = cards[j];
                        if (Mathf.Abs(a.row - b.row) + Mathf.Abs(a.col - b.col) == 1)
                        {
                            if (a.boardRow - b.boardRow == a.row - b.row && a.boardCol - b.boardCol == a.col - b.col)
                            {
                                hasPreMatch = true;
                                break;
                            }
                        }
                    }
                    if (hasPreMatch) break;
                }

                if (!hasPreMatch) break;
            }
        }

        private void ApplyMysteryFaceDownCards()
        {
            int total = cards.Count;
            int targetFaceDown = Mathf.Max(2, Mathf.FloorToInt(total * 0.22f));
            var indices = new List<int>();
            for (int i = 0; i < total; i++) indices.Add(i);

            var rng = new System.Random();
            int n = indices.Count;
            while (n > 1)
            {
                n--;
                int k = rng.Next(n + 1);
                int tmp = indices[k];
                indices[k] = indices[n];
                indices[n] = tmp;
            }

            for (int i = 0; i < total; i++)
            {
                cards[indices[i]].faceDown = (i < targetFaceDown);
            }
        }

        private void BakeCardTexture(SolitaireCard card, Texture2D source)
        {
            int sliceW = Mathf.Max(4, source.width / cols);
            int sliceH = Mathf.Max(4, source.height / rows);

            var tex = new Texture2D(sliceW, sliceH, TextureFormat.RGBA32, false);
            int startX = card.col * sliceW;
            // Unity textures have 0,0 at bottom-left; rows in board are 0 at top
            int startY = (rows - 1 - card.row) * sliceH;

            Color[] pixels = source.GetPixels(startX, startY, sliceW, sliceH);
            tex.SetPixels(pixels);
            tex.Apply();

            card.cardTexture = tex;
        }

        public SolitaireCard GetCardAtSlot(int bRow, int bCol)
        {
            return cards.Find(c => c.boardRow == bRow && c.boardCol == bCol);
        }

        public int CountConnectedCards()
        {
            var counts = new Dictionary<int, int>();
            foreach (var c in cards)
            {
                if (!counts.ContainsKey(c.groupId)) counts[c.groupId] = 0;
                counts[c.groupId]++;
            }
            int connected = 0;
            foreach (var kvp in counts)
            {
                if (kvp.Value > 1) connected += kvp.Value;
            }
            return connected;
        }

        public bool CheckWinCondition()
        {
            if (cards.Count == 0) return false;
            int firstGid = cards[0].groupId;
            return cards.TrueForAll(c => !c.faceDown && c.groupId == firstGid);
        }

        // --- MERGE & CARD CONNECTION LOGIC ---
        public bool EvaluateBoardMerges(bool silent = false)
        {
            bool anyMerged = false;
            bool loopMerged = true;
            var seamPoints = new List<Vector2>();
            var mergedGroupIds = new HashSet<int>();

            while (loopMerged)
            {
                loopMerged = false;
                for (int i = 0; i < cards.Count; i++)
                {
                    var a = cards[i];
                    if (a.faceDown) continue;

                    for (int j = i + 1; j < cards.Count; j++)
                    {
                        var b = cards[j];
                        if (b.faceDown || a.groupId == b.groupId) continue;

                        int dTrueR = a.row - b.row;
                        int dTrueC = a.col - b.col;
                        if (Mathf.Abs(dTrueR) + Mathf.Abs(dTrueC) != 1) continue;

                        int dBoardR = a.boardRow - b.boardRow;
                        int dBoardC = a.boardCol - b.boardCol;

                        if (dBoardR == dTrueR && dBoardC == dTrueC)
                        {
                            int targetGid = Mathf.Min(a.groupId, b.groupId);
                            int oldGid = Mathf.Max(a.groupId, b.groupId);

                            foreach (var c in cards)
                            {
                                if (c.groupId == oldGid) c.groupId = targetGid;
                            }

                            mergedGroupIds.Add(targetGid);

                            float tileW = boardWidth / cols;
                            float tileH = boardHeight / rows;
                            float seamX = ((a.boardCol + b.boardCol) * 0.5f + 0.5f) * tileW;
                            float seamY = ((a.boardRow + b.boardRow) * 0.5f + 0.5f) * tileH;
                            seamPoints.Add(new Vector2(seamX, seamY));

                            loopMerged = true;
                            anyMerged = true;
                        }
                    }
                }
            }

            if (anyMerged && mysteryMode)
            {
                bool flippedAny = false;
                int[][] dirs = new int[][] { new int[] { -1, 0 }, new int[] { 1, 0 }, new int[] { 0, -1 }, new int[] { 0, 1 } };
                foreach (var c in cards)
                {
                    if (!c.faceDown) continue;
                    foreach (var d in dirs)
                    {
                        var nb = GetCardAtSlot(c.boardRow + d[0], c.boardCol + d[1]);
                        if (nb != null && !nb.faceDown && mergedGroupIds.Contains(nb.groupId))
                        {
                            c.faceDown = false;
                            flippedAny = true;
                            break;
                        }
                    }
                }
                if (flippedAny)
                {
                    EvaluateBoardMerges(true);
                }
            }

            if (anyMerged && !silent)
            {
                TriggerSeamMergeEffects(mergedGroupIds, seamPoints);
            }

            if (CheckWinCondition() && !runSubmitted)
            {
                RegisterWin();
            }

            return anyMerged;
        }

        private void RegisterWin()
        {
            isPuzzleComplete = true;
            runSubmitted = true;
            coins += 25;

            long elapsedMs = (long)(runElapsedSeconds * 1000f);
            int leaguePts = 100 + (rows * cols * 5) + Mathf.Max(0, 300 - Mathf.RoundToInt(runElapsedSeconds * 2f));

            int deltaBest = LbSubmitRun(LeaderboardMode.SolitaireLeague, elapsedMs, out int newBestRank);
            LbSubmitRun(LeaderboardMode.UnifiedLeague, leaguePts, out int newUniRank);

            winSummaryText = $"Puzzle cleared in {Mathf.FloorToInt(runElapsedSeconds / 60):00}:{(runElapsedSeconds % 60):00}! +25 Coins & +{leaguePts} League Points!";
            winRankBadgeText = deltaBest > 0 ? $"Rank #{newBestRank} (+{deltaBest} spots!)" : $"Rank #{newBestRank}";

            PlayCalmSynth(523.25f, 0.45f);
        }

        private void TriggerSeamMergeEffects(HashSet<int> mergedGids, List<Vector2> seamPoints)
        {
            float tileMin = Mathf.Min(boardWidth / cols, boardHeight / rows);
            foreach (var pt in seamPoints)
            {
                // Ripple ring
                activeEffects.Add(new VisualEffectItem
                {
                    type = VisualEffectItem.EffectType.Ring,
                    pos = pt,
                    startRadius = tileMin * 0.25f,
                    targetRadius = tileMin * 0.85f,
                    radius = tileMin * 0.25f,
                    color = new Color(0.98f, 0.80f, 0.25f, 0.95f),
                    alpha = 1f,
                    age = 0f,
                    duration = 0.52f
                });

                // Sparkles
                for (int i = 0; i < 8; i++)
                {
                    float angle = (i / 8f) * Mathf.PI * 2f;
                    float speed = UnityEngine.Random.Range(40f, 110f);
                    activeEffects.Add(new VisualEffectItem
                    {
                        type = VisualEffectItem.EffectType.Sparkle,
                        pos = pt,
                        velocity = new Vector2(Mathf.Cos(angle) * speed, Mathf.Sin(angle) * speed),
                        color = new Color(0.35f, 0.85f, 0.98f, 0.95f),
                        alpha = 1f,
                        age = 0f,
                        duration = 0.48f,
                        size = UnityEngine.Random.Range(3f, 6f)
                    });
                }
            }

            PlayCalmSynth(392f, 0.28f);
        }

        public void PlayCalmSynth(float freq, float duration)
        {
            if (audioSource == null) return;
            try
            {
                int sampleRate = 44100;
                int sampleCount = (int)(sampleRate * duration);
                float[] samples = new float[sampleCount];
                for (int i = 0; i < sampleCount; i++)
                {
                    float t = (float)i / sampleRate;
                    float envelope = Mathf.Exp(-5f * t);
                    samples[i] = Mathf.Sin(2f * Mathf.PI * freq * t) * envelope * 0.25f;
                }

                AudioClip clip = AudioClip.Create("Chime", sampleCount, 1, sampleRate, false);
                clip.SetData(samples, 0);
                audioSource.PlayOneShot(clip);
            }
            catch { }
        }

        // --- MULTI-TILE DISPLACEMENT SWAP ---
        public void ExecuteClusterDrop(SolitaireCard lead, int targetR, int targetC)
        {
            int dRow = targetR - lead.boardRow;
            int dCol = targetC - lead.boardCol;

            if (dRow == 0 && dCol == 0) return;

            var cluster = draggingCluster;
            var movingSet = new HashSet<SolitaireCard>(cluster);

            var sourceSlots = new List<Vector2Int>();
            var targetSlots = new List<Vector2Int>();

            foreach (var c in cluster)
            {
                sourceSlots.Add(new Vector2Int(c.boardRow, c.boardCol));
                targetSlots.Add(new Vector2Int(c.boardRow + dRow, c.boardCol + dCol));
            }

            var targetKeySet = new HashSet<Vector2Int>(targetSlots);
            var sourceKeySet = new HashSet<Vector2Int>(sourceSlots);

            var vacatedSlots = new List<Vector2Int>();
            foreach (var s in sourceSlots)
            {
                if (!targetKeySet.Contains(s)) vacatedSlots.Add(s);
            }

            var displacedCards = new List<SolitaireCard>();
            foreach (var s in targetSlots)
            {
                if (!sourceKeySet.Contains(s))
                {
                    var occ = GetCardAtSlot(s.x, s.y);
                    if (occ != null && !movingSet.Contains(occ))
                    {
                        displacedCards.Add(occ);
                    }
                }
            }

            // Split displaced cards' groups so contiguity is re-evaluated cleanly
            var affectedGroupIds = new HashSet<int>();
            foreach (var dc in displacedCards)
            {
                affectedGroupIds.Add(dc.groupId);
                dc.groupId = nextGroupId++;
            }

            var flightMoves = new List<CardFlightAnim>();
            for (int idx = 0; idx < displacedCards.Count; idx++)
            {
                var dc = displacedCards[idx];
                var vac = vacatedSlots[idx];
                flightMoves.Add(new CardFlightAnim
                {
                    card = dc,
                    fromRow = dc.boardRow,
                    fromCol = dc.boardCol,
                    toRow = vac.x,
                    toCol = vac.y,
                    progress = 0f,
                    duration = 0.28f
                });
            }

            foreach (var c in cluster)
            {
                c.boardRow += dRow;
                c.boardCol += dCol;
            }

            foreach (var fl in flightMoves)
            {
                fl.card.boardRow = fl.toRow;
                fl.card.boardCol = fl.toCol;
            }

            activeFlights.AddRange(flightMoves);

            EvaluateBoardMerges(false);
        }

        // --- POWERUPS ---
        public void TriggerHint()
        {
            if (isAnimatingPowerup || isPuzzleComplete) return;
            if (hintsLeft <= 0)
            {
                ShowToast("No Hints remaining!", true);
                return;
            }

            int[][] dirs = new int[][] { new int[] { -1, 0 }, new int[] { 1, 0 }, new int[] { 0, -1 }, new int[] { 0, 1 } };
            SolitaireCard anchor = null;
            SolitaireCard mover = null;
            SolitaireCard occupant = null;
            int targetR = 0, targetC = 0;

            var shuffled = new List<SolitaireCard>(cards);
            shuffled.Sort((a, b) => UnityEngine.Random.value.CompareTo(0.5f));

            foreach (var cand in shuffled)
            {
                foreach (var d in dirs)
                {
                    int tr = cand.boardRow + d[0];
                    int tc = cand.boardCol + d[1];
                    if (tr < 0 || tr >= rows || tc < 0 || tc >= cols) continue;

                    var trueNb = cards.Find(c => c.row == cand.row + d[0] && c.col == cand.col + d[1]);
                    if (trueNb == null || trueNb.groupId == cand.groupId) continue;

                    var occ = GetCardAtSlot(tr, tc);
                    if (occ == null) continue;

                    int moverGrpSize = cards.FindAll(c => c.groupId == trueNb.groupId).Count;
                    int occGrpSize = cards.FindAll(c => c.groupId == occ.groupId).Count;

                    if (moverGrpSize == 1 && occGrpSize == 1)
                    {
                        anchor = cand;
                        mover = trueNb;
                        occupant = occ;
                        targetR = tr;
                        targetC = tc;
                        break;
                    }
                }
                if (mover != null) break;
            }

            if (mover == null)
            {
                var misplaced = cards.Find(c => c.boardRow != c.row || c.boardCol != c.col);
                if (misplaced != null)
                {
                    var occ = GetCardAtSlot(misplaced.row, misplaced.col);
                    if (occ != null)
                    {
                        mover = misplaced;
                        occupant = occ;
                        targetR = misplaced.row;
                        targetC = misplaced.col;
                    }
                }
            }

            if (mover == null || occupant == null)
            {
                ShowToast("All pieces already in connected pairs!");
                return;
            }

            hintsLeft--;
            mover.faceDown = false;
            occupant.faceDown = false;

            int oldMoverR = mover.boardRow;
            int oldMoverC = mover.boardCol;

            mover.boardRow = targetR;
            mover.boardCol = targetC;
            occupant.boardRow = oldMoverR;
            occupant.boardCol = oldMoverC;

            ShowToast("💡 Swapping matching neighbor cards together...");

            activeFlights.Add(new CardFlightAnim { card = mover, fromRow = oldMoverR, fromCol = oldMoverC, toRow = targetR, toCol = targetC, duration = 0.32f });
            activeFlights.Add(new CardFlightAnim { card = occupant, fromRow = targetR, fromCol = targetC, toRow = oldMoverR, toCol = oldMoverC, duration = 0.32f });

            EvaluateBoardMerges(false);
        }

        public void TriggerMagnet()
        {
            if (isAnimatingPowerup || isPuzzleComplete) return;
            if (magnetsLeft <= 0)
            {
                ShowToast("No Magnet powerups remaining!", true);
                return;
            }

            var misplaced = cards.FindAll(c => c.boardRow != c.row || c.boardCol != c.col || c.faceDown);
            if (misplaced.Count == 0) return;

            var seed = misplaced[UnityEngine.Random.Range(0, misplaced.Count)];
            int[][] dirs = new int[][] { new int[] { 0, 0 }, new int[] { -1, 0 }, new int[] { 1, 0 }, new int[] { 0, -1 }, new int[] { 0, 1 } };
            var trio = new List<SolitaireCard>();
            foreach (var d in dirs)
            {
                var cand = cards.Find(c => c.row == seed.row + d[0] && c.col == seed.col + d[1]);
                if (cand != null && trio.Count < 3) trio.Add(cand);
            }

            magnetsLeft--;

            foreach (var card in trio)
            {
                card.faceDown = false;
                if (card.boardRow == card.row && card.boardCol == card.col) continue;

                var occupant = GetCardAtSlot(card.row, card.col);
                if (occupant == null || occupant == card) continue;

                int fromR = card.boardRow;
                int fromC = card.boardCol;
                int toR = card.row;
                int toC = card.col;

                card.boardRow = toR;
                card.boardCol = toC;
                occupant.boardRow = fromR;
                occupant.boardCol = fromC;
                occupant.groupId = nextGroupId++;

                activeFlights.Add(new CardFlightAnim { card = card, fromRow = fromR, fromCol = fromC, toRow = toR, toCol = toC, duration = 0.32f });
                activeFlights.Add(new CardFlightAnim { card = occupant, fromRow = toR, fromCol = toC, toRow = fromR, toCol = fromC, duration = 0.32f });
            }

            ShowToast($"🧲 Magnetized {trio.Count} neighboring cards into place...");
            EvaluateBoardMerges(false);
        }

        public void TriggerShuffle()
        {
            if (isAnimatingPowerup || isPuzzleComplete) return;

            var counts = new Dictionary<int, int>();
            foreach (var c in cards)
            {
                if (!counts.ContainsKey(c.groupId)) counts[c.groupId] = 0;
                counts[c.groupId]++;
            }

            var singleCards = cards.FindAll(c => counts[c.groupId] == 1);
            if (singleCards.Count < 2)
            {
                ShowToast("All remaining cards are already in connected groups!");
                return;
            }

            var slots = new List<Vector2Int>();
            foreach (var c in singleCards) slots.Add(new Vector2Int(c.boardRow, c.boardCol));

            var rng = new System.Random();
            int n = slots.Count;
            while (n > 1)
            {
                n--;
                int k = rng.Next(n + 1);
                var tmp = slots[k];
                slots[k] = slots[n];
                slots[n] = tmp;
            }

            for (int i = 0; i < singleCards.Count; i++)
            {
                var card = singleCards[i];
                int fromR = card.boardRow;
                int fromC = card.boardCol;
                int toR = slots[i].x;
                int toC = slots[i].y;

                card.boardRow = toR;
                card.boardCol = toC;

                if (fromR != toR || fromC != toC)
                {
                    activeFlights.Add(new CardFlightAnim { card = card, fromRow = fromR, fromCol = fromC, toRow = toR, toCol = toC, duration = 0.30f });
                }
            }

            ShowToast("🔀 Shuffled unconnected cards!");
            EvaluateBoardMerges(false);
        }

        // --- PROCEDURAL TEXTURES (SUNSET, FLAMINGO, FOREST, COAST, CARD BACK) ---
        private Texture2D GenerateFullBoardTexture(int w, int h, SolitaireScene sc)
        {
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            Color[] colsArr = new Color[w * h];

            for (int y = 0; y < h; y++)
            {
                float ny = (float)y / h;
                float yTop = 1f - ny;

                for (int x = 0; x < w; x++)
                {
                    float nx = (float)x / w;
                    Color pixelColor = Color.black;

                    if (sc == SolitaireScene.Flamingo)
                    {
                        if (yTop < 0.54f)
                        {
                            float tSky = yTop / 0.54f;
                            pixelColor = Color.Lerp(new Color(0.98f, 0.66f, 0.46f), new Color(0.97f, 0.62f, 0.40f), tSky);

                            // Sun
                            float dx = nx - 0.28f;
                            float dy = yTop - 0.42f;
                            if (dx * dx + dy * dy < 0.012f)
                            {
                                pixelColor = new Color(1f, 0.96f, 0.75f);
                            }
                        }
                        else
                        {
                            float tWater = (yTop - 0.54f) / 0.46f;
                            pixelColor = Color.Lerp(new Color(0.22f, 0.64f, 0.65f), new Color(0.96f, 0.64f, 0.38f), tWater);
                        }

                        // Flamingo Body
                        float bdx = (nx - 0.58f) / 0.21f;
                        float bdy = (yTop - 0.53f) / 0.10f;
                        if (bdx * bdx + bdy * bdy < 1f)
                        {
                            pixelColor = Color.Lerp(new Color(1f, 0.46f, 0.56f), new Color(0.79f, 0.09f, 0.29f), (nx - 0.37f) / 0.4f);
                        }

                        // Flamingo Head
                        float hdx = (nx - 0.33f) / 0.048f;
                        float hdy = (yTop - 0.20f) / 0.048f;
                        if (hdx * hdx + hdy * hdy < 1f)
                        {
                            pixelColor = new Color(1f, 0.36f, 0.56f);
                        }
                    }
                    else if (sc == SolitaireScene.Sunset)
                    {
                        if (yTop < 0.65f)
                        {
                            float tSky = yTop / 0.65f;
                            pixelColor = Color.Lerp(new Color(0.23f, 0.11f, 0.37f), new Color(0.97f, 0.70f, 0.40f), tSky);

                            // Sun
                            float dx = nx - 0.62f;
                            float dy = yTop - 0.34f;
                            if (dx * dx + dy * dy < 0.018f) pixelColor = new Color(1f, 0.90f, 0.60f);
                        }
                        else
                        {
                            float tLake = (yTop - 0.65f) / 0.35f;
                            pixelColor = Color.Lerp(new Color(0.88f, 0.48f, 0.37f), new Color(0.15f, 0.08f, 0.23f), tLake);
                        }
                    }
                    else if (sc == SolitaireScene.Forest)
                    {
                        pixelColor = Color.Lerp(new Color(0.66f, 0.85f, 0.86f), new Color(0.27f, 0.48f, 0.62f), yTop);
                        if (yTop > 0.65f)
                        {
                            pixelColor = new Color(0.11f, 0.26f, 0.20f);
                        }
                    }
                    else // Coast
                    {
                        if (yTop < 0.55f)
                        {
                            pixelColor = Color.Lerp(new Color(0.28f, 0.79f, 0.89f), new Color(0.79f, 0.94f, 0.97f), yTop / 0.55f);
                        }
                        else
                        {
                            pixelColor = Color.Lerp(new Color(0f, 0.47f, 0.71f), new Color(0.96f, 0.89f, 0.52f), (yTop - 0.55f) / 0.45f);
                        }
                    }

                    colsArr[y * w + x] = pixelColor;
                }
            }

            tex.SetPixels(colsArr);
            tex.Apply();
            return tex;
        }

        private Texture2D GenerateCardBackTexture()
        {
            int w = 120, h = 150;
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            Color c1 = new Color(0.43f, 0.16f, 0.85f); // #6d28d9
            Color c2 = new Color(0.30f, 0.11f, 0.58f); // #4c1d95

            for (int y = 0; y < h; y++)
            {
                float t = (float)y / h;
                for (int x = 0; x < w; x++)
                {
                    bool isBorder = (x < 3 || x >= w - 3 || y < 3 || y >= h - 3);
                    if (isBorder)
                    {
                        tex.SetPixel(x, y, new Color(0.85f, 0.71f, 0.99f, 0.75f));
                    }
                    else
                    {
                        tex.SetPixel(x, y, Color.Lerp(c1, c2, t));
                    }
                }
            }
            tex.Apply();
            return tex;
        }

        // --- BACKEND SERVER PUZZLE CATALOG LOADER ---
        private IEnumerator AutoFetchServerPuzzlesCoroutine()
        {
            var bridge = JigsawSolitaireServerBridge.EnsureInitialized();
            yield return bridge.GetPublishedManifestCoroutine(
                JigsawSolitaireServerBridge.DefaultServerUrl,
                onSuccess: list =>
                {
                    serverPuzzles = list;
                    Debug.Log($"[JigsawSolitaire] Loaded {serverPuzzles.Count} published server puzzles.");
                },
                onError: err =>
                {
                    Debug.Log($"[JigsawSolitaire] Server catalog not reached (will use demo scenes): {err}");
                }
            );
        }

        public void LoadServerPuzzle(int index)
        {
            if (index < 0 || index >= serverPuzzles.Count || isDownloadingServerImage) return;

            var item = serverPuzzles[index];
            activeServerPuzzleIndex = index;
            isDownloadingServerImage = true;
            ShowToast($"🌐 Downloading {item.title} from Backend Server...");

            var bridge = JigsawSolitaireServerBridge.EnsureInitialized();
            bridge.LoadPuzzleImageViaBridge(
                item,
                1080,
                onLoaded: (tex, info) =>
                {
                    isDownloadingServerImage = false;
                    activeServerTexture = tex;
                    InitPuzzle(false);
                    ShowToast($"✅ Loaded {item.title} ({info})!");
                },
                onError: err =>
                {
                    isDownloadingServerImage = false;
                    ShowToast($"Failed to load puzzle: {err}", true);
                }
            );
        }

        // --- REAL-TIME OFFLINE SIMULATION LEADERBOARD ENGINE ---
        private static long NowEpochMs()
        {
            return (long)(DateTime.UtcNow - new DateTime(1970, 1, 1)).TotalMilliseconds;
        }

        private void LbInitSeasons()
        {
            lbSolitaireState = LbLoadState(LeaderboardMode.SolitaireLeague);
            lbUnifiedState = LbLoadState(LeaderboardMode.UnifiedLeague);
        }

        private LeaderboardSeasonState LbLoadState(LeaderboardMode mode)
        {
            string key = mode == LeaderboardMode.SolitaireLeague ? "agy_lb_v1_jigsaw_solitaire" : "agy_lb_v1_unified_league";
            string raw = PlayerPrefs.GetString(key, "");
            if (!string.IsNullOrEmpty(raw))
            {
                try
                {
                    var parsed = JsonUtility.FromJson<LeaderboardSeasonState>(raw);
                    if (parsed != null && parsed.bots != null && parsed.bots.Count > 0)
                        return parsed;
                }
                catch { }
            }
            return LbCreateSeason(mode, 1);
        }

        private void LbSaveState(LeaderboardMode mode, LeaderboardSeasonState st)
        {
            string key = mode == LeaderboardMode.SolitaireLeague ? "agy_lb_v1_jigsaw_solitaire" : "agy_lb_v1_unified_league";
            PlayerPrefs.SetString(key, JsonUtility.ToJson(st));
            PlayerPrefs.Save();
        }

        public LeaderboardSeasonState LbCreateSeason(LeaderboardMode mode, int seasonNum)
        {
            bool isCumulative = (mode == LeaderboardMode.UnifiedLeague);
            long now = NowEpochMs() + lbSimOffsetMs;
            long seasonDur = 7L * 24L * 3600L * 1000L;
            long minTarget = isCumulative ? 900L : 28000L;
            long maxTarget = isCumulative ? 4800L : 240000L;
            long span = maxTarget - minTarget;
            int botCount = 29;
            int seed = seasonNum * 101 + (isCumulative ? 518 : 312);

            var st = new LeaderboardSeasonState
            {
                schemaVersion = 1,
                seasonNumber = seasonNum,
                seasonId = $"S0{seasonNum}_{(isCumulative ? "UNIFIED" : "SOLITAIRE")}",
                gameMode = isCumulative ? "unified_league" : "jigsaw_solitaire",
                scoringRule = isCumulative ? "cumulative" : "best_time",
                startTimeMs = now,
                endTimeMs = now + seasonDur,
                lastSimulatedAtMs = now,
                lastSeenTimestampMs = now,
                status = "active",
                rewardClaimed = false,
                playerScore = 0,
                runsCompleted = 0,
                previousRank = 30,
                currentRank = 30,
                bots = new List<LeaderboardBotEntry>()
            };

            for (int i = 0; i < botCount; i++)
            {
                float ratio = (float)i / Mathf.Max(1, botCount - 1);
                string archetype = ratio < 0.15f ? "elite" : (ratio < 0.45f ? "rival" : (ratio < 0.75f ? "steady" : "casual"));
                float jitter = ((seed * (i + 17) * 7919) % 100) / 100f;

                if (isCumulative)
                {
                    long targetEnd = (long)Mathf.Round(maxTarget - ratio * span + (jitter - 0.5f) * 180f);
                    long initScore = (long)Mathf.Round(targetEnd * (0.04f + jitter * 0.08f));
                    st.bots.Add(new LeaderboardBotEntry
                    {
                        botId = $"bot_{i + 1}",
                        name = LB_BOT_NAMES[i % LB_BOT_NAMES.Length],
                        archetype = archetype,
                        score = Math.Max(20L, initScore),
                        targetEndScore = Math.Max(250L, targetEnd)
                    });
                }
                else
                {
                    long targetBestMs = (long)Mathf.Round(minTarget + Mathf.Pow(ratio, 1.15f) * span + (jitter - 0.5f) * 4000f);
                    long initMs = (long)Mathf.Round(targetBestMs * (1.15f + jitter * 0.15f));
                    st.bots.Add(new LeaderboardBotEntry
                    {
                        botId = $"bot_{i + 1}",
                        name = LB_BOT_NAMES[i % LB_BOT_NAMES.Length],
                        archetype = archetype,
                        score = initMs,
                        targetEndScore = Math.Max(minTarget, targetBestMs)
                    });
                }
            }

            LbSaveState(mode, st);
            return st;
        }

        private LeaderboardComputedView LbAdvanceAndCompute(LeaderboardMode mode, LeaderboardSeasonState st)
        {
            bool isCumulative = (mode == LeaderboardMode.UnifiedLeague);
            long rawNow = NowEpochMs() + lbSimOffsetMs;
            long effectiveNow = Math.Max(rawNow, st.lastSeenTimestampMs);
            st.lastSeenTimestampMs = effectiveNow;

            long simTarget = Math.Min(effectiveNow, st.endTimeMs);
            long dtMs = Math.Max(0L, simTarget - st.lastSimulatedAtMs);

            if (dtMs > 0L)
            {
                float totalDur = Mathf.Max(1f, (float)(st.endTimeMs - st.startTimeMs));
                float prevProg = Mathf.Clamp01((float)(st.lastSimulatedAtMs - st.startTimeMs) / totalDur);
                float newProg = Mathf.Clamp01((float)(simTarget - st.startTimeMs) / totalDur);
                float deltaProg = Mathf.Max(0f, newProg - prevProg);

                if (deltaProg > 0f)
                {
                    bool isLate = newProg >= 0.65f;
                    long pScore = st.playerScore;

                    for (int idx = 0; idx < st.bots.Count; idx++)
                    {
                        var bot = st.bots[idx];
                        if (isLate && pScore > 0 && (bot.archetype == "rival" || bot.archetype == "elite"))
                        {
                            if (isCumulative)
                            {
                                float proj = pScore / Mathf.Max(0.35f, newProg);
                                float diff = proj - bot.targetEndScore;
                                if (Mathf.Abs(diff) < 1200f)
                                    bot.targetEndScore = (long)Mathf.Round(bot.targetEndScore + diff * 0.18f);
                            }
                            else
                            {
                                float diff = pScore - bot.targetEndScore;
                                if (Mathf.Abs(diff) < 25000f)
                                    bot.targetEndScore = (long)Mathf.Round(bot.targetEndScore + diff * 0.15f);
                            }
                        }

                        float remProg = Mathf.Max(0.01f, 1f - prevProg);
                        float stepFrac = Mathf.Min(1f, deltaProg / remProg);
                        float variance = 0.85f + (((idx * 31 + Mathf.FloorToInt(newProg * 100f)) % 30) / 100f);

                        if (isCumulative)
                        {
                            long remPts = Math.Max(0L, bot.targetEndScore - bot.score);
                            bot.score += (long)Mathf.Round(remPts * stepFrac * variance);
                        }
                        else
                        {
                            long remRoom = Math.Max(0L, bot.score - bot.targetEndScore);
                            bot.score = Math.Max(bot.targetEndScore, bot.score - (long)Mathf.Round(remRoom * stepFrac * variance * 0.75f));
                        }
                    }
                }
                st.lastSimulatedAtMs = simTarget;
            }

            if (effectiveNow >= st.endTimeMs && st.status == "active")
            {
                st.status = "expired_unclaimed";
            }

            var entries = new List<LeaderboardRowView>(st.bots.Count + 1);
            foreach (var b in st.bots)
            {
                entries.Add(new LeaderboardRowView
                {
                    name = b.name,
                    archetype = b.archetype,
                    score = b.score,
                    isPlayer = false,
                    formattedScore = LbFormatScore(b.score, isCumulative)
                });
            }
            entries.Add(new LeaderboardRowView
            {
                name = "You (Player)",
                archetype = "YOU",
                score = st.playerScore,
                isPlayer = true,
                formattedScore = LbFormatScore(st.playerScore, isCumulative)
            });

            entries.Sort((a, b) =>
            {
                if (isCumulative)
                {
                    if (b.score != a.score) return b.score.CompareTo(a.score);
                    return a.isPlayer ? -1 : 1;
                }
                else
                {
                    long aVal = a.score > 0 ? a.score : long.MaxValue;
                    long bVal = b.score > 0 ? b.score : long.MaxValue;
                    if (aVal != bVal) return aVal.CompareTo(bVal);
                    return a.isPlayer ? -1 : 1;
                }
            });

            int playerRank = entries.Count;
            for (int i = 0; i < entries.Count; i++)
            {
                entries[i].rank = i + 1;
                int r = i + 1;
                entries[i].rewardCoinText = r == 1 ? "500c" : (r <= 3 ? "250c" : (r <= 10 ? "100c" : "25c"));
                if (entries[i].isPlayer) playerRank = r;
            }

            st.currentRank = playerRank;

            var windowed = new List<LeaderboardRowView>();
            int pIdx = entries.FindIndex(e => e.isPlayer);
            var top3 = entries.GetRange(0, Math.Min(3, entries.Count));
            windowed.AddRange(top3);

            if (pIdx > 4)
            {
                windowed.Add(new LeaderboardRowView { isDivider = true, skippedCount = pIdx - 4 });
            }

            int wStart = Math.Max(3, pIdx - 1);
            int wEnd = Math.Min(entries.Count - 1, pIdx + 1);
            for (int k = wStart; k <= wEnd; k++)
            {
                if (!windowed.Contains(entries[k])) windowed.Add(entries[k]);
            }

            int lastIdx = windowed.Count > 0 ? windowed[windowed.Count - 1].rank - 1 : 0;
            if (entries.Count - 1 > lastIdx + 1)
            {
                windowed.Add(new LeaderboardRowView { isDivider = true, skippedCount = entries.Count - 2 - lastIdx });
                windowed.Add(entries[entries.Count - 1]);
            }
            else if (entries.Count - 1 > lastIdx)
            {
                windowed.Add(entries[entries.Count - 1]);
            }

            long remMs = Math.Max(0L, st.endTimeMs - effectiveNow);
            long days = remMs / (24L * 3600L * 1000L);
            long hours = (remMs % (24L * 3600L * 1000L)) / (3600L * 1000L);

            return new LeaderboardComputedView
            {
                totalEntries = entries.Count,
                playerRank = playerRank,
                netDelta = (st.previousRank - playerRank),
                countdownText = $"{days:D2}d {hours:D2}h",
                phaseText = effectiveNow >= st.endTimeMs ? "Ended" : "Live Sync",
                isExpired = effectiveNow >= st.endTimeMs,
                rewardClaimed = st.rewardClaimed,
                playerFormattedScore = LbFormatScore(st.playerScore, isCumulative),
                scoreLabel = isCumulative ? "Total:" : "Best:",
                rewardTierName = playerRank == 1 ? "Champion" : (playerRank <= 3 ? "Top 3" : (playerRank <= 10 ? "Top 10" : "Participant")),
                rewardTierCoins = playerRank == 1 ? 500 : (playerRank <= 3 ? 250 : (playerRank <= 10 ? 100 : 25)),
                windowedRows = windowed
            };
        }

        public void LbRefreshCurrentView()
        {
            if (lbSolitaireState == null || lbUnifiedState == null) LbInitSeasons();
            var solView = LbAdvanceAndCompute(LeaderboardMode.SolitaireLeague, lbSolitaireState);
            var uniView = LbAdvanceAndCompute(LeaderboardMode.UnifiedLeague, lbUnifiedState);
            topHeaderRank = solView.playerRank;
            lbCurrentView = (lbActiveMode == LeaderboardMode.SolitaireLeague) ? solView : uniView;
        }

        public int LbSubmitRun(LeaderboardMode mode, long rawScore, out int newRank)
        {
            if (lbSolitaireState == null || lbUnifiedState == null) LbInitSeasons();
            var st = (mode == LeaderboardMode.SolitaireLeague) ? lbSolitaireState : lbUnifiedState;
            var preView = LbAdvanceAndCompute(mode, st);
            newRank = preView.playerRank;

            if (preView.isExpired || st.status != "active") return 0;

            int prevRank = preView.playerRank;
            st.previousRank = prevRank;
            st.runsCompleted += 1;

            if (mode == LeaderboardMode.UnifiedLeague)
            {
                st.playerScore += rawScore;
            }
            else
            {
                if (st.playerScore == 0 || rawScore < st.playerScore)
                    st.playerScore = rawScore;
            }

            var postView = LbAdvanceAndCompute(mode, st);
            newRank = postView.playerRank;
            LbSaveState(mode, st);
            return prevRank - newRank;
        }

        public void LbPlus4h()
        {
            lbSimOffsetMs += 4L * 3600L * 1000L;
            LbRefreshCurrentView();
            ShowToast("+4h Advanced • Simulated bot progression!");
        }

        public void LbClaimReward()
        {
            var st = (lbActiveMode == LeaderboardMode.SolitaireLeague) ? lbSolitaireState : lbUnifiedState;
            if (st.rewardClaimed) return;
            st.rewardClaimed = true;
            coins += lbCurrentView.rewardTierCoins;
            LbSaveState(lbActiveMode, st);
            LbRefreshCurrentView();
            ShowToast($"🎉 Claimed {lbCurrentView.rewardTierCoins} Coins for {lbCurrentView.rewardTierName} rank!");
        }

        public void LbResetSeason()
        {
            var st = LbCreateSeason(lbActiveMode, (lbActiveMode == LeaderboardMode.SolitaireLeague ? lbSolitaireState.seasonNumber : lbUnifiedState.seasonNumber) + 1);
            if (lbActiveMode == LeaderboardMode.SolitaireLeague) lbSolitaireState = st;
            else lbUnifiedState = st;
            LbRefreshCurrentView();
            ShowToast("🔄 New Season initialized!");
        }

        private static string LbFormatScore(long score, bool isCumulative)
        {
            if (score <= 0) return "--";
            if (isCumulative) return $"{score:N0} pts";
            int totalSec = (int)(score / 1000L);
            return $"{totalSec / 60:D2}:{totalSec % 60:D2}";
        }
    }
}
