using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CalmJigsaw.Core
{
    public enum PieceStatus
    {
        Tray,
        Board,
        Locked
    }

    public enum PuzzleScene
    {
        Sunset,
        Forest,
        Coast
    }

    public class JigsawPiece
    {
        public string id;
        public int row;
        public int col;
        public int topEdge;    // -1 (hole/blank), 0 (flat border), +1 (tab)
        public int rightEdge;
        public int bottomEdge;
        public int leftEdge;
        public bool isEdge;
        public PieceStatus status;
        public int groupId;

        // Position in board coordinates (0,0 is board top-left)
        public float x;
        public float y;

        public float tileSize;
        public float pad;
        public float spriteSize;

        public Texture2D pieceTexture;
        public Texture2D thumbTexture;
        public Texture2D outlineTexture;

        public float pulseScale = 1f;
        public float pulseTimer = 0f;
        public Color flashColor = Color.clear;
    }

    public class PieceFlightAnim
    {
        public JigsawPiece piece;
        public Vector2 startPos;
        public Vector2 targetPos;
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
        public float maxAlpha;
        public float age;
        public float duration;
        public float size;
    }

    public enum RightPanelTab
    {
        Drawer,
        Leaderboard
    }

    public enum LeaderboardMode
    {
        CalmJigsaw,
        UnifiedLeague
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

    public class CalmJigsawGameManager : MonoBehaviour
    {
        public static CalmJigsawGameManager Instance { get; private set; }

        public int gridSize = 4; // 4x4 up to 10x10
        public PuzzleScene currentScene = PuzzleScene.Sunset;
        public int coins = 150;
        public int hintsLeft = 3;
        public int magnetsLeft = 3;

        public bool edgesOnly = false;
        public bool showPreview = false;
        public bool drawerExpanded = true;
        public RightPanelTab rightTab = RightPanelTab.Drawer;
        public LeaderboardMode lbActiveMode = LeaderboardMode.CalmJigsaw;
        public bool isPuzzleComplete = false;
        public bool isAnimatingPowerup = false;

        // Run Timer & Win Summary
        public float runElapsedSeconds = 0f;
        public bool runSubmitted = false;
        public string winSummaryText = "Every interlocking piece placed! +25 Coins earned.";
        public string winRankBadgeText = "League Rank Updated!";

        // Offline Leaderboard State
        public long lbSimOffsetMs = 0L;
        private LeaderboardSeasonState lbJigsawState;
        private LeaderboardSeasonState lbUnifiedState;
        public LeaderboardComputedView lbCurrentView = new LeaderboardComputedView();
        public int topHeaderJigsawRank = 30;
        private float lbSyncTimer = 0f;

        private static readonly string[] LB_BOT_NAMES = new string[]
        {
            "Whiskers_99", "ZenSolver", "TileMaster", "PawsAndClaws", "CalicoQueen",
            "JigsawNinja", "MochiCat", "PixelPuzzler", "ShadowTail", "CozyCrafter",
            "SolverPrime", "NekoSweeper", "VelvetPaw", "GridWalker", "BiscuitMaker",
            "PurrfectLine", "CornerPiece", "EdgeHunter", "TabAndBlank", "CatnapKing",
            "MineSniffer", "TuxedoCat", "AmberEyes", "SilentPounce", "LuckyClover",
            "StarlightCat", "SnapMaster", "SolitaireAce", "GingerSnap"
        };

        public float boardPixelSize = 480f;
        public float snapThreshold = 28f;

        // Server Puzzle Images & Native Image Bridge state
        public List<ServerPuzzleItem> serverPuzzles = new List<ServerPuzzleItem>();
        public List<string> serverPublishedUrls = new List<string>();
        public int activeServerPuzzleIndex = -1; // -1 = procedural scene, >= 0 = serverPuzzles[index]
        public Texture2D activeServerTexture = null;
        public string serverStatusText = "Sync";

        public Texture2D fullBoardTexture;
        public List<JigsawPiece> pieces = new List<JigsawPiece>();
        public int nextGroupId = 1;

        // Active dragging
        public JigsawPiece draggingLead = null;
        public List<JigsawPiece> draggingGroup = new List<JigsawPiece>();
        public Vector2 dragOffset;
        public bool dragIsFromDrawer = false;
        public bool dragIsOverBoard = false;

        // Animations & Effects
        private List<PieceFlightAnim> activeFlights = new List<PieceFlightAnim>();
        public List<VisualEffectItem> activeEffects = new List<VisualEffectItem>();

        public static float Hypot(float a, float b) => Mathf.Sqrt(a * a + b * b);

        // Toast message banner
        public string toastMessage = "";
        public bool isToastError = false;
        private float toastTimer = 0f;

        // Audio
        private AudioSource audioSource;
        private readonly float[] PentatonicScale = new float[] { 261.63f, 293.66f, 329.63f, 392.00f, 440.00f, 523.25f, 587.33f, 659.25f, 783.99f };

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else { Destroy(gameObject); return; }

            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            coins = PlayerPrefs.GetInt("agy_liveops_wallet_coins", 150);
            CalmJigsawServerBridge.EnsureInitialized();
        }

        private void Start()
        {
            LbInitSeasons();
            InitPuzzle(false);
            LbRefreshView();
            SyncServerPuzzles(true);
        }

        private void Update()
        {
            float dt = Time.deltaTime;

            // Live run timer
            if (!isPuzzleComplete && !runSubmitted)
            {
                runElapsedSeconds += dt;
            }

            // Periodic bot sync every 3 seconds
            lbSyncTimer += dt;
            if (lbSyncTimer >= 3f)
            {
                lbSyncTimer = 0f;
                LbRefreshView();
            }

            // Toast timeout
            if (toastTimer > 0f)
            {
                toastTimer -= dt;
                if (toastTimer <= 0f) toastMessage = "";
            }

            // Powerup flight animations
            for (int i = activeFlights.Count - 1; i >= 0; i--)
            {
                var fl = activeFlights[i];
                fl.progress += dt / fl.duration;
                float t = Mathf.Clamp01(fl.progress);
                // Ease out cubic
                float ease = 1f - Mathf.Pow(1f - t, 3f);
                fl.piece.x = Mathf.Lerp(fl.startPos.x, fl.targetPos.x, ease);
                fl.piece.y = Mathf.Lerp(fl.startPos.y, fl.targetPos.y, ease);

                if (t >= 1f)
                {
                    fl.piece.x = fl.targetPos.x;
                    fl.piece.y = fl.targetPos.y;
                    fl.onComplete?.Invoke();
                    activeFlights.RemoveAt(i);
                }
            }

            if (activeFlights.Count == 0 && isAnimatingPowerup)
            {
                isAnimatingPowerup = false;
            }

            // Visual effects update
            for (int i = activeEffects.Count - 1; i >= 0; i--)
            {
                var eff = activeEffects[i];
                eff.age += dt;
                if (eff.age >= eff.duration)
                {
                    activeEffects.RemoveAt(i);
                    continue;
                }
                float prog = eff.age / eff.duration;
                eff.alpha = (1f - prog) * eff.maxAlpha;

                if (eff.type == VisualEffectItem.EffectType.Ring)
                {
                    eff.radius = Mathf.Lerp(eff.startRadius, eff.targetRadius, prog);
                }
                else if (eff.type == VisualEffectItem.EffectType.Sparkle)
                {
                    eff.pos += eff.velocity * dt;
                    eff.velocity *= 0.94f;
                }
            }

            // Piece pulse scales & flash fading
            foreach (var p in pieces)
            {
                if (p.pulseScale > 1f)
                {
                    p.pulseScale = Mathf.Max(1f, p.pulseScale - dt * 0.7f);
                }
                if (p.flashColor.a > 0f)
                {
                    p.flashColor.a = Mathf.Max(0f, p.flashColor.a - dt * 2.2f);
                }
            }
        }

        public void ShowToast(string message, bool isError = false, float duration = 3.5f)
        {
            toastMessage = message;
            isToastError = isError;
            toastTimer = duration;
        }

        // --- SERVER PUZZLE SYNC & NATIVE IMAGE BRIDGE INTEGRATION ---
        public void SyncServerPuzzles(bool autoSelectFirst = false)
        {
            var bridge = CalmJigsawServerBridge.EnsureInitialized();
            serverStatusText = "...";

            // Call Helper 1 (GetPublishedImageUrlsCoroutine) & Helper 2 (GetPublishedPuzzlesCoroutine)
            StartCoroutine(bridge.GetPublishedImageUrlsCoroutine(
                CalmJigsawServerBridge.DefaultServerUrl,
                urls => { serverPublishedUrls = urls; },
                _ => { }
            ));

            StartCoroutine(bridge.GetPublishedPuzzlesCoroutine(
                CalmJigsawServerBridge.DefaultServerUrl,
                manifest =>
                {
                    serverPuzzles = manifest != null && manifest.puzzles != null
                        ? manifest.puzzles
                        : new List<ServerPuzzleItem>();
                    serverStatusText = $"{serverPuzzles.Count} Live";

                    if (autoSelectFirst && serverPuzzles.Count > 0)
                    {
                        SelectServerPuzzle(0);
                    }
                    else if (!autoSelectFirst)
                    {
                        ShowToast($"Synced {serverPuzzles.Count} published puzzle(s) from Server!");
                    }
                },
                err =>
                {
                    serverStatusText = "Offline";
                    if (!autoSelectFirst)
                    {
                        ShowToast("Puzzle Server unreachable (http://localhost:4200)", true);
                    }
                }
            ));
        }

        public void SelectServerPuzzle(int index)
        {
            if (index < 0 || index >= serverPuzzles.Count) return;
            var item = serverPuzzles[index];
            var bridge = CalmJigsawServerBridge.EnsureInitialized();

            bridge.LoadServerPuzzleTexture(
                item,
                512,
                (tex, srcInfo) =>
                {
                    activeServerPuzzleIndex = index;
                    activeServerTexture = tex;
                    hintsLeft = 3;
                    magnetsLeft = 3;
                    InitPuzzle(false);
                    string label = string.IsNullOrEmpty(item.title) ? item.id : item.title;
                    ShowToast($"Loaded '{label}' via {srcInfo}");
                },
                err =>
                {
                    ShowToast($"Failed loading '{item.id}': {err}", true);
                }
            );
        }

        public void CyclePuzzleArtwork()
        {
            int serverCount = serverPuzzles != null ? serverPuzzles.Count : 0;
            if (serverCount > 0)
            {
                if (activeServerPuzzleIndex >= 0 && activeServerPuzzleIndex + 1 < serverCount)
                {
                    SelectServerPuzzle(activeServerPuzzleIndex + 1);
                    return;
                }
                else if (activeServerPuzzleIndex == serverCount - 1)
                {
                    // Switch to first procedural scene
                    activeServerPuzzleIndex = -1;
                    activeServerTexture = null;
                    currentScene = PuzzleScene.Sunset;
                    hintsLeft = 3;
                    magnetsLeft = 3;
                    InitPuzzle(false);
                    ShowToast("Switched to Demo Scene: Alpine Sunset");
                    return;
                }
                else if (activeServerPuzzleIndex == -1 && currentScene == PuzzleScene.Coast)
                {
                    // Loop back to first server puzzle
                    SelectServerPuzzle(0);
                    return;
                }
            }

            // Advance procedural scene
            activeServerPuzzleIndex = -1;
            activeServerTexture = null;
            currentScene = (PuzzleScene)(((int)currentScene + 1) % 3);
            hintsLeft = 3;
            magnetsLeft = 3;
            InitPuzzle(false);
        }

        public string GetActivePuzzleTitle()
        {
            if (activeServerPuzzleIndex >= 0 && serverPuzzles != null && activeServerPuzzleIndex < serverPuzzles.Count)
            {
                var item = serverPuzzles[activeServerPuzzleIndex];
                string raw = string.IsNullOrEmpty(item.title) ? item.id : item.title;
                return raw.Length > 13 ? "🌐 " + raw.Substring(0, 12) + "…" : "🌐 " + raw;
            }
            return currentScene == PuzzleScene.Sunset ? "Alpine Sunset" :
                   currentScene == PuzzleScene.Forest ? "Misty Pines" : "Calm Cove";
        }

        public void OpenBridgeCacheExplorer()
        {
            string activeId = "";
            if (activeServerPuzzleIndex >= 0 && serverPuzzles != null && activeServerPuzzleIndex < serverPuzzles.Count)
            {
                activeId = serverPuzzles[activeServerPuzzleIndex].id;
            }
            CalmJigsawServerBridge.EnsureInitialized().OpenIndexedDbCacheExplorer(activeId);
        }

        // --- PUZZLE INITIALIZATION ---
        public void InitPuzzle(bool preserveState = false)
        {
            isPuzzleComplete = false;
            runElapsedSeconds = 0f;
            runSubmitted = false;
            activeFlights.Clear();
            activeEffects.Clear();
            isAnimatingPowerup = false;
            draggingLead = null;
            draggingGroup.Clear();

            int N = gridSize;
            boardPixelSize = 480f;
            float tileSize = boardPixelSize / N;
            float pad = Mathf.Round(tileSize * 0.28f);
            float spriteSize = tileSize + pad * 2f;
            snapThreshold = Mathf.Clamp(tileSize * 0.35f, 16f, 34f);

            // Use Server Puzzle Texture from Native Image Bridge if selected, otherwise procedural demo scene
            if (activeServerPuzzleIndex >= 0 && activeServerTexture != null)
            {
                fullBoardTexture = activeServerTexture;
            }
            else
            {
                fullBoardTexture = GenerateFullBoardTexture(512, currentScene);
            }

            // Generate interlocking edge matrix
            int[,] horizontalEdges = new int[N, N - 1]; // right edges
            int[,] verticalEdges = new int[N - 1, N];   // bottom edges

            var rng = new System.Random();
            for (int r = 0; r < N; r++)
                for (int c = 0; c < N - 1; c++)
                    horizontalEdges[r, c] = rng.Next(2) == 0 ? 1 : -1;

            for (int r = 0; r < N - 1; r++)
                for (int c = 0; c < N; c++)
                    verticalEdges[r, c] = rng.Next(2) == 0 ? 1 : -1;

            pieces.Clear();
            nextGroupId = 1;

            for (int r = 0; r < N; r++)
            {
                for (int c = 0; c < N; c++)
                {
                    int top = (r == 0) ? 0 : -verticalEdges[r - 1, c];
                    int right = (c == N - 1) ? 0 : horizontalEdges[r, c];
                    int bottom = (r == N - 1) ? 0 : verticalEdges[r, c];
                    int left = (c == 0) ? 0 : -horizontalEdges[r, c - 1];

                    bool isEdge = (r == 0 || r == N - 1 || c == 0 || c == N - 1);

                    var p = new JigsawPiece
                    {
                        id = $"piece-{r}-{c}",
                        row = r,
                        col = c,
                        topEdge = top,
                        rightEdge = right,
                        bottomEdge = bottom,
                        leftEdge = left,
                        isEdge = isEdge,
                        status = PieceStatus.Tray,
                        groupId = nextGroupId++,
                        tileSize = tileSize,
                        pad = pad,
                        spriteSize = spriteSize,
                        x = 0,
                        y = 0
                    };

                    BakePieceTextures(p, fullBoardTexture);
                    pieces.Add(p);
                }
            }

            // Shuffle drawer pieces in-place
            ShuffleTrayPieces();
        }

        public void ShuffleTrayPieces()
        {
            var trayPieces = pieces.FindAll(p => p.status == PieceStatus.Tray);
            var otherPieces = pieces.FindAll(p => p.status != PieceStatus.Tray);
            var rng = new System.Random();
            int n = trayPieces.Count;
            while (n > 1)
            {
                n--;
                int k = rng.Next(n + 1);
                var val = trayPieces[k];
                trayPieces[k] = trayPieces[n];
                trayPieces[n] = val;
            }
            pieces.Clear();
            pieces.AddRange(otherPieces);
            pieces.AddRange(trayPieces);
        }

        // --- PROCEDURAL FULL BOARD PHOTO GENERATOR (MATCHES WEB VERSION 1:1) ---
        private Texture2D GenerateFullBoardTexture(int size, PuzzleScene scene)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            Color[] cols = new Color[size * size];

            // Predefine polygons in normalized top-down HTML coordinates (x: 0..1, yTop: 0..1)
            List<Vector2> sunsetMtnBack = new List<Vector2>
            {
                new Vector2(0f, 0.62f), new Vector2(0.25f, 0.38f), new Vector2(0.52f, 0.55f),
                new Vector2(0.80f, 0.34f), new Vector2(1f, 0.52f), new Vector2(1f, 0.65f), new Vector2(0f, 0.65f)
            };
            List<Vector2> sunsetMtnFront = new List<Vector2>
            {
                new Vector2(0f, 0.66f), new Vector2(0.15f, 0.52f), new Vector2(0.44f, 0.42f),
                new Vector2(0.72f, 0.58f), new Vector2(0.92f, 0.45f), new Vector2(1f, 0.55f),
                new Vector2(1f, 0.66f), new Vector2(0f, 0.66f)
            };

            for (int y = 0; y < size; y++)
            {
                float ny = (float)y / size;         // 0 (bottom) to 1 (top)
                float yTop = 1f - ny;               // 0 (top) to 1 (bottom), matching HTML5 Canvas
                for (int x = 0; x < size; x++)
                {
                    float nx = (float)x / size;
                    Vector2 ptTop = new Vector2(nx, yTop);
                    Color c = Color.black;

                    if (scene == PuzzleScene.Sunset)
                    {
                        if (yTop <= 0.65f)
                        {
                            // Sky gradient: #3b1d5e (0) -> #b84a62 (0.45) -> #f7b267 (1.0)
                            float tSky = yTop / 0.65f;
                            if (tSky < 0.45f)
                                c = Color.Lerp(new Color(0.231f, 0.114f, 0.369f), new Color(0.722f, 0.290f, 0.384f), tSky / 0.45f);
                            else
                                c = Color.Lerp(new Color(0.722f, 0.290f, 0.384f), new Color(0.969f, 0.698f, 0.404f), (tSky - 0.45f) / 0.55f);

                            // Sun circle #ffe599 at (0.62, 0.36), r = 0.13
                            if (Hypot(nx - 0.62f, yTop - 0.36f) <= 0.13f)
                            {
                                c = new Color(1.0f, 0.898f, 0.600f);
                            }

                            // Back mountain polygon #6d3b6e
                            if (IsPointInPolygon(ptTop, sunsetMtnBack))
                            {
                                c = new Color(0.427f, 0.231f, 0.431f);
                            }
                            // Front mountain polygon #43234f
                            if (IsPointInPolygon(ptTop, sunsetMtnFront))
                            {
                                c = new Color(0.263f, 0.137f, 0.310f);
                            }
                        }
                        else
                        {
                            // Lake gradient: #e07a5f (0) -> #813b65 (0.5) -> #26143b (1.0)
                            float tLake = (yTop - 0.65f) / 0.35f;
                            if (tLake < 0.5f)
                                c = Color.Lerp(new Color(0.878f, 0.478f, 0.373f), new Color(0.506f, 0.231f, 0.396f), tLake / 0.5f);
                            else
                                c = Color.Lerp(new Color(0.506f, 0.231f, 0.396f), new Color(0.149f, 0.078f, 0.231f), (tLake - 0.5f) / 0.5f);

                            // 6 horizontal sun reflection bars
                            for (int i = 0; i < 6; i++)
                            {
                                float barY = 0.69f + i * 0.045f;
                                float barW = 0.22f - i * 0.025f;
                                if (Mathf.Abs(yTop - barY) <= 0.004f && Mathf.Abs(nx - 0.62f) <= barW * 0.5f)
                                {
                                    c = Color.Lerp(c, new Color(1.0f, 0.898f, 0.600f), 0.45f);
                                }
                            }
                        }
                    }
                    else if (scene == PuzzleScene.Forest)
                    {
                        c = Color.Lerp(new Color(0.27f, 0.48f, 0.62f), new Color(0.66f, 0.85f, 0.86f), yTop);
                        if (Hypot(nx - 0.30f, yTop - 0.25f) <= 0.10f)
                            c = new Color(1f, 0.95f, 0.69f);

                        float hill = 0.78f - 0.14f * Mathf.Sin(nx * 4f);
                        if (yTop > hill)
                            c = (nx < 0.5f) ? new Color(0.11f, 0.26f, 0.20f) : new Color(0.18f, 0.42f, 0.31f);
                    }
                    else // Coast
                    {
                        if (yTop <= 0.55f)
                        {
                            c = Color.Lerp(new Color(0.28f, 0.79f, 0.89f), new Color(0.79f, 0.94f, 0.97f), yTop / 0.55f);
                        }
                        else
                        {
                            float tSea = (yTop - 0.55f) / 0.45f;
                            if (tSea < 0.65f)
                                c = Color.Lerp(new Color(0f, 0.47f, 0.71f), new Color(0f, 0.71f, 0.85f), tSea / 0.65f);
                            else
                                c = Color.Lerp(new Color(0f, 0.71f, 0.85f), new Color(0.96f, 0.89f, 0.52f), (tSea - 0.65f) / 0.35f);
                        }
                    }

                    cols[y * size + x] = c;
                }
            }

            tex.SetPixels(cols);
            tex.Apply();
            return tex;
        }

        // --- CUBIC BEZIER TAB & BLANK CURVE GENERATOR (MATCHES WEB VERSION) ---
        private static Vector2 EvalCubicBezier(Vector2 p0, Vector2 p1, Vector2 p2, Vector2 p3, float t)
        {
            float u = 1f - t;
            float tt = t * t;
            float uu = u * u;
            return (uu * u * p0) + (3f * uu * t * p1) + (3f * u * tt * p2) + (tt * t * p3);
        }

        private static void AddBezierSegment(List<Vector2> poly, Vector2 p0, Vector2 p1, Vector2 p2, Vector2 p3, int steps = 6)
        {
            for (int i = 1; i <= steps; i++)
            {
                float t = (float)i / steps;
                poly.Add(EvalCubicBezier(p0, p1, p2, p3, t));
            }
        }

        private static void AddSpriteSide(List<Vector2> poly, Vector2 pStart, Vector2 pEnd, int sign)
        {
            if (sign == 0)
            {
                poly.Add(pEnd);
                return;
            }

            Vector2 d = pEnd - pStart;
            float len = d.magnitude;
            if (len < 0.001f) { poly.Add(pEnd); return; }

            Vector2 n = new Vector2(d.y / len * sign, -d.x / len * sign);

            Func<float, float, Vector2> pt = (along, outDist) =>
                pStart + d * along + n * (len * outDist);

            float tabOut = 0.24f;

            Vector2 b0 = pt(0.36f, 0f);
            poly.Add(b0);

            // Segment 1: Neck narrowing into head
            Vector2 c1_0 = pt(0.41f, -0.02f);
            Vector2 c1_1 = pt(0.34f, tabOut * 0.55f);
            Vector2 c1_2 = pt(0.38f, tabOut * 0.85f);
            AddBezierSegment(poly, b0, c1_0, c1_1, c1_2, 6);

            // Segment 2: Rounded head of tab
            Vector2 c2_0 = pt(0.42f, tabOut * 1.15f);
            Vector2 c2_1 = pt(0.58f, tabOut * 1.15f);
            Vector2 c2_2 = pt(0.62f, tabOut * 0.85f);
            AddBezierSegment(poly, c1_2, c2_0, c2_1, c2_2, 8);

            // Segment 3: Head narrowing back down into base
            Vector2 c3_0 = pt(0.66f, tabOut * 0.55f);
            Vector2 c3_1 = pt(0.59f, -0.02f);
            Vector2 c3_2 = pt(0.64f, 0f);
            AddBezierSegment(poly, c2_2, c3_0, c3_1, c3_2, 6);

            poly.Add(pEnd);
        }

        private static bool IsPointInPolygon(Vector2 p, List<Vector2> poly)
        {
            bool inside = false;
            int count = poly.Count;
            for (int i = 0, j = count - 1; i < count; j = i++)
            {
                if (((poly[i].y > p.y) != (poly[j].y > p.y)) &&
                    (p.x < (poly[j].x - poly[i].x) * (p.y - poly[i].y) / (poly[j].y - poly[i].y) + poly[i].x))
                {
                    inside = !inside;
                }
            }
            return inside;
        }

        private static float DistToSegmentSq(Vector2 p, Vector2 a, Vector2 b)
        {
            Vector2 ab = b - a;
            float lenSq = ab.sqrMagnitude;
            if (lenSq < 0.00001f) return (p - a).sqrMagnitude;
            float t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / lenSq);
            Vector2 proj = a + t * ab;
            return (p - proj).sqrMagnitude;
        }

        // --- PIECE TEXTURE & JIGSAW SLOT OUTLINE BAKING ---
        private void BakePieceTextures(JigsawPiece p, Texture2D fullBoard)
        {
            int texSize = 160;
            var tex = new Texture2D(texSize, texSize, TextureFormat.RGBA32, false);
            var outlineTex = new Texture2D(texSize, texSize, TextureFormat.RGBA32, false);

            Color[] pixels = new Color[texSize * texSize];
            Color[] outlinePixels = new Color[texSize * texSize];

            // Match exact 0.28f pad-to-tileSize ratio used in UI and web version (0.28 / 1.56)
            float pad = texSize * (0.28f / 1.56f);
            float tileSize = texSize - pad * 2f;

            // Define four corners of the piece tile (in Texture2D space where Y=0 is bottom, Y=texSize is top)
            Vector2 pBL = new Vector2(pad, pad);
            Vector2 pBR = new Vector2(pad + tileSize, pad);
            Vector2 pTR = new Vector2(pad + tileSize, pad + tileSize);
            Vector2 pTL = new Vector2(pad, pad + tileSize);

            var poly = new List<Vector2>(128);
            poly.Add(pBL);
            AddSpriteSide(poly, pBL, pBR, p.bottomEdge); // Bottom: outwards into -Y
            AddSpriteSide(poly, pBR, pTR, p.rightEdge);  // Right: outwards into +X
            AddSpriteSide(poly, pTR, pTL, p.topEdge);    // Top: outwards into +Y
            AddSpriteSide(poly, pTL, pBL, p.leftEdge);   // Left: outwards into -X

            // Precompute fast distance field to polygon segments within 4px band
            float[] distField = new float[texSize * texSize];
            for (int i = 0; i < distField.Length; i++) distField[i] = 100f;

            int polyCount = poly.Count;
            for (int i = 0; i < polyCount; i++)
            {
                Vector2 a = poly[i];
                Vector2 b = poly[(i + 1) % polyCount];
                int minX = Mathf.Max(0, Mathf.FloorToInt(Mathf.Min(a.x, b.x) - 4f));
                int maxX = Mathf.Min(texSize - 1, Mathf.CeilToInt(Mathf.Max(a.x, b.x) + 4f));
                int minY = Mathf.Max(0, Mathf.FloorToInt(Mathf.Min(a.y, b.y) - 4f));
                int maxY = Mathf.Min(texSize - 1, Mathf.CeilToInt(Mathf.Max(a.y, b.y) + 4f));

                for (int y = minY; y <= maxY; y++)
                {
                    float py = y + 0.5f;
                    int rowOffset = y * texSize;
                    for (int x = minX; x <= maxX; x++)
                    {
                        float d = Mathf.Sqrt(DistToSegmentSq(new Vector2(x + 0.5f, py), a, b));
                        if (d < distField[rowOffset + x])
                            distField[rowOffset + x] = d;
                    }
                }
            }

            // UV coordinates relative to master photo
            float boardSize = fullBoard.width;
            float masterTileSize = boardSize / gridSize;
            float masterPad = masterTileSize * 0.28f;

            float u0 = (p.col * masterTileSize - masterPad) / boardSize;
            float v0 = ((gridSize - 1 - p.row) * masterTileSize - masterPad) / boardSize;
            float uSpan = (masterTileSize + 2f * masterPad) / boardSize;
            float vSpan = uSpan;

            for (int y = 0; y < texSize; y++)
            {
                float py = y + 0.5f;
                int rowOffset = y * texSize;
                for (int x = 0; x < texSize; x++)
                {
                    int idx = rowOffset + x;
                    float px = x + 0.5f;
                    Vector2 ptPos = new Vector2(px, py);

                    bool inside = IsPointInPolygon(ptPos, poly);
                    if (!inside)
                    {
                        pixels[idx] = Color.clear;
                        outlinePixels[idx] = Color.clear;
                        continue;
                    }

                    float d = distField[idx];

                    // 1. Bake Jigsaw-Shaped Slot Outline Texture (matches outlineSpriteUrl in web version)
                    if (d <= 2.4f)
                    {
                        float highlightAlpha = Mathf.Clamp01((2.4f - d) / 1.4f) * 0.45f;
                        Color slotCol = new Color(0.90f, 0.94f, 1.0f, highlightAlpha);
                        if (d <= 0.85f)
                        {
                            slotCol = Color.Lerp(slotCol, new Color(0.06f, 0.09f, 0.16f, 0.72f), Mathf.Clamp01(1f - d / 0.85f));
                        }
                        outlinePixels[idx] = slotCol;
                    }
                    else
                    {
                        outlinePixels[idx] = Color.clear;
                    }

                    // 2. Bake Piece Photo Texture + 3D Bevel & Dark Seam Contour
                    float u = u0 + (x / (float)texSize) * uSpan;
                    float v = v0 + (y / (float)texSize) * vSpan;
                    Color baseCol = fullBoard.GetPixelBilinear(Mathf.Clamp01(u), Mathf.Clamp01(v));

                    if (d <= 2.4f)
                    {
                        // Inner light bevel stroke (matches rgba(255, 255, 255, 0.42) lineWidth 3)
                        float bevelT = Mathf.Clamp01((2.4f - d) / 1.5f) * 0.34f;
                        baseCol = Color.Lerp(baseCol, Color.white, bevelT);

                        // Outer dark contour stroke (matches rgba(15, 23, 42, 0.65) lineWidth 1.25)
                        if (d <= 1.15f)
                        {
                            float seamT = Mathf.Clamp01((1.15f - d) / 1.15f) * 0.65f;
                            baseCol = Color.Lerp(baseCol, new Color(0.06f, 0.09f, 0.16f, 1f), seamT);
                        }

                        // Smooth anti-aliased edge coverage
                        if (d < 0.75f)
                        {
                            baseCol.a = Mathf.Clamp01(0.25f + d);
                        }
                    }

                    pixels[idx] = baseCol;
                }
            }

            tex.SetPixels(pixels);
            tex.Apply();

            outlineTex.SetPixels(outlinePixels);
            outlineTex.Apply();

            p.pieceTexture = tex;
            p.thumbTexture = tex;
            p.outlineTexture = outlineTex;
        }

        // --- GAMEPLAY: HINT & MAGNET POWERUPS ---
        public void TriggerHint()
        {
            if (isAnimatingPowerup) return;
            if (hintsLeft <= 0)
            {
                ShowToast("No Hints remaining!", true);
                return;
            }

            var unplaced = pieces.FindAll(p => p.status != PieceStatus.Locked);
            if (unplaced.Count == 0) return;

            var target = unplaced[UnityEngine.Random.Range(0, unplaced.Count)];
            hintsLeft--;

            float targetX = target.col * target.tileSize;
            float targetY = target.row * target.tileSize;

            ShowToast("💡 Moving piece to its board position...");
            isAnimatingPowerup = true;

            activeFlights.Add(new PieceFlightAnim
            {
                piece = target,
                startPos = new Vector2(target.x, target.y),
                targetPos = new Vector2(targetX, targetY),
                duration = 0.58f,
                onComplete = () =>
                {
                    target.status = PieceStatus.Locked;
                    target.x = targetX;
                    target.y = targetY;
                    TriggerPlacementEffect(new List<JigsawPiece> { target });
                    CheckWinCondition();
                }
            });
        }

        public void TriggerMagnet()
        {
            if (isAnimatingPowerup) return;
            if (magnetsLeft <= 0)
            {
                ShowToast("No Magnet powerups remaining!", true);
                return;
            }

            var unplaced = pieces.FindAll(p => p.status != PieceStatus.Locked);
            if (unplaced.Count < 2)
            {
                ShowToast("Not enough unplaced pieces left to group!");
                return;
            }

            // Find 2 or 3 unplaced orthogonal neighbors
            var unplacedMap = new Dictionary<string, JigsawPiece>();
            foreach (var p in unplaced) unplacedMap[$"{p.row},{p.col}"] = p;

            Func<JigsawPiece, List<JigsawPiece>> getNeighbors = p =>
            {
                var res = new List<JigsawPiece>();
                int[,] dirs = new int[,] { { -1, 0 }, { 1, 0 }, { 0, -1 }, { 0, 1 } };
                for (int d = 0; d < 4; d++)
                {
                    string key = $"{p.row + dirs[d, 0]},{p.col + dirs[d, 1]}";
                    if (unplacedMap.TryGetValue(key, out var nb)) res.Add(nb);
                }
                return res;
            };

            List<JigsawPiece> chosenTrio = null;
            var shuffledSeeds = new List<JigsawPiece>(unplaced);
            for (int i = 0; i < shuffledSeeds.Count; i++)
            {
                int r = UnityEngine.Random.Range(i, shuffledSeeds.Count);
                var tmp = shuffledSeeds[i]; shuffledSeeds[i] = shuffledSeeds[r]; shuffledSeeds[r] = tmp;
            }

            foreach (var seed in shuffledSeeds)
            {
                var nbs = getNeighbors(seed);
                foreach (var second in nbs)
                {
                    var cands = new HashSet<JigsawPiece>(getNeighbors(seed));
                    foreach (var n in getNeighbors(second)) cands.Add(n);
                    cands.Remove(seed);
                    cands.Remove(second);

                    foreach (var third in cands)
                    {
                        bool allSameGroup = (seed.status == PieceStatus.Board && second.status == PieceStatus.Board && third.status == PieceStatus.Board && seed.groupId == second.groupId && second.groupId == third.groupId);
                        if (!allSameGroup)
                        {
                            chosenTrio = new List<JigsawPiece> { seed, second, third };
                            break;
                        }
                    }
                    if (chosenTrio != null) break;
                }
                if (chosenTrio != null) break;
            }

            if (chosenTrio == null)
            {
                foreach (var seed in shuffledSeeds)
                {
                    var nbs = getNeighbors(seed);
                    foreach (var second in nbs)
                    {
                        if (!(seed.status == PieceStatus.Board && second.status == PieceStatus.Board && seed.groupId == second.groupId))
                        {
                            chosenTrio = new List<JigsawPiece> { seed, second };
                            break;
                        }
                    }
                    if (chosenTrio != null) break;
                }
            }

            if (chosenTrio == null)
            {
                ShowToast("No ungrouped neighboring pieces left to magnetize!");
                return;
            }

            magnetsLeft--;
            int newGrpId = nextGroupId++;

            // Preserve full connected cluster so pre-existing board groups aren't torn apart
            var existingBoardGroupIds = new HashSet<int>();
            foreach (var p in chosenTrio)
            {
                if (p.status == PieceStatus.Board) existingBoardGroupIds.Add(p.groupId);
            }
            var fullCluster = new HashSet<JigsawPiece>(chosenTrio);
            foreach (var p in pieces)
            {
                if (p.status == PieceStatus.Board && existingBoardGroupIds.Contains(p.groupId))
                {
                    fullCluster.Add(p);
                }
            }

            var clusterList = new List<JigsawPiece>(fullCluster);
            int minCol = int.MaxValue, maxCol = int.MinValue;
            int minRow = int.MaxValue, maxRow = int.MinValue;
            foreach (var p in clusterList)
            {
                if (p.col < minCol) minCol = p.col;
                if (p.col > maxCol) maxCol = p.col;
                if (p.row < minRow) minRow = p.row;
                if (p.row > maxRow) maxRow = p.row;
            }

            float tileSize = clusterList[0].tileSize;
            float clusterW = (maxCol - minCol + 1) * tileSize;
            float clusterH = (maxRow - minRow + 1) * tileSize;

            float baseX = UnityEngine.Random.Range(0f, Mathf.Max(0f, boardPixelSize - clusterW));
            float baseY = UnityEngine.Random.Range(0f, Mathf.Max(0f, boardPixelSize - clusterH));

            ShowToast($"🧲 Moving {clusterList.Count} neighboring pieces together onto the board...");
            isAnimatingPowerup = true;

            foreach (var p in clusterList)
            {
                float tX = baseX + (p.col - minCol) * tileSize;
                float tY = baseY + (p.row - minRow) * tileSize;

                activeFlights.Add(new PieceFlightAnim
                {
                    piece = p,
                    startPos = new Vector2(p.x, p.y),
                    targetPos = new Vector2(tX, tY),
                    duration = 0.58f,
                    onComplete = () =>
                    {
                        p.status = PieceStatus.Board;
                        p.groupId = newGrpId;
                        p.x = tX;
                        p.y = tY;
                    }
                });
            }

            // Once flights finish, trigger group join effect
            StartCoroutine(WaitAndTriggerGroupJoin(newGrpId, 0.60f));
        }

        private IEnumerator WaitAndTriggerGroupJoin(int grpId, float delay)
        {
            yield return new WaitForSeconds(delay);
            TriggerGroupJoinEffect(grpId);
        }

        // --- NEIGHBOR MERGE ON BOARD ---
        public void CheckAndMergeBoardNeighbors(int droppedGroupId)
        {
            bool merged = true;
            var seamPoints = new List<Vector2>();

            while (merged)
            {
                merged = false;
                var members = pieces.FindAll(p => p.status == PieceStatus.Board && p.groupId == droppedGroupId);
                var others = pieces.FindAll(p => p.status == PieceStatus.Board && p.groupId != droppedGroupId);

                foreach (var member in members)
                {
                    foreach (var other in others)
                    {
                        int dr = Mathf.Abs(member.row - other.row);
                        int dc = Mathf.Abs(member.col - other.col);

                        if (dr + dc == 1)
                        {
                            float expDx = (member.col - other.col) * member.tileSize;
                            float expDy = (member.row - other.row) * member.tileSize;
                            float actDx = member.x - other.x;
                            float actDy = member.y - other.y;
                            float dist = Hypot(actDx - expDx, actDy - expDy);

                            if (dist <= snapThreshold)
                            {
                                float shiftX = (other.x + expDx) - member.x;
                                float shiftY = (other.y + expDy) - member.y;

                                int oldOtherGrp = other.groupId;
                                foreach (var m in members)
                                {
                                    m.x += shiftX;
                                    m.y += shiftY;
                                }
                                foreach (var p in pieces)
                                {
                                    if (p.status == PieceStatus.Board && p.groupId == oldOtherGrp)
                                        p.groupId = droppedGroupId;
                                }

                                Vector2 seam = new Vector2((member.x + other.x) * 0.5f + member.tileSize * 0.5f, (member.y + other.y) * 0.5f + member.tileSize * 0.5f);
                                seamPoints.Add(seam);

                                merged = true;
                                break;
                            }
                        }
                    }
                    if (merged) break;
                }
            }

            if (seamPoints.Count > 0)
            {
                TriggerGroupJoinEffect(droppedGroupId, seamPoints);
            }
        }

        // --- WIN CONDITION CHECK ---
        public void CheckWinCondition()
        {
            if (pieces.TrueForAll(p => p.status == PieceStatus.Locked))
            {
                isPuzzleComplete = true;
                if (!runSubmitted)
                {
                    runSubmitted = true;
                    coins += 25;
                    PlayerPrefs.SetInt("agy_liveops_wallet_coins", coins);
                    PlayerPrefs.Save();

                    long elapsedMs = Mathf.Max(4000, Mathf.RoundToInt(runElapsedSeconds * 1000f));
                    int totalCount = gridSize * gridSize;
                    long leaguePts = totalCount * 15 + hintsLeft * 25;

                    int deltaBest = LbSubmitRun(LeaderboardMode.CalmJigsaw, elapsedMs, out int newBestRank);
                    LbSubmitRun(LeaderboardMode.UnifiedLeague, leaguePts, out int newUniRank);

                    winSummaryText = $"Solved {gridSize}×{gridSize} ({totalCount}pc) in {(elapsedMs / 1000f):F1}s! +25 Coins & +{leaguePts} League Pts.";
                    winRankBadgeText = deltaBest > 0
                        ? $"Rank #{newBestRank} (+{deltaBest} Overtake!)"
                        : $"Calm Jigsaw Rank #{newBestRank} • Unified #{newUniRank}";

                    LbRefreshView();
                }
                PlayChime(AudioChimeType.Win);
            }
        }

        // --- REAL-TIME OFFLINE LEADERBOARD ENGINE (Matches calm-jigsaw.html & Meowsweeper) ---
        private static long NowEpochMs() => DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

        public void LbInitSeasons()
        {
            lbJigsawState = LbLoadState(LeaderboardMode.CalmJigsaw);
            lbUnifiedState = LbLoadState(LeaderboardMode.UnifiedLeague);
        }

        private LeaderboardSeasonState LbLoadState(LeaderboardMode mode)
        {
            string key = mode == LeaderboardMode.CalmJigsaw ? "agy_lb_v1_calm_jigsaw" : "agy_lb_v1_unified_league";
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
            string key = mode == LeaderboardMode.CalmJigsaw ? "agy_lb_v1_calm_jigsaw" : "agy_lb_v1_unified_league";
            PlayerPrefs.SetString(key, JsonUtility.ToJson(st));
            PlayerPrefs.Save();
        }

        public LeaderboardSeasonState LbCreateSeason(LeaderboardMode mode, int seasonNum)
        {
            bool isCumulative = (mode == LeaderboardMode.UnifiedLeague);
            long now = NowEpochMs() + lbSimOffsetMs;
            long seasonDur = 7L * 24L * 3600L * 1000L;
            long minTarget = isCumulative ? 900L : 35000L;
            long maxTarget = isCumulative ? 4800L : 290000L;
            long span = maxTarget - minTarget;
            int botCount = 29;
            int seed = seasonNum * 101 + (isCumulative ? 518 : 407);

            var st = new LeaderboardSeasonState
            {
                schemaVersion = 1,
                seasonNumber = seasonNum,
                seasonId = $"S0{seasonNum}_{(isCumulative ? "UNIFIED" : "JIGSAW")}",
                gameMode = isCumulative ? "unified_league" : "calm_jigsaw",
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
            bool clamped = rawNow < st.lastSeenTimestampMs;
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

            // Build sorted entries
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
            LbSaveState(mode, st);

            // Build user-centered windowed rows (Top 3 + Player neighborhood ±2)
            var selected = new HashSet<int> { 0, 1, 2 };
            int pIdx = playerRank - 1;
            for (int i = Mathf.Max(0, pIdx - 2); i <= Mathf.Min(entries.Count - 1, pIdx + 2); i++)
                selected.Add(i);
            int padIdx = 3;
            while (selected.Count < 8 && padIdx < entries.Count)
                selected.Add(padIdx++);

            var sortedIndices = new List<int>(selected);
            sortedIndices.Sort();

            var windowedRows = new List<LeaderboardRowView>();
            int prev = -1;
            foreach (int idx in sortedIndices)
            {
                if (prev != -1 && idx - prev > 1)
                {
                    windowedRows.Add(new LeaderboardRowView
                    {
                        isDivider = true,
                        skippedCount = idx - prev - 1
                    });
                }
                windowedRows.Add(entries[idx]);
                prev = idx;
            }

            long remMs = Math.Max(0L, st.endTimeMs - effectiveNow);
            long totalMins = remMs / 60000L;
            long d = totalMins / 1440L;
            long h = (totalMins % 1440L) / 60L;
            long m = totalMins % 60L;

            float pct = Mathf.Clamp01((float)(effectiveNow - st.startTimeMs) / Mathf.Max(1f, (float)(st.endTimeMs - st.startTimeMs)));
            string phaseTxt = clamped ? "Clamped" : (pct >= 1f ? "Locked" : (pct >= 0.65f ? "Convergence" : "Live Sync"));

            string tierName = playerRank == 1 ? "Gold Crown" : (playerRank <= 3 ? "Silver Chest" : (playerRank <= 10 ? "Bronze Pack" : "Participation"));
            int tierCoins = playerRank == 1 ? 500 : (playerRank <= 3 ? 250 : (playerRank <= 10 ? 100 : 25));

            return new LeaderboardComputedView
            {
                totalEntries = entries.Count,
                playerRank = playerRank,
                netDelta = st.previousRank - playerRank,
                countdownText = remMs <= 0L ? "ENDED" : $"{d}d {h:D2}h {m:D2}m",
                phaseText = phaseTxt,
                isClamped = clamped,
                isExpired = remMs <= 0L,
                rewardClaimed = st.rewardClaimed,
                rewardTierName = tierName,
                rewardTierCoins = tierCoins,
                playerFormattedScore = LbFormatScore(st.playerScore, isCumulative),
                scoreLabel = isCumulative ? "Score:" : "Best:",
                windowedRows = windowedRows
            };
        }

        public static string LbFormatScore(long score, bool isCumulative)
        {
            if (isCumulative) return $"{score:N0} pts";
            if (score <= 0) return "Unranked";
            return $"{(score / 1000f):F2}s";
        }

        public void LbRefreshView()
        {
            if (lbJigsawState == null || lbUnifiedState == null) LbInitSeasons();
            var jigsawView = LbAdvanceAndCompute(LeaderboardMode.CalmJigsaw, lbJigsawState);
            var unifiedView = LbAdvanceAndCompute(LeaderboardMode.UnifiedLeague, lbUnifiedState);
            topHeaderJigsawRank = jigsawView.playerRank;
            lbCurrentView = (lbActiveMode == LeaderboardMode.CalmJigsaw) ? jigsawView : unifiedView;
        }

        public int LbSubmitRun(LeaderboardMode mode, long rawScore, out int newRank)
        {
            if (lbJigsawState == null || lbUnifiedState == null) LbInitSeasons();
            var st = (mode == LeaderboardMode.CalmJigsaw) ? lbJigsawState : lbUnifiedState;
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
            return prevRank - newRank;
        }

        public void LbPlus4h()
        {
            lbSimOffsetMs += 4L * 3600L * 1000L;
            LbRefreshView();
            ShowToast("+4h Advanced • Simulated bot progression!");
        }

        public void LbMinus6h()
        {
            lbSimOffsetMs -= 6L * 3600L * 1000L;
            LbRefreshView();
            ShowToast("Clock Rollback Detected (-6h) • Clamped to High-Water Timestamp!", true);
        }

        public void LbExpireSeason()
        {
            var st = (lbActiveMode == LeaderboardMode.CalmJigsaw) ? lbJigsawState : lbUnifiedState;
            long rem = Math.Max(0L, st.endTimeMs - (NowEpochMs() + lbSimOffsetMs));
            lbSimOffsetMs += rem + 1000L;
            LbRefreshView();
            ShowToast("Season Finished! Claim your rank reward.");
        }

        public void LbClaimReward()
        {
            var st = (lbActiveMode == LeaderboardMode.CalmJigsaw) ? lbJigsawState : lbUnifiedState;
            var view = LbAdvanceAndCompute(lbActiveMode, st);
            if (!view.isExpired) return;
            if (st.rewardClaimed)
            {
                ShowToast("Idempotent Guard: Season reward already claimed!", true);
                return;
            }
            st.rewardClaimed = true;
            st.status = "archived";
            LbSaveState(lbActiveMode, st);

            coins += view.rewardTierCoins;
            PlayerPrefs.SetInt("agy_liveops_wallet_coins", coins);
            PlayerPrefs.Save();

            LbRefreshView();
            ShowToast($"Claimed {view.rewardTierName} (+{view.rewardTierCoins} Coins)!");
        }

        public void LbNextSeason()
        {
            var st = (lbActiveMode == LeaderboardMode.CalmJigsaw) ? lbJigsawState : lbUnifiedState;
            lbSimOffsetMs = 0L;
            int nextNum = st.seasonNumber + 1;
            if (lbActiveMode == LeaderboardMode.CalmJigsaw)
                lbJigsawState = LbCreateSeason(LeaderboardMode.CalmJigsaw, nextNum);
            else
                lbUnifiedState = LbCreateSeason(LeaderboardMode.UnifiedLeague, nextNum);

            LbRefreshView();
            ShowToast($"Season {nextNum} Started!");
        }

        // --- VISUAL & AUDIO EFFECTS ---
        public void TriggerPlacementEffect(List<JigsawPiece> placedList)
        {
            foreach (var p in placedList)
            {
                p.pulseScale = 1.08f;
                p.flashColor = new Color(1f, 0.85f, 0.2f, 0.7f);

                Vector2 center = new Vector2(p.x + p.tileSize * 0.5f, p.y + p.tileSize * 0.5f);

                // Golden Shockwave Ring
                activeEffects.Add(new VisualEffectItem
                {
                    type = VisualEffectItem.EffectType.Ring,
                    pos = center,
                    startRadius = p.tileSize * 0.25f,
                    targetRadius = p.tileSize * 1.15f,
                    radius = p.tileSize * 0.25f,
                    color = new Color(1f, 0.84f, 0.15f),
                    maxAlpha = 0.9f,
                    alpha = 0.9f,
                    duration = 0.45f
                });

                // Sparkle Stars
                for (int i = 0; i < 12; i++)
                {
                    float ang = (Mathf.PI * 2f * i) / 12f + UnityEngine.Random.Range(-0.2f, 0.2f);
                    float spd = (p.tileSize * 1.4f) * UnityEngine.Random.Range(0.6f, 1.2f);
                    activeEffects.Add(new VisualEffectItem
                    {
                        type = VisualEffectItem.EffectType.Sparkle,
                        pos = center,
                        velocity = new Vector2(Mathf.Cos(ang) * spd, Mathf.Sin(ang) * spd),
                        color = (i % 2 == 0) ? new Color(1f, 0.88f, 0.3f) : Color.white,
                        size = UnityEngine.Random.Range(2.5f, 4.5f),
                        maxAlpha = 0.95f,
                        alpha = 0.95f,
                        duration = 0.50f
                    });
                }
            }

            PlayChime(AudioChimeType.Place);
        }

        public void TriggerGroupJoinEffect(int groupId, List<Vector2> seamPoints = null)
        {
            var members = pieces.FindAll(p => p.status == PieceStatus.Board && p.groupId == groupId);
            foreach (var m in members)
            {
                m.pulseScale = 1.06f;
                m.flashColor = new Color(0.22f, 0.74f, 0.97f, 0.6f);
            }

            if (seamPoints != null && seamPoints.Count > 0)
            {
                foreach (var seam in seamPoints)
                {
                    activeEffects.Add(new VisualEffectItem
                    {
                        type = VisualEffectItem.EffectType.Ring,
                        pos = seam,
                        startRadius = 15f,
                        targetRadius = 55f,
                        radius = 15f,
                        color = new Color(0.22f, 0.74f, 0.97f),
                        maxAlpha = 0.85f,
                        alpha = 0.85f,
                        duration = 0.42f
                    });
                }
            }

            PlayChime(AudioChimeType.Join);
        }

        public enum AudioChimeType { Place, Join, Win }

        public void PlayChime(AudioChimeType type)
        {
            try
            {
                int sampleRate = 44100;
                float duration = 0.35f;

                if (type == AudioChimeType.Place)
                {
                    int placed = pieces.FindAll(p => p.status == PieceStatus.Locked).Count;
                    float freq = PentatonicScale[placed % PentatonicScale.Length];
                    int samples = (int)(sampleRate * duration);
                    float[] data = new float[samples];
                    for (int i = 0; i < samples; i++)
                    {
                        float t = (float)i / sampleRate;
                        float env = Mathf.Exp(-t * 9f);
                        data[i] = Mathf.Sin(2f * Mathf.PI * freq * t) * env * 0.22f;
                    }
                    var clip = AudioClip.Create("ChimePlace", samples, 1, sampleRate, false);
                    clip.SetData(data, 0);
                    audioSource.PlayOneShot(clip);
                }
                else if (type == AudioChimeType.Join)
                {
                    float freq1 = 440.0f;
                    float freq2 = 659.25f;
                    int samples = (int)(sampleRate * 0.4f);
                    float[] data = new float[samples];
                    for (int i = 0; i < samples; i++)
                    {
                        float t = (float)i / sampleRate;
                        float env = Mathf.Exp(-t * 8f);
                        data[i] = (Mathf.Sin(2f * Mathf.PI * freq1 * t) + Mathf.Sin(2f * Mathf.PI * freq2 * t)) * env * 0.15f;
                    }
                    var clip = AudioClip.Create("ChimeJoin", samples, 1, sampleRate, false);
                    clip.SetData(data, 0);
                    audioSource.PlayOneShot(clip);
                }
                else if (type == AudioChimeType.Win)
                {
                    float[] notes = new float[] { 523.25f, 659.25f, 783.99f, 1046.5f };
                    int samples = (int)(sampleRate * 0.65f);
                    float[] data = new float[samples];
                    for (int i = 0; i < samples; i++)
                    {
                        float t = (float)i / sampleRate;
                        int noteIdx = Mathf.Clamp((int)(t / 0.12f), 0, notes.Length - 1);
                        float freq = notes[noteIdx];
                        float env = Mathf.Exp(-((t % 0.12f)) * 7f);
                        data[i] = Mathf.Sin(2f * Mathf.PI * freq * t) * env * 0.20f;
                    }
                    var clip = AudioClip.Create("ChimeWin", samples, 1, sampleRate, false);
                    clip.SetData(data, 0);
                    audioSource.PlayOneShot(clip);
                }
            }
            catch { }
        }
    }
}
